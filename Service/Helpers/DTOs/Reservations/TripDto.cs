namespace Service.Helpers.DTOs.Reservations;

public sealed record ReservationCreated(string Reference, string Status, decimal Total, string Currency);

public sealed record TripDto(
    string Reference,
    string PropertyId,
    string PropertyName,
    string City,
    string Country,
    string Area,
    string Image,
    string RoomId,
    string RoomName,
    DateOnly CheckIn,
    DateOnly CheckOut,
    bool Refundable,
    decimal Nightly,
    decimal Total,
    int Rooms,
    int Adults,
    int Children,
    string Name,
    string Email,
    bool Cancelled,
    decimal Refund,
    string Currency,
    string Status);

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
