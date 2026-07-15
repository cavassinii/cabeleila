using System;
using System.Collections.Generic;
using System.Linq;

namespace DTO.Cabeleila.Responses
{
    public class AppointmentItemDetail
    {
        public long Id { get; set; }
        public long ServiceId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int DurationMinutes { get; set; }
        public string StatusCode { get; set; } = string.Empty;
        public string StatusDescription { get; set; } = string.Empty;
    }

    public class AppointmentDetailResponse
    {
        public long Id { get; set; }
        public long CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public string StatusCode { get; set; } = string.Empty;
        public string StatusDescription { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime? CancelledAt { get; set; }
        public List<AppointmentItemDetail> Items { get; set; } = new();
        public decimal TotalPrice => Items.Sum(i => i.Price);
    }

    public class AppointmentListItemResponse
    {
        public long Id { get; set; }
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public string StatusCode { get; set; } = string.Empty;
        public string StatusDescription { get; set; } = string.Empty;
        public string ServicesSummary { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
    }

    public class StaffAppointmentListItemResponse
    {
        public long Id { get; set; }
        public long CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public string StatusCode { get; set; } = string.Empty;
        public string StatusDescription { get; set; } = string.Empty;
        public string ServicesSummary { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
    }

    public class SameWeekSuggestionResponse
    {
        public bool HasSuggestion { get; set; }
        public long? ExistingAppointmentId { get; set; }
        public DateTime? SuggestedDate { get; set; }
    }

    public class WeeklyPerformanceResponse
    {
        public DateTime WeekStart { get; set; }
        public DateTime WeekEnd { get; set; }
        public int TotalAppointments { get; set; }
        public int CompletedAppointments { get; set; }
        public int CancelledAppointments { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class AvailabilityResponse
    {
        public DateTime Date { get; set; }
        public bool IsClosed { get; set; }
        public TimeSpan? OpensAt { get; set; }
        public TimeSpan? ClosesAt { get; set; }
        public List<string> AvailableSlots { get; set; } = new();
    }

    public class BusinessHourResponse
    {
        public int DayOfWeek { get; set; }
        public string DayName { get; set; } = string.Empty;
        public TimeSpan OpensAt { get; set; }
        public TimeSpan ClosesAt { get; set; }
        public bool IsClosed { get; set; }
    }
}
