using System;

namespace DTO.Cabeleila
{
    public class Service
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Duration_minutes { get; set; }
        public decimal Price { get; set; }
        public bool Active { get; set; } = true;
        public DateTime? Created_at { get; set; }
        public DateTime? Updated_at { get; set; }
    }
}
