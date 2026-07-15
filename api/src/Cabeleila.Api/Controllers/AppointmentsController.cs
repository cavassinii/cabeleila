using API.Application;
using API.Security;
using API.Utils;
using DAL.Cabeleila;
using DTO.Cabeleila;
using DTO.Cabeleila.Requests;
using DTO.Cabeleila.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/appointments")]
    [Authorize(Roles = TokenService.RoleCustomer)]
    public class AppointmentsController : ControllerBase
    {
        // Horarios livres para uma data, dada a duracao total dos servicos escolhidos.
        // excludeAppointmentId e usado ao reagendar/editar servicos do proprio agendamento
        // (senao ele apareceria ocupando o proprio horario e sumiria da grade).
        [HttpGet("availability")]
        public async Task<ActionResult<AvailabilityResponse>> GetAvailability(
            [FromQuery] DateTime date, [FromQuery] int durationMinutes, [FromQuery] long? excludeAppointmentId)
        {
            if (durationMinutes <= 0)
            {
                return BadRequest("Informe a duracao total dos servicos selecionados.");
            }

            if (excludeAppointmentId.HasValue)
            {
                var owned = await Appointments.GetById(excludeAppointmentId.Value);
                if (owned == null || owned.Customer_id != User.GetUserId())
                {
                    return BadRequest("Agendamento invalido.");
                }
            }

            return Ok(await AvailabilityService.GetAvailability(date, durationMinutes, excludeAppointmentId));
        }

        // Chamado pelo front antes de confirmar o agendamento: avisa se ja existe um agendamento
        // ativo do cliente na mesma semana, sugerindo usar a mesma data (a do primeiro agendamento).
        [HttpGet("suggest-date")]
        public async Task<ActionResult<SameWeekSuggestionResponse>> SuggestDate([FromQuery] DateTime date)
        {
            var customerId = User.GetUserId();
            var (weekStart, weekEnd) = DateHelper.GetWeekRange(date);
            var existing = await Appointments.GetFirstActiveInWeek(customerId, weekStart, weekEnd);

            if (existing == null || existing.Appointment_date.Date == date.Date)
            {
                return Ok(new SameWeekSuggestionResponse { HasSuggestion = false });
            }

            return Ok(new SameWeekSuggestionResponse
            {
                HasSuggestion = true,
                ExistingAppointmentId = existing.Id,
                SuggestedDate = existing.Appointment_date,
            });
        }

        [HttpPost]
        public async Task<ActionResult<AppointmentDetailResponse>> Create([FromBody] CreateAppointmentRequest request)
        {
            var customerId = User.GetUserId();

            if (request.ServiceIds == null || request.ServiceIds.Count == 0)
            {
                return BadRequest("Selecione ao menos um servico.");
            }

            if (request.AppointmentDate.Date < DateTime.Today)
            {
                return BadRequest("Nao e possivel agendar em uma data passada.");
            }

            var distinctServiceIds = request.ServiceIds.Distinct().ToList();
            var services = await DAL.Cabeleila.Services.GetByIds(distinctServiceIds);
            if (services.Count != distinctServiceIds.Count)
            {
                return BadRequest("Um ou mais servicos selecionados sao invalidos.");
            }

            if (!request.KeepOriginalDate)
            {
                var (weekStart, weekEnd) = DateHelper.GetWeekRange(request.AppointmentDate);
                var existing = await Appointments.GetFirstActiveInWeek(customerId, weekStart, weekEnd);
                if (existing != null && existing.Appointment_date.Date != request.AppointmentDate.Date)
                {
                    return Conflict(new SameWeekSuggestionResponse
                    {
                        HasSuggestion = true,
                        ExistingAppointmentId = existing.Id,
                        SuggestedDate = existing.Appointment_date,
                    });
                }
            }

            var totalDuration = services.Sum(s => s.Duration_minutes);
            var slotValidation = await AvailabilityService.ValidateSlot(request.AppointmentDate, request.AppointmentTime, totalDuration);
            if (!slotValidation.IsValid)
            {
                return StatusCode(slotValidation.StatusCode, slotValidation.Message);
            }

            var pendingStatus = await AppointmentStatuses.GetByCode(AppointmentStatusCodes.Pending);

            var appointment = new Appointment
            {
                Customer_id = customerId,
                Appointment_date = request.AppointmentDate.Date,
                Appointment_time = request.AppointmentTime,
                Status_id = pendingStatus!.Id,
                Notes = request.Notes,
            };
            appointment.Id = await Appointments.Create(appointment);

            foreach (var service in services)
            {
                await AppointmentItems.Create(new AppointmentItem
                {
                    Appointment_id = appointment.Id,
                    Service_id = service.Id,
                    Status_id = pendingStatus.Id,
                    Price_at_booking = service.Price,
                    Duration_minutes_at_booking = service.Duration_minutes,
                });
            }

            await AppointmentHistories.Create(new AppointmentHistory
            {
                Appointment_id = appointment.Id,
                Changed_by_customer_id = customerId,
                Change_type = AppointmentChangeTypes.Created,
                New_value = JsonSerializer.Serialize(new
                {
                    appointment.Appointment_date,
                    appointment.Appointment_time,
                    ServiceIds = distinctServiceIds,
                }),
            });

            var detail = await AppointmentAssembler.BuildDetail(appointment);
            return CreatedAtAction(nameof(GetById), new { id = appointment.Id }, detail);
        }

        [HttpGet]
        public async Task<ActionResult<List<AppointmentListItemResponse>>> GetHistory([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var customerId = User.GetUserId();
            var effectiveTo = to ?? DateTime.Today.AddDays(90);
            var effectiveFrom = from ?? DateTime.Today.AddDays(-90);

            return Ok(await Appointments.GetCustomerHistory(customerId, effectiveFrom, effectiveTo));
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<AppointmentDetailResponse>> GetById(long id)
        {
            var customerId = User.GetUserId();
            var appointment = await Appointments.GetById(id);
            if (appointment == null || appointment.Customer_id != customerId)
            {
                return NotFound();
            }

            return Ok(await AppointmentAssembler.BuildDetail(appointment));
        }

        [HttpPut("{id:long}/reschedule")]
        public async Task<ActionResult<AppointmentDetailResponse>> Reschedule(long id, [FromBody] RescheduleAppointmentRequest request)
        {
            var customerId = User.GetUserId();
            var appointment = await Appointments.GetById(id);
            if (appointment == null || appointment.Customer_id != customerId)
            {
                return NotFound();
            }

            var status = await AppointmentStatuses.GetById(appointment.Status_id);
            if (status!.Code is AppointmentStatusCodes.Cancelled or AppointmentStatusCodes.Completed)
            {
                return BadRequest("Nao e possivel alterar um agendamento cancelado ou ja concluido.");
            }

            if ((appointment.Appointment_date.Date - DateTime.Today).TotalDays < BusinessRules.MinDaysForCustomerChange)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    "Alteracoes com menos de 2 dias de antecedencia so podem ser feitas por telefone com o salao.");
            }

            if (request.AppointmentDate.Date < DateTime.Today)
            {
                return BadRequest("Nao e possivel agendar em uma data passada.");
            }

            var currentItems = await AppointmentItems.GetByAppointmentId(id);
            var totalDuration = currentItems.Sum(i => i.Duration_minutes_at_booking);
            var slotValidation = await AvailabilityService.ValidateSlot(request.AppointmentDate, request.AppointmentTime, totalDuration, excludeAppointmentId: id);
            if (!slotValidation.IsValid)
            {
                return StatusCode(slotValidation.StatusCode, slotValidation.Message);
            }

            var oldValue = JsonSerializer.Serialize(new { appointment.Appointment_date, appointment.Appointment_time });
            await Appointments.UpdateSchedule(id, request.AppointmentDate.Date, request.AppointmentTime);

            await AppointmentHistories.Create(new AppointmentHistory
            {
                Appointment_id = id,
                Changed_by_customer_id = customerId,
                Change_type = AppointmentChangeTypes.Reschedule,
                Old_value = oldValue,
                New_value = JsonSerializer.Serialize(new { AppointmentDate = request.AppointmentDate.Date, request.AppointmentTime }),
            });

            appointment = await Appointments.GetById(id);
            return Ok(await AppointmentAssembler.BuildDetail(appointment!));
        }

        // Cliente troca os servicos solicitados (adiciona/remove), mantendo a mesma data/hora
        // e a regra dos 2 dias. Revalida a agenda porque a nova duracao total pode nao caber mais.
        [HttpPut("{id:long}/services")]
        public async Task<ActionResult<AppointmentDetailResponse>> EditServices(long id, [FromBody] EditAppointmentServicesRequest request)
        {
            var customerId = User.GetUserId();
            var appointment = await Appointments.GetById(id);
            if (appointment == null || appointment.Customer_id != customerId)
            {
                return NotFound();
            }

            if (request.ServiceIds == null || request.ServiceIds.Count == 0)
            {
                return BadRequest("Selecione ao menos um servico.");
            }

            var status = await AppointmentStatuses.GetById(appointment.Status_id);
            if (status!.Code is AppointmentStatusCodes.Cancelled or AppointmentStatusCodes.Completed)
            {
                return BadRequest("Nao e possivel alterar um agendamento cancelado ou ja concluido.");
            }

            if ((appointment.Appointment_date.Date - DateTime.Today).TotalDays < BusinessRules.MinDaysForCustomerChange)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    "Alteracoes com menos de 2 dias de antecedencia so podem ser feitas por telefone com o salao.");
            }

            var distinctServiceIds = request.ServiceIds.Distinct().ToList();
            var services = await DAL.Cabeleila.Services.GetByIds(distinctServiceIds);
            if (services.Count != distinctServiceIds.Count)
            {
                return BadRequest("Um ou mais servicos selecionados sao invalidos.");
            }

            var totalDuration = services.Sum(s => s.Duration_minutes);
            var slotValidation = await AvailabilityService.ValidateSlot(appointment.Appointment_date, appointment.Appointment_time, totalDuration, excludeAppointmentId: id);
            if (!slotValidation.IsValid)
            {
                return StatusCode(slotValidation.StatusCode, slotValidation.Message + " Reagende para um horario com espaco suficiente antes de alterar os servicos.");
            }

            var oldItems = await AppointmentItems.GetByAppointmentId(id);
            var oldValue = JsonSerializer.Serialize(oldItems.Select(i => new { i.Service_id, i.Duration_minutes_at_booking }));

            await AppointmentItems.DeleteByAppointmentId(id);

            var pendingStatus = await AppointmentStatuses.GetByCode(AppointmentStatusCodes.Pending);
            foreach (var service in services)
            {
                await AppointmentItems.Create(new AppointmentItem
                {
                    Appointment_id = id,
                    Service_id = service.Id,
                    Status_id = pendingStatus!.Id,
                    Price_at_booking = service.Price,
                    Duration_minutes_at_booking = service.Duration_minutes,
                });
            }

            // Servicos mudaram: a confirmacao anterior da Leila nao vale mais para o novo conjunto.
            if (status.Code == AppointmentStatusCodes.Confirmed)
            {
                await Appointments.UpdateStatus(id, pendingStatus!.Id);
            }

            await AppointmentHistories.Create(new AppointmentHistory
            {
                Appointment_id = id,
                Changed_by_customer_id = customerId,
                Change_type = AppointmentChangeTypes.ItemsChanged,
                Old_value = oldValue,
                New_value = JsonSerializer.Serialize(new { ServiceIds = distinctServiceIds }),
            });

            appointment = await Appointments.GetById(id);
            return Ok(await AppointmentAssembler.BuildDetail(appointment!));
        }

        [HttpPut("{id:long}/cancel")]
        public async Task<IActionResult> Cancel(long id)
        {
            var customerId = User.GetUserId();
            var appointment = await Appointments.GetById(id);
            if (appointment == null || appointment.Customer_id != customerId)
            {
                return NotFound();
            }

            var status = await AppointmentStatuses.GetById(appointment.Status_id);
            if (status!.Code is AppointmentStatusCodes.Cancelled or AppointmentStatusCodes.Completed)
            {
                return BadRequest("Este agendamento ja esta cancelado ou concluido.");
            }

            if ((appointment.Appointment_date.Date - DateTime.Today).TotalDays < BusinessRules.MinDaysForCustomerChange)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    "Cancelamentos com menos de 2 dias de antecedencia so podem ser feitos por telefone com o salao.");
            }

            var cancelledStatus = await AppointmentStatuses.GetByCode(AppointmentStatusCodes.Cancelled);
            await Appointments.Cancel(id, cancelledStatus!.Id);

            var items = await AppointmentItems.GetByAppointmentId(id);
            foreach (var item in items)
            {
                await AppointmentItems.UpdateStatus(item.Id, cancelledStatus.Id);
            }

            await AppointmentHistories.Create(new AppointmentHistory
            {
                Appointment_id = id,
                Changed_by_customer_id = customerId,
                Change_type = AppointmentChangeTypes.Cancellation,
            });

            return NoContent();
        }
    }
}
