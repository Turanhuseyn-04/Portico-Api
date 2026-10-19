namespace Service.Helpers.Exceptions;

// The caller has tried too often and must wait, such as after too many wrong passwords.
public class TooManyRequestsException : Exception
{
    public TooManyRequestsException(string message) : base(message)
    {
    }
}
