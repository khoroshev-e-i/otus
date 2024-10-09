namespace Api.Exceptions;

public class UnauthrorizedException : Exception
{
    public UnauthrorizedException() : base("Пользователь не авторизован")
    {
    }
}