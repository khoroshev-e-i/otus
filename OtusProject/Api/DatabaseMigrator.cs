using System.Text;
using Npgsql;

namespace Api;

public class DatabaseMigrator(IConfiguration configuration, DatabaseConnectionProvider _connectionProvider)
{
    public async Task ApplyMigrations()
    {
        await using var connection = _connectionProvider.CreateConnection();
        await connection.OpenAsync();
        
        await EnsureDatabaseCreated(connection);
            
        var migrationFolder = configuration.GetValue<string>("MigrationScriptFolder");
        var sqlFiles = Directory.GetFiles(migrationFolder);

        foreach (var scriptFile in sqlFiles)
        {
            using var streamReader = new StreamReader(scriptFile);

            var sb = new StringBuilder();
            var res = await streamReader.ReadToEndAsync();

            var transaction = await connection.BeginTransactionAsync();
            await new NpgsqlCommand(res, connection).ExecuteNonQueryAsync();
            await transaction.CommitAsync();
        }
        
        
    }

    private async Task EnsureDatabaseCreated(NpgsqlConnection connection)
    {
        var databaseName = "otus_khoroshev";
        var command = new NpgsqlCommand($"select 1 From pg_database where datname = '{databaseName}'", connection);
        var result = await command.ExecuteScalarAsync();

        if ((int)(result ?? 0) != 1) await new NpgsqlCommand($"Create database {databaseName}", connection)
            .ExecuteNonQueryAsync();
    }
}