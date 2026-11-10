using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Portico.Domain.Accounts;
using Portico.Domain.Content;
using Portico.Domain.Messages;
using Portico.Domain.Properties;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Admin;
using Service.Helpers.DTOs.Messages;
using Service.Helpers.Exceptions;
using Service.Services;

namespace Portico.Tests.Unit.Services;

public class AccountAdminServiceTests
{
    private readonly Mock<IAccountRepository> _accounts = new();
    private readonly AccountAdminService _service;

    public AccountAdminServiceTests()
    {
        _service = new AccountAdminService(_accounts.Object, NullLogger<AccountAdminService>.Instance);
    }

    private Account Target(string role)
    {
        var account = new Account { Name = "Target", Email = "target@portico.test", Role = role, PasswordHash = "x", EmailConfirmed = true };
        _accounts.Setup(r => r.FindByEmailAsync("target@portico.test", false, It.IsAny<CancellationToken>())).ReturnsAsync(account);
        return account;
    }

    [Theory]
    [InlineData("Owner")]
    [InlineData("")]
    public async Task SetRole_UnknownRole_ThrowsBadRequest(string role)
    {
        await Assert.ThrowsAsync<BadRequestException>(() => _service.SetRoleAsync("boss@portico.test", "target@portico.test", role, default));
    }

    [Fact]
    public async Task SetRole_OnOwnAccount_ThrowsForbidden()
    {
        await Assert.ThrowsAsync<ForbiddenException>(() => _service.SetRoleAsync("Boss@Portico.test", " boss@portico.test ", "Guest", default));
    }

    [Fact]
    public async Task SetRole_UnknownAccount_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.SetRoleAsync("boss@portico.test", "nobody@portico.test", "Admin", default));
    }

    [Fact]
    public async Task SetRole_GuestToAdmin_SavesAndReportsPreviousRole()
    {
        var account = Target(AccountRoles.Guest);

        var changed = await _service.SetRoleAsync("boss@portico.test", "target@portico.test", " admin ", default);

        Assert.Equal(AccountRoles.Admin, account.Role); // normalised casing
        Assert.Equal(AccountRoles.Guest, changed.PreviousRole);
        Assert.Equal(new AccountRowDto("Target", "target@portico.test", "Admin", true, true), changed.Account);
        _accounts.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SetRole_SameRole_DoesNotSave()
    {
        Target(AccountRoles.Admin);
        await _service.SetRoleAsync("boss@portico.test", "target@portico.test", "Admin", default);
        _accounts.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SetRole_DemotingTheLastSuperAdmin_ThrowsConflict()
    {
        var account = Target(AccountRoles.SuperAdmin);
        _accounts.Setup(r => r.CountInRoleAsync(AccountRoles.SuperAdmin, It.IsAny<CancellationToken>())).ReturnsAsync(1);

        await Assert.ThrowsAsync<ConflictException>(() => _service.SetRoleAsync("boss@portico.test", "target@portico.test", "Admin", default));
        Assert.Equal(AccountRoles.SuperAdmin, account.Role);
    }

    [Fact]
    public async Task SetRole_DemotingOneOfSeveralSuperAdmins_Succeeds()
    {
        var account = Target(AccountRoles.SuperAdmin);
        _accounts.Setup(r => r.CountInRoleAsync(AccountRoles.SuperAdmin, It.IsAny<CancellationToken>())).ReturnsAsync(2);

        await _service.SetRoleAsync("boss@portico.test", "target@portico.test", "Guest", default);

        Assert.Equal(AccountRoles.Guest, account.Role);
    }

    [Fact]
    public async Task GetAll_ReportsPasswordAndConfirmationState()
    {
        _accounts.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[]
        {
            new Account { Name = "Legacy", Email = "legacy@portico.test", PasswordHash = null, EmailConfirmed = false }
        });

        var row = Assert.Single(await _service.GetAllAsync(default));

        Assert.False(row.HasPassword);
        Assert.False(row.EmailConfirmed);
    }
}

public class AdminPropertyServiceTests
{
    private readonly Mock<IAdminPropertyRepository> _repo = new();
    private readonly AdminPropertyService _service;

    public AdminPropertyServiceTests()
    {
        _service = new AdminPropertyService(_repo.Object);
    }

    private static AdminPropertyRequest Request(string id = "new-stay", params AdminRoomRequest[] rooms) => new()
    {
        Id = id, Name = " New Stay ", Type = " Hotel ", Stars = 4, Street = " 1 Main St ", Area = "Centre", City = " Baku ",
        Postcode = "AZ1000", Country = "Azerbaijan", Lat = 40.4, Lng = 49.8, Summary = " Nice ", Description = new() { "One", "Two" },
        Rooms = rooms.ToList()
    };

    private static AdminRoomRequest RoomRequest(string id, decimal price) =>
        new() { Id = id, Name = $" {id} ", Price = price, Sleeps = 2, Inventory = 3, Refundable = true };

    private Property Existing(params string[] roomIds)
    {
        var property = new Property { Id = "marble", DescriptionJson = "[]" };
        foreach (var id in roomIds) property.Rooms.Add(new Room { Id = id, PropertyId = "marble", Price = 100m });
        _repo.Setup(r => r.FindAsync("marble", It.IsAny<CancellationToken>())).ReturnsAsync(property);
        return property;
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("two words")]
    public async Task Create_MissingOrSpacedId_ThrowsBadRequest(string id)
    {
        await Assert.ThrowsAsync<BadRequestException>(() => _service.CreateAsync(Request(id), default));
    }

    [Fact]
    public async Task Create_ExistingId_ThrowsConflict()
    {
        _repo.Setup(r => r.ExistsAsync("marble", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        await Assert.ThrowsAsync<ConflictException>(() => _service.CreateAsync(Request("marble"), default));
    }

    [Fact]
    public async Task Create_Valid_TrimsFieldsRoundsPricesAndSortsRooms()
    {
        Property? added = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<Property>(), It.IsAny<CancellationToken>()))
            .Callback<Property, CancellationToken>((p, _) => added = p);

        var dto = await _service.CreateAsync(Request(" new-stay ", RoomRequest("suite", 250.555m), RoomRequest("double", 99.994m), RoomRequest("  ", 1m)), default);

        Assert.Equal("new-stay", dto.Id);
        Assert.Equal(("New Stay", "Hotel", "Baku", "Nice"), (dto.Name, dto.Type, dto.City, dto.Summary));
        Assert.Equal(new[] { "One", "Two" }, dto.Description);
        Assert.Equal(new[] { "double", "suite" }, dto.Rooms.Select(r => r.Id)); // blank room id skipped, sorted by price
        Assert.Equal(99.99m, dto.Rooms[0].Price);
        Assert.Equal(250.56m, dto.Rooms[1].Price);
        Assert.Equal("double", dto.Rooms[0].Name);
        Assert.Equal(new[] { "One", "Two" }, JsonSerializer.Deserialize<string[]>(added!.DescriptionJson));
        _repo.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_Unknown_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync("nowhere", default));
    }

    [Fact]
    public async Task Edit_UpdatesExistingRoomsAddsNewAndRemovesDropped()
    {
        var property = Existing("double", "suite");

        await _service.EditAsync("marble", Request("marble", RoomRequest("double", 150m), RoomRequest("loft", 200m)), default);

        Assert.Equal(new[] { "double", "loft" }, property.Rooms.Select(r => r.Id).OrderBy(id => id));
        Assert.Equal(150m, property.Rooms.Single(r => r.Id == "double").Price);
    }

    [Fact]
    public async Task Edit_RemovingBookedRoom_ThrowsConflict()
    {
        var property = Existing("double", "suite");
        _repo.Setup(r => r.RoomHasReservationAsync("marble", "suite", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _service.EditAsync("marble", Request("marble", RoomRequest("double", 150m)), default));
        Assert.Contains(property.Rooms, r => r.Id == "suite");
        _repo.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Edit_Unknown_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.EditAsync("nowhere", Request("nowhere"), default));
    }

    [Fact]
    public async Task Delete_WithReservation_ThrowsConflict()
    {
        Existing();
        _repo.Setup(r => r.HasReservationAsync("marble", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => _service.DeleteAsync("marble", default));
        _repo.Verify(r => r.Remove(It.IsAny<Property>()), Times.Never);
    }

    [Fact]
    public async Task Delete_WithoutReservation_RemovesSavedStaysAndProperty()
    {
        var property = Existing();

        await _service.DeleteAsync("marble", default);

        _repo.Verify(r => r.RemoveSavedStaysAsync("marble", It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.Remove(property), Times.Once);
        _repo.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class AdminReviewServiceTests
{
    private readonly Mock<IReviewRepository> _reviews = new();
    private readonly Mock<IAdminPropertyRepository> _properties = new();
    private readonly AdminReviewService _service;

    public AdminReviewServiceTests()
    {
        _properties.Setup(p => p.ExistsAsync("marble", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _service = new AdminReviewService(_reviews.Object, _properties.Object);
    }

    private static ReviewRequest Request(string propertyId = " marble ", string? roomId = "  ") => new()
    {
        PropertyId = propertyId, RoomId = roomId, Author = " Ana ", Country = "PT", Traveller = "Couple", Nights = 2,
        Date = new DateOnly(2026, 5, 1), Score = 9.2m, Title = " Great ", Liked = "View", Disliked = "-"
    };

    [Fact]
    public async Task Create_UnknownStay_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync(Request("nowhere"), default));
        _reviews.Verify(r => r.AddAsync(It.IsAny<Review>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Create_Valid_TrimsAndTurnsBlankRoomIntoNull()
    {
        var dto = await _service.CreateAsync(Request(), default);

        Assert.Equal("marble", dto.PropertyId);
        Assert.Null(dto.RoomId);
        Assert.Equal("Ana", dto.Author);
        Assert.Equal("Great", dto.Title);
        _reviews.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Edit_MovingToUnknownStay_ThrowsNotFound()
    {
        _reviews.Setup(r => r.FindAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new Review { Id = 1, PropertyId = "marble" });
        await Assert.ThrowsAsync<NotFoundException>(() => _service.EditAsync(1, Request("nowhere"), default));
    }

    [Fact]
    public async Task GetEditDelete_UnknownReview_ThrowNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync(42, default));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.EditAsync(42, Request(), default));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(42, default));
    }

    [Fact]
    public async Task Delete_Existing_RemovesAndSaves()
    {
        var review = new Review { Id = 1 };
        _reviews.Setup(r => r.FindAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(review);

        await _service.DeleteAsync(1, default);

        _reviews.Verify(r => r.Remove(review), Times.Once);
        _reviews.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class AdminPhotoServiceTests
{
    private readonly Mock<IPropertyPhotoRepository> _photos = new();
    private readonly Mock<IAdminPropertyRepository> _properties = new();
    private readonly AdminPhotoService _service;

    public AdminPhotoServiceTests()
    {
        _properties.Setup(p => p.ExistsAsync("marble", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _service = new AdminPhotoService(_photos.Object, _properties.Object);
    }

    [Fact]
    public async Task GetAllAndCreate_UnknownStay_ThrowNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAllAsync("nowhere", default));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateAsync("nowhere", new PhotoRequest(), default));
    }

    [Fact]
    public async Task Create_KnownStay_AddsTrimmedPhotoToThatStay()
    {
        PropertyPhoto? added = null;
        _photos.Setup(r => r.AddAsync(It.IsAny<PropertyPhoto>(), It.IsAny<CancellationToken>()))
            .Callback<PropertyPhoto, CancellationToken>((p, _) => added = p);

        var dto = await _service.CreateAsync("marble", new PhotoRequest { Src = " /a.jpg ", Alt = " Pool ", Order = 3 }, default);

        Assert.Equal(("/a.jpg", "Pool", 3), (dto.Src, dto.Alt, dto.Order));
        Assert.Equal("marble", added!.PropertyId);
    }

    [Fact]
    public async Task GetEditDelete_PhotoOfAnotherStay_ThrowNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync("marble", 7, default));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.EditAsync("marble", 7, new PhotoRequest(), default));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync("marble", 7, default));
    }
}

public class SlideServiceTests
{
    private readonly Mock<ISlideRepository> _slides = new();
    private readonly SlideService _service;

    public SlideServiceTests() => _service = new SlideService(_slides.Object);

    [Fact]
    public async Task Create_TrimsTextAndSaves()
    {
        var dto = await _service.CreateAsync(new SlideRequest { City = " Lisbon ", Country = " Portugal ", Image = " /l.jpg ", Stays = 4, Order = 1 }, default);

        Assert.Equal(("Lisbon", "Portugal", "/l.jpg", 4, 1), (dto.City, dto.Country, dto.Image, dto.Stays, dto.Order));
        _slides.Verify(r => r.AddAsync(It.IsAny<Slide>(), It.IsAny<CancellationToken>()), Times.Once);
        _slides.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Edit_Existing_UpdatesFields()
    {
        var slide = new Slide { Id = 1, City = "Old" };
        _slides.Setup(r => r.FindAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(slide);

        await _service.EditAsync(1, new SlideRequest { City = "New" }, default);

        Assert.Equal("New", slide.City);
    }

    [Fact]
    public async Task GetEditDelete_Unknown_ThrowNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync(9, default));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.EditAsync(9, new SlideRequest(), default));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(9, default));
    }
}

public class VideoServiceTests
{
    private readonly Mock<IVideoRepository> _videos = new();
    private readonly VideoService _service;

    public VideoServiceTests() => _service = new VideoService(_videos.Object);

    [Fact]
    public async Task Create_TrimsSourceAndSaves()
    {
        var dto = await _service.CreateAsync(new VideoRequest { Src = " /v.mp4 ", Order = 2 }, default);
        Assert.Equal(("/v.mp4", 2), (dto.Src, dto.Order));
        _videos.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Delete_Existing_RemovesIt()
    {
        var video = new SiteVideo { Id = 3 };
        _videos.Setup(r => r.FindAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync(video);

        await _service.DeleteAsync(3, default);

        _videos.Verify(r => r.Remove(video), Times.Once);
    }

    [Fact]
    public async Task GetEditDelete_Unknown_ThrowNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync(9, default));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.EditAsync(9, new VideoRequest(), default));
        await Assert.ThrowsAsync<NotFoundException>(() => _service.DeleteAsync(9, default));
    }
}

public class MessageServiceTests
{
    private readonly Mock<IMessageRepository> _messages = new();
    private readonly MessageService _service;

    public MessageServiceTests() => _service = new MessageService(_messages.Object);

    [Fact]
    public async Task Create_TrimsFieldsAndStampsCurrentTime()
    {
        var before = DateTimeOffset.UtcNow;

        var note = await _service.CreateAsync(new MessageRequest { Name = " Ana ", Email = " ana@portico.test ", Stay = " marble ", Message = " Hi " }, default);

        Assert.Equal(("Ana", "ana@portico.test", "marble", "Hi"), (note.Name, note.Email, note.Stay, note.Message));
        Assert.InRange(note.At, before, DateTimeOffset.UtcNow);
        _messages.Verify(r => r.AddAsync(It.Is<GuestMessage>(m => m.Body == "Hi"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Create_KeepsGivenTime()
    {
        var at = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        var note = await _service.CreateAsync(new MessageRequest { Name = "A", Email = "a@b.c", Stay = "", Message = "x", At = at }, default);
        Assert.Equal(at, note.At);
    }
}
