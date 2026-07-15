using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Cabeleila
{
    public static class AppointmentItems
    {
        private const string Columns = "id, appointment_id, service_id, status_id, price_at_booking, duration_minutes_at_booking, created_at, updated_at";
        private const string TableName = "appointment_items";

        // Usado ao editar os servicos de um agendamento: remove os itens atuais para
        // recriar do zero com o novo conjunto de servicos (mesma logica da criacao).
        public static async Task DeleteByAppointmentId(long appointmentId)
        {
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                DELETE FROM {TableName}
                WHERE appointment_id = @appointment_id;
            ", conn);
            cmd.Parameters.AddWithValue("@appointment_id", appointmentId);

            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<long> Create(DTO.Cabeleila.AppointmentItem obj)
        {
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                INSERT INTO {TableName}
                (
                    appointment_id,
                    service_id,
                    status_id,
                    price_at_booking,
                    duration_minutes_at_booking,
                    created_at,
                    updated_at
                )
                VALUES
                (
                    @appointment_id,
                    @service_id,
                    @status_id,
                    @price_at_booking,
                    @duration_minutes_at_booking,
                    now(),
                    now()
                )
                RETURNING id;
            ", conn);

            cmd.Parameters.AddWithValue("@appointment_id", obj.Appointment_id);
            cmd.Parameters.AddWithValue("@service_id", obj.Service_id);
            cmd.Parameters.AddWithValue("@status_id", obj.Status_id);
            cmd.Parameters.AddWithValue("@price_at_booking", obj.Price_at_booking);
            cmd.Parameters.AddWithValue("@duration_minutes_at_booking", obj.Duration_minutes_at_booking);

            return (long)(await cmd.ExecuteScalarAsync())!;
        }

        public static async Task<DTO.Cabeleila.AppointmentItem?> GetById(long id)
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
            return await reader.ReadAsync() ? Connection.MapDataReaderToObject<DTO.Cabeleila.AppointmentItem>(reader) : null;
        }

        public static async Task<List<DTO.Cabeleila.AppointmentItem>> GetByAppointmentId(long appointmentId)
        {
            var objs = new List<DTO.Cabeleila.AppointmentItem>();
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                SELECT {Columns}
                FROM {TableName}
                WHERE appointment_id = @appointment_id
                ORDER BY id;
            ", conn);
            cmd.Parameters.AddWithValue("@appointment_id", appointmentId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                objs.Add(Connection.MapDataReaderToObject<DTO.Cabeleila.AppointmentItem>(reader));
            }
            return objs;
        }

        // Join com services + appointment_status para montar o detalhe do agendamento (leitura, nao usa o mapper generico).
        public static async Task<List<DTO.Cabeleila.Responses.AppointmentItemDetail>> GetDetailByAppointmentId(long appointmentId)
        {
            var objs = new List<DTO.Cabeleila.Responses.AppointmentItemDetail>();
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(@"
                SELECT
                    ai.id,
                    ai.service_id,
                    sv.name AS service_name,
                    ai.price_at_booking,
                    ai.duration_minutes_at_booking,
                    st.code AS status_code,
                    st.description AS status_description
                FROM appointment_items ai
                JOIN services sv ON sv.id = ai.service_id
                JOIN appointment_status st ON st.id = ai.status_id
                WHERE ai.appointment_id = @appointment_id
                ORDER BY ai.id;
            ", conn);
            cmd.Parameters.AddWithValue("@appointment_id", appointmentId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                objs.Add(new DTO.Cabeleila.Responses.AppointmentItemDetail
                {
                    Id = reader.GetInt64(reader.GetOrdinal("id")),
                    ServiceId = reader.GetInt64(reader.GetOrdinal("service_id")),
                    ServiceName = reader.GetString(reader.GetOrdinal("service_name")),
                    Price = reader.GetDecimal(reader.GetOrdinal("price_at_booking")),
                    DurationMinutes = reader.GetInt32(reader.GetOrdinal("duration_minutes_at_booking")),
                    StatusCode = reader.GetString(reader.GetOrdinal("status_code")),
                    StatusDescription = reader.GetString(reader.GetOrdinal("status_description")),
                });
            }
            return objs;
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

        // Usado apos marcar um item como concluido, para saber se o agendamento inteiro pode ser fechado.
        public static async Task<bool> AllCompleted(long appointmentId)
        {
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(@"
                SELECT NOT EXISTS (
                    SELECT 1
                    FROM appointment_items ai
                    JOIN appointment_status s ON s.id = ai.status_id
                    WHERE ai.appointment_id = @appointment_id
                      AND s.code <> 'COMPLETED'
                );
            ", conn);
            cmd.Parameters.AddWithValue("@appointment_id", appointmentId);

            return (bool)(await cmd.ExecuteScalarAsync())!;
        }
    }
}
