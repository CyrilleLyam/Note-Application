using System.Reflection;
using Dapper;
using Microsoft.Data.SqlClient;
using Serilog;

namespace server.src.Data;

public static class DatabaseMigrator
{
    private const string ResourcePrefix = "server.src.Data.Migrations.";

    public static async Task Migrate(string connectionString)
    {
        await EnsureDatabase(connectionString);

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();

        await connection.ExecuteAsync("""
            IF OBJECT_ID(N'schema_migrations', N'U') IS NULL
            CREATE TABLE schema_migrations (
                id NVARCHAR(150) NOT NULL CONSTRAINT pk_schema_migrations PRIMARY KEY,
                applied_at DATETIME2 NOT NULL CONSTRAINT df_schema_migrations_applied_at DEFAULT SYSUTCDATETIME()
            );
            """);

        var applied = (await connection.QueryAsync<string>("SELECT id FROM schema_migrations")).ToHashSet();

        foreach (var (id, script) in LoadMigrations())
        {
            if (applied.Contains(id))
            {
                continue;
            }

            await using var transaction = await connection.BeginTransactionAsync();
            await connection.ExecuteAsync(script, transaction: transaction);
            await connection.ExecuteAsync(
                "INSERT INTO schema_migrations (id) VALUES (@Id)",
                new { Id = id },
                transaction);
            await transaction.CommitAsync();

            Log.Information("Applied migration {Migration}", id);
        }
    }

    private static async Task EnsureDatabase(string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var databaseName = builder.InitialCatalog;
        builder.InitialCatalog = "master";

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.ExecuteAsync("""
            IF DB_ID(@Name) IS NULL
            BEGIN
                DECLARE @sql NVARCHAR(MAX) = N'CREATE DATABASE ' + QUOTENAME(@Name);
                EXEC (@sql);
            END
            """, new { Name = databaseName });
    }

    private static IEnumerable<(string Id, string Script)> LoadMigrations()
    {
        var assembly = Assembly.GetExecutingAssembly();

        return assembly.GetManifestResourceNames()
            .Where(name => name.StartsWith(ResourcePrefix) && name.EndsWith(".sql"))
            .OrderBy(name => name, StringComparer.Ordinal)
            .Select(name =>
            {
                using var stream = assembly.GetManifestResourceStream(name)!;
                using var reader = new StreamReader(stream);
                var id = Path.GetFileNameWithoutExtension(name[ResourcePrefix.Length..]);
                return (id, reader.ReadToEnd());
            })
            .ToList();
    }
}
