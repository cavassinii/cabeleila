using API.Application;
using API.Security;
using DAL.Cabeleila;
using DTO.Cabeleila;
using DTO.Cabeleila.Requests;
using DTO.Cabeleila.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/staff/appointments")]
    [Authorize(Roles = $"{StaffRoles.Owner},{StaffRoles.Attendant}")]
    public class StaffAppointmentsController : ControllerBase
    {
        // Mesma grade de horarios do cliente, para a Leila enxergar os espacos livres ao reagendar por telefone.
        [HttpGet("availability")]
        public async Task<ActionResult<AvailabilityResponse>> GetAvailability(
            [FromQuery] DateTime date, [FromQuery] int durationMinutes, [FromQuery] long? excludeAppointmentId)
        {
            if (durationMinutes <= 0)
            {
                return BadRequest("Informe a duracao total dos servicos selecionados.");
            }

            return Ok(await AvailabilityService.GetAvailability(date, durationMinutes, excludeAppointmentId));
        }

        // Listagem operacional dos agendamentos recebidos, com filtro opcional por dia e por status.
        [HttpGet]
        public async Task<ActionResult<List<StaffAppointmentListItemResponse>>> GetAll([FromQuery] DateTime? date, [FromQuery] string? status)
        {
            if (!string.IsNullOrWhiteSpace(status) && await AppointmentStatuses.GetByCode(status.ToUpperInvariant()) == null)
            {
                return BadRequest("Status invalido.");
            }

            return Ok(await Appointments.GetStaffListing(date, status?.ToUpperInvariant()));
        }

        [HttpGet("{id:long}")]
        public async Task<ActionResult<AppointmentDetailResponse>> GetById(long id)
        {
            var appointment = await Appointments.GetById(id);
            if (appointment == null)
            {
                return NotFound();
            }

            return Ok(await AppointmentAssembler.BuildDetail(appointment));
        }

        [HttpGet("{id:long}/history")]
        public async Task<ActionResult<List<AppointmentHistory>>> GetHistory(long id)
        {
            var appointment = await Appointments.GetById(id);
            if (appointment == null)
            {
                return NotFound();
            }

            return Ok(await AppointmentHistories.GetByAppointmentId(id));
        }

        // A Leila confirma o agendamento ao cliente.
        [HttpPut("{id:long}/confirm")]
        public async Task<ActionResult<AppointmentDetailResponse>> Confirm(long id)
        {
            var appointment = await Appointments.GetById(id);
            if (appointment == null)
            {
                return NotFound();
            }

            var status = await AppointmentStatuses.GetById(appointment.Status_id);
            if (status!.Code != AppointmentStatusCodes.Pending)
            {
                return BadRequest("Somente agendamentos pendentes podem ser confirmados.");
            }

            var staffId = User.GetUserId();
            var confirmedStatus = await AppointmentStatuses.GetByCode(AppointmentStatusCodes.Confirmed);
            await Appointments.Confirm(id, confirmedStatus!.Id, staffId);

            await AppointmentHistories.Create(new AppointmentHistory
            {
                Appointment_id = id,
                Changed_by_staff_id = staffId,
                Change_type = AppointmentChangeTypes.StatusChange,
                Old_value = JsonSerializer.Serialize(new { Status = status.Code }),
                New_value = JsonSerializer.Serialize(new { Status = confirmedStatus.Code }),
            });

            appointment = await Appointments.GetById(id);
            return Ok(await AppointmentAssembler.BuildDetail(appointment!));
        }

        // Alteracao feita pela Leila quando o cliente liga pedindo mudanca - sem a restricao dos 2 dias.
        [HttpPut("{id:long}/reschedule")]
        public async Task<ActionResult<AppointmentDetailResponse>> Reschedule(long id, [FromBody] RescheduleAppointmentRequest request)
        {
            var appointment = await Appointments.GetById(id);
            if (appointment == null)
            {
                return NotFound();
            }

            var status = await AppointmentStatuses.GetById(appointment.Status_id);
            if (status!.Code is AppointmentStatusCodes.Cancelled or AppointmentStatusCodes.Completed)
            {
                return BadRequest("Nao e possivel alterar um agendamento cancelado ou ja concluido.");
            }

            var currentItems = await AppointmentItems.GetByAppointmentId(id);
            var totalDuration = currentItems.Sum(i => i.Duration_minutes_at_booking);
            var slotValidation = await AvailabilityService.ValidateSlot(request.AppointmentDate, request.AppointmentTime, totalDuration, excludeAppointmentId: id);
            if (!slotValidation.IsValid)
            {
                return StatusCode(slotValidation.StatusCode, slotValidation.Message);
            }

            var staffId = User.GetUserId();
            var oldValue = JsonSerializer.Serialize(new { appointment.Appointment_date, appointment.Appointment_time });
            await Appointments.UpdateSchedule(id, request.AppointmentDate.Date, request.AppointmentTime);

            await AppointmentHistories.Create(new AppointmentHistory
            {
                Appointment_id = id,
                Changed_by_staff_id = staffId,
                Change_type = AppointmentChangeTypes.Reschedule,
                Old_value = oldValue,
                New_value = JsonSerializer.Serialize(new { AppointmentDate = request.AppointmentDate.Date, request.AppointmentTime }),
            });

            appointment = await Appointments.GetById(id);
            return Ok(await AppointmentAssembler.BuildDetail(appointment!));
        }

        // Leila troca os servicos por telefone - sem a restricao dos 2 dias.
        [HttpPut("{id:long}/services")]
        public async Task<ActionResult<AppointmentDetailResponse>> EditServices(long id, [FromBody] EditAppointmentServicesRequest request)
        {
            var appointment = await Appointments.GetById(id);
            if (appointment == null)
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

            var staffId = User.GetUserId();
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

            if (status.Code == AppointmentStatusCodes.Confirmed)
            {
                await Appointments.UpdateStatus(id, pendingStatus!.Id);
            }

            await AppointmentHistories.Create(new AppointmentHistory
            {
                Appointment_id = id,
                Changed_by_staff_id = staffId,
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
            var appointment = await Appointments.GetById(id);
            if (appointment == null)
            {
                return NotFound();
            }

            var status = await AppointmentStatuses.GetById(appointment.Status_id);
            if (status!.Code is AppointmentStatusCodes.Cancelled or AppointmentStatusCodes.Completed)
            {
                return BadRequest("Este agendamento ja esta cancelado ou concluido.");
            }

            var staffId = User.GetUserId();
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
                Changed_by_staff_id = staffId,
                Change_type = AppointmentChangeTypes.Cancellation,
            });

            return NoContent();
        }

        // Gerenciamento do status de cada servico solicitado (Pendente / Em andamento / Concluido / Cancelado).
        [HttpPut("items/{itemId:long}/status")]
        public async Task<IActionResult> UpdateItemStatus(long itemId, [FromBody] UpdateAppointmentItemStatusRequest request)
        {
            var item = await AppointmentItems.GetById(itemId);
            if (item == null)
            {
                return NotFound();
            }

            var newStatus = await AppointmentStatuses.GetByCode(request.StatusCode.ToUpperInvariant());
            if (newStatus == null)
            {
                return BadRequest("Status invalido.");
            }

            var staffId = User.GetUserId();
            var oldStatus = await AppointmentStatuses.GetById(item.Status_id);
            await AppointmentItems.UpdateStatus(itemId, newStatus.Id);

            await AppointmentHistories.Create(new AppointmentHistory
            {
                Appointment_id = item.Appointment_id,
                Changed_by_staff_id = staffId,
                Change_type = AppointmentChangeTypes.StatusChange,
                Old_value = JsonSerializer.Serialize(new { ItemId = itemId, Status = oldStatus?.Code }),
                New_value = JsonSerializer.Serialize(new { ItemId = itemId, Status = newStatus.Code }),
            });

            // Se todos os servicos do agendamento ja foram concluidos, fecha o agendamento automaticamente.
            if (newStatus.Code == AppointmentStatusCodes.Completed && await AppointmentItems.AllCompleted(item.Appointment_id))
            {
                await Appointments.UpdateStatus(item.Appointment_id, newStatus.Id);
            }

            return NoContent();
        }
    }
}
