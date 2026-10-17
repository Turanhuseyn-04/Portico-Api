using System.Text.Json;
using Portico.Domain.Properties;
using Portico.Domain.Reservations;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Properties;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services;

public class PropertyService : IPropertyService
{
    private static readonly string[] Order =
    [
        "marble", "screens", "atelier", "riad", "orchard", "patio", "greenhouse", "gallery"
    ];

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IPropertyRepository _properties;

    public PropertyService(IPropertyRepository properties)
    {
        _properties = properties;
    }

    public async Task<IReadOnlyList<StayListItem>> GetAllAsync(CancellationToken cancellationToken)
    {
        var rows = await _properties.ListAsync(cancellationToken);
        return rows
            .OrderBy(property => OrderOf(property.Id))
            .ThenBy(property => property.Name, StringComparer.Ordinal)
            .Select(MapList)
            .ToArray();
    }

    public async Task<StayDetail> GetAsync(string id, CancellationToken cancellationToken)
    {
        var property = await _properties.GetWithDetailsAsync(id, cancellationToken);
        if (property is null) throw new NotFoundException("That stay does not exist.");
        return MapDetail(property);
    }

    public async Task<IReadOnlyList<RoomAvailability>> AvailabilityAsync(string id, DateOnly checkIn, DateOnly checkOut, CancellationToken cancellationToken)
    {
        var property = await _properties.GetWithRoomsAsync(id, cancellationToken);
        if (property is null) throw new NotFoundException("That stay does not exist.");

        var nights = Nights(checkIn, checkOut);
        var reservations = await _properties.ActiveOverlappingAsync(id, checkIn, checkOut, cancellationToken);
        return property.Rooms
            .OrderBy(room => room.Price)
            .ThenBy(room => room.Id, StringComparer.Ordinal)
            .Select(room => MapAvailability(room, nights, reservations))
            .ToArray();
    }

    public async Task<IReadOnlyList<StayReview>> ReviewsAsync(string id, CancellationToken cancellationToken)
    {
        if (!await _properties.ExistsAsync(id, cancellationToken)) throw new NotFoundException("That stay does not exist.");
        var reviews = await _properties.ReviewsAsync(id, cancellationToken);
        return reviews.Select(MapReview).ToArray();
    }

    private static StayListItem MapList(Property property)
    {
        return new StayListItem(
            property.Id,
            property.Name,
            property.Type,
            property.Stars,
            new StayAddress(property.Street, property.Area, property.City, property.Postcode, property.Country),
            new StayLocation(property.Latitude ?? 0, property.Longitude ?? 0),
            property.Score,
            property.ReviewCount,
            property.Summary,
            Read<List<StayPhoto>>(property.GalleryJson),
            property.Rooms
                .OrderBy(room => room.Price)
                .ThenBy(room => room.Id, StringComparer.Ordinal)
                .Select(MapRoom)
                .ToArray());
    }

    private static StayRoom MapRoom(Room room)
    {
        return new StayRoom(
            room.Id,
            room.Name,
            room.Image,
            room.Size,
            room.Beds,
            room.Sleeps,
            room.Price,
            room.Refundable,
            room.Breakfast,
            room.Inventory,
            Read<List<string>>(room.FeaturesJson));
    }

    private static StayDetail MapDetail(Property property)
    {
        return new StayDetail(
            property.Id,
            property.Name,
            property.Type,
            property.Stars,
            new StayAddress(property.Street, property.Area, property.City, property.Postcode, property.Country),
            new StayLocation(property.Latitude ?? 0, property.Longitude ?? 0),
            property.Score,
            property.ReviewCount,
            Read<Dictionary<string, decimal>>(property.ScoresJson),
            property.Summary,
            Read<List<string>>(property.DescriptionJson),
            Read<List<StayHighlight>>(property.HighlightsJson),
            Read<List<string>>(property.PopularJson),
            Read<List<StayAmenityGroup>>(property.AmenityGroupsJson),
            Read<List<StayPhoto>>(property.GalleryJson),
            property.Rooms
                .OrderBy(room => room.Price)
                .ThenBy(room => room.Id, StringComparer.Ordinal)
                .Select(MapRoom)
                .ToArray(),
            Read<List<StayNearby>>(property.NearbyJson),
            ReadPolicies(property.PoliciesJson),
            property.Reviews
                .OrderByDescending(review => review.Date)
                .ThenBy(review => review.Author, StringComparer.Ordinal)
                .Select(MapReview)
                .ToArray());
    }

    private static StayReview MapReview(Review review)
    {
        return new StayReview(
            review.Author,
            review.Country,
            review.Traveller,
            review.RoomId,
            review.Nights,
            review.Date,
            review.Score,
            review.Title,
            review.Liked,
            review.Disliked);
    }

    private static RoomAvailability MapAvailability(Room room, IReadOnlyList<DateOnly> nights, IReadOnlyList<Reservation> reservations)
    {
        var perNight = new Dictionary<string, int>();
        var left = nights.Count == 0 ? room.Inventory : int.MaxValue;
        string? soldOutOn = null;
        foreach (var night in nights)
        {
            var free = room.Inventory - Booked(reservations, room.Id, night);
            perNight[night.ToString("yyyy-MM-dd")] = free;
            if (free <= 0 && soldOutOn is null) soldOutOn = night.ToString("yyyy-MM-dd");
            left = Math.Min(left, Math.Max(0, free));
        }

        return new RoomAvailability(
            room.Id,
            left,
            soldOutOn,
            nights.Count,
            room.Price,
            room.Price * nights.Count,
            perNight);
    }

    private static int Booked(IReadOnlyList<Reservation> reservations, string roomId, DateOnly night)
    {
        return reservations
            .Where(reservation => reservation.RoomId == roomId && reservation.CheckIn <= night && night < reservation.CheckOut)
            .Sum(reservation => reservation.Rooms);
    }

    private static List<DateOnly> Nights(DateOnly checkIn, DateOnly checkOut)
    {
        var nights = new List<DateOnly>();
        for (var day = checkIn; day < checkOut; day = day.AddDays(1)) nights.Add(day);
        return nights;
    }

    private static StayPolicies ReadPolicies(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new StayPolicies("", "", "", "", "", "", "", 1);
        return JsonSerializer.Deserialize<StayPolicies>(json, Json) ?? new StayPolicies("", "", "", "", "", "", "", 1);
    }

    private static T Read<T>(string json) where T : new()
    {
        return JsonSerializer.Deserialize<T>(json, Json) ?? new T();
    }

    private static int OrderOf(string id)
    {
        var index = Array.IndexOf(Order, id);
        return index < 0 ? Order.Length : index;
    }
}
