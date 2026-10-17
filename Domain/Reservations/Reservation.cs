namespace Portico.Domain.Reservations;

public class Reservation
{
    public int Id { get; set; }
    public string Reference { get; set; } = "";
    public string PropertyId { get; set; } = "";
    public string PropertyName { get; set; } = "";
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
    public string Area { get; set; } = "";
    public string Image { get; set; } = "";
    public string RoomId { get; set; } = "";
    public string RoomName { get; set; } = "";
    public DateOnly CheckIn { get; set; }
    public DateOnly CheckOut { get; set; }
    public bool Refundable { get; set; }
    public decimal Nightly { get; set; }
    public decimal Total { get; set; }
    public int Rooms { get; set; }
    public int Adults { get; set; }
    public int Children { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public bool Cancelled { get; set; }
    public decimal Refund { get; set; }
    public string Currency { get; set; } = "EUR";
    public string Status { get; set; } = "Pending";
}
