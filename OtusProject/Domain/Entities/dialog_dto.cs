namespace Domain.Entities;

public class dialog_dto
{
    public dialog_dto(string from, string to, string text, DateTime lastUpdated)
    {
        this.from = from;
        this.to = to;
        last_updated = lastUpdated;
        this.text = text;
    }

    public dialog_dto()
    {
        
    }

    public string from { get; set; }
    public string to { get; set; }
    public string text { get; set; }
    public DateTime last_updated { get; set; }
}