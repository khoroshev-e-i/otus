using System.Security.Cryptography;
using System.Text;
using Api.Exceptions;
using Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Npgsql;

namespace Api;

public class UserService(DatabaseConnectionProvider _connectionProvider, IMemoryCache _memoryCache)
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
            throw new InvalidDataExceptiond();

        var session = Guid.NewGuid().ToString();
        if (Sessions.Active.ContainsKey(request.username))
            return "User authenticated";

        Sessions.Active.Add(user.username, session);

        return new { token = session };
    }

    public async Task<ResponseDto<user>> GetUser(string username)
    {
        var user = (await ExecuteRequest<user>(
                "SELECT * FROM users where username = @username limit 1 ",
                new Dictionary<string, object> { { nameof(username), username } }))
            .SingleOrDefault();

        return new ResponseDto<user>("Успешное получение анкеты пользователя", user);
    }

    public async Task<string> RegisterUser(RegisterUserRequest request)
    {
        if (await Exists($"select true from public.users where {nameof(user.username)} = '{request.username}'"))
            throw new InvalidDataExceptiond();

        request.password = GetPasswordHash(request.password);

        var userId = Guid.NewGuid().ToString();
        await ExecuteInsertCommand("users", new user(
            userId,
            request.username,
            request.first_name,
            request.second_name,
            DateOnly.FromDateTime(DateTime.Parse(request.birthdate)),
            request.biography,
            request.city,
            request.password));

        var session = Guid.NewGuid().ToString();
        Sessions.Active.Add(request.username, session);

        return userId;
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

    public async Task<UserDto[]> SearchUsers(string firstName, string secondName)
    {
        var users = (await ExecuteRequest<user>(
            "SELECT * FROM users where first_name like @firstName and second_name like @secondName ",
            new Dictionary<string, object>
                { { nameof(firstName), firstName + '%' }, { nameof(secondName), secondName + '%' } }));

        return users.Select(UserDto.FromUser).ToArray();
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

                    if (res is DBNull)
                    {
                        continue;
                    }

                    if (res is DateTime)
                    {
                        if (resultType.GetProperty(propertyName).PropertyType == typeof(DateOnly))
                            resultType.GetProperty(propertyName).SetValue(current, DateOnly.FromDateTime((DateTime)res));
                        else
                            resultType.GetProperty(propertyName).SetValue(current, (DateTime)res);
                        
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

    public async Task ExecuteScalar(string query, Dictionary<string, object> args)
    {
        using var connection = _connectionProvider.CreateConnection();
        await connection.OpenAsync();

        try
        {
            var command = new NpgsqlCommand(query, connection);

            foreach (var arg in args)
            {
                command.Parameters.AddWithValue(arg.Key, arg.Value);
            }

            await command.ExecuteScalarAsync();
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

    public async Task ExecuteInsertCommand<TRequestModel>(string tableName, TRequestModel model)
    {
        await using var connection = _connectionProvider.CreateConnection();
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

    public async Task<ResponseDto> AddFriend(string friendId, string userId)
    {
        var id = Guid.NewGuid().ToString();
        var userFriend = new user_friend
        {
            id = id,
            user_id = userId,
            friend_id = friendId
        };

        await ExecuteInsertCommand(nameof(user_friend), userFriend);
        return new ResponseDto("Пользователь успешно указал своего друга");
    }

    public async Task<ResponseDto> AddPost(string bodyText, string userId)
    {
        var id = Guid.NewGuid().ToString();
        var userPost = new user_post()
        {
            id = id,
            user_id = userId,
            post_body = bodyText
        };

        await ExecuteInsertCommand(nameof(user_post), userPost);
        return new ResponseDto("Пользователь добавил пост");
    }

    public async Task<ResponseDto<user_post>> GetPost(string id)
    {
        var post = (await ExecuteRequest<user_post>(
            "SELECT * FROM user_post where id = @id limit 1 ",
            new Dictionary<string, object> { { nameof(id), id } })).SingleOrDefault();

        return new ResponseDto<user_post>("Успешно получен пост", post);
    }

    public async Task<ResponseDto> UpdatePost(string post_id, string bodyText, string userId)
    {
        var now = DateTime.UtcNow;
        await ExecuteScalar(
            "update user_post set post_body = @body, last_updated = @now where id = @id and user_id = @userId",
            new Dictionary<string, object>
                { { "body", bodyText }, { "id", post_id }, { "userId", userId }, { "now", now } });

        return new ResponseDto("Пользователь успешно обновил пост");
    }

    public async Task<ResponseDto> DeletePost(string post_id, string userId)
    {
        await ExecuteScalar($"delete from user_post where id = @id and user_id = @userId",
            new Dictionary<string, object> { { "id", post_id }, { "userId", userId } });

        return new ResponseDto("Пользователь успешно удалил пост");
    }

    public async Task<ResponseDto> DeleteFriend(string userId, string friendId)
    {
        await ExecuteScalar($"delete from user_friend where user_id = @userId and friend_id=@friendId",
            new Dictionary<string, object> { { "userId", userId }, { "friendId", friendId } });

        return new ResponseDto("Пользователь успешно удалил друга");
    }

    public async Task<ResponseDto<List<user_post_dto>?>> Feed(string userId, int limit, int offset)
    {
        var feed = await GetFeed(userId);

        return new ResponseDto<List<user_post_dto>?>("Успешно получена лента из кеша", feed.Skip(offset).Take(limit).ToList());
    }

    private async Task<user_post_dto[]?> GetFeed(string userId)
    {
        return await _memoryCache.GetOrCreateAsync<user_post_dto[]?>(userId, async (cache) =>
        {
            return await ExecuteRequest<user_post_dto>(
                "SELECT u.username, p.post_body, p.last_updated " +
                "FROM user_friend f join user_post p on f.friend_id = p.user_id " +
                "join users u on u.id = f.friend_id where u.id = @userId order by last_updated desc limit 1000",
                new Dictionary<string, object>() { { nameof(userId), userId } });
        });
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