namespace PSI1.Api.Exceptions;

public class UserNotFoundException : Exception
{
    public UserNotFoundException(string message = "User not found.")
    : base(message)
    {
    }
}