using Npgsql;
using System.Threading.Tasks;

namespace DAL.Cabeleila
{
    public static class Customers
    {
        private const string Columns = "id, full_name, email, phone, password_hash, active, created_at, updated_at";
        private const string TableName = "customers";

        public static async Task<DTO.Cabeleila.Customer?> GetByEmail(string email)
        {
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                SELECT {Columns}
                FROM {TableName}
                WHERE email = @email AND active = TRUE;
            ", conn);
            cmd.Parameters.AddWithValue("@email", email);

            await using var reader = await cmd.ExecuteReaderAsync();
            return await reader.ReadAsync() ? Connection.MapDataReaderToObject<DTO.Cabeleila.Customer>(reader) : null;
        }

        public static async Task<DTO.Cabeleila.Customer?> GetById(long id)
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
            return await reader.ReadAsync() ? Connection.MapDataReaderToObject<DTO.Cabeleila.Customer>(reader) : null;
        }

        public static async Task<bool> EmailExists(string email)
        {
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                SELECT 1
                FROM {TableName}
                WHERE email = @email;
            ", conn);
            cmd.Parameters.AddWithValue("@email", email);

            var result = await cmd.ExecuteScalarAsync();
            return result != null;
        }

        public static async Task<long> Create(DTO.Cabeleila.Customer obj)
        {
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                INSERT INTO {TableName}
                (
                    full_name,
                    email,
                    phone,
                    password_hash,
                    active,
                    created_at,
                    updated_at
                )
                VALUES
                (
                    @full_name,
                    @email,
                    @phone,
                    @password_hash,
                    TRUE,
                    now(),
                    now()
                )
                RETURNING id;
            ", conn);

            cmd.Parameters.AddWithValue("@full_name", obj.Full_name);
            cmd.Parameters.AddWithValue("@email", obj.Email);
            cmd.Parameters.AddWithValue("@phone", obj.Phone);
            cmd.Parameters.AddWithValue("@password_hash", obj.Password_hash);

            return (long)(await cmd.ExecuteScalarAsync())!;
        }
    }
}
