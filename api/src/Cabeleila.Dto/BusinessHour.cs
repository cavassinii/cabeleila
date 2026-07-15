using System;

namespace DTO.Cabeleila
{
    public class BusinessHour
    {
        public int Day_of_week { get; set; }
        public TimeSpan Opens_at { get; set; }
        public TimeSpan Closes_at { get; set; }
        public bool Is_closed { get; set; }
        public DateTime? Updated_at { get; set; }
    }
}
