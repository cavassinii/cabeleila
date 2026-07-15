using API.Utils;
using DAL.Cabeleila;
using DTO.Cabeleila;
using DTO.Cabeleila.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/staff/dashboard")]
    [Authorize(Roles = $"{StaffRoles.Owner},{StaffRoles.Attendant}")]
    public class StaffDashboardController : ControllerBase
    {
        // Desempenho semanal: numero de agendamentos e faturamento da semana (padrao: semana atual).
        [HttpGet("weekly-performance")]
        public async Task<ActionResult<WeeklyPerformanceResponse>> WeeklyPerformance([FromQuery] DateTime? referenceDate)
        {
            var (weekStart, weekEnd) = DateHelper.GetWeekRange(referenceDate ?? DateTime.Today);
            return Ok(await Appointments.GetWeeklyPerformance(weekStart, weekEnd));
        }
    }
}
