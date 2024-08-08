namespace Domain.Entities;

public class user
{
    public user(string id, string username, string firstName, string secondName, DateOnly birthdate, string biography,
        string city, string password)
    {
        this.id = id;
        first_name = firstName;
        second_name = secondName;
        this.birthdate = birthdate;
        this.biography = biography;
        this.city = city;
        this.password = password;
        this.username = username;
    }

    public string id { get; set; }
    public string username { get; set; }
    public string first_name { get; set; }
    public string? second_name { get; set; }
    public DateOnly birthdate { get; set; }
    public string? biography { get; set; }
    public string? city { get; set; }
    public string password { get; set; }

    public user()
    {
        
    }
}