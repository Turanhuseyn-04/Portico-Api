using System.Text.Json.Serialization;

namespace Service.Helpers.DTOs.Properties;

public sealed record StayDetail(
    string Id,
    string Name,
    string Type,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)] int? Stars,
    StayAddress Address,
    StayLocation Location,
    decimal Score,
    int ReviewCount,
    IReadOnlyDictionary<string, decimal> Scores,
    string Summary,
    IReadOnlyList<string> Description,
    IReadOnlyList<StayHighlight> Highlights,
    IReadOnlyList<string> Popular,
    IReadOnlyList<StayAmenityGroup> AmenityGroups,
    IReadOnlyList<StayPhoto> Gallery,
    IReadOnlyList<StayRoom> Rooms,
    IReadOnlyList<StayNearby> Nearby,
    StayPolicies Policies,
    IReadOnlyList<StayReview> Reviews);

public sealed record StayHighlight(string Icon, string Title, string Text);

public sealed record StayAmenityGroup(string Name, IReadOnlyList<string> Items);

public sealed record StayNearby(string Group, IReadOnlyList<IReadOnlyList<string>> Items);

public sealed record StayPolicies(
    string CheckIn,
    string CheckOut,
    string Children,
    string Age,
    string Pets,
    string Quiet,
    string Payment,
    int MinNights);

public sealed record StayReview(
    string Author,
    string Country,
    string Traveller,
    string? RoomId,
    int Nights,
    DateOnly Date,
    decimal Score,
    string Title,
    string Liked,
    string Disliked);

public sealed record RoomAvailability(
    string RoomId,
    int Left,
    string? SoldOutOn,
    int Nights,
    decimal Nightly,
    decimal Total,
    IReadOnlyDictionary<string, int> PerNight);
