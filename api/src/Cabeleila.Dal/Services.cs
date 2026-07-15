using Npgsql;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DAL.Cabeleila
{
    public static class Services
    {
        private const string Columns = "id, name, description, duration_minutes, price, active, created_at, updated_at";
        private const string TableName = "services";

        public static async Task<List<DTO.Cabeleila.Service>> GetAllActive()
        {
            var objs = new List<DTO.Cabeleila.Service>();
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                SELECT {Columns}
                FROM {TableName}
                WHERE active = TRUE
                ORDER BY name;
            ", conn);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                objs.Add(Connection.MapDataReaderToObject<DTO.Cabeleila.Service>(reader));
            }
            return objs;
        }

        public static async Task<DTO.Cabeleila.Service?> GetById(long id)
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
            return await reader.ReadAsync() ? Connection.MapDataReaderToObject<DTO.Cabeleila.Service>(reader) : null;
        }

        public static async Task<List<DTO.Cabeleila.Service>> GetByIds(IEnumerable<long> ids)
        {
            var objs = new List<DTO.Cabeleila.Service>();
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                SELECT {Columns}
                FROM {TableName}
                WHERE id = ANY(@ids) AND active = TRUE;
            ", conn);
            cmd.Parameters.AddWithValue("@ids", System.Linq.Enumerable.ToArray(ids));

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                objs.Add(Connection.MapDataReaderToObject<DTO.Cabeleila.Service>(reader));
            }
            return objs;
        }

        public static async Task<long> Create(DTO.Cabeleila.Service obj)
        {
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                INSERT INTO {TableName}
                (
                    name,
                    description,
                    duration_minutes,
                    price,
                    active,
                    created_at,
                    updated_at
                )
                VALUES
                (
                    @name,
                    @description,
                    @duration_minutes,
                    @price,
                    TRUE,
                    now(),
                    now()
                )
                RETURNING id;
            ", conn);

            cmd.Parameters.AddWithValue("@name", obj.Name);
            cmd.Parameters.AddWithValue("@description", (object?)obj.Description ?? System.DBNull.Value);
            cmd.Parameters.AddWithValue("@duration_minutes", obj.Duration_minutes);
            cmd.Parameters.AddWithValue("@price", obj.Price);

            return (long)(await cmd.ExecuteScalarAsync())!;
        }

        public static async Task Update(DTO.Cabeleila.Service obj)
        {
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                UPDATE {TableName}
                SET
                    name = @name,
                    description = @description,
                    duration_minutes = @duration_minutes,
                    price = @price,
                    updated_at = now()
                WHERE id = @id;
            ", conn);

            cmd.Parameters.AddWithValue("@name", obj.Name);
            cmd.Parameters.AddWithValue("@description", (object?)obj.Description ?? System.DBNull.Value);
            cmd.Parameters.AddWithValue("@duration_minutes", obj.Duration_minutes);
            cmd.Parameters.AddWithValue("@price", obj.Price);
            cmd.Parameters.AddWithValue("@id", obj.Id);

            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task Deactivate(long id)
        {
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                UPDATE {TableName}
                SET active = FALSE,
                    updated_at = now()
                WHERE id = @id;
            ", conn);
            cmd.Parameters.AddWithValue("@id", id);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
