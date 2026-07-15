namespace DTO.Cabeleila
{
    public class AppointmentStatus
    {
        public int Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public static class AppointmentStatusCodes
    {
        public const string Pending = "PENDING";
        public const string Confirmed = "CONFIRMED";
        public const string InProgress = "IN_PROGRESS";
        public const string Completed = "COMPLETED";
        public const string Cancelled = "CANCELLED";
    }
}
