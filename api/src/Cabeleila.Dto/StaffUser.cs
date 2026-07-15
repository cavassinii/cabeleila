using System;

namespace DTO.Cabeleila
{
    public class StaffUser
    {
        public long Id { get; set; }
        public string Full_name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password_hash { get; set; } = string.Empty;
        public string Role { get; set; } = StaffRoles.Owner;
        public bool Active { get; set; } = true;
        public DateTime? Created_at { get; set; }
        public DateTime? Updated_at { get; set; }
    }

    public static class StaffRoles
    {
        public const string Owner = "OWNER";
        public const string Attendant = "ATTENDANT";
    }
}
