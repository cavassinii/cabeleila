using System;

namespace DTO.Cabeleila
{
    public class AppointmentHistory
    {
        public long Id { get; set; }
        public long Appointment_id { get; set; }
        public long? Changed_by_customer_id { get; set; }
        public long? Changed_by_staff_id { get; set; }
        public string Change_type { get; set; } = string.Empty;
        public string? Old_value { get; set; }
        public string? New_value { get; set; }
        public DateTime? Changed_at { get; set; }
    }

    public static class AppointmentChangeTypes
    {
        public const string Created = "CREATED";
        public const string Reschedule = "RESCHEDULE";
        public const string StatusChange = "STATUS_CHANGE";
        public const string Cancellation = "CANCELLATION";
        public const string ItemsChanged = "ITEMS_CHANGED";
    }
}
