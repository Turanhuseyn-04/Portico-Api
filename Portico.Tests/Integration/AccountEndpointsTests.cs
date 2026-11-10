using System.Net;
using System.Net.Http.Json;
using Portico.Api.Security;
using Service.Helpers.DTOs.Accounts;

namespace Portico.Tests.Integration;

[Collection(ApiCollection.Name)]
public class AccountEndpointsTests : IClassFixture<PorticoApiFactory>
{
    private readonly PorticoApiFactory _factory;
    private readonly HttpClient _anonymous;

    public AccountEndpointsTests(PorticoApiFactory factory)
    {
        _factory = factory;
        _anonymous = factory.CreateClient();
    }

    private static string NewEmail() => $"guest-{Guid.NewGuid():N}@portico.test";

    [Fact]
    public async Task Register_Confirm_SignIn_FullFlowReturnsToken()
    {
        var email = NewEmail();

        var register = await _anonymous.PostAsJsonAsync("/account/register", new { name = "Turan", email, password = "secret-pass" });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        Assert.Equal(new Registered("Turan", email, true, true), await register.ReadAsync<Registered>());

        var confirm = await _anonymous.PostAsJsonAsync("/account/confirm", new { email, token = _factory.Emails.ConfirmTokenFor(email) });
        Assert.Equal(HttpStatusCode.OK, confirm.StatusCode);

        var signIn = await _anonymous.PostAsJsonAsync("/account/signin", new { email, password = "secret-pass" });
        Assert.Equal(HttpStatusCode.OK, signIn.StatusCode);
        var signedIn = await signIn.ReadAsync<SignedIn>();
        Assert.Equal(("Turan", email, "Guest"), (signedIn.Name, signedIn.Email, signedIn.Role));
        Assert.False(string.IsNullOrWhiteSpace(signedIn.Token));
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400WithErrors()
    {
        var response = await _anonymous.PostAsJsonAsync("/account/register", new { name = "Turan", email = "nope", password = "secret-pass" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("A valid email is required.", await response.ErrorsAsync());
    }

    [Fact]
    public async Task SignIn_BeforeConfirming_Returns403()
    {
        var email = NewEmail();
        await _anonymous.PostAsJsonAsync("/account/register", new { name = "Turan", email, password = "secret-pass" });

        var response = await _anonymous.PostAsJsonAsync("/account/signin", new { email, password = "secret-pass" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task SignIn_WrongPassword_Returns401()
    {
        var email = NewEmail();
        await _factory.NewGuestAsync(email, "right-password");

        var response = await _anonymous.PostAsJsonAsync("/account/signin", new { email, password = "wrong-password" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SignIn_FiveWrongPasswords_LocksAccountWith429()
    {
        var email = NewEmail();
        await _factory.NewGuestAsync(email, "right-password");

        for (var i = 0; i < 5; i++)
            await _anonymous.PostAsJsonAsync("/account/signin", new { email, password = "wrong-password" });
        var response = await _anonymous.PostAsJsonAsync("/account/signin", new { email, password = "right-password" });

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
    }

    [Fact]
    public async Task Confirm_BadToken_Returns400()
    {
        var email = NewEmail();
        await _anonymous.PostAsJsonAsync("/account/register", new { name = "Turan", email, password = "secret-pass" });

        var response = await _anonymous.PostAsJsonAsync("/account/confirm", new { email, token = "forged" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ResendConfirmation_UnknownEmail_StillAnswersSent()
    {
        var response = await _anonymous.PostAsJsonAsync("/account/confirm/resend", new { email = NewEmail() });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetAccount_WithoutToken_Returns401()
    {
        var response = await _anonymous.GetAsync($"/account/{NewEmail()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAccount_WithForgedToken_Returns401()
    {
        var response = await _factory.ClientWithToken("not.a.jwt").GetAsync($"/account/{NewEmail()}");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetAccount_Own_Returns200_Others_Returns403()
    {
        var email = NewEmail();
        var guest = await _factory.NewGuestAsync(email);
        var otherEmail = NewEmail();
        await _factory.NewGuestAsync(otherEmail);

        var own = await guest.GetAsync($"/account/{email}");
        Assert.Equal(HttpStatusCode.OK, own.StatusCode);
        Assert.Equal(email, (await own.ReadAsync<AccountProfile>()).Email);

        Assert.Equal(HttpStatusCode.Forbidden, (await guest.GetAsync($"/account/{otherEmail}")).StatusCode);
    }

    [Fact]
    public async Task GetAccount_StaffMayReadAnyGuest()
    {
        var email = NewEmail();
        await _factory.NewGuestAsync(email);
        var boss = await _factory.SuperAdminAsync();

        Assert.Equal(HttpStatusCode.OK, (await boss.GetAsync($"/account/{email}")).StatusCode);
    }

    [Fact]
    public async Task SavedStays_SaveListUnsave()
    {
        var email = NewEmail();
        var guest = await _factory.NewGuestAsync(email);

        var saved = await guest.PostAsync($"/account/{email}/saved/marble", null);
        Assert.Equal(new[] { "marble" }, await saved.ReadAsync<string[]>());

        Assert.Equal(new[] { "marble" }, await (await guest.GetAsync($"/account/{email}/saved")).ReadAsync<string[]>());

        var removed = await guest.DeleteAsync($"/account/{email}/saved/marble");
        Assert.Empty(await removed.ReadAsync<string[]>());
    }

    [Fact]
    public async Task SavedStays_UnknownStay_Returns404()
    {
        var email = NewEmail();
        var guest = await _factory.NewGuestAsync(email);

        Assert.Equal(HttpStatusCode.NotFound, (await guest.PostAsync($"/account/{email}/saved/nowhere", null)).StatusCode);
    }

    [Fact]
    public async Task SavedStays_EvenStaffCannotChangeAnotherAccount()
    {
        var email = NewEmail();
        await _factory.NewGuestAsync(email);
        var boss = await _factory.SuperAdminAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await boss.PostAsync($"/account/{email}/saved/marble", null)).StatusCode);
    }
}
