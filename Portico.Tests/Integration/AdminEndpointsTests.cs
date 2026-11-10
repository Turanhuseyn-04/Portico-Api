using System.Net;
using System.Net.Http.Json;
using Service.Helpers.DTOs.Admin;
using Service.Helpers.DTOs.Messages;

namespace Portico.Tests.Integration;

[Collection(ApiCollection.Name)]
public class AdminEndpointsTests : IClassFixture<PorticoApiFactory>
{
    private readonly PorticoApiFactory _factory;

    public AdminEndpointsTests(PorticoApiFactory factory) => _factory = factory;

    public static TheoryData<string> StaffOnlyUrls => new()
    {
        "/admin/slides", "/admin/videos", "/admin/reviews", "/admin/properties", "/admin/properties/marble/photos", "/messages"
    };

    public static TheoryData<string> SuperAdminOnlyUrls => new() { "/admin/accounts", "/admin/audit" };

    [Theory]
    [MemberData(nameof(StaffOnlyUrls))]
    [MemberData(nameof(SuperAdminOnlyUrls))]
    public async Task AdminRead_Anonymous401_Guest403_SuperAdmin200(string url)
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await _factory.CreateClient().GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await (await _factory.NewGuestAsync()).GetAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await (await _factory.SuperAdminAsync()).GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task Promotion_AppliesToExistingTokenImmediately()
    {
        var email = $"staff-{Guid.NewGuid():N}@portico.test";
        var user = await _factory.NewGuestAsync(email);
        var boss = await _factory.SuperAdminAsync();
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/admin/slides")).StatusCode);

        var promote = await boss.PutAsJsonAsync($"/admin/accounts/{email}/role", new { role = "Admin" });
        Assert.Equal(HttpStatusCode.OK, promote.StatusCode);
        Assert.Equal("Admin", (await promote.ReadAsync<AccountRowDto>()).Role);

        // Same token as before: the role is re-read from the database on every request.
        Assert.Equal(HttpStatusCode.OK, (await user.GetAsync("/admin/slides")).StatusCode);
        // Admin is staff but not SuperAdmin.
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/admin/accounts")).StatusCode);

        await boss.PutAsJsonAsync($"/admin/accounts/{email}/role", new { role = "Guest" });
        Assert.Equal(HttpStatusCode.Forbidden, (await user.GetAsync("/admin/slides")).StatusCode);
    }

    [Fact]
    public async Task SetRole_OnOwnAccount_Returns403()
    {
        var boss = await _factory.SuperAdminAsync();
        var response = await boss.PutAsJsonAsync($"/admin/accounts/{PorticoApiFactory.SuperAdminEmail}/role", new { role = "Guest" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SetRole_UnknownRole_Returns400()
    {
        var email = $"guest-{Guid.NewGuid():N}@portico.test";
        await _factory.NewGuestAsync(email);
        var boss = await _factory.SuperAdminAsync();

        var response = await boss.PutAsJsonAsync($"/admin/accounts/{email}/role", new { role = "Owner" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Slides_FullCrudLifecycle()
    {
        var boss = await _factory.SuperAdminAsync();

        var created = await (await boss.PostAsJsonAsync("/admin/slides", new { city = "Baku", country = "Azerbaijan", image = "/b.jpg", stays = 3, order = 1 }))
            .ReadAsync<SlideDto>();
        Assert.True(created.Id > 0);

        var edited = await (await boss.PutAsJsonAsync($"/admin/slides/{created.Id}", new { city = "Ganja", country = "Azerbaijan", image = "/g.jpg", stays = 1, order = 2 }))
            .ReadAsync<SlideDto>();
        Assert.Equal("Ganja", edited.City);

        Assert.Equal(HttpStatusCode.NoContent, (await boss.DeleteAsync($"/admin/slides/{created.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await boss.GetAsync($"/admin/slides/{created.Id}")).StatusCode);
    }

    [Fact]
    public async Task Properties_CreateDuplicateReturns409_DeleteWithReservationReturns409()
    {
        var boss = await _factory.SuperAdminAsync();
        var id = $"stay-{Guid.NewGuid():N}"[..20];
        var body = new
        {
            id, name = "Test Stay", type = "Hotel", stars = 4, street = "1 St", area = "A", city = "Baku", postcode = "AZ1000",
            country = "Azerbaijan", lat = 40.4, lng = 49.8, summary = "S", description = new[] { "D" },
            rooms = new[] { new { id = "double", name = "Double", price = 100m, sleeps = 2, refundable = true, breakfast = false, inventory = 2 } }
        };

        Assert.Equal(HttpStatusCode.OK, (await boss.PostAsJsonAsync("/admin/properties", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await boss.PostAsJsonAsync("/admin/properties", body)).StatusCode);

        // The seeded catalog has a reservation on "marble".
        Assert.Equal(HttpStatusCode.Conflict, (await boss.DeleteAsync("/admin/properties/marble")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await boss.DeleteAsync($"/admin/properties/{id}")).StatusCode);
    }

    [Fact]
    public async Task AdminChanges_AreWrittenToTheAuditLog()
    {
        var boss = await _factory.SuperAdminAsync();
        var created = await (await boss.PostAsJsonAsync("/admin/videos", new { src = "/v.mp4", order = 1 })).ReadAsync<VideoDto>();
        await boss.DeleteAsync($"/admin/videos/{created.Id}");

        var audit = await (await boss.GetAsync("/admin/audit?take=50")).ReadAsync<AuditEntryDto[]>();

        Assert.Contains(audit, e => e.Method == "POST" && e.Path == "/admin/videos" && e.StatusCode == 200 && e.ActorEmail == PorticoApiFactory.SuperAdminEmail);
        Assert.Contains(audit, e => e.Method == "DELETE" && e.Path == $"/admin/videos/{created.Id}" && e.StatusCode == 204);
    }

    [Fact]
    public async Task Settings_PublicRead_SuperAdminWrite()
    {
        var anonymous = _factory.CreateClient();
        var settings = await (await anonymous.GetAsync("/settings")).ReadAsync<Dictionary<string, string>>();
        Assert.True(settings.ContainsKey("SupportEmail"));

        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.PutAsJsonAsync("/admin/settings", new { SupportPhone = "1" })).StatusCode);

        var boss = await _factory.SuperAdminAsync();
        Assert.Equal(HttpStatusCode.BadRequest, (await boss.PutAsJsonAsync("/admin/settings", new { Unknown = "x" })).StatusCode);

        // Keys are case-sensitive and spelled as GET /settings returns them, so send a dictionary (not a camel-cased object).
        var response = await boss.PutAsJsonAsync("/admin/settings", new Dictionary<string, string> { ["SupportPhone"] = "+994 12 000 00 00" });
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        var updated = await response.ReadAsync<Dictionary<string, string>>();
        Assert.Equal("+994 12 000 00 00", updated["SupportPhone"]);
    }

    [Fact]
    public async Task Messages_AnyoneSends_OnlyStaffRead()
    {
        var anonymous = _factory.CreateClient();
        var marker = Guid.NewGuid().ToString("N");

        var sent = await anonymous.PostAsJsonAsync("/messages", new { name = "Ana", email = "ana@portico.test", stay = "marble", message = marker });
        Assert.Equal(HttpStatusCode.OK, sent.StatusCode);

        var boss = await _factory.SuperAdminAsync();
        var notes = await (await boss.GetAsync("/messages")).ReadAsync<GuestNote[]>();
        Assert.Contains(notes, n => n.Message == marker);
    }
}
