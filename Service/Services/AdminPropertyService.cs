using System.Text.Json;
using Portico.Domain.Properties;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Admin;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services;

public class AdminPropertyService : IAdminPropertyService
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly IAdminPropertyRepository _properties;

    public AdminPropertyService(IAdminPropertyRepository properties)
    {
        _properties = properties;
    }

    public async Task<IReadOnlyList<AdminPropertyDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var rows = await _properties.ListAsync(cancellationToken);
        return rows.Select(Map).ToArray();
    }

    public async Task<AdminPropertyDto> GetAsync(string id, CancellationToken cancellationToken)
    {
        var property = await _properties.FindAsync(id, cancellationToken);
        if (property is null) throw new NotFoundException("That stay does not exist.");
        return Map(property);
    }

    public async Task<AdminPropertyDto> CreateAsync(AdminPropertyRequest request, CancellationToken cancellationToken)
    {
        var id = request.Id.Trim();
        if (string.IsNullOrWhiteSpace(id) || id.Any(char.IsWhiteSpace))
            throw new BadRequestException("A stay id is required.");
        if (await _properties.ExistsAsync(id, cancellationToken))
            throw new ConflictException("That stay already exists.");

        var property = new Property
        {
            Id = id,
            DescriptionJson = "[]",
            HighlightsJson = "[]",
            ScoresJson = "{}",
            PopularJson = "[]",
            AmenityGroupsJson = "[]",
            GalleryJson = "[]",
            NearbyJson = "[]"
        };
        Apply(property, request);
        await _properties.AddAsync(property, cancellationToken);
        await _properties.SaveAsync(cancellationToken);
        return Map(property);
    }

    public async Task<AdminPropertyDto> EditAsync(string id, AdminPropertyRequest request, CancellationToken cancellationToken)
    {
        var property = await _properties.FindAsync(id, cancellationToken);
        if (property is null) throw new NotFoundException("That stay does not exist.");
        await ApplyRoomsAsync(property, request.Rooms, cancellationToken);
        Apply(property, request);
        await _properties.SaveAsync(cancellationToken);
        return Map(property);
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var property = await _properties.FindAsync(id, cancellationToken);
        if (property is null) throw new NotFoundException("That stay does not exist.");
        if (await _properties.HasReservationAsync(id, cancellationToken))
            throw new ConflictException("This stay has a reservation.");

        await _properties.RemoveSavedStaysAsync(id, cancellationToken);
        _properties.Remove(property);
        await _properties.SaveAsync(cancellationToken);
    }

    private async Task ApplyRoomsAsync(Property property, IReadOnlyList<AdminRoomRequest> rooms, CancellationToken cancellationToken)
    {
        var incoming = rooms
            .Select(room => room.Id.Trim())
            .Where(roomId => roomId.Length > 0)
            .ToHashSet(StringComparer.Ordinal);
        var removed = property.Rooms.Where(room => !incoming.Contains(room.Id)).ToList();
        foreach (var room in removed)
        {
            if (await _properties.RoomHasReservationAsync(property.Id, room.Id, cancellationToken))
                throw new ConflictException("This stay has a reservation.");
            property.Rooms.Remove(room);
        }
    }

    private static void Apply(Property property, AdminPropertyRequest request)
    {
        property.Name = request.Name.Trim();
        property.Type = request.Type.Trim();
        property.Stars = request.Stars;
        property.Street = request.Street.Trim();
        property.Area = request.Area.Trim();
        property.City = request.City.Trim();
        property.Postcode = request.Postcode.Trim();
        property.Country = request.Country.Trim();
        property.Latitude = request.Lat;
        property.Longitude = request.Lng;
        property.Summary = request.Summary.Trim();
        property.DescriptionJson = JsonSerializer.Serialize(request.Description ?? new List<string>(), Json);

        foreach (var roomRequest in request.Rooms)
        {
            var roomId = roomRequest.Id.Trim();
            if (roomId.Length == 0) continue;
            var room = property.Rooms.FirstOrDefault(item => item.Id == roomId);
            if (room is null)
            {
                room = new Room { Id = roomId, PropertyId = property.Id, FeaturesJson = "[]" };
                property.Rooms.Add(room);
            }

            room.Name = roomRequest.Name.Trim();
            room.Price = decimal.Round(roomRequest.Price, 2, MidpointRounding.AwayFromZero) + 0.00m;
            room.Sleeps = roomRequest.Sleeps;
            room.Refundable = roomRequest.Refundable;
            room.Breakfast = roomRequest.Breakfast;
            room.Inventory = roomRequest.Inventory;
        }
    }

    private static AdminPropertyDto Map(Property property)
    {
        var description = JsonSerializer.Deserialize<List<string>>(property.DescriptionJson, Json) ?? new List<string>();
        var rooms = property.Rooms
            .OrderBy(room => room.Price)
            .ThenBy(room => room.Id, StringComparer.Ordinal)
            .Select(room => new AdminRoomDto(room.Id, room.Name, room.Price, room.Sleeps, room.Refundable, room.Breakfast, room.Inventory))
            .ToArray();
        return new AdminPropertyDto(
            property.Id,
            property.Name,
            property.Type,
            property.Stars,
            property.Street,
            property.Area,
            property.City,
            property.Postcode,
            property.Country,
            property.Latitude ?? 0,
            property.Longitude ?? 0,
            property.Summary,
            description,
            rooms);
    }
}
