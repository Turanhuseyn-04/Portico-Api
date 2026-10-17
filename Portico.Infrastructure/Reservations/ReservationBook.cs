using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Portico.Application.Properties;
using Portico.Application.Reservations;
using Portico.Domain.Properties;
using Portico.Domain.Reservations;
using Portico.Infrastructure.Persistence;

namespace Portico.Infrastructure.Reservations;

public sealed class ReservationBook : IReservationBook
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly PorticoDbContext _db;
    private readonly IPropertyCatalog _catalog;

    public ReservationBook(PorticoDbContext db, IPropertyCatalog catalog)
    {
        _db = db;
        _catalog = catalog;
    }

    public async Task<BookResult> CreateAsync(StayRequest request, CancellationToken cancellationToken)
    {
        var rooms = request.Rooms < 1 ? 1 : request.Rooms;
        var adults = request.Adults < 1 ? 1 : request.Adults;
        if (request.CheckOut <= request.CheckIn) return new BookMissing();

        var property = await _db.Properties
            .AsNoTracking()
            .Include(item => item.Rooms)
            .FirstOrDefaultAsync(item => item.Id == request.PropertyId, cancellationToken);
        var room = property?.Rooms.FirstOrDefault(item => item.Id == request.RoomId);
        if (property is null || room is null) return new BookMissing();

        var offers = await _catalog.AvailabilityAsync(property.Id, request.CheckIn, request.CheckOut, cancellationToken);
        var offer = offers?.FirstOrDefault(item => item.RoomId == room.Id);
        if (offer is null) return new BookMissing();
        if (offer.Left < rooms) return new BookTaken();

        var reference = await NextReference(cancellationToken);
        var total = offer.Nightly * offer.Nights * rooms;
        var reservation = new Reservation
        {
            Reference = reference,
            PropertyId = property.Id,
            PropertyName = property.Name,
            City = property.City,
            Country = property.Country,
            Area = property.Area,
            Image = FirstPhoto(property.GalleryJson),
            RoomId = room.Id,
            RoomName = room.Name,
            CheckIn = request.CheckIn,
            CheckOut = request.CheckOut,
            Refundable = room.Refundable,
            Nightly = room.Price,
            Total = total,
            Rooms = rooms,
            Adults = adults,
            Children = request.Children < 0 ? 0 : request.Children,
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Currency = "EUR",
            Status = "Pending"
        };
        _db.Reservations.Add(reservation);
        await _db.SaveChangesAsync(cancellationToken);
        return new Booked(new ReservationCreated(reference, reservation.Status, total, reservation.Currency));
    }

    public async Task<IReadOnlyList<TripDto>> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.Reservations
            .AsNoTracking()
            .OrderBy(reservation => reservation.CheckIn)
            .ThenBy(reservation => reservation.Reference)
            .ToListAsync(cancellationToken);
        return rows.Select(Map).ToArray();
    }

    public async Task<TripDto?> GetAsync(string reference, CancellationToken cancellationToken)
    {
        var row = await _db.Reservations.AsNoTracking().FirstOrDefaultAsync(reservation => reservation.Reference == reference, cancellationToken);
        return row is null ? null : Map(row);
    }

    public async Task<TripDto?> CancelAsync(string reference, CancellationToken cancellationToken)
    {
        var row = await _db.Reservations.FirstOrDefaultAsync(reservation => reservation.Reference == reference, cancellationToken);
        if (row is null) return null;

        row.Cancelled = true;
        row.Status = "Cancelled";
        row.Refund = Refund(row);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(row);
    }

    private async Task<string> NextReference(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var reference = $"PT-{Random.Shared.Next(100000, 1000000)}";
            var taken = await _db.Reservations.AnyAsync(reservation => reservation.Reference == reference, cancellationToken);
            if (!taken) return reference;
        }

        throw new InvalidOperationException("A reservation reference could not be assigned.");
    }

    private static decimal Refund(Reservation reservation)
    {
        if (!reservation.Refundable) return 0;
        var limit = reservation.CheckIn.AddDays(-3);
        return DateOnly.FromDateTime(DateTime.Today) <= limit ? reservation.Total : 0;
    }

    private static TripDto Map(Reservation reservation)
    {
        return new TripDto(
            reservation.Reference,
            reservation.PropertyId,
            reservation.PropertyName,
            reservation.City,
            reservation.Country,
            reservation.Area,
            reservation.Image,
            reservation.RoomId,
            reservation.RoomName,
            reservation.CheckIn,
            reservation.CheckOut,
            reservation.Refundable,
            reservation.Nightly,
            reservation.Total,
            reservation.Rooms,
            reservation.Adults,
            reservation.Children,
            reservation.Name,
            reservation.Email,
            reservation.Cancelled,
            reservation.Refund,
            reservation.Currency,
            reservation.Status);
    }

    private static string FirstPhoto(string galleryJson)
    {
        var photos = JsonSerializer.Deserialize<List<GalleryPhoto>>(galleryJson, Json);
        return photos?.FirstOrDefault()?.Src ?? "";
    }

    private sealed class GalleryPhoto
    {
        public string Src { get; set; } = "";
    }
}
