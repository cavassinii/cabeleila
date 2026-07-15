using API.Security;
using Cabeleila.Security;
using DAL.Cabeleila;
using DTO.Cabeleila;
using DTO.Cabeleila.Requests;
using DTO.Cabeleila.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        [HttpPost("customer/register")]
        public async Task<ActionResult<AuthResponse>> RegisterCustomer([FromBody] RegisterCustomerRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.FullName) ||
                string.IsNullOrWhiteSpace(request.Email) ||
                string.IsNullOrWhiteSpace(request.Phone) ||
                string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest("Nome, email, telefone e senha sao obrigatorios.");
            }

            if (request.Password.Length < 6)
            {
                return BadRequest("A senha deve ter no minimo 6 caracteres.");
            }

            if (await Customers.EmailExists(request.Email))
            {
                return Conflict("Ja existe um cadastro com este email.");
            }

            var customer = new Customer
            {
                Full_name = request.FullName.Trim(),
                Email = request.Email.Trim().ToLowerInvariant(),
                Phone = request.Phone.Trim(),
                Password_hash = PasswordHasher.Hash(request.Password),
            };

            customer.Id = await Customers.Create(customer);

            var (token, expiresAt) = TokenService.GenerateCustomerToken(customer);
            return Ok(new AuthResponse
            {
                Token = token,
                ExpiresAt = expiresAt,
                Id = customer.Id,
                FullName = customer.Full_name,
                Email = customer.Email,
                Role = TokenService.RoleCustomer,
            });
        }

        [HttpPost("customer/login")]
        public async Task<ActionResult<AuthResponse>> LoginCustomer([FromBody] LoginRequest request)
        {
            var customer = await Customers.GetByEmail(request.Email.Trim().ToLowerInvariant());
            if (customer == null || !PasswordHasher.Verify(request.Password, customer.Password_hash))
            {
                return Unauthorized("Email ou senha invalidos.");
            }

            var (token, expiresAt) = TokenService.GenerateCustomerToken(customer);
            return Ok(new AuthResponse
            {
                Token = token,
                ExpiresAt = expiresAt,
                Id = customer.Id,
                FullName = customer.Full_name,
                Email = customer.Email,
                Role = TokenService.RoleCustomer,
            });
        }

        [HttpPost("staff/login")]
        public async Task<ActionResult<AuthResponse>> LoginStaff([FromBody] LoginRequest request)
        {
            var staff = await StaffUsers.GetByEmail(request.Email.Trim().ToLowerInvariant());
            if (staff == null || !PasswordHasher.Verify(request.Password, staff.Password_hash))
            {
                return Unauthorized("Email ou senha invalidos.");
            }

            var (token, expiresAt) = TokenService.GenerateStaffToken(staff);
            return Ok(new AuthResponse
            {
                Token = token,
                ExpiresAt = expiresAt,
                Id = staff.Id,
                FullName = staff.Full_name,
                Email = staff.Email,
                Role = staff.Role,
            });
        }

        [HttpPut("staff/password")]
        [Authorize(Roles = $"{StaffRoles.Owner},{StaffRoles.Attendant}")]
        public async Task<IActionResult> ChangeStaffPassword([FromBody] ChangePasswordRequest request)
        {
            var staffId = User.GetUserId();
            var staff = await StaffUsers.GetById(staffId);
            if (staff == null)
            {
                return Unauthorized();
            }

            if (!PasswordHasher.Verify(request.CurrentPassword, staff.Password_hash))
            {
                return BadRequest("Senha atual incorreta.");
            }

            if (request.NewPassword.Length < 6)
            {
                return BadRequest("A nova senha deve ter no minimo 6 caracteres.");
            }

            await StaffUsers.UpdatePassword(staffId, PasswordHasher.Hash(request.NewPassword));
            return NoContent();
        }
    }
}
