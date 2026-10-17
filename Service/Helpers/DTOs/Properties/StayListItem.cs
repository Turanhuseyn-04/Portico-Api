using System.Text.Json.Serialization;

namespace Service.Helpers.DTOs.Properties;

public sealed record StayListItem(
    string Id,
    string Name,
    string Type,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Stars,
    StayAddress Address,
    StayLocation Location,
    decimal Score,
    int ReviewCount,
    string Summary,
    IReadOnlyList<StayPhoto> Gallery,
    IReadOnlyList<StayRoom> Rooms);

public sealed record StayAddress(string Street, string Area, string City, string Postcode, string Country);

public sealed record StayLocation(double Lat, double Lng);

public sealed record StayPhoto(string Src, string Alt);

public sealed record StayRoom(
    string Id,
    string Name,
    string Image,
    int Size,
    string Beds,
    int Sleeps,
    decimal Price,
    bool Refundable,
    bool Breakfast,
    int Inventory,
    IReadOnlyList<string> Features);
