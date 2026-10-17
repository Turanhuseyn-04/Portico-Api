namespace Service.Helpers.Exceptions;

public class BadRequestException : Exception
{
    public string[] Errors { get; }

    public BadRequestException(string message) : base(message)
    {
        Errors = [message];
    }
}
