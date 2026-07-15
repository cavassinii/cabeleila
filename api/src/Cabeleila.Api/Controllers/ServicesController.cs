using DAL.Cabeleila;
using DTO.Cabeleila;
using DTO.Cabeleila.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/services")]
    public class ServicesController : ControllerBase
    {
        // Publico: a tela de agendamento do cliente precisa listar os servicos sem estar autenticada ainda.
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<List<Service>>> GetAll()
        {
            return Ok(await Services.GetAllActive());
        }

        [HttpGet("{id:long}")]
        [AllowAnonymous]
        public async Task<ActionResult<Service>> GetById(long id)
        {
            var service = await Services.GetById(id);
            return service == null ? NotFound() : Ok(service);
        }

        [HttpPost]
        [Authorize(Roles = $"{StaffRoles.Owner},{StaffRoles.Attendant}")]
        public async Task<ActionResult<Service>> Create([FromBody] CreateServiceRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name) || request.DurationMinutes <= 0 || request.Price < 0)
            {
                return BadRequest("Nome, duracao (> 0) e preco (>= 0) sao obrigatorios.");
            }

            if (request.DurationMinutes % 30 != 0)
            {
                return BadRequest("A duracao deve ser em multiplos de 30 minutos (30, 60, 90...).");
            }

            var service = new Service
            {
                Name = request.Name.Trim(),
                Description = request.Description,
                Duration_minutes = request.DurationMinutes,
                Price = request.Price,
            };
            var newId = await Services.Create(service);
            var created = await Services.GetById(newId);

            return CreatedAtAction(nameof(GetById), new { id = newId }, created);
        }

        [HttpPut("{id:long}")]
        [Authorize(Roles = $"{StaffRoles.Owner},{StaffRoles.Attendant}")]
        public async Task<IActionResult> Update(long id, [FromBody] CreateServiceRequest request)
        {
            var existing = await Services.GetById(id);
            if (existing == null)
            {
                return NotFound();
            }

            if (string.IsNullOrWhiteSpace(request.Name) || request.DurationMinutes <= 0 || request.Price < 0)
            {
                return BadRequest("Nome, duracao (> 0) e preco (>= 0) sao obrigatorios.");
            }

            if (request.DurationMinutes % 30 != 0)
            {
                return BadRequest("A duracao deve ser em multiplos de 30 minutos (30, 60, 90...).");
            }

            existing.Name = request.Name.Trim();
            existing.Description = request.Description;
            existing.Duration_minutes = request.DurationMinutes;
            existing.Price = request.Price;

            await Services.Update(existing);
            return NoContent();
        }

        [HttpDelete("{id:long}")]
        [Authorize(Roles = $"{StaffRoles.Owner},{StaffRoles.Attendant}")]
        public async Task<IActionResult> Deactivate(long id)
        {
            var existing = await Services.GetById(id);
            if (existing == null)
            {
                return NotFound();
            }

            await Services.Deactivate(id);
            return NoContent();
        }
    }
}
