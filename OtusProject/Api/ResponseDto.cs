namespace Api;

public class ResponseDto<TEntity>(string description, TEntity content) : ResponseDto(description)
{
    public TEntity content { get; set; } = content;
}

public class ResponseDto(string description)
{
    public string description { get; set; } = description;
}