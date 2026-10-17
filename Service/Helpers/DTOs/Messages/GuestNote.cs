namespace Service.Helpers.DTOs.Messages;

public sealed record GuestNote(string Name, string Email, string Stay, string Message, DateTimeOffset At);

public sealed class MessageRequest
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Stay { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTimeOffset? At { get; set; }
}
