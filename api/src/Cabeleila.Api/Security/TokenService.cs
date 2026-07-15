using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace API.Security
{
    public static class TokenService
    {
        public const string RoleCustomer = "Customer";

        public static (string token, DateTime expiresAt) GenerateCustomerToken(DTO.Cabeleila.Customer customer)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, customer.Id.ToString()),
                new(ClaimTypes.Email, customer.Email),
                new(ClaimTypes.Name, customer.Full_name),
                new(ClaimTypes.Role, RoleCustomer),
            };

            return Generate(claims);
        }

        public static (string token, DateTime expiresAt) GenerateStaffToken(DTO.Cabeleila.StaffUser staff)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, staff.Id.ToString()),
                new(ClaimTypes.Email, staff.Email),
                new(ClaimTypes.Name, staff.Full_name),
                new(ClaimTypes.Role, staff.Role),
            };

            return Generate(claims);
        }

        private static (string token, DateTime expiresAt) Generate(List<Claim> claims)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(API.JwtSettings.Key));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expiresAt = DateTime.UtcNow.AddMinutes(API.JwtSettings.ExpirationMinutes);

            var token = new JwtSecurityToken(
                issuer: API.JwtSettings.Issuer,
                audience: API.JwtSettings.Audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
        }
    }
}
