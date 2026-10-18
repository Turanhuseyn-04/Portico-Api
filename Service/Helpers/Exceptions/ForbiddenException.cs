namespace Service.Helpers.Exceptions;

// The caller is known but may not do this, such as an admin changing their own role.
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
