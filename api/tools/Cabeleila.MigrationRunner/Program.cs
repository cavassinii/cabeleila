using Npgsql;

if (args.Length < 2)
{
    Console.WriteLine("Uso: Cabeleila.MigrationRunner <connection-string> <caminho-do-arquivo.sql>");
    return 1;
}

var connectionString = args[0];
var sqlPath = args[1];

if (!File.Exists(sqlPath))
{
    Console.WriteLine($"Arquivo nao encontrado: {sqlPath}");
    return 1;
}

var sql = await File.ReadAllTextAsync(sqlPath);

await using var conn = new NpgsqlConnection(connectionString);
await conn.OpenAsync();

await using var cmd = new NpgsqlCommand(sql, conn);
await cmd.ExecuteNonQueryAsync();

Console.WriteLine($"Migracao aplicada com sucesso: {sqlPath}");
return 0;
