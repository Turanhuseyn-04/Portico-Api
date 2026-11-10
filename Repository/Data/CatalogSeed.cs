using System.Reflection;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Portico.Domain.Content;
using Portico.Domain.Platform;
using Portico.Domain.Properties;
using Portico.Domain.Reservations;

namespace Repository.Data;

public static class CatalogSeed
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task EnsureAsync(PorticoDbContext db, ILogger logger, CancellationToken cancellationToken)
    {
        var stays = ReadStays();
        if (!await db.Properties.AnyAsync(cancellationToken))
        {
            db.Properties.AddRange(stays.Select(ToProperty));
            await db.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Seeded an empty database with {StayCount} catalog stays.", stays.Count);
        }
        else
        {
            if (!await db.Reviews.AnyAsync(cancellationToken))
            {
                db.Reviews.AddRange(stays.SelectMany(stay => stay.Reviews.Select(review => ToReview(stay.Id, review))));
                await db.SaveChangesAsync(cancellationToken);
                logger.LogInformation("Seeded catalog reviews for {StayCount} stays.", stays.Count);
            }

            await EnsureNewStaysAsync(db, stays, logger, cancellationToken);
        }

        await RememberSeededAsync(db, stays, cancellationToken);

        await EnsureSampleReservationAsync(db, cancellationToken);
        await EnsurePhotosAsync(db, cancellationToken);
        await EnsureHomeContentAsync(db, logger, cancellationToken);
    }

    private const string HomeContentSeededKey = "HomeContentSeeded";

    // The home page's trending destinations and video band, which used to be written into Portico.Web's views.
    // Seeded once, and only into an empty table, so slides and videos an admin deleted or added are left alone.
    private static readonly (string City, string Country, string Image)[] HomeSlides =
    [
        ("Rome", "Italy", "/assets/rome.jpg"),
        ("Athens", "Greece", "/assets/athens.jpg"),
        ("Paris", "France", "/assets/paris.jpg"),
        ("Istanbul", "Türkiye", "/assets/istanbul.jpg"),
        ("Venice", "Italy", "/assets/venice.jpg"),
        ("Lisbon", "Portugal", "/assets/lisbon-city.jpg"),
        ("Kyoto", "Japan", "/assets/kyoto-city.jpg"),
        ("Marrakech", "Morocco", "/assets/marrakech-city.jpg")
    ];

    private static readonly string[] HomeVideos =
    [
        "/assets/videos/villas/14041585_1920_1080_25fps.mp4",
        "/assets/videos/resorts/3576398-uhd_3840_2160_25fps.mp4"
    ];

    private static async Task EnsureHomeContentAsync(PorticoDbContext db, ILogger logger, CancellationToken cancellationToken)
    {
        if (await db.Settings.AnyAsync(setting => setting.Key == HomeContentSeededKey, cancellationToken)) return;

        if (!await db.Slides.AnyAsync(cancellationToken))
        {
            var cities = await db.Properties.Select(property => property.City).ToListAsync(cancellationToken);
            db.Slides.AddRange(HomeSlides.Select((slide, index) => new Slide
            {
                City = slide.City,
                Country = slide.Country,
                Image = slide.Image,
                Stays = cities.Count(city => string.Equals(city, slide.City, StringComparison.OrdinalIgnoreCase)),
                Order = index
            }));
            logger.LogInformation("Seeded {SlideCount} home page slides.", HomeSlides.Length);
        }

        if (!await db.Videos.AnyAsync(cancellationToken))
        {
            db.Videos.AddRange(HomeVideos.Select((src, index) => new SiteVideo { Src = src, Order = index }));
            logger.LogInformation("Seeded {VideoCount} home page videos.", HomeVideos.Length);
        }

        db.Settings.Add(new SiteSetting { Key = HomeContentSeededKey, Value = DateTime.UtcNow.ToString("O") });
        await db.SaveChangesAsync(cancellationToken);
    }

    // Stays added to the catalog after a database was first seeded. A stay is seeded once, so one an admin deletes stays deleted.
    private static async Task EnsureNewStaysAsync(PorticoDbContext db, List<CatalogStay> stays, ILogger logger, CancellationToken cancellationToken)
    {
        var seeded = await ReadSeededAsync(db, cancellationToken);
        var known = await db.Properties.Select(property => property.Id).ToListAsync(cancellationToken);
        var missing = stays.Where(stay => !seeded.Contains(stay.Id) && !known.Contains(stay.Id)).ToList();
        if (missing.Count == 0) return;

        db.Properties.AddRange(missing.Select(ToProperty));
        await db.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Added {StayCount} new catalog stays: {StayIds}.", missing.Count, missing.Select(stay => stay.Id).ToArray());
    }

    private static async Task RememberSeededAsync(PorticoDbContext db, List<CatalogStay> stays, CancellationToken cancellationToken)
    {
        var seeded = await ReadSeededAsync(db, cancellationToken);
        seeded.UnionWith(stays.Select(stay => stay.Id));
        var value = string.Join(",", seeded.Order(StringComparer.Ordinal));

        var setting = await db.Settings.FirstOrDefaultAsync(row => row.Key == SeededStaysKey, cancellationToken);
        if (setting is null) db.Settings.Add(new SiteSetting { Key = SeededStaysKey, Value = value });
        else if (setting.Value == value) return;
        else setting.Value = value;
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<HashSet<string>> ReadSeededAsync(PorticoDbContext db, CancellationToken cancellationToken)
    {
        var setting = await db.Settings.AsNoTracking().FirstOrDefaultAsync(row => row.Key == SeededStaysKey, cancellationToken);
        // Databases seeded before this list existed already had the first eight stays.
        var ids = setting is null ? FirstStays : setting.Value.Split(',', StringSplitOptions.RemoveEmptyEntries);
        return new HashSet<string>(ids, StringComparer.Ordinal);
    }

    private const string SeededStaysKey = "CatalogSeededStays";

    private static readonly string[] FirstStays =
    [
        "marble", "screens", "atelier", "riad", "orchard", "patio", "greenhouse", "gallery"
    ];

    private static async Task EnsurePhotosAsync(PorticoDbContext db, CancellationToken cancellationToken)
    {
        var properties = await db.Properties.Include(property => property.Photos).ToListAsync(cancellationToken);
        var added = false;
        foreach (var property in properties)
        {
            if (property.Photos.Count > 0) continue;
            var gallery = JsonSerializer.Deserialize<List<GallerySeed>>(property.GalleryJson, Json) ?? new List<GallerySeed>();
            var order = 0;
            foreach (var item in gallery)
            {
                if (string.IsNullOrWhiteSpace(item.Src)) continue;
                property.Photos.Add(new PropertyPhoto
                {
                    Src = item.Src,
                    Alt = item.Alt ?? "",
                    Order = order
                });
                order++;
                added = true;
            }
        }

        if (added) await db.SaveChangesAsync(cancellationToken);
    }

    private sealed class GallerySeed
    {
        public string Src { get; set; } = "";
        public string? Alt { get; set; }
    }

    private static async Task EnsureSampleReservationAsync(PorticoDbContext db, CancellationToken cancellationToken)
    {
        if (await db.Reservations.AnyAsync(reservation => reservation.Reference == "PT-656069", cancellationToken)) return;

        db.Reservations.Add(new Reservation
        {
            Reference = "PT-656069",
            PropertyId = "marble",
            PropertyName = "The Marble Court",
            City = "Lisbon",
            Country = "Portugal",
            Area = "Chiado",
            Image = "/assets/lisbon.jpg",
            RoomId = "suite",
            RoomName = "Deluxe Courtyard Suite",
            CheckIn = new DateOnly(2026, 11, 12),
            CheckOut = new DateOnly(2026, 11, 15),
            Refundable = true,
            Nightly = 342,
            Total = 1026,
            Rooms = 1,
            Adults = 2,
            Children = 0,
            Cancelled = false,
            Refund = 0,
            Currency = "EUR",
            Status = "Pending"
        });
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
