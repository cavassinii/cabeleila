using System;

namespace DTO.Cabeleila
{
    public class Customer
    {
        public long Id { get; set; }
        public string Full_name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Password_hash { get; set; } = string.Empty;
        public bool Active { get; set; } = true;
        public DateTime? Created_at { get; set; }
        public DateTime? Updated_at { get; set; }
    }
}
