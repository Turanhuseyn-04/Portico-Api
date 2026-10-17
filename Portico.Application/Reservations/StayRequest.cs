namespace Portico.Application.Reservations;

public sealed class StayRequest
{
    public string PropertyId { get; set; } = "";
    public string RoomId { get; set; } = "";
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public int Rooms { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}
