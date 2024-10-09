namespace Domain.Entities;

public class user_post
{
    public string id { get; set; }
    public string user_id { get; set; }
    public string post_body { get; set; }
    public DateTime last_update { get; set; }
}