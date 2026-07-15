using DAL.Cabeleila;
using DTO.Cabeleila;
using DTO.Cabeleila.Requests;
using DTO.Cabeleila.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/staff/business-hours")]
    [Authorize(Roles = $"{StaffRoles.Owner},{StaffRoles.Attendant}")]
    public class StaffBusinessHoursController : ControllerBase
    {
        private static readonly string[] DayNames =
        {
            "Domingo", "Segunda-feira", "Terca-feira", "Quarta-feira", "Quinta-feira", "Sexta-feira", "Sabado",
        };

        [HttpGet]
        public async Task<ActionResult<List<BusinessHourResponse>>> GetAll()
        {
            var hours = await BusinessHours.GetAll();
            return Ok(hours.Select(ToResponse).ToList());
        }

        [HttpPut]
        public async Task<ActionResult<List<BusinessHourResponse>>> UpdateAll([FromBody] UpdateBusinessHoursRequest request)
        {
            if (request.Days == null || request.Days.Count != 7 || request.Days.Select(d => d.DayOfWeek).Distinct().Count() != 7)
            {
                return BadRequest("Envie exatamente um horario para cada um dos 7 dias da semana (0 a 6).");
            }

            foreach (var day in request.Days)
            {
                if (day.DayOfWeek is < 0 or > 6)
                {
                    return BadRequest("Dia da semana invalido (use 0 a 6).");
                }

                if (!day.IsClosed && day.ClosesAt <= day.OpensAt)
                {
                    return BadRequest($"Horario invalido para {DayNames[day.DayOfWeek]}: fechamento deve ser depois da abertura.");
                }

                await BusinessHours.Update(day.DayOfWeek, day.OpensAt, day.ClosesAt, day.IsClosed);
            }

            var updated = await BusinessHours.GetAll();
            return Ok(updated.Select(ToResponse).ToList());
        }

        private static BusinessHourResponse ToResponse(BusinessHour hour) => new()
        {
            DayOfWeek = hour.Day_of_week,
            DayName = DayNames[hour.Day_of_week],
            OpensAt = hour.Opens_at,
            ClosesAt = hour.Closes_at,
            IsClosed = hour.Is_closed,
        };
    }
}
