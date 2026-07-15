using System;
using System.Collections.Generic;

namespace DTO.Cabeleila.Requests
{
    public class CreateAppointmentRequest
    {
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
        public List<long> ServiceIds { get; set; } = new();
        public string? Notes { get; set; }

        // Cliente ja viu a sugestao de mesma semana e decidiu manter a data original.
        public bool KeepOriginalDate { get; set; }
    }

    public class RescheduleAppointmentRequest
    {
        public DateTime AppointmentDate { get; set; }
        public TimeSpan AppointmentTime { get; set; }
    }

    public class UpdateAppointmentItemStatusRequest
    {
        public string StatusCode { get; set; } = string.Empty;
    }

    public class CreateServiceRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationMinutes { get; set; }
        public decimal Price { get; set; }
    }

    public class EditAppointmentServicesRequest
    {
        public List<long> ServiceIds { get; set; } = new();
    }

    public class BusinessHourEntry
    {
        public int DayOfWeek { get; set; }
        public TimeSpan OpensAt { get; set; }
        public TimeSpan ClosesAt { get; set; }
        public bool IsClosed { get; set; }
    }

    public class UpdateBusinessHoursRequest
    {
        public List<BusinessHourEntry> Days { get; set; } = new();
    }
}
