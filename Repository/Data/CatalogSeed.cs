using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Portico.Domain.Properties;

namespace Repository.Data;

public static class CatalogSeed
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task EnsureAsync(PorticoDbContext db, CancellationToken cancellationToken)
    {
        var stays = ReadStays();
        if (!await db.Properties.AnyAsync(cancellationToken))
        {
            db.Properties.AddRange(stays.Select(ToProperty));
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        if (await db.Reviews.AnyAsync(cancellationToken)) return;

        db.Reviews.AddRange(stays.SelectMany(stay => stay.Reviews.Select(review => ToReview(stay.Id, review))));
        await db.SaveChangesAsync(cancellationToken);
    }

    private static List<CatalogStay> ReadStays()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Repository.Data.catalog.json")
            ?? throw new InvalidOperationException("The stay catalog is missing from the API.");
        return JsonSerializer.Deserialize<List<CatalogStay>>(stream, Json)
            ?? throw new InvalidOperationException("The stay catalog is empty.");
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
            }).ToList(),
            Reviews = stay.Reviews.Select(review => ToReview(stay.Id, review)).ToList()
        };
    }

    private static Review ToReview(string propertyId, CatalogReview review)
    {
        return new Review
        {
            PropertyId = propertyId,
            RoomId = review.RoomId,
            Author = review.Author,
            Country = review.Country,
            Traveller = review.Traveller,
            Nights = review.Nights,
            Date = review.Date,
            Score = review.Score,
            Title = review.Title,
            Liked = review.Liked,
            Disliked = review.Disliked
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
        public List<CatalogReview> Reviews { get; set; } = [];
    }

    private sealed class CatalogReview
    {
        public string Author { get; set; } = "";
        public string Country { get; set; } = "";
        public string Traveller { get; set; } = "";
        public string? RoomId { get; set; }
        public int Nights { get; set; }
        public DateOnly Date { get; set; }
        public decimal Score { get; set; }
        public string Title { get; set; } = "";
        public string Liked { get; set; } = "";
        public string Disliked { get; set; } = "";
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
