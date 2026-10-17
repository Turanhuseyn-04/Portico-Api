namespace Portico.Domain.Messages;

public class GuestMessage
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Stay { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTimeOffset At { get; set; }
}
