using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Portico.Domain.Properties;

namespace Portico.Infrastructure.Persistence;

public static class CatalogSeed
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task EnsureAsync(PorticoDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Properties.AnyAsync(cancellationToken)) return;

        db.Properties.AddRange(Read());
        await db.SaveChangesAsync(cancellationToken);
    }

    private static IEnumerable<Property> Read()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Portico.Infrastructure.Persistence.catalog.json")
            ?? throw new InvalidOperationException("The stay catalog is missing from the API.");
        var stays = JsonSerializer.Deserialize<List<CatalogStay>>(stream, Json)
            ?? throw new InvalidOperationException("The stay catalog is empty.");
        return stays.Select(ToProperty);
    }

    private static Property ToProperty(CatalogStay stay)
    {
        return new Property
        {
            Id = stay.Id,
            Name = stay.Name,
            Type = stay.Type,
            Stars = stay.Stars,
            Street = stay.Address.Street,
            Area = stay.Address.Area,
            City = stay.Address.City,
            Postcode = stay.Address.Postcode,
            Country = stay.Address.Country,
            Latitude = stay.Location.Lat,
            Longitude = stay.Location.Lng,
            Score = stay.Score,
            ReviewCount = stay.ReviewCount,
            Summary = stay.Summary,
            DescriptionJson = AsJson(stay.Description),
            HighlightsJson = AsJson(stay.Highlights),
            ScoresJson = AsJson(stay.Scores),
            PopularJson = AsJson(stay.Popular),
            AmenityGroupsJson = AsJson(stay.AmenityGroups),
            GalleryJson = AsJson(stay.Gallery),
            NearbyJson = AsJson(stay.Nearby),
            PoliciesJson = stay.Policies is null ? null : AsJson(stay.Policies),
            Rooms = stay.Rooms.Select(room => new Room
            {
                PropertyId = stay.Id,
                Id = room.Id,
                Name = room.Name,
                Image = room.Image,
                Size = room.Size,
                Beds = room.Beds,
                Sleeps = room.Sleeps,
                Price = room.Price,
                Refundable = room.Refundable,
                Breakfast = room.Breakfast,
                Inventory = room.Inventory,
                FeaturesJson = AsJson(room.Features)
            }).ToList()
        };
    }

    private static string AsJson<T>(T value) => JsonSerializer.Serialize(value, Json);

    private sealed class CatalogStay
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Type { get; set; } = "";
        public int? Stars { get; set; }
        public CatalogAddress Address { get; set; } = new();
        public CatalogLocation Location { get; set; } = new();
        public decimal Score { get; set; }
        public int ReviewCount { get; set; }
        public Dictionary<string, decimal> Scores { get; set; } = new();
        public string Summary { get; set; } = "";
        public List<string> Description { get; set; } = [];
        public List<CatalogHighlight> Highlights { get; set; } = [];
        public List<string> Popular { get; set; } = [];
        public List<CatalogGroup> AmenityGroups { get; set; } = [];
        public List<CatalogPhoto> Gallery { get; set; } = [];
        public List<CatalogNearby> Nearby { get; set; } = [];
        public CatalogPolicies? Policies { get; set; }
        public List<CatalogRoom> Rooms { get; set; } = [];
    }

    private sealed class CatalogAddress
    {
        public string Street { get; set; } = "";
        public string Area { get; set; } = "";
        public string City { get; set; } = "";
        public string Postcode { get; set; } = "";
        public string Country { get; set; } = "";
    }

    private sealed class CatalogLocation
    {
        public double Lat { get; set; }
        public double Lng { get; set; }
    }

    private sealed class CatalogHighlight
    {
        public string Icon { get; set; } = "";
        public string Title { get; set; } = "";
        public string Text { get; set; } = "";
    }

    private sealed class CatalogGroup
    {
        public string Name { get; set; } = "";
        public List<string> Items { get; set; } = [];
    }

    private sealed class CatalogPhoto
    {
        public string Src { get; set; } = "";
        public string Alt { get; set; } = "";
    }

    private sealed class CatalogNearby
    {
        public string Group { get; set; } = "";
        public List<List<string>> Items { get; set; } = [];
    }

    private sealed class CatalogPolicies
    {
        public string CheckIn { get; set; } = "";
        public string CheckOut { get; set; } = "";
        public string Children { get; set; } = "";
        public string Age { get; set; } = "";
        public string Pets { get; set; } = "";
        public string Quiet { get; set; } = "";
        public string Payment { get; set; } = "";
        public int MinNights { get; set; }
    }

    private sealed class CatalogRoom
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Image { get; set; } = "";
        public int Size { get; set; }
        public string Beds { get; set; } = "";
        public int Sleeps { get; set; }
        public decimal Price { get; set; }
        public bool Refundable { get; set; }
        public bool Breakfast { get; set; }
        public int Inventory { get; set; }
        public List<string> Features { get; set; } = [];
    }
}
