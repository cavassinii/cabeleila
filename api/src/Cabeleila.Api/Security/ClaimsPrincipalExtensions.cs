using System.Security.Claims;

namespace API.Security
{
    public static class ClaimsPrincipalExtensions
    {
        public static long GetUserId(this ClaimsPrincipal user)
        {
            var value = user.FindFirstValue(ClaimTypes.NameIdentifier);
            return long.Parse(value ?? "0");
        }
    }
}
