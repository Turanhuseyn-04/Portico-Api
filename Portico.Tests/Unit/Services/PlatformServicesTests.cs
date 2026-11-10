using Moq;
using Portico.Domain.Platform;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Admin;
using Service.Helpers.Exceptions;
using Service.Helpers.Logging;
using Service.Services;

namespace Portico.Tests.Unit.Services;

public class SettingServiceTests
{
    private readonly Mock<ISiteSettingRepository> _repo = new();
    private readonly SettingService _service;

    public SettingServiceTests()
    {
        _repo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<SiteSetting>());
        _service = new SettingService(_repo.Object);
    }

    [Fact]
    public async Task GetAll_FillsUnsavedKeysWithDefaultsAndHidesUnknownRows()
    {
        _repo.Setup(r => r.ListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new[]
        {
            new SiteSetting { Key = SiteSettingKeys.SupportPhone, Value = "+1 555" },
            new SiteSetting { Key = "CatalogSeededStays", Value = "marble" }
        });

        var all = await _service.GetAllAsync(default);

        Assert.Equal(3, all.Count);
        Assert.Equal("+1 555", all[SiteSettingKeys.SupportPhone]);
        Assert.Equal(SiteSettingKeys.Defaults[SiteSettingKeys.SupportEmail], all[SiteSettingKeys.SupportEmail]);
        Assert.False(all.ContainsKey("CatalogSeededStays"));
    }

    public static TheoryData<string, string> InvalidValues => new()
    {
        { "Unknown", "x" },
        { SiteSettingKeys.SupportPhone, "   " },
        { SiteSettingKeys.SupportLocation, new string('x', 401) },
        { SiteSettingKeys.SupportEmail, "not-an-email" },
        { SiteSettingKeys.SupportEmail, "Support <support@portico.az>" },
    };

    [Theory]
    [MemberData(nameof(InvalidValues))]
    public async Task Update_InvalidValue_ThrowsBadRequestAndSavesNothing(string key, string value)
    {
        await Assert.ThrowsAsync<BadRequestException>(() => _service.UpdateAsync(new Dictionary<string, string> { [key] = value }, default));
        _repo.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Update_AddsNewKeyAndChangesExistingOne()
    {
        var phone = new SiteSetting { Key = SiteSettingKeys.SupportPhone, Value = "old" };
        _repo.Setup(r => r.FindAsync(SiteSettingKeys.SupportPhone, It.IsAny<CancellationToken>())).ReturnsAsync(phone);

        await _service.UpdateAsync(new Dictionary<string, string>
        {
            [SiteSettingKeys.SupportPhone] = " +994 12 ",
            [SiteSettingKeys.SupportEmail] = "help@portico.az"
        }, default);

        Assert.Equal("+994 12", phone.Value);
        _repo.Verify(r => r.AddAsync(It.Is<SiteSetting>(s => s.Key == SiteSettingKeys.SupportEmail && s.Value == "help@portico.az"), It.IsAny<CancellationToken>()), Times.Once);
        _repo.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class AuditServiceTests
{
    private readonly Mock<IAuditRepository> _repo = new();
    private readonly AuditService _service;

    public AuditServiceTests()
    {
        _repo.Setup(r => r.ListAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(Array.Empty<AuditEntry>());
        _service = new AuditService(_repo.Object);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(200, 200)]
    [InlineData(10_000, 500)]
    public async Task GetRecent_ClampsTake(int take, int expected)
    {
        await _service.GetRecentAsync(take, default);
        _repo.Verify(r => r.ListAsync(expected, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Record_ClipsOverlongValues()
    {
        AuditEntry? saved = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<AuditEntry>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEntry, CancellationToken>((e, _) => saved = e).Returns(Task.CompletedTask);

        await _service.RecordAsync(new AuditRecord(new string('e', 300), "SuperAdmin", "DELETE", "/admin/" + new string('p', 500), 204, new string('d', 500)), default);

        Assert.Equal(256, saved!.ActorEmail.Length);
        Assert.Equal(400, saved.Path.Length);
        Assert.Equal(400, saved.Detail!.Length);
        Assert.Equal(204, saved.StatusCode);
        Assert.InRange(saved.At, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow);
    }

    [Fact]
    public async Task Record_KeepsNullDetail()
    {
        AuditEntry? saved = null;
        _repo.Setup(r => r.AddAsync(It.IsAny<AuditEntry>(), It.IsAny<CancellationToken>()))
            .Callback<AuditEntry, CancellationToken>((e, _) => saved = e).Returns(Task.CompletedTask);

        await _service.RecordAsync(new AuditRecord("a@b.c", "Admin", "POST", "/admin/slides", 200, null), default);

        Assert.Null(saved!.Detail);
    }
}

public class LogMaskTests
{
    [Theory]
    [InlineData("turan@gmail.com", "t***@gmail.com")]
    [InlineData("  a@b.c  ", "a***@b.c")]
    [InlineData("", "(none)")]
    [InlineData(null, "(none)")]
    [InlineData("no-at-sign", "***")]
    [InlineData("@domain.com", "***")]
    public void Email_HidesEverythingButFirstLetterAndDomain(string? input, string expected)
    {
        Assert.Equal(expected, LogMask.Email(input));
    }
}
