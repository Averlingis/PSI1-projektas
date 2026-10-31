namespace PSI1.Api.Exceptions;

public class InvalidTokenException : Exception
{
    public InvalidTokenException(string message = "Invalid or missing token.")
        : base(message)
    {
    }
}