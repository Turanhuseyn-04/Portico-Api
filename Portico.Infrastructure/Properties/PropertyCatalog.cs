using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Portico.Application.Properties;
using Portico.Domain.Properties;
using Portico.Infrastructure.Persistence;

namespace Portico.Infrastructure.Properties;

public sealed class PropertyCatalog : IPropertyCatalog
{
    private static readonly string[] Order =
    [
        "marble", "screens", "atelier", "riad", "orchard", "patio", "greenhouse", "gallery"
    ];

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private readonly PorticoDbContext _db;

    public PropertyCatalog(PorticoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<StayListItem>> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.Properties
            .AsNoTracking()
            .Include(property => property.Rooms)
            .ToListAsync(cancellationToken);

        return rows
            .OrderBy(property => OrderOf(property.Id))
            .ThenBy(property => property.Name, StringComparer.Ordinal)
            .Select(Map)
            .ToArray();
    }

    private static StayListItem Map(Property property)
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
