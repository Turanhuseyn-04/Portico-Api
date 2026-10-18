using System.Text.Json;
using Portico.Domain.Properties;
using Portico.Domain.Reservations;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Reservations;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services;

public class ReservationService : IReservationService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IReservationRepository _reservations;
    private readonly IPropertyRepository _properties;
    private readonly IPropertyService _catalog;

    public ReservationService(IReservationRepository reservations, IPropertyRepository properties, IPropertyService catalog)
    {
        _reservations = reservations;
        _properties = properties;
        _catalog = catalog;
    }

    public async Task<ReservationCreated> CreateAsync(StayRequest request, CancellationToken cancellationToken)
    {
        var rooms = request.Rooms < 1 ? 1 : request.Rooms;
        var adults = request.Adults < 1 ? 1 : request.Adults;
        if (request.CheckOut <= request.CheckIn) throw new NotFoundException("That stay does not exist.");

        var property = await _properties.GetWithRoomsAsync(request.PropertyId, cancellationToken);
        var room = property?.Rooms.FirstOrDefault(item => item.Id == request.RoomId);
        if (property is null || room is null) throw new NotFoundException("That stay does not exist.");

        var offers = await _catalog.AvailabilityAsync(property.Id, request.CheckIn, request.CheckOut, cancellationToken);
        var offer = offers.FirstOrDefault(item => item.RoomId == room.Id);
        if (offer is null) throw new NotFoundException("That stay does not exist.");
        if (offer.Left < rooms) throw new ConflictException("That room was just taken for these dates.");

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
            Image = FirstPhoto(property),
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
        await _reservations.AddAsync(reservation, cancellationToken);
        return new ReservationCreated(reference, reservation.Status, total, reservation.Currency);
    }

    public async Task<IReadOnlyList<TripDto>> GetAllAsync(string? email, CancellationToken cancellationToken)
    {
        var rows = await _reservations.ListAsync(email?.Trim(), cancellationToken);
        return rows.Select(Map).ToArray();
    }

    public async Task<TripDto> GetAsync(string reference, CancellationToken cancellationToken)
    {
        var row = await _reservations.FindByReferenceAsync(reference, tracking: false, cancellationToken);
        if (row is null) throw new NotFoundException("That reservation does not exist.");
        return Map(row);
    }

    public async Task<TripDto> CancelAsync(string reference, CancellationToken cancellationToken)
    {
        var row = await _reservations.FindByReferenceAsync(reference, tracking: true, cancellationToken);
        if (row is null) throw new NotFoundException("That reservation does not exist.");

        row.Cancelled = true;
        row.Status = "Cancelled";
        row.Refund = Refund(row);
        await _reservations.SaveAsync(cancellationToken);
        return Map(row);
    }

    private async Task<string> NextReference(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var reference = $"PT-{Random.Shared.Next(100000, 1000000)}";
            if (!await _reservations.ReferenceExistsAsync(reference, cancellationToken)) return reference;
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

    private static string FirstPhoto(Property property)
    {
        var photo = property.Photos.OrderBy(item => item.Order).ThenBy(item => item.Id).FirstOrDefault();
        if (photo is not null) return photo.Src;
        var photos = JsonSerializer.Deserialize<List<GalleryPhoto>>(property.GalleryJson, Json);
        return photos?.FirstOrDefault()?.Src ?? "";
    }

    private sealed class GalleryPhoto
    {
        public string Src { get; set; } = "";
    }
}
