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

        await InitData(connection, migrationFolder);
    }

    private async Task InitData(NpgsqlConnection connection, string migrationFolder)
    {
        string csvFilePath = $@"{migrationFolder}/Data/users.csv";

        var command = new NpgsqlCommand("select count(*) from public.users", connection);
        var count = await command.ExecuteScalarAsync();
        if ((long?) count >= 1_000_000)
            return;

        using (var reader = new StreamReader(csvFilePath))
        await using (var writer =
                     await connection.BeginTextImportAsync("COPY users FROM STDIN WITH (FORMAT CSV, HEADER TRUE, DELIMITER ',')"))
        {
            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                await writer.WriteLineAsync(line);
            }
        }

        Console.WriteLine("Data imported successfully.");
    }

    private async Task EnsureDatabaseCreated(NpgsqlConnection connection)
    {
        var databaseName = "otus_khoroshev";
        var command = new NpgsqlCommand($"select 1 From pg_database where datname = '{databaseName}'", connection);
        var result = await command.ExecuteScalarAsync();

        if ((int)(result ?? 0) != 1)
            await new NpgsqlCommand($"Create database {databaseName}", connection)
                .ExecuteNonQueryAsync();
    }
}