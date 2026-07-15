using Npgsql;
using System.Threading.Tasks;

namespace DAL.Cabeleila
{
    public static class StaffUsers
    {
        private const string Columns = "id, full_name, email, password_hash, role, active, created_at, updated_at";
        private const string TableName = "staff_users";

        public static async Task<DTO.Cabeleila.StaffUser?> GetByEmail(string email)
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
            return await reader.ReadAsync() ? Connection.MapDataReaderToObject<DTO.Cabeleila.StaffUser>(reader) : null;
        }

        public static async Task<DTO.Cabeleila.StaffUser?> GetById(long id)
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
            return await reader.ReadAsync() ? Connection.MapDataReaderToObject<DTO.Cabeleila.StaffUser>(reader) : null;
        }

        public static async Task UpdatePassword(long id, string passwordHash)
        {
            await using var conn = Connection.Get();
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand($@"
                UPDATE {TableName}
                SET password_hash = @password_hash,
                    updated_at = now()
                WHERE id = @id;
            ", conn);
            cmd.Parameters.AddWithValue("@password_hash", passwordHash);
            cmd.Parameters.AddWithValue("@id", id);

            await cmd.ExecuteNonQueryAsync();
        }
    }
}
