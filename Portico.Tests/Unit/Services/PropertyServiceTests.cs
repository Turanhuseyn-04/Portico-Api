using Moq;
using Portico.Domain.Properties;
using Portico.Domain.Reservations;
using Repository.Repositories.Interfaces;
using Service.Helpers.Exceptions;
using Service.Services;

namespace Portico.Tests.Unit.Services;

public class PropertyServiceTests
{
    private static readonly DateOnly Night1 = new(2027, 3, 1);

    private readonly Mock<IPropertyRepository> _repo = new();
    private readonly PropertyService _service;

    public PropertyServiceTests()
    {
        _service = new PropertyService(_repo.Object);
    }

    private static Property Stay(string id, string name = "Stay") => new() { Id = id, Name = name };

    private static Room Room(string id, decimal price, int inventory) =>
        new() { Id = id, PropertyId = "marble", Name = id, Price = price, Inventory = inventory, FeaturesJson = """["Wi-Fi"]""" };

    private void Reservations(params Reservation[] rows) =>
        _repo.Setup(r => r.ActiveOverlappingAsync("marble", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>())).ReturnsAsync(rows);

    // ---------- GetAll ----------

    [Fact]
    public async Task GetAll_OrdersCuratedStaysFirstThenOthersByName()
    {
        _repo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[]
        {
            Stay("zzz-new", "Beta"), Stay("riad"), Stay("aaa-new", "Alpha"), Stay("marble")
        });

        var stays = await _service.GetAllAsync(default);

        Assert.Equal(new[] { "marble", "riad", "aaa-new", "zzz-new" }, stays.Select(s => s.Id));
    }

    [Fact]
    public async Task GetAll_SortsRoomsByPriceAndMapsMissingCoordinatesToZero()
    {
        var stay = Stay("marble");
        stay.Rooms.Add(Room("suite", 300m, 1));
        stay.Rooms.Add(Room("double", 120m, 2));
        _repo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[] { stay });

        var item = Assert.Single(await _service.GetAllAsync(default));

        Assert.Equal(new[] { "double", "suite" }, item.Rooms.Select(r => r.Id));
        Assert.Equal(new[] { "Wi-Fi" }, item.Rooms[0].Features);
        Assert.Equal(0, item.Location.Lat);
        Assert.Equal(0, item.Location.Lng);
    }

    // ---------- GetAsync ----------

    [Fact]
    public async Task Get_UnknownStay_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync("nowhere", default));
    }

    [Fact]
    public async Task Get_MapsJsonColumnsAndDefaultsMissingPolicies()
    {
        var stay = Stay("marble");
        stay.DescriptionJson = """["First","Second"]""";
        stay.ScoresJson = """{"staff":9.4}""";
        stay.GalleryJson = """[{"src":"a.jpg","alt":"A"}]""";
        stay.PoliciesJson = null;
        stay.Reviews.Add(new Review { Author = "Old", Date = new DateOnly(2025, 1, 1) });
        stay.Reviews.Add(new Review { Author = "New", Date = new DateOnly(2026, 1, 1) });
        _repo.Setup(r => r.GetWithDetailsAsync("marble", It.IsAny<CancellationToken>())).ReturnsAsync(stay);

        var detail = await _service.GetAsync("marble", default);

        Assert.Equal(new[] { "First", "Second" }, detail.Description);
        Assert.Equal(9.4m, detail.Scores["staff"]);
        Assert.Equal("a.jpg", Assert.Single(detail.Gallery).Src);
        Assert.Equal(1, detail.Policies.MinNights);
        Assert.Equal(new[] { "New", "Old" }, detail.Reviews.Select(r => r.Author));
    }

    [Fact]
    public async Task Get_PhotosTableWinsOverGalleryJson()
    {
        var stay = Stay("marble");
        stay.GalleryJson = """[{"src":"legacy.jpg","alt":""}]""";
        stay.Photos.Add(new PropertyPhoto { Id = 5, Src = "b.jpg", Order = 2 });
        stay.Photos.Add(new PropertyPhoto { Id = 9, Src = "a.jpg", Order = 1 });
        _repo.Setup(r => r.GetWithDetailsAsync("marble", It.IsAny<CancellationToken>())).ReturnsAsync(stay);

        var detail = await _service.GetAsync("marble", default);

        Assert.Equal(new[] { "a.jpg", "b.jpg" }, detail.Gallery.Select(p => p.Src));
    }

    // ---------- Availability ----------

    [Fact]
    public async Task Availability_UnknownStay_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.AvailabilityAsync("nowhere", Night1, Night1.AddDays(1), default));
    }

    [Fact]
    public async Task Availability_CountsBookedRoomsPerNightAndTakesTheMinimum()
    {
        var stay = Stay("marble");
        stay.Rooms.Add(Room("double", 100m, 3));
        _repo.Setup(r => r.GetWithRoomsAsync("marble", It.IsAny<CancellationToken>())).ReturnsAsync(stay);
        Reservations(
            new Reservation { RoomId = "double", CheckIn = Night1, CheckOut = Night1.AddDays(2), Rooms = 1 },
            new Reservation { RoomId = "double", CheckIn = Night1.AddDays(1), CheckOut = Night1.AddDays(3), Rooms = 1 },
            new Reservation { RoomId = "suite", CheckIn = Night1, CheckOut = Night1.AddDays(3), Rooms = 4 }); // other room

        var offer = Assert.Single(await _service.AvailabilityAsync("marble", Night1, Night1.AddDays(3), default));

        Assert.Equal(3, offer.Nights);
        Assert.Equal(new Dictionary<string, int> { ["2027-03-01"] = 2, ["2027-03-02"] = 1, ["2027-03-03"] = 2 }, offer.PerNight);
        Assert.Equal(1, offer.Left);
        Assert.Null(offer.SoldOutOn);
        Assert.Equal(100m, offer.Nightly);
        Assert.Equal(300m, offer.Total);
    }

    [Fact]
    public async Task Availability_CheckOutDayIsNotBooked()
    {
        var stay = Stay("marble");
        stay.Rooms.Add(Room("double", 100m, 1));
        _repo.Setup(r => r.GetWithRoomsAsync("marble", It.IsAny<CancellationToken>())).ReturnsAsync(stay);
        Reservations(new Reservation { RoomId = "double", CheckIn = Night1.AddDays(-2), CheckOut = Night1, Rooms = 1 });

        var offer = Assert.Single(await _service.AvailabilityAsync("marble", Night1, Night1.AddDays(1), default));

        Assert.Equal(1, offer.Left);
    }

    [Fact]
    public async Task Availability_OverbookedNight_ReportsSoldOutAndNeverNegative()
    {
        var stay = Stay("marble");
        stay.Rooms.Add(Room("double", 100m, 1));
        _repo.Setup(r => r.GetWithRoomsAsync("marble", It.IsAny<CancellationToken>())).ReturnsAsync(stay);
        Reservations(new Reservation { RoomId = "double", CheckIn = Night1.AddDays(1), CheckOut = Night1.AddDays(2), Rooms = 2 });

        var offer = Assert.Single(await _service.AvailabilityAsync("marble", Night1, Night1.AddDays(2), default));

        Assert.Equal(0, offer.Left);
        Assert.Equal("2027-03-02", offer.SoldOutOn);
        Assert.Equal(-1, offer.PerNight["2027-03-02"]);
    }

    [Fact]
    public async Task Availability_NoNights_ReturnsFullInventoryAndZeroTotal()
    {
        var stay = Stay("marble");
        stay.Rooms.Add(Room("double", 100m, 4));
        _repo.Setup(r => r.GetWithRoomsAsync("marble", It.IsAny<CancellationToken>())).ReturnsAsync(stay);
        Reservations();

        var offer = Assert.Single(await _service.AvailabilityAsync("marble", Night1, Night1, default));

        Assert.Equal(4, offer.Left);
        Assert.Equal(0, offer.Nights);
        Assert.Equal(0m, offer.Total);
    }

    // ---------- Reviews ----------

    [Fact]
    public async Task Reviews_UnknownStay_ThrowsNotFound()
    {
        _repo.Setup(r => r.ExistsAsync("nowhere", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.ReviewsAsync("nowhere", default));
    }

    [Fact]
    public async Task Reviews_KnownStay_MapsReviews()
    {
        _repo.Setup(r => r.ExistsAsync("marble", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _repo.Setup(r => r.ReviewsAsync("marble", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Review { Author = "Ana", Score = 9.5m, Title = "Lovely" } });

        var review = Assert.Single(await _service.ReviewsAsync("marble", default));

        Assert.Equal(("Ana", 9.5m, "Lovely"), (review.Author, review.Score, review.Title));
    }
}
