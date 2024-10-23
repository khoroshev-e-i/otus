namespace Domain.Entities;

public class dialog
{
    public dialog(string id, string fromUser, string toUser, string text, DateTime lastUpdated)
    {
        this.id = id;
        from_user = fromUser;
        to_user = toUser;
        last_updated = lastUpdated;
        this.text = text;
    }

    public string id { get; set; }
    public string from_user { get; set; }
    public string to_user { get; set; }
    public string text { get; set; }
    public DateTime last_updated { get; set; }
}