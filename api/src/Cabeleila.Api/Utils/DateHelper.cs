using System;

namespace API.Utils
{
    public static class DateHelper
    {
        // Semana comercial: segunda a domingo (ISO 8601), igual ao calendario usado no Brasil para "semana".
        public static (DateTime WeekStart, DateTime WeekEnd) GetWeekRange(DateTime date)
        {
            var d = date.Date;
            var diffToMonday = ((int)d.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
            var weekStart = d.AddDays(-diffToMonday);
            var weekEnd = weekStart.AddDays(6);
            return (weekStart, weekEnd);
        }
    }
}
