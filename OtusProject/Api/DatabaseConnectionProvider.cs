using Npgsql;

namespace Api;

public class DatabaseConnectionProvider(IConfiguration _configuration)
{
    public NpgsqlConnection CreateConnection()
    {
        var connectionString = _configuration.GetConnectionString("Postgres");
        return new NpgsqlConnection(connectionString);
    }
}