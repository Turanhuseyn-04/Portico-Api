using System.Net;
using System.Net.Http.Json;
using Service.Helpers.DTOs.Properties;
using Service.Helpers.DTOs.Reservations;

namespace Portico.Tests.Integration;

[Collection(ApiCollection.Name)]
public class PropertiesEndpointsTests : IClassFixture<PorticoApiFactory>
{
    private readonly HttpClient _client;

    public PropertiesEndpointsTests(PorticoApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task GetAll_IsPublicAndStartsWithCuratedOrder()
    {
        var response = await _client.GetAsync("/properties");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var stays = await response.ReadAsync<StayListItem[]>();
        Assert.NotEmpty(stays);
        Assert.Equal("marble", stays[0].Id);
    }

    [Fact]
    public async Task Get_KnownStay_ReturnsDetail()
    {
        var stay = await (await _client.GetAsync("/properties/marble")).ReadAsync<StayDetail>();

        Assert.Equal("marble", stay.Id);
        Assert.NotEmpty(stay.Rooms);
    }

    [Theory]
    [InlineData("/properties/nowhere")]
    [InlineData("/properties/nowhere/reviews")]
    [InlineData("/properties/nowhere/availability?checkIn=2030-01-01&checkOut=2030-01-03")]
    public async Task UnknownStay_Returns404WithErrorBody(string url)
    {
        var response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(new[] { "That stay does not exist." }, await response.ErrorsAsync());
    }

    [Fact]
    public async Task Availability_ReturnsEveryRoomWithNightlyBreakdown()
    {
        var offers = await (await _client.GetAsync("/properties/marble/availability?checkIn=2030-01-01&checkOut=2030-01-04")).ReadAsync<RoomAvailability[]>();

        Assert.NotEmpty(offers);
        Assert.All(offers, offer =>
        {
            Assert.Equal(3, offer.Nights);
            Assert.Equal(3, offer.PerNight.Count);
            Assert.Equal(offer.Nightly * 3, offer.Total);
        });
    }
}

[Collection(ApiCollection.Name)]
public class ReservationEndpointsTests : IClassFixture<PorticoApiFactory>
{
    private static DateOnly Today => DateOnly.FromDateTime(DateTime.UtcNow);

    private readonly PorticoApiFactory _factory;

    public ReservationEndpointsTests(PorticoApiFactory factory) => _factory = factory;

    private async Task<object> RequestFor(HttpClient client, int daysAhead = 40, int nights = 2)
    {
        var stay = await (await client.GetAsync("/properties/marble")).ReadAsync<StayDetail>();
        var room = stay.Rooms.Where(r => r.Inventory > 0).OrderByDescending(r => r.Refundable).First();
        return new
        {
            propertyId = "marble",
            roomId = room.Id,
            checkIn = Today.AddDays(daysAhead),
            checkOut = Today.AddDays(daysAhead + nights),
            rooms = 1,
            adults = 2,
            children = 0,
            name = "Test Guest",
            email = "someone-else@portico.test" // ignored: the token's email is used
        };
    }

    private async Task<(HttpClient Guest, string Email, ReservationCreated Created)> BookAsync(int daysAhead = 40)
    {
        var email = $"guest-{Guid.NewGuid():N}@portico.test";
        var guest = await _factory.NewGuestAsync(email);
        var response = await guest.PostAsJsonAsync("/reservations", await RequestFor(guest, daysAhead));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (guest, email, await response.ReadAsync<ReservationCreated>());
    }

    [Fact]
    public async Task Create_WithoutToken_Returns401()
    {
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/reservations", await RequestFor(client));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_BooksUnderSignedInAccount()
    {
        var (guest, email, created) = await BookAsync();

        Assert.StartsWith("PT-", created.Reference);
        Assert.Equal("Pending", created.Status);

        var trip = await (await guest.GetAsync($"/reservations/{created.Reference}")).ReadAsync<TripDto>();
        Assert.Equal(email, trip.Email);
        Assert.Equal(created.Total, trip.Total);
    }

    [Fact]
    public async Task Create_CheckOutBeforeCheckIn_Returns400()
    {
        var guest = await _factory.NewGuestAsync();
        var response = await guest.PostAsJsonAsync("/reservations", new
        {
            propertyId = "marble", roomId = "suite", checkIn = Today.AddDays(10), checkOut = Today.AddDays(9),
            rooms = 1, adults = 1, children = 0, name = "Test Guest"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Check-out must be after check-in.", await response.ErrorsAsync());
    }

    [Fact]
    public async Task Create_BookingMoreRoomsThanLeft_Returns409()
    {
        var guest = await _factory.NewGuestAsync();
        var stay = await (await guest.GetAsync("/properties/marble")).ReadAsync<StayDetail>();
        var room = stay.Rooms.First(r => r.Inventory is > 0 and <= 4);
        var checkIn = Today.AddDays(200);

        HttpResponseMessage last = null!;
        for (var i = 0; i <= room.Inventory; i++)
        {
            last = await guest.PostAsJsonAsync("/reservations", new
            {
                propertyId = "marble", roomId = room.Id, checkIn, checkOut = checkIn.AddDays(1),
                rooms = 1, adults = 1, children = 0, name = "Test Guest"
            });
        }

        Assert.Equal(HttpStatusCode.Conflict, last.StatusCode);
    }

    [Fact]
    public async Task Get_AnotherGuestsReservation_Returns404()
    {
        var (_, _, created) = await BookAsync();
        var stranger = await _factory.NewGuestAsync();

        Assert.Equal(HttpStatusCode.NotFound, (await stranger.GetAsync($"/reservations/{created.Reference}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await stranger.PostAsync($"/reservations/{created.Reference}/cancel", null)).StatusCode);
    }

    [Fact]
    public async Task Get_StaffMayOpenAnyReservation()
    {
        var (_, _, created) = await BookAsync();
        var boss = await _factory.SuperAdminAsync();

        Assert.Equal(HttpStatusCode.OK, (await boss.GetAsync($"/reservations/{created.Reference}")).StatusCode);
    }

    [Fact]
    public async Task List_GuestSeesOwnTripsOnly()
    {
        var (guest, email, created) = await BookAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await guest.GetAsync("/reservations")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await guest.GetAsync("/reservations?email=other@portico.test")).StatusCode);

        var trips = await (await guest.GetAsync($"/reservations?email={email}")).ReadAsync<TripDto[]>();
        Assert.Equal(created.Reference, Assert.Single(trips).Reference);
    }

    [Fact]
    public async Task List_StaffSeesEveryReservation()
    {
        var (_, _, created) = await BookAsync();
        var boss = await _factory.SuperAdminAsync();

        var trips = await (await boss.GetAsync("/reservations")).ReadAsync<TripDto[]>();

        Assert.Contains(trips, t => t.Reference == created.Reference);
    }

    [Fact]
    public async Task Cancel_ThenCancelAgain_Returns200Then409()
    {
        var (guest, _, created) = await BookAsync();

        var first = await guest.PostAsync($"/reservations/{created.Reference}/cancel", null);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var trip = await first.ReadAsync<TripDto>();
        Assert.True(trip.Cancelled);
        Assert.Equal(trip.Refundable ? trip.Total : 0m, trip.Refund);

        Assert.Equal(HttpStatusCode.Conflict, (await guest.PostAsync($"/reservations/{created.Reference}/cancel", null)).StatusCode);
    }
}
