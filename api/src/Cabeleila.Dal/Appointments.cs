using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Cabeleila
{
    public static class Appointments
    {
        private const string Columns = "id, customer_id, appointment_date, appointment_time, status_id, notes, confirmed_at, confirmed_by, cancelled_at, created_by_staff_id, created_at, updated_at";
        private const string TableName = "appointments";

        public static async Task<long> Create(DTO.Cabeleila.Appointment obj)
        {
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                INSERT INTO {TableName}
                (
                    customer_id,
                    appointment_date,
                    appointment_time,
                    status_id,
                    notes,
                    created_by_staff_id,
                    created_at,
                    updated_at
                )
                VALUES
                (
                    @customer_id,
                    @appointment_date,
                    @appointment_time,
                    @status_id,
                    @notes,
                    @created_by_staff_id,
                    now(),
                    now()
                )
                RETURNING id;
            ", conn);

            cmd.Parameters.AddWithValue("@customer_id", obj.Customer_id);
            cmd.Parameters.AddWithValue("@appointment_date", obj.Appointment_date.Date);
            cmd.Parameters.AddWithValue("@appointment_time", obj.Appointment_time);
            cmd.Parameters.AddWithValue("@status_id", obj.Status_id);
            cmd.Parameters.AddWithValue("@notes", (object?)obj.Notes ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@created_by_staff_id", (object?)obj.Created_by_staff_id ?? DBNull.Value);

            return (long)(await cmd.ExecuteScalarAsync())!;
        }

        public static async Task<DTO.Cabeleila.Appointment?> GetById(long id)
        {
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                SELECT {Columns}
                FROM {TableName}
                WHERE id = @id;
            ", conn);
            cmd.Parameters.AddWithValue("@id", id);

            await using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? Connection.MapDataReaderToObject<DTO.Cabeleila.Appointment>(reader) : null;
        }

        // Faixas de horario ja ocupadas num dia (agendamento nao cancelado + soma da duracao dos seus servicos).
        // Usado tanto para calcular os horarios livres quanto para checar conflito ao criar/reagendar.
        public static async Task<List<BusyRange>> GetBusyRanges(DateTime date, long? excludeAppointmentId = null)
        {
            var ranges = new List<BusyRange>();
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(@"
                SELECT a.id, a.appointment_time, COALESCE(SUM(ai.duration_minutes_at_booking), 0) AS total_duration
                FROM appointments a
                JOIN appointment_status s ON s.id = a.status_id
                LEFT JOIN appointment_items ai ON ai.appointment_id = a.id
                WHERE a.appointment_date = @date
                  AND s.code <> 'CANCELLED'
                  AND (@exclude_id IS NULL OR a.id <> @exclude_id)
                GROUP BY a.id, a.appointment_time;
            ", conn);
            cmd.Parameters.AddWithValue("@date", date.Date);
            cmd.Parameters.Add(new NpgsqlParameter("@exclude_id", NpgsqlDbType.Bigint) { Value = (object?)excludeAppointmentId ?? DBNull.Value });

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                ranges.Add(new BusyRange
                {
                    AppointmentId = reader.GetInt64(reader.GetOrdinal("id")),
                    StartTime = reader.GetTimeSpan(reader.GetOrdinal("appointment_time")),
                    DurationMinutes = reader.GetInt32(reader.GetOrdinal("total_duration")),
                });
            }
            return ranges;
        }

        public static async Task<List<DTO.Cabeleila.Appointment>> GetByCustomerAndDateRange(long customerId, DateTime from, DateTime to)
        {
            var objs = new List<DTO.Cabeleila.Appointment>();
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                SELECT {Columns}
                FROM {TableName}
                WHERE customer_id = @customer_id
                  AND appointment_date BETWEEN @from AND @to
                ORDER BY appointment_date DESC, appointment_time DESC;
            ", conn);
            cmd.Parameters.AddWithValue("@customer_id", customerId);
            cmd.Parameters.AddWithValue("@from", from.Date);
            cmd.Parameters.AddWithValue("@to", to.Date);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                objs.Add(Connection.MapDataReaderToObject<DTO.Cabeleila.Appointment>(reader));
            }
            return objs;
        }

        // Usado na sugestao de "mesma semana": retorna o primeiro agendamento ativo
        // (nao cancelado) do cliente cuja data cai dentro da semana informada.
        public static async Task<DTO.Cabeleila.Appointment?> GetFirstActiveInWeek(long customerId, DateTime weekStart, DateTime weekEnd)
        {
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                SELECT a.id, a.customer_id, a.appointment_date, a.appointment_time, a.status_id,
                       a.notes, a.confirmed_at, a.confirmed_by, a.cancelled_at, a.created_by_staff_id,
                       a.created_at, a.updated_at
                FROM {TableName} a
                JOIN appointment_status s ON s.id = a.status_id
                WHERE a.customer_id = @customer_id
                  AND a.appointment_date BETWEEN @week_start AND @week_end
                  AND s.code <> 'CANCELLED'
                ORDER BY a.appointment_date ASC, a.created_at ASC
                LIMIT 1;
            ", conn);
            cmd.Parameters.AddWithValue("@customer_id", customerId);
            cmd.Parameters.AddWithValue("@week_start", weekStart.Date);
            cmd.Parameters.AddWithValue("@week_end", weekEnd.Date);

            await using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? Connection.MapDataReaderToObject<DTO.Cabeleila.Appointment>(reader) : null;
        }

        public static async Task UpdateSchedule(long id, DateTime appointmentDate, TimeSpan appointmentTime)
        {
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                UPDATE {TableName}
                SET appointment_date = @appointment_date,
                    appointment_time = @appointment_time,
                    updated_at = now()
                WHERE id = @id;
            ", conn);
            cmd.Parameters.AddWithValue("@appointment_date", appointmentDate.Date);
            cmd.Parameters.AddWithValue("@appointment_time", appointmentTime);
            cmd.Parameters.AddWithValue("@id", id);

            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task UpdateStatus(long id, int statusId)
        {
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                UPDATE {TableName}
                SET status_id = @status_id,
                    updated_at = now()
                WHERE id = @id;
            ", conn);
            cmd.Parameters.AddWithValue("@status_id", statusId);
            cmd.Parameters.AddWithValue("@id", id);

            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task Confirm(long id, int confirmedStatusId, long staffId)
        {
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                UPDATE {TableName}
                SET status_id = @status_id,
                    confirmed_at = now(),
                    confirmed_by = @staff_id,
                    updated_at = now()
                WHERE id = @id;
            ", conn);
            cmd.Parameters.AddWithValue("@status_id", confirmedStatusId);
            cmd.Parameters.AddWithValue("@staff_id", staffId);
            cmd.Parameters.AddWithValue("@id", id);

            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task Cancel(long id, int cancelledStatusId)
        {
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                UPDATE {TableName}
                SET status_id = @status_id,
                    cancelled_at = now(),
                    updated_at = now()
                WHERE id = @id;
            ", conn);
            cmd.Parameters.AddWithValue("@status_id", cancelledStatusId);
            cmd.Parameters.AddWithValue("@id", id);

            await cmd.ExecuteNonQueryAsync();
        }

        // Historico do cliente (aba "meus agendamentos"): 1 linha por agendamento, servicos resumidos e total,
        // resolvido em uma unica query (sem N+1) via string_agg/SUM agrupado.
        public static async Task<List<DTO.Cabeleila.Responses.AppointmentListItemResponse>> GetCustomerHistory(long customerId, DateTime from, DateTime to)
        {
            var objs = new List<DTO.Cabeleila.Responses.AppointmentListItemResponse>();
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(@"
                SELECT
                    a.id,
                    a.appointment_date,
                    a.appointment_time,
                    s.code AS status_code,
                    s.description AS status_description,
                    COALESCE(string_agg(sv.name, ', ' ORDER BY sv.name), '') AS services_summary,
                    COALESCE(SUM(ai.price_at_booking), 0) AS total_price
                FROM appointments a
                JOIN appointment_status s ON s.id = a.status_id
                LEFT JOIN appointment_items ai ON ai.appointment_id = a.id
                LEFT JOIN services sv ON sv.id = ai.service_id
                WHERE a.customer_id = @customer_id
                  AND a.appointment_date BETWEEN @from AND @to
                GROUP BY a.id, a.appointment_date, a.appointment_time, s.code, s.description
                ORDER BY a.appointment_date DESC, a.appointment_time DESC;
            ", conn);
            cmd.Parameters.AddWithValue("@customer_id", customerId);
            cmd.Parameters.AddWithValue("@from", from.Date);
            cmd.Parameters.AddWithValue("@to", to.Date);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                objs.Add(new DTO.Cabeleila.Responses.AppointmentListItemResponse
                {
                    Id = reader.GetInt64(reader.GetOrdinal("id")),
                    AppointmentDate = reader.GetDateTime(reader.GetOrdinal("appointment_date")),
                    AppointmentTime = reader.GetTimeSpan(reader.GetOrdinal("appointment_time")),
                    StatusCode = reader.GetString(reader.GetOrdinal("status_code")),
                    StatusDescription = reader.GetString(reader.GetOrdinal("status_description")),
                    ServicesSummary = reader.GetString(reader.GetOrdinal("services_summary")),
                    TotalPrice = reader.GetDecimal(reader.GetOrdinal("total_price")),
                });
            }
            return objs;
        }

        // Listagem operacional (painel da Leila): agendamentos recebidos, com cliente e resumo dos servicos.
        public static async Task<List<DTO.Cabeleila.Responses.StaffAppointmentListItemResponse>> GetStaffListing(DateTime? date, string? statusCode)
        {
            var objs = new List<DTO.Cabeleila.Responses.StaffAppointmentListItemResponse>();
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(@"
                SELECT
                    a.id,
                    a.customer_id,
                    c.full_name AS customer_name,
                    c.phone AS customer_phone,
                    a.appointment_date,
                    a.appointment_time,
                    s.code AS status_code,
                    s.description AS status_description,
                    COALESCE(string_agg(sv.name, ', ' ORDER BY sv.name), '') AS services_summary,
                    COALESCE(SUM(ai.price_at_booking), 0) AS total_price
                FROM appointments a
                JOIN customers c ON c.id = a.customer_id
                JOIN appointment_status s ON s.id = a.status_id
                LEFT JOIN appointment_items ai ON ai.appointment_id = a.id
                LEFT JOIN services sv ON sv.id = ai.service_id
                WHERE (@date IS NULL OR a.appointment_date = @date)
                  AND (@status_code IS NULL OR s.code = @status_code)
                GROUP BY a.id, a.customer_id, c.full_name, c.phone, a.appointment_date, a.appointment_time, s.code, s.description
                ORDER BY a.appointment_date, a.appointment_time;
            ", conn);
            cmd.Parameters.Add(new NpgsqlParameter("@date", NpgsqlDbType.Date) { Value = (object?)date?.Date ?? DBNull.Value });
            cmd.Parameters.Add(new NpgsqlParameter("@status_code", NpgsqlDbType.Varchar) { Value = (object?)statusCode ?? DBNull.Value });

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                objs.Add(new DTO.Cabeleila.Responses.StaffAppointmentListItemResponse
                {
                    Id = reader.GetInt64(reader.GetOrdinal("id")),
                    CustomerId = reader.GetInt64(reader.GetOrdinal("customer_id")),
                    CustomerName = reader.GetString(reader.GetOrdinal("customer_name")),
                    CustomerPhone = reader.GetString(reader.GetOrdinal("customer_phone")),
                    AppointmentDate = reader.GetDateTime(reader.GetOrdinal("appointment_date")),
                    AppointmentTime = reader.GetTimeSpan(reader.GetOrdinal("appointment_time")),
                    StatusCode = reader.GetString(reader.GetOrdinal("status_code")),
                    StatusDescription = reader.GetString(reader.GetOrdinal("status_description")),
                    ServicesSummary = reader.GetString(reader.GetOrdinal("services_summary")),
                    TotalPrice = reader.GetDecimal(reader.GetOrdinal("total_price")),
                });
            }
            return objs;
        }

        public static async Task<DTO.Cabeleila.Responses.WeeklyPerformanceResponse> GetWeeklyPerformance(DateTime weekStart, DateTime weekEnd)
        {
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(@"
                SELECT
                    COUNT(DISTINCT a.id) AS total_appointments,
                    COUNT(DISTINCT a.id) FILTER (WHERE s.code = 'COMPLETED') AS completed_appointments,
                    COUNT(DISTINCT a.id) FILTER (WHERE s.code = 'CANCELLED') AS cancelled_appointments,
                    COALESCE(SUM(ai.price_at_booking) FILTER (WHERE s.code = 'COMPLETED'), 0) AS total_revenue
                FROM appointments a
                JOIN appointment_status s ON s.id = a.status_id
                LEFT JOIN appointment_items ai ON ai.appointment_id = a.id
                WHERE a.appointment_date BETWEEN @week_start AND @week_end;
            ", conn);
            cmd.Parameters.AddWithValue("@week_start", weekStart.Date);
            cmd.Parameters.AddWithValue("@week_end", weekEnd.Date);

            await using var reader = await cmd.ExecuteReaderAsync();
            await reader.ReadAsync();

            return new DTO.Cabeleila.Responses.WeeklyPerformanceResponse
            {
                WeekStart = weekStart.Date,
                WeekEnd = weekEnd.Date,
                TotalAppointments = reader.GetInt32(reader.GetOrdinal("total_appointments")),
                CompletedAppointments = reader.GetInt32(reader.GetOrdinal("completed_appointments")),
                CancelledAppointments = reader.GetInt32(reader.GetOrdinal("cancelled_appointments")),
                TotalRevenue = reader.GetDecimal(reader.GetOrdinal("total_revenue")),
            };
        }
    }
}
