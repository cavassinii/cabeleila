using Npgsql;
using NpgsqlTypes;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Cabeleila
{
    public static class AppointmentHistories
    {
        private const string Columns = "id, appointment_id, changed_by_customer_id, changed_by_staff_id, change_type, old_value, new_value, changed_at";
        private const string TableName = "appointment_history";

        public static async Task<long> Create(DTO.Cabeleila.AppointmentHistory obj)
        {
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                INSERT INTO {TableName}
                (
                    appointment_id,
                    changed_by_customer_id,
                    changed_by_staff_id,
                    change_type,
                    old_value,
                    new_value,
                    changed_at
                )
                VALUES
                (
                    @appointment_id,
                    @changed_by_customer_id,
                    @changed_by_staff_id,
                    @change_type,
                    @old_value,
                    @new_value,
                    now()
                )
                RETURNING id;
            ", conn);

            cmd.Parameters.AddWithValue("@appointment_id", obj.Appointment_id);
            cmd.Parameters.AddWithValue("@changed_by_customer_id", (object?)obj.Changed_by_customer_id ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@changed_by_staff_id", (object?)obj.Changed_by_staff_id ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@change_type", obj.Change_type);
            cmd.Parameters.Add(new NpgsqlParameter("@old_value", NpgsqlDbType.Jsonb) { Value = (object?)obj.Old_value ?? DBNull.Value });
            cmd.Parameters.Add(new NpgsqlParameter("@new_value", NpgsqlDbType.Jsonb) { Value = (object?)obj.New_value ?? DBNull.Value });

            return (long)(await cmd.ExecuteScalarAsync())!;
        }

        public static async Task<List<DTO.Cabeleila.AppointmentHistory>> GetByAppointmentId(long appointmentId)
        {
            var objs = new List<DTO.Cabeleila.AppointmentHistory>();
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                SELECT {Columns}
                FROM {TableName}
                WHERE appointment_id = @appointment_id
                ORDER BY changed_at DESC;
            ", conn);
            cmd.Parameters.AddWithValue("@appointment_id", appointmentId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                objs.Add(Connection.MapDataReaderToObject<DTO.Cabeleila.AppointmentHistory>(reader));
            }
            return objs;
        }
    }
}
