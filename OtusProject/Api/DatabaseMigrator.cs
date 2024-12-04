using System.Text;
using Bogus;
using Domain.Entities;
using Npgsql;

namespace Api;

public class DatabaseMigrator(
    IConfiguration configuration,
    DatabaseConnectionProvider _connectionProvider)
{
    public async Task ApplyMigrations()
    {
        await EnsureDatabaseCreated(_connectionProvider.ConnectionString);
        await using var connection = _connectionProvider.CreateConnection();
        await connection.OpenAsync();
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
        if ((long?)count < 1_000_000)
        {
            using var reader = new StreamReader(csvFilePath);
            await using (var writer =
                         await connection.BeginTextImportAsync(
                             "COPY users FROM STDIN WITH (FORMAT CSV, HEADER TRUE, DELIMITER ',')"))
            {
                string line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    await writer.WriteLineAsync(line);
                }
            }
        }

        await SeedPosts(connection);

        Console.WriteLine("Data imported successfully.");
    }

    private async Task SeedPosts(NpgsqlConnection connection)
    {
        var userIds = await GetUserIds(connection);
        var faker = new Faker();
        var command = new NpgsqlCommand("select count(*) from public.user_post", connection);
        var count = await command.ExecuteScalarAsync();
        if ((long?)count <= 1000)
        {
            for (int i = 0; i < 1111; ++i)
            {
                var post = new user_post
                {
                    id = Guid.NewGuid().ToString(),
                    user_id = faker.PickRandom(userIds),
                    post_body = faker.Lorem.Sentence(25).Substring(0, Math.Min(50, faker.Lorem.Sentence(10).Length))
                };
                var insertPostCommand = new NpgsqlCommand($"insert into user_post (id, user_id, post_body, last_update) " +
                                                      $"values ('{post.id}', '{post.user_id}', '{post.post_body}', now())",
                    connection);
                await insertPostCommand.ExecuteNonQueryAsync();
                await insertPostCommand.ExecuteScalarAsync();
            }
        }

        command = new NpgsqlCommand("select count(*) from public.user_friend", connection);
        count = await command.ExecuteScalarAsync();
        if ((long?)count <= 100)
        {
            IEnumerable<(string userId1, string userId2)> userPairs = from userId1 in userIds
                from userId2 in userIds
                where userId1 != userId2
                select (userId1, userId2);
            foreach (var userPair in userPairs)
            {
                var insertCommand = new NpgsqlCommand($"insert into user_friend (id, user_id, friend_id) " +
                                                      $"values ('{Guid.NewGuid().ToString()}', '{userPair.userId1}', '{userPair.userId2}')",
                    connection);
                await insertCommand.ExecuteScalarAsync();
            }
            Console.WriteLine("Friends generated.");
        }

        command = new NpgsqlCommand("select count(*) from public.dialog", connection);
        count = await command.ExecuteScalarAsync();
        if ((long?)count <= 100)
        {
            IEnumerable<(string userId1, string userId2)> userPairs = from userId1 in userIds
                from userId2 in userIds
                where userId1 != userId2
                select (userId1, userId2);
            foreach (var userPair in userPairs)
            {
                var insertCommand = new NpgsqlCommand($"insert into dialog (id, from_user, to_user, text, last_updated) " +
                                                      $"values ('{Guid.NewGuid().ToString()}', '{userPair.userId1}'," + 
                                                      $"'{userPair.userId2}', '{faker.Lorem.Sentence(10)}', now())",
                    connection);
                await insertCommand.ExecuteScalarAsync();
            }
            Console.WriteLine("Dialog generated.");
        }

        var en = userIds.GetEnumerator();
        string[] staticSessions =
        [
            "278bb767-29ce-4148-9117-dce4ab360cd3",
            "278bb767-29ce-4148-9117-dce4ab360cd2",
            "278bb767-29ce-4148-9117-dce4ab360cd1",
            "278bb767-29ce-4148-9117-dce4ab360cd0",
            "278bb767-29ce-4148-9117-dce4ab360cd4",
            "278bb767-29ce-4148-9117-dce4ab360cd5"
        ];
        var r = staticSessions.Select(x =>
        {
            en.MoveNext();
            Sessions.Active.Add(x, en.Current);
            return x;
        }).ToArray();
        Console.WriteLine("Posts generated.");
    }

    private async Task EnsureDatabaseCreated(string connectionString)
    {
        var databaseName = "postgres";
        var defaultConnectionString = connectionString.Replace($"Database={databaseName}", "Database=postgres");

        await using var connection = new NpgsqlConnection(defaultConnectionString);
        await connection.OpenAsync();
        var command = new NpgsqlCommand($"select 1 From pg_database where datname = '{databaseName}'", connection);
        var result = await command.ExecuteScalarAsync();

        if ((int)(result ?? 0) != 1)
            await new NpgsqlCommand($"Create database {databaseName}", connection)
                .ExecuteNonQueryAsync();
    }

    private async Task<List<string>> GetUserIds(NpgsqlConnection connection)
    {
        // SQL-запрос для получения первых 10 user_id
        var sql = "SELECT id FROM users LIMIT 10";
        var result = new List<string>();

        using (var command = new NpgsqlCommand(sql, connection))
        {
            using (var reader = await command.ExecuteReaderAsync())
            {
                while (reader.Read())
                {
                    string userId = reader.GetString(0);
                    result.Add(userId);
                }
            }
        }

        return result;
    }
}