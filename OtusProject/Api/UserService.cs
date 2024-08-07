using System.Security.Cryptography;
using System.Text;
using Api.Exceptions;
using Domain.Entities;
using Npgsql;
using InvalidDataException = Api.Exceptions.InvalidDataException;

namespace Api;

public class UserService(DatabaseConnectionProvider _connectionProvider)
{
    public async Task<object> SignIn(SignInRequest request)
    {
        var user = (await ExecuteRequest<user>(
            "SELECT * FROM users where username = @username limit 1 ",
            new Dictionary<string, object> { { nameof(request.username), request.username } })).SingleOrDefault();
        if (user is null)
            throw new UserNotFoundException();

        var hash = GetPasswordHash(request.password);
        if (hash != user.password)
            throw new InvalidDataException();

        var session = Guid.NewGuid().ToString();
        if (Sessions.Active.ContainsKey(request.username))
            return "User authenticated";

        Sessions.Active.Add(user.username, session);

        return new { token = session };
    }

    public async Task<string> GetUser(string username)
    {
        return UserDto.FromUser((await ExecuteRequest<user>(
                "SELECT * FROM users where username = @username limit 1 ",
                new Dictionary<string, object> { { nameof(username), username } }))
            .SingleOrDefault()) is null ? throw new InvalidDataException() : "Успешное получение анкеты пользователя";
    }

    public async Task<string> RegisterUser(RegisterUserRequest request)
    {
        if (await Exists($"select true from public.users where {nameof(user.username)} = '{request.username}'"))
            throw new InvalidDataException();

        request.password = GetPasswordHash(request.password);

        await ExecuteInsertCommand("users", new user(
            Guid.NewGuid().ToString(),
            request.username,
            request.first_name,
            request.second_name,
            DateOnly.FromDateTime(DateTime.Parse(request.birthdate)),
            request.biography,
            request.city,
            request.password));

        var session = Guid.NewGuid().ToString();
        Sessions.Active.Add(request.username, session);

        return "Успешная регистрация";
    }

    public async Task<bool> Exists(string query)
    {
        await using var connection = _connectionProvider.CreateConnection();
        await connection.OpenAsync();

        try
        {
            var command = new NpgsqlCommand(query, connection);
            var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
                return reader.GetBoolean(0);

            return false;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public async Task<TResult[]?> ExecuteRequest<TResult>(string query, Dictionary<string, object> args)
        where TResult : new()
    {
        using var connection = _connectionProvider.CreateConnection();
        await connection.OpenAsync();
        var result = new List<TResult>();

        try
        {
            var command = new NpgsqlCommand(query, connection);

            foreach (var arg in args)
            {
                command.Parameters.AddWithValue(arg.Key, arg.Value);
            }


            var reader = await command.ExecuteReaderAsync();
            var resultType = typeof(TResult);
            var props = resultType.GetProperties().Select(x => x.Name).Except(["id"]).ToArray();

            while (await reader.ReadAsync())
            {
                var current = new TResult();
                foreach (var propertyName in props)
                {
                    object res = reader[propertyName];

                    if (res is DateTime)
                    {
                        resultType.GetProperty(propertyName).SetValue(current, DateOnly.FromDateTime((DateTime)res));
                        continue;
                    }

                    resultType.GetProperty(propertyName).SetValue(current, res);
                }

                result.Add(current);
            }

            return result.ToArray();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            await connection.CloseAsync();
        }

        return null;
    }

    private async Task ExecuteInsertCommand<TRequestModel>(string tableName, TRequestModel model)
    {
        using var connection = _connectionProvider.CreateConnection();
        await connection.OpenAsync();
        var result = new List<TRequestModel>();

        try
        {
            var requestType = typeof(TRequestModel);
            var props = requestType.GetProperties().Select(x => x.Name).ToArray();
            var valueVariableNames = string.Join(',', props.Select(x => $"@{x}").ToArray());
            var commandText =
                $@"insert into public.{tableName} ({string.Join(",", props)}) values ({valueVariableNames})";
            var command = new NpgsqlCommand(commandText, connection);

            foreach (var prop in props)
            {
                if (prop == "id")
                {
                    command.Parameters.AddWithValue($"{prop}", Guid.NewGuid().ToString());
                }

                command.Parameters.AddWithValue($"{prop}", requestType.GetProperty(prop).GetValue(model));
            }

            await command.ExecuteNonQueryAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            await connection.CloseAsync();
        }
    }

    public static string GetPasswordHash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            return null;

        using var sha1 = SHA1.Create();
        byte[] hashBytes = sha1.ComputeHash(Encoding.UTF8.GetBytes(password));
        long encrypted = 0L;
        for (var i = sizeof(long) - 1; i >= 0; --i)
        {
            encrypted = (encrypted << 8) | hashBytes[i];
        }

        return encrypted.ToString("x16");
    }
}