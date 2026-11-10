using System.Net;
using System.Net.Http.Json;
using Service.Helpers.DTOs.Admin;

namespace Portico.Tests.Integration;

// An Admin reads and edits; only a SuperAdmin creates or deletes.
[Collection(ApiCollection.Name)]
public class AdminRestrictionsTests : IClassFixture<PorticoApiFactory>
{
    private readonly PorticoApiFactory _factory;

    public AdminRestrictionsTests(PorticoApiFactory factory) => _factory = factory;

    private static object NewStay(string id) => new
    {
        id, name = "Test Stay", type = "Hotel", stars = 4, street = "1 St", area = "A", city = "Baku", postcode = "AZ1000",
        country = "Azerbaijan", lat = 40.4, lng = 49.8, summary = "S", description = new[] { "D" },
        rooms = new[] { new { id = "double", name = "Double", price = 100m, sleeps = 2, refundable = true, breakfast = false, inventory = 2 } }
    };

    private static string NewStayId() => $"stay-{Guid.NewGuid():N}"[..20];

    // Create endpoints with a valid body for each.
    public static TheoryData<string, object> Creates => new()
    {
        { "/admin/slides", new { city = "Baku", country = "Azerbaijan", image = "/b.jpg", stays = 3, order = 1 } },
        { "/admin/videos", new { src = "/v.mp4", order = 1 } },
        { "/admin/reviews", new { propertyId = "marble", author = "Ana", country = "PT", traveller = "Couple", nights = 2, date = "2026-05-01", score = 9.0m, title = "T", liked = "L", disliked = "D" } },
        { "/admin/properties/marble/photos", new { src = "/p.jpg", alt = "Pool", order = 1 } },
        { "/admin/properties", NewStay("admin-made-stay") },
    };

    [Theory]
    [MemberData(nameof(Creates))]
    public async Task Admin_CannotCreate_SuperAdminCan(string url, object body)
    {
        var admin = await _factory.NewAdminAsync();
        var boss = await _factory.SuperAdminAsync();
        var before = await CountAsync(boss, url);

        var refused = await admin.PostAsJsonAsync(url, body);

        Assert.Equal(HttpStatusCode.Forbidden, refused.StatusCode);
        Assert.Equal(before, await CountAsync(boss, url));

        // The same request is fine for a SuperAdmin (the stay id is made unique so it cannot clash).
        var allowed = await boss.PostAsJsonAsync(url, url == "/admin/properties" ? NewStay(NewStayId()) : body);
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);
    }

    public static TheoryData<string, string> Deletes => new()
    {
        { "/admin/slides", "/admin/slides/{0}" },
        { "/admin/videos", "/admin/videos/{0}" },
        { "/admin/reviews", "/admin/reviews/{0}" },
        { "/admin/properties/marble/photos", "/admin/properties/marble/photos/{0}" },
    };

    [Theory]
    [MemberData(nameof(Deletes))]
    public async Task Admin_CannotDelete_SuperAdminCan(string createUrl, string itemUrl)
    {
        var boss = await _factory.SuperAdminAsync();
        var admin = await _factory.NewAdminAsync();
        var body = Creates.Single(row => (string)row[0] == createUrl)[1];
        var created = await (await boss.PostAsJsonAsync(createUrl, body)).ReadAsync<IdOnly>();
        var url = string.Format(itemUrl, created.Id);

        Assert.Equal(HttpStatusCode.Forbidden, (await admin.DeleteAsync(url)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await boss.GetAsync(url)).StatusCode); // still there

        Assert.Equal(HttpStatusCode.NoContent, (await boss.DeleteAsync(url)).StatusCode);
    }

    [Fact]
    public async Task Admin_CannotDeleteStay()
    {
        var boss = await _factory.SuperAdminAsync();
        var admin = await _factory.NewAdminAsync();
        var id = NewStayId();
        (await boss.PostAsJsonAsync("/admin/properties", NewStay(id))).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Forbidden, (await admin.DeleteAsync($"/admin/properties/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await boss.GetAsync($"/admin/properties/{id}")).StatusCode);
    }

    [Fact]
    public async Task Admin_CanStillReadAndEdit()
    {
        var boss = await _factory.SuperAdminAsync();
        var admin = await _factory.NewAdminAsync();
        var slide = await (await boss.PostAsJsonAsync("/admin/slides", new { city = "Baku", country = "Azerbaijan", image = "/b.jpg", stays = 3, order = 1 })).ReadAsync<SlideDto>();

        Assert.Equal(HttpStatusCode.OK, (await admin.GetAsync("/admin/slides")).StatusCode);
        var edited = await admin.PutAsJsonAsync($"/admin/slides/{slide.Id}", new { city = "Ganja", country = "Azerbaijan", image = "/g.jpg", stays = 1, order = 2 });

        Assert.Equal(HttpStatusCode.OK, edited.StatusCode);
        Assert.Equal("Ganja", (await edited.ReadAsync<SlideDto>()).City);
    }

    [Fact]
    public async Task Admin_CanEditStayAndItsRooms_ButNotAddOrRemoveRooms()
    {
        var boss = await _factory.SuperAdminAsync();
        var admin = await _factory.NewAdminAsync();
        var id = NewStayId();
        var stay = await (await boss.PostAsJsonAsync("/admin/properties", NewStay(id))).ReadAsync<AdminPropertyDto>();
        var room = stay.Rooms.Single();

        // Same rooms, new name and price: allowed.
        var edit = await admin.PutAsJsonAsync($"/admin/properties/{id}", stay with { Name = "Renamed", Rooms = new[] { room with { Price = 150m } } });
        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        Assert.Equal(150m, (await edit.ReadAsync<AdminPropertyDto>()).Rooms.Single().Price);

        // Adding a room: refused.
        var add = await admin.PutAsJsonAsync($"/admin/properties/{id}", stay with { Rooms = new[] { room, room with { Id = "loft" } } });
        Assert.Equal(HttpStatusCode.Forbidden, add.StatusCode);
        Assert.Contains("Only a SuperAdmin can add or remove rooms.", await add.ErrorsAsync());

        // Removing every room: refused.
        Assert.Equal(HttpStatusCode.Forbidden, (await admin.PutAsJsonAsync($"/admin/properties/{id}", stay with { Rooms = Array.Empty<AdminRoomDto>() })).StatusCode);

        var after = await (await boss.GetAsync($"/admin/properties/{id}")).ReadAsync<AdminPropertyDto>();
        Assert.Equal(new[] { "double" }, after.Rooms.Select(r => r.Id));

        // A SuperAdmin may add the room.
        Assert.Equal(HttpStatusCode.OK, (await boss.PutAsJsonAsync($"/admin/properties/{id}", stay with { Rooms = new[] { room, room with { Id = "loft" } } })).StatusCode);
    }

    [Fact]
    public async Task RefusedAdminAttempt_IsRecordedInAuditLog()
    {
        var admin = await _factory.NewAdminAsync();
        await admin.PostAsJsonAsync("/admin/videos", new { src = "/refused.mp4", order = 1 });

        var audit = await (await (await _factory.SuperAdminAsync()).GetAsync("/admin/audit?take=100")).ReadAsync<AuditEntryDto[]>();

        Assert.Contains(audit, e => e.ActorRole == "Admin" && e.Method == "POST" && e.Path == "/admin/videos" && e.StatusCode == 403);
    }

    private static async Task<int> CountAsync(HttpClient client, string url) =>
        (await (await client.GetAsync(url)).ReadAsync<IdOnly[]>()).Length;

    private sealed record IdOnly(object Id);
}
