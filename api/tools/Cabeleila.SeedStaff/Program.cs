using Cabeleila.Security;
using Npgsql;

if (args.Length < 4)
{
    Console.WriteLine("Uso: Cabeleila.SeedStaff <connection-string> <nome-completo> <email> <senha> [role: OWNER|ATTENDANT]");
    Console.WriteLine("Exemplo:");
    Console.WriteLine("  dotnet run -- \"Server=localhost;Port=5432;User Id=postgres;Password=123;Database=cabeleila;\" \"Leila\" leila@cabeleila.com.br MinhaSenh@123 OWNER");
    return 1;
}

var connectionString = args[0];
var fullName = args[1];
var email = args[2].Trim().ToLowerInvariant();
var password = args[3];
var role = args.Length > 4 ? args[4].ToUpperInvariant() : "OWNER";

if (role != "OWNER" && role != "ATTENDANT")
{
    Console.WriteLine("Role invalida. Use OWNER ou ATTENDANT.");
    return 1;
}

var passwordHash = PasswordHasher.Hash(password);

await using var conn = new NpgsqlConnection(connectionString);
await conn.OpenAsync();

// Upsert: roda quantas vezes precisar (ex.: resetar a senha da Leila) sem duplicar cadastro.
await using var cmd = new NpgsqlCommand(@"
    INSERT INTO staff_users (full_name, email, password_hash, role, active, created_at, updated_at)
    VALUES (@full_name, @email, @password_hash, @role, TRUE, now(), now())
    ON CONFLICT (email) DO UPDATE SET
        full_name = EXCLUDED.full_name,
        password_hash = EXCLUDED.password_hash,
        role = EXCLUDED.role,
        active = TRUE,
        updated_at = now();
", conn);

cmd.Parameters.AddWithValue("@full_name", fullName);
cmd.Parameters.AddWithValue("@email", email);
cmd.Parameters.AddWithValue("@password_hash", passwordHash);
cmd.Parameters.AddWithValue("@role", role);

await cmd.ExecuteNonQueryAsync();

Console.WriteLine($"Usuario staff '{email}' ({role}) criado/atualizado com sucesso.");
return 0;
