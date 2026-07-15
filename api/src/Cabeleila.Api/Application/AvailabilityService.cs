using DAL.Cabeleila;
using DTO.Cabeleila.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Application
{
    public class SlotValidationResult
    {
        public bool IsValid { get; private set; }
        public int StatusCode { get; private set; }
        public string? Message { get; private set; }

        public static SlotValidationResult Ok() => new() { IsValid = true };

        public static SlotValidationResult Fail(int statusCode, string message) =>
            new() { IsValid = false, StatusCode = statusCode, Message = message };
    }

    // Centraliza a logica de horario comercial + grade de 30 min + conflito de agenda,
    // reaproveitada tanto pelo fluxo do cliente quanto pelo painel da Leila.
    public static class AvailabilityService
    {
        public const int SlotMinutes = 30;

        public static bool IsSlotAligned(TimeSpan time) => time.Minutes % SlotMinutes == 0 && time.Seconds == 0;

        public static async Task<AvailabilityResponse> GetAvailability(DateTime date, int durationMinutes, long? excludeAppointmentId = null)
        {
            var response = new AvailabilityResponse { Date = date.Date };
            var hours = await BusinessHours.GetByDayOfWeek((int)date.DayOfWeek);

            if (hours == null || hours.Is_closed)
            {
                response.IsClosed = true;
                return response;
            }

            response.OpensAt = hours.Opens_at;
            response.ClosesAt = hours.Closes_at;

            var busyRanges = await Appointments.GetBusyRanges(date, excludeAppointmentId);
            var duration = TimeSpan.FromMinutes(durationMinutes);
            var isToday = date.Date == DateTime.Today;
            var nowTime = DateTime.Now.TimeOfDay;

            var slots = new List<string>();
            for (var slotStart = hours.Opens_at; slotStart + duration <= hours.Closes_at; slotStart += TimeSpan.FromMinutes(SlotMinutes))
            {
                if (isToday && slotStart <= nowTime)
                {
                    continue;
                }

                var slotEnd = slotStart + duration;
                var hasConflict = busyRanges.Any(b => slotStart < b.EndTime && b.StartTime < slotEnd);
                if (!hasConflict)
                {
                    slots.Add(FormatTime(slotStart));
                }
            }

            response.AvailableSlots = slots;
            return response;
        }

        // Revalida no servidor tudo que a grade de horarios ja deveria ter garantido no front:
        // alinhamento de 30 min, dentro do expediente, nao esta no passado e nao sobrepoe outro agendamento.
        public static async Task<SlotValidationResult> ValidateSlot(DateTime date, TimeSpan time, int durationMinutes, long? excludeAppointmentId = null)
        {
            if (!IsSlotAligned(time))
            {
                return SlotValidationResult.Fail(400, "O horario deve ser em intervalos de 30 minutos.");
            }

            var hours = await BusinessHours.GetByDayOfWeek((int)date.DayOfWeek);
            if (hours == null || hours.Is_closed)
            {
                return SlotValidationResult.Fail(400, "O salao nao funciona nesse dia.");
            }

            var slotEnd = time + TimeSpan.FromMinutes(durationMinutes);
            if (time < hours.Opens_at || slotEnd > hours.Closes_at)
            {
                return SlotValidationResult.Fail(400, $"Horario fora do expediente ({FormatTime(hours.Opens_at)} as {FormatTime(hours.Closes_at)}).");
            }

            if (date.Date == DateTime.Today && time <= DateTime.Now.TimeOfDay)
            {
                return SlotValidationResult.Fail(400, "Nao e possivel agendar em um horario que ja passou.");
            }

            var busyRanges = await Appointments.GetBusyRanges(date, excludeAppointmentId);
            if (busyRanges.Any(b => time < b.EndTime && b.StartTime < slotEnd))
            {
                return SlotValidationResult.Fail(409, "Esse horario acabou de ficar indisponivel. Escolha outro.");
            }

            return SlotValidationResult.Ok();
        }

        private static string FormatTime(TimeSpan time) => time.ToString(@"hh\:mm");
    }
}
