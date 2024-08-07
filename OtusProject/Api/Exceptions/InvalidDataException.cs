namespace Api.Exceptions;

public class InvalidDataException : Exception
{
    public InvalidDataException() : base("Невалидные данные")
    {
    }
}