using System.Net.Mail;
using System.Security.Cryptography;
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

    // The same limits the Web's booking form uses.
    public const int MaxRooms = 4;
    public const int MaxAdults = 8;
    public const int MaxChildren = 6;
    public const int MaxNights = 30;
    public const int MaxNameLength = 120;

    // No 0/O or 1/I, so a reference read aloud or copied by hand stays right. 32^10 references cannot be guessed.
    private const string ReferenceAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    private const int ReferenceLength = 10;

    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    public async Task<ReservationCreated> CreateAsync(StayRequest request, string email, CancellationToken cancellationToken)
    {
        var name = (request.Name ?? "").Trim();
        email = (email ?? "").Trim();
        var rooms = request.Rooms < 1 ? 1 : request.Rooms;
        var adults = request.Adults < 1 ? 1 : request.Adults;
        var children = request.Children < 0 ? 0 : request.Children;
        var nights = request.CheckOut.DayNumber - request.CheckIn.DayNumber;

        if (string.IsNullOrWhiteSpace(request.PropertyId) || string.IsNullOrWhiteSpace(request.RoomId)) throw new BadRequestException("Choose a stay and a room.");
        if (name.Length < 2 || name.Length > MaxNameLength) throw new BadRequestException($"The main guest's name must be 2 to {MaxNameLength} characters.");
        if (!MailAddress.TryCreate(email, out _)) throw new BadRequestException("A valid email is required.");
        if (nights < 1) throw new BadRequestException("Check-out must be after check-in.");
        if (request.CheckIn < Today) throw new BadRequestException("Check-in cannot be in the past.");
        if (nights > MaxNights) throw new BadRequestException($"Stays can be up to {MaxNights} nights.");
        if (rooms > MaxRooms) throw new BadRequestException($"A reservation can hold up to {MaxRooms} rooms.");
        if (adults > MaxAdults) throw new BadRequestException($"A reservation can hold up to {MaxAdults} adults.");
        if (children > MaxChildren) throw new BadRequestException($"A reservation can hold up to {MaxChildren} children.");

        return await _reservations.WithPropertyLockAsync(request.PropertyId, async () =>
        {
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
                Children = children,
                Name = name,
                Email = email,
                Currency = "EUR",
                Status = "Pending"
            };
            await _reservations.AddAsync(reservation, cancellationToken);
            return new ReservationCreated(reference, reservation.Status, total, reservation.Currency);
        }, cancellationToken);
    }

    public async Task<IReadOnlyList<TripDto>> GetAllAsync(string? email, CancellationToken cancellationToken)
    {
        var rows = await _reservations.ListAsync(email?.Trim(), cancellationToken);
        return rows.Select(Map).ToArray();
    }

    public async Task<TripDto> GetAsync(string reference, string? ownerEmail, CancellationToken cancellationToken)
    {
        return Map(await FindAsync(reference, ownerEmail, tracking: false, cancellationToken));
    }

    public async Task<TripDto> CancelAsync(string reference, string? ownerEmail, CancellationToken cancellationToken)
    {
        var row = await FindAsync(reference, ownerEmail, tracking: true, cancellationToken);
        if (row.Cancelled) throw new ConflictException("This reservation is already cancelled.");
        if (row.CheckIn <= Today) throw new ConflictException("This stay has already started, so it can no longer be cancelled.");

        row.Cancelled = true;
        row.Status = "Cancelled";
        row.Refund = Refund(row);
        await _reservations.SaveAsync(cancellationToken);
        return Map(row);
    }

    // Someone else's reservation answers as missing, so a reference cannot be used to find out whether it exists.
    private async Task<Reservation> FindAsync(string reference, string? ownerEmail, bool tracking, CancellationToken cancellationToken)
    {
        var row = await _reservations.FindByReferenceAsync(reference.Trim(), tracking, cancellationToken);
        if (row is null || (ownerEmail is not null && !string.Equals(row.Email.Trim(), ownerEmail.Trim(), StringComparison.OrdinalIgnoreCase)))
            throw new NotFoundException("That reservation does not exist.");
        return row;
    }

    private async Task<string> NextReference(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            var reference = "PT-" + RandomNumberGenerator.GetString(ReferenceAlphabet, ReferenceLength);
            if (!await _reservations.ReferenceExistsAsync(reference, cancellationToken)) return reference;
        }

        throw new InvalidOperationException("A reservation reference could not be assigned.");
    }

    private static decimal Refund(Reservation reservation)
    {
        if (!reservation.Refundable) return 0;
        var limit = reservation.CheckIn.AddDays(-3);
        return Today <= limit ? reservation.Total : 0;
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
