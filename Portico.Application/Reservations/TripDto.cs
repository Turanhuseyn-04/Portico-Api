namespace Portico.Application.Reservations;

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

public abstract record BookResult;

public sealed record BookMissing : BookResult;

public sealed record BookTaken : BookResult;

public sealed record Booked(ReservationCreated Reservation) : BookResult;
