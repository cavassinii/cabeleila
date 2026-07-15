using System;

namespace DTO.Cabeleila
{
    public class Appointment
    {
        public long Id { get; set; }
        public long Customer_id { get; set; }
        public DateTime Appointment_date { get; set; }
        public TimeSpan Appointment_time { get; set; }
        public int Status_id { get; set; }
        public string? Notes { get; set; }
        public DateTime? Confirmed_at { get; set; }
        public long? Confirmed_by { get; set; }
        public DateTime? Cancelled_at { get; set; }
        public long? Created_by_staff_id { get; set; }
        public DateTime? Created_at { get; set; }
        public DateTime? Updated_at { get; set; }
    }
}
