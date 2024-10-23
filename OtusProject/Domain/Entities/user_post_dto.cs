namespace Domain.Entities;

public class user_post_dto
{
    public string id { get; set; }
    public string username { get; set; }
    public string post_body { get; set; }
    public DateTime last_updated { get; set; }
}