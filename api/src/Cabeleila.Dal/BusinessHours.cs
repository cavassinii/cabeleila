using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Cabeleila
{
    public static class BusinessHours
    {
        private const string Columns = "day_of_week, opens_at, closes_at, is_closed, updated_at";
        private const string TableName = "business_hours";

        public static async Task<List<DTO.Cabeleila.BusinessHour>> GetAll()
        {
            var objs = new List<DTO.Cabeleila.BusinessHour>();
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                SELECT {Columns}
                FROM {TableName}
                ORDER BY day_of_week;
            ", conn);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                objs.Add(Connection.MapDataReaderToObject<DTO.Cabeleila.BusinessHour>(reader));
            }
            return objs;
        }

        public static async Task<DTO.Cabeleila.BusinessHour?> GetByDayOfWeek(int dayOfWeek)
        {
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                SELECT {Columns}
                FROM {TableName}
                WHERE day_of_week = @day_of_week;
            ", conn);
            cmd.Parameters.AddWithValue("@day_of_week", dayOfWeek);

            await using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? Connection.MapDataReaderToObject<DTO.Cabeleila.BusinessHour>(reader) : null;
        }

        public static async Task Update(int dayOfWeek, System.TimeSpan opensAt, System.TimeSpan closesAt, bool isClosed)
        {
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                UPDATE {TableName}
                SET opens_at = @opens_at,
                    closes_at = @closes_at,
                    is_closed = @is_closed,
                    updated_at = now()
                WHERE day_of_week = @day_of_week;
            ", conn);
            cmd.Parameters.AddWithValue("@opens_at", opensAt);
            cmd.Parameters.AddWithValue("@closes_at", closesAt);
            cmd.Parameters.AddWithValue("@is_closed", isClosed);
            cmd.Parameters.AddWithValue("@day_of_week", dayOfWeek);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
