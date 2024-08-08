namespace Domain.Entities;

public record UserDto(
    string id,
    string username,
    string first_name,
    string? second_name,
    DateOnly birthdate,
    string? biography,
    string? city)
{
    public static UserDto FromUser(user user) => new UserDto(user.id, user.username, user.first_name, user.second_name,
        user.birthdate, user.biography, user.city);
}