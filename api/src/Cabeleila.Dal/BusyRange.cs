using System;

namespace DAL.Cabeleila
{
    public class BusyRange
    {
        public long AppointmentId { get; set; }
        public TimeSpan StartTime { get; set; }
        public int DurationMinutes { get; set; }
        public TimeSpan EndTime => StartTime + TimeSpan.FromMinutes(DurationMinutes);
    }
}
