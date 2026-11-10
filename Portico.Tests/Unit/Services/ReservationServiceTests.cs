using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Portico.Domain.Properties;
using Portico.Domain.Reservations;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Properties;
using Service.Helpers.DTOs.Reservations;
using Service.Helpers.Exceptions;
using Service.Services;
using Service.Services.Interfaces;

namespace Portico.Tests.Unit.Services;

public class ReservationServiceTests
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private readonly Mock<IReservationRepository> _reservations = new();
    private readonly Mock<IPropertyRepository> _properties = new();
    private readonly Mock<IPropertyService> _catalog = new();
    private readonly ReservationService _service;
    private Reservation? _added;

    public ReservationServiceTests()
    {
        // The lock is a database concern; here the work just runs.
        _reservations.Setup(r => r.WithPropertyLockAsync(It.IsAny<string>(), It.IsAny<Func<Task<ReservationCreated>>>(), It.IsAny<CancellationToken>()))
            .Returns((string _, Func<Task<ReservationCreated>> work, CancellationToken _) => work());
        _reservations.Setup(r => r.AddAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()))
            .Callback<Reservation, CancellationToken>((r, _) => _added = r);
        _reservations.Setup(r => r.ReferenceExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        _properties.Setup(p => p.GetWithRoomsAsync("marble", It.IsAny<CancellationToken>())).ReturnsAsync(Property());
        Offer(left: 3, nightly: 120m, nights: 2);

        _service = new ReservationService(_reservations.Object, _properties.Object, _catalog.Object, NullLogger<ReservationService>.Instance);
    }

    private static Property Property() => new()
    {
        Id = "marble",
        Name = "Marble House",
        City = "Lisbon",
        Country = "Portugal",
        Area = "Alfama",
        GalleryJson = """[{"src":"gallery.jpg","alt":""}]""",
        Rooms = { new Room { PropertyId = "marble", Id = "double", Name = "Double", Price = 120m, Refundable = true, Inventory = 3 } }
    };

    private void Offer(int left, decimal nightly, int nights) =>
        _catalog.Setup(c => c.AvailabilityAsync("marble", It.IsAny<DateOnly>(), It.IsAny<DateOnly>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new RoomAvailability("double", left, null, nights, nightly, nightly * nights, new Dictionary<string, int>()) });

    private static StayRequest Valid() => new()
    {
        PropertyId = "marble",
        RoomId = "double",
        CheckIn = Today.AddDays(10),
        CheckOut = Today.AddDays(12),
        Rooms = 1,
        Adults = 2,
        Children = 0,
        Name = "Turan Huseyn",
        Email = "ignored@portico.test"
    };

    // ---------- Create: validation ----------

    public static TheoryData<string, Action<StayRequest>> InvalidRequests => new()
    {
        { "no property", r => r.PropertyId = "" },
        { "no room", r => r.RoomId = " " },
        { "name too short", r => r.Name = "A" },
        { "name too long", r => r.Name = new string('a', ReservationService.MaxNameLength + 1) },
        { "check-out before check-in", r => r.CheckOut = r.CheckIn.AddDays(-1) },
        { "same day", r => r.CheckOut = r.CheckIn },
        { "check-in in the past", r => { r.CheckIn = Today.AddDays(-1); r.CheckOut = Today.AddDays(1); } },
        { "too many nights", r => r.CheckOut = r.CheckIn.AddDays(ReservationService.MaxNights + 1) },
        { "too many rooms", r => r.Rooms = ReservationService.MaxRooms + 1 },
        { "too many adults", r => r.Adults = ReservationService.MaxAdults + 1 },
        { "too many children", r => r.Children = ReservationService.MaxChildren + 1 },
    };

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Create_InvalidRequest_ThrowsBadRequestAndBooksNothing(string _, Action<StayRequest> spoil)
    {
        var request = Valid();
        spoil(request);

        await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateAsync(request, "guest@portico.test", default));
        _reservations.Verify(r => r.AddAsync(It.IsAny<Reservation>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not an email")]
    public async Task Create_InvalidAccountEmail_ThrowsBadRequest(string email)
    {
        await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateAsync(Valid(), email, default));
    }

    [Fact]
    public async Task Create_ThirtyNights_IsAllowed()
    {
        var request = Valid();
        request.CheckOut = request.CheckIn.AddDays(ReservationService.MaxNights);
        await _service.CreateAsync(request, "guest@portico.test", default);
        Assert.NotNull(_added);
    }

    // ---------- Create: booking ----------

    [Fact]
    public async Task Create_ValidRequest_BooksPendingReservationUnderAccountEmail()
    {
        var request = Valid();
        request.Rooms = 2;

        var created = await _service.CreateAsync(request, " guest@portico.test ", default);

        Assert.Matches("^PT-[ABCDEFGHJKLMNPQRSTUVWXYZ23456789]{10}$", created.Reference);
        Assert.Equal("Pending", created.Status);
        Assert.Equal(480m, created.Total); // 120 × 2 nights × 2 rooms
        Assert.Equal("EUR", created.Currency);

        Assert.NotNull(_added);
        Assert.Equal("guest@portico.test", _added!.Email); // the account's, not the request's
        Assert.Equal("Marble House", _added.PropertyName);
        Assert.Equal("Double", _added.RoomName);
        Assert.Equal(120m, _added.Nightly);
        Assert.True(_added.Refundable);
        Assert.Equal("gallery.jpg", _added.Image);
        _reservations.Verify(r => r.WithPropertyLockAsync("marble", It.IsAny<Func<Task<ReservationCreated>>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_NonPositiveCounts_AreRaisedToMinimums()
    {
        var request = Valid();
        request.Rooms = 0;
        request.Adults = 0;
        request.Children = -3;

        await _service.CreateAsync(request, "guest@portico.test", default);

        Assert.Equal(1, _added!.Rooms);
        Assert.Equal(1, _added.Adults);
        Assert.Equal(0, _added.Children);
    }

    [Fact]
    public async Task Create_UsesFirstPhotoByOrderBeforeGallery()
    {
        var property = Property();
        property.Photos.Add(new PropertyPhoto { Id = 1, Src = "second.jpg", Order = 2 });
        property.Photos.Add(new PropertyPhoto { Id = 2, Src = "first.jpg", Order = 1 });
        _properties.Setup(p => p.GetWithRoomsAsync("marble", It.IsAny<CancellationToken>())).ReturnsAsync(property);

        await _service.CreateAsync(Valid(), "guest@portico.test", default);

        Assert.Equal("first.jpg", _added!.Image);
    }

    [Fact]
    public async Task Create_NotEnoughRoomsLeft_ThrowsConflict()
    {
        Offer(left: 1, nightly: 120m, nights: 2);
        var request = Valid();
        request.Rooms = 2;

        await Assert.ThrowsAsync<ConflictException>(() => _service.CreateAsync(request, "guest@portico.test", default));
        Assert.Null(_added);
    }

    [Fact]
    public async Task Create_UnknownProperty_ThrowsNotFound()
    {
        var request = Valid();
        request.PropertyId = "nowhere";
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(request, "guest@portico.test", default));
    }

    [Fact]
    public async Task Create_UnknownRoom_ThrowsNotFound()
    {
        var request = Valid();
        request.RoomId = "penthouse";
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(request, "guest@portico.test", default));
    }

    [Fact]
    public async Task Create_ReferenceCollision_TriesAnotherReference()
    {
        _reservations.SetupSequence(r => r.ReferenceExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true).ReturnsAsync(true).ReturnsAsync(false);

        await _service.CreateAsync(Valid(), "guest@portico.test", default);

        _reservations.Verify(r => r.ReferenceExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(3));
    }

    [Fact]
    public async Task Create_EveryReferenceTaken_GivesUpAfterEightTries()
    {
        _reservations.Setup(r => r.ReferenceExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.CreateAsync(Valid(), "guest@portico.test", default));
        _reservations.Verify(r => r.ReferenceExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(8));
    }

    // ---------- Get / list ----------

    private Reservation Stored(DateOnly checkIn, bool refundable = true, bool cancelled = false, string email = "guest@portico.test")
    {
        var row = new Reservation
        {
            Reference = "PT-ABCDEFGHJK", PropertyId = "marble", RoomId = "double", Email = email,
            CheckIn = checkIn, CheckOut = checkIn.AddDays(2), Total = 240m, Refundable = refundable, Cancelled = cancelled
        };
        _reservations.Setup(r => r.FindByReferenceAsync("PT-ABCDEFGHJK", It.IsAny<bool>(), It.IsAny<CancellationToken>())).ReturnsAsync(row);
        return row;
    }

    [Theory]
    [InlineData("guest@portico.test")]
    [InlineData("GUEST@portico.test ")]
    [InlineData(null)] // staff
    public async Task Get_OwnerOrStaff_ReturnsTrip(string? owner)
    {
        Stored(Today.AddDays(10));
        var trip = await _service.GetAsync(" PT-ABCDEFGHJK ", owner, default);
        Assert.Equal("PT-ABCDEFGHJK", trip.Reference);
    }

    [Fact]
    public async Task Get_SomeoneElsesReservation_LooksMissing()
    {
        Stored(Today.AddDays(10));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync("PT-ABCDEFGHJK", "other@portico.test", default));
    }

    [Fact]
    public async Task Get_UnknownReference_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync("PT-NOPE", null, default));
    }

    [Fact]
    public async Task GetAll_PassesTrimmedEmailFilter()
    {
        _reservations.Setup(r => r.ListAsync("guest@portico.test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { new Reservation { Reference = "PT-1" }, new Reservation { Reference = "PT-2" } });

        var trips = await _service.GetAllAsync(" guest@portico.test ", default);

        Assert.Equal(new[] { "PT-1", "PT-2" }, trips.Select(t => t.Reference));
    }

    // ---------- Cancel ----------

    [Fact]
    public async Task Cancel_RefundableMoreThanThreeDaysAhead_RefundsInFull()
    {
        var row = Stored(Today.AddDays(10));

        var trip = await _service.CancelAsync("PT-ABCDEFGHJK", "guest@portico.test", default);

        Assert.True(trip.Cancelled);
        Assert.Equal("Cancelled", trip.Status);
        Assert.Equal(240m, trip.Refund);
        Assert.True(row.Cancelled);
        _reservations.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_RefundableExactlyThreeDaysAhead_StillRefunds()
    {
        Stored(Today.AddDays(3));
        Assert.Equal(240m, (await _service.CancelAsync("PT-ABCDEFGHJK", null, default)).Refund);
    }

    [Fact]
    public async Task Cancel_RefundableWithinThreeDays_RefundsNothing()
    {
        Stored(Today.AddDays(2));
        Assert.Equal(0m, (await _service.CancelAsync("PT-ABCDEFGHJK", null, default)).Refund);
    }

    [Fact]
    public async Task Cancel_NonRefundable_RefundsNothing()
    {
        Stored(Today.AddDays(30), refundable: false);
        Assert.Equal(0m, (await _service.CancelAsync("PT-ABCDEFGHJK", null, default)).Refund);
    }

    [Fact]
    public async Task Cancel_AlreadyCancelled_ThrowsConflict()
    {
        Stored(Today.AddDays(10), cancelled: true);
        await Assert.ThrowsAsync<ConflictException>(() => _service.CancelAsync("PT-ABCDEFGHJK", null, default));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task Cancel_StayAlreadyStarted_ThrowsConflict(int daysFromToday)
    {
        Stored(Today.AddDays(daysFromToday));
        await Assert.ThrowsAsync<ConflictException>(() => _service.CancelAsync("PT-ABCDEFGHJK", null, default));
    }

    [Fact]
    public async Task Cancel_SomeoneElsesReservation_ThrowsNotFound()
    {
        var row = Stored(Today.AddDays(10));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CancelAsync("PT-ABCDEFGHJK", "other@portico.test", default));
        Assert.False(row.Cancelled);
    }
}
