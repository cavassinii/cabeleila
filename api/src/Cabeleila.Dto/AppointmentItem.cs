using System;

namespace DTO.Cabeleila
{
    public class AppointmentItem
    {
        public long Id { get; set; }
        public long Appointment_id { get; set; }
        public long Service_id { get; set; }
        public int Status_id { get; set; }
        public decimal Price_at_booking { get; set; }
        public int Duration_minutes_at_booking { get; set; }
        public DateTime? Created_at { get; set; }
        public DateTime? Updated_at { get; set; }
    }
}
