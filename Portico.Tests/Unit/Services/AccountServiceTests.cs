using System.Net.Mail;
using System.Web;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Portico.Domain.Accounts;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Accounts;
using Service.Helpers.Email;
using Service.Helpers.Exceptions;
using Service.Services;

namespace Portico.Tests.Unit.Services;

public class AccountServiceTests
{
    private const string Password = "correct-horse";
    private const string ConfirmUrl = "https://web.test/confirm-email";

    private readonly Mock<IAccountRepository> _accounts = new();
    private readonly Mock<IPropertyRepository> _properties = new();
    private readonly Mock<IEmailSender> _email = new();
    private readonly PasswordHasher<Account> _hasher = new();
    private readonly List<EmailMessage> _sent = new();
    private readonly AccountService _service;

    public AccountServiceTests()
    {
        _email.Setup(e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((message, _) => _sent.Add(message))
            .Returns(Task.CompletedTask);
        _service = new AccountService(_accounts.Object, _properties.Object, _hasher, _email.Object,
            new EmailOptions { ConfirmUrl = ConfirmUrl }, NullLogger<AccountService>.Instance);
    }

    private void Returns(Account? account, bool includeSaved = false) =>
        _accounts.Setup(r => r.FindByEmailAsync(It.IsAny<string>(), includeSaved, It.IsAny<CancellationToken>())).ReturnsAsync(account);

    private Account Confirmed(string email = "guest@portico.test")
    {
        var account = new Account { Name = "Guest", Email = email, EmailConfirmed = true, Role = AccountRoles.Guest };
        account.PasswordHash = _hasher.HashPassword(account, Password);
        return account;
    }

    private static string TokenFrom(EmailMessage message)
    {
        var link = message.Text.Split('\n').Single(line => line.StartsWith(ConfirmUrl));
        return HttpUtility.ParseQueryString(new Uri(link).Query)["token"]!;
    }

    // ---------- Register ----------

    [Fact]
    public async Task Register_NewEmail_CreatesUnconfirmedGuestAndSendsConfirmationLink()
    {
        Returns(null);
        Account? added = null;
        _accounts.Setup(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .Callback<Account, CancellationToken>((a, _) => added = a);

        var result = await _service.RegisterAsync(new AccountRequest { Name = "  Turan  ", Email = " new@portico.test ", Password = Password }, default);

        Assert.Equal(new Registered("Turan", "new@portico.test", ConfirmationRequired: true, EmailSent: true), result);
        Assert.NotNull(added);
        Assert.Equal("Turan", added!.Name);
        Assert.Equal("new@portico.test", added.Email);
        Assert.Equal(AccountRoles.Guest, added.Role);
        Assert.False(added.EmailConfirmed);
        Assert.NotNull(added.ConfirmTokenHash);
        Assert.InRange(added.ConfirmTokenExpires!.Value, DateTime.UtcNow.AddHours(23.9), DateTime.UtcNow.AddHours(24.1));
        Assert.NotEqual(PasswordVerificationResult.Failed, _hasher.VerifyHashedPassword(added, added.PasswordHash!, Password));
        _accounts.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);

        var mail = Assert.Single(_sent);
        Assert.Equal("new@portico.test", mail.To);
        Assert.Contains($"{ConfirmUrl}?email=new%40portico.test&token=", mail.Text);
        // Only the hash is stored, never the token itself.
        Assert.NotEqual(TokenFrom(mail), added.ConfirmTokenHash);
    }

    [Theory]
    [InlineData("A", "guest@portico.test", Password, "name")]
    [InlineData("Guest", "not-an-email", Password, "email")]
    [InlineData("Guest", "Guest <guest@portico.test>", Password, "email")]
    [InlineData("Guest", "guest@portico.test", "short", "password")]
    public async Task Register_InvalidInput_ThrowsBadRequest(string name, string email, string password, string mentioned)
    {
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.RegisterAsync(new AccountRequest { Name = name, Email = email, Password = password }, default));

        Assert.Contains(mentioned, ex.Message, StringComparison.OrdinalIgnoreCase);
        _accounts.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Register_AlreadyConfirmedEmail_NotifiesOwnerAndChangesNothing()
    {
        var account = Confirmed();
        var hash = account.PasswordHash;
        Returns(account);

        var result = await _service.RegisterAsync(new AccountRequest { Name = "Intruder", Email = account.Email, Password = "another-password" }, default);

        Assert.True(result.ConfirmationRequired);
        Assert.Equal(hash, account.PasswordHash);
        Assert.Equal("Guest", account.Name);
        Assert.Equal("You already have a Portico account", Assert.Single(_sent).Subject);
        _accounts.Verify(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Register_UnconfirmedWithRecentLink_DoesNotResendOrReplacePassword()
    {
        var account = Confirmed();
        account.EmailConfirmed = false;
        account.ConfirmTokenHash = "EXISTING";
        account.ConfirmTokenExpires = DateTime.UtcNow.AddHours(24); // sent just now
        var hash = account.PasswordHash;
        Returns(account);

        var result = await _service.RegisterAsync(new AccountRequest { Name = "Guest", Email = account.Email, Password = "stranger-password" }, default);

        Assert.True(result.EmailSent); // a link is already on its way
        Assert.Empty(_sent);
        Assert.Equal("EXISTING", account.ConfirmTokenHash);
        Assert.Equal(hash, account.PasswordHash);
    }

    [Fact]
    public async Task Register_UnconfirmedWithOldLink_SendsNewLinkButKeepsFirstPassword()
    {
        var account = Confirmed();
        account.EmailConfirmed = false;
        account.ConfirmTokenHash = "OLD";
        account.ConfirmTokenExpires = DateTime.UtcNow.AddHours(20); // sent 4 hours ago
        var hash = account.PasswordHash;
        Returns(account);

        await _service.RegisterAsync(new AccountRequest { Name = "Guest", Email = account.Email, Password = "stranger-password" }, default);

        Assert.Equal("Confirm your Portico email", Assert.Single(_sent).Subject);
        Assert.NotEqual("OLD", account.ConfirmTokenHash);
        Assert.Equal(hash, account.PasswordHash);
    }

    [Fact]
    public async Task Register_LegacyAccountWithoutPassword_IsClaimedWithoutCreatingAnother()
    {
        var legacy = new Account { Email = "old@portico.test", Name = "", PasswordHash = null, EmailConfirmed = true };
        Returns(legacy);

        await _service.RegisterAsync(new AccountRequest { Name = "Owner", Email = legacy.Email, Password = Password }, default);

        Assert.Equal("Owner", legacy.Name);
        Assert.NotNull(legacy.PasswordHash);
        Assert.False(legacy.EmailConfirmed);
        _accounts.Verify(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
        Assert.Single(_sent);
    }

    [Fact]
    public async Task Register_MailServerDown_StillRegistersAndReportsEmailNotSent()
    {
        Returns(null);
        _email.Setup(e => e.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>())).ThrowsAsync(new SmtpException("down"));

        var result = await _service.RegisterAsync(new AccountRequest { Name = "Guest", Email = "guest@portico.test", Password = Password }, default);

        Assert.False(result.EmailSent);
        _accounts.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Confirm email ----------

    [Fact]
    public async Task Confirm_TokenFromEmail_ConfirmsAccountAndClearsToken()
    {
        Returns(null);
        Account? added = null;
        _accounts.Setup(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .Callback<Account, CancellationToken>((a, _) => added = a);
        await _service.RegisterAsync(new AccountRequest { Name = "Guest", Email = "guest@portico.test", Password = Password }, default);
        Returns(added);

        var profile = await _service.ConfirmEmailAsync(new ConfirmEmailRequest { Email = "guest@portico.test", Token = TokenFrom(_sent[0]) }, default);

        Assert.Equal(new AccountProfile("Guest", "guest@portico.test", AccountRoles.Guest), profile);
        Assert.True(added!.EmailConfirmed);
        Assert.Null(added.ConfirmTokenHash);
        Assert.Null(added.ConfirmTokenExpires);
    }

    [Fact]
    public async Task Confirm_WrongToken_ThrowsBadRequest()
    {
        var account = new Account { Email = "guest@portico.test", ConfirmTokenHash = "ABC", ConfirmTokenExpires = DateTime.UtcNow.AddHours(1) };
        Returns(account);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.ConfirmEmailAsync(new ConfirmEmailRequest { Email = account.Email, Token = "wrong" }, default));
        Assert.False(account.EmailConfirmed);
    }

    [Fact]
    public async Task Confirm_ExpiredToken_ThrowsBadRequest()
    {
        Returns(null);
        Account? added = null;
        _accounts.Setup(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .Callback<Account, CancellationToken>((a, _) => added = a);
        await _service.RegisterAsync(new AccountRequest { Name = "Guest", Email = "guest@portico.test", Password = Password }, default);
        added!.ConfirmTokenExpires = DateTime.UtcNow.AddMinutes(-1);
        Returns(added);

        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.ConfirmEmailAsync(new ConfirmEmailRequest { Email = "guest@portico.test", Token = TokenFrom(_sent[0]) }, default));
        Assert.False(added.EmailConfirmed);
    }

    [Fact]
    public async Task Confirm_UnknownEmail_ThrowsBadRequest()
    {
        Returns(null);
        await Assert.ThrowsAsync<BadRequestException>(() =>
            _service.ConfirmEmailAsync(new ConfirmEmailRequest { Email = "nobody@portico.test", Token = "x" }, default));
    }

    [Fact]
    public async Task Confirm_AlreadyConfirmed_ReturnsProfileWithoutCheckingToken()
    {
        Returns(Confirmed());
        var profile = await _service.ConfirmEmailAsync(new ConfirmEmailRequest { Email = "guest@portico.test", Token = "anything" }, default);
        Assert.Equal("guest@portico.test", profile.Email);
        _accounts.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    // ---------- Resend confirmation ----------

    [Fact]
    public async Task Resend_UnknownOrConfirmedAccount_SendsNothing()
    {
        Returns(null);
        await _service.ResendConfirmationAsync(new ResendConfirmationRequest { Email = "nobody@portico.test" }, default);
        Returns(Confirmed());
        await _service.ResendConfirmationAsync(new ResendConfirmationRequest { Email = "guest@portico.test" }, default);

        Assert.Empty(_sent);
    }

    [Fact]
    public async Task Resend_UnconfirmedAndDue_SendsNewLink()
    {
        var account = Confirmed();
        account.EmailConfirmed = false;
        account.ConfirmTokenExpires = DateTime.UtcNow.AddHours(23); // sent an hour ago
        Returns(account);

        await _service.ResendConfirmationAsync(new ResendConfirmationRequest { Email = account.Email }, default);

        Assert.Single(_sent);
        _accounts.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---------- Sign in ----------

    [Fact]
    public async Task SignIn_RightPassword_ReturnsProfileAndResetsFailures()
    {
        var account = Confirmed();
        account.FailedSignIns = 3;
        Returns(account);

        var profile = await _service.SignInAsync(new SignInRequest { Email = " guest@portico.test ", Password = Password }, default);

        Assert.Equal(new AccountProfile("Guest", "guest@portico.test", AccountRoles.Guest), profile);
        Assert.Equal(0, account.FailedSignIns);
        Assert.Null(account.LockedUntil);
    }

    [Fact]
    public async Task SignIn_UnknownEmailOrNoPassword_ReturnsNull()
    {
        Returns(null);
        Assert.Null(await _service.SignInAsync(new SignInRequest { Email = "nobody@portico.test", Password = Password }, default));

        Returns(new Account { Email = "legacy@portico.test", PasswordHash = null });
        Assert.Null(await _service.SignInAsync(new SignInRequest { Email = "legacy@portico.test", Password = Password }, default));
    }

    [Fact]
    public async Task SignIn_WrongPassword_ReturnsNullAndCountsFailure()
    {
        var account = Confirmed();
        Returns(account);

        var profile = await _service.SignInAsync(new SignInRequest { Email = account.Email, Password = "wrong-password" }, default);

        Assert.Null(profile);
        Assert.Equal(1, account.FailedSignIns);
        Assert.Null(account.LockedUntil);
        _accounts.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SignIn_FifthWrongPassword_LocksAccountFor15Minutes()
    {
        var account = Confirmed();
        account.FailedSignIns = AccountService.MaxFailedSignIns - 1;
        Returns(account);

        await _service.SignInAsync(new SignInRequest { Email = account.Email, Password = "wrong-password" }, default);

        Assert.Equal(0, account.FailedSignIns);
        Assert.NotNull(account.LockedUntil);
        Assert.InRange(account.LockedUntil!.Value, DateTime.UtcNow.AddMinutes(14), DateTime.UtcNow.AddMinutes(15.1));
    }

    [Fact]
    public async Task SignIn_WhileLocked_RefusesEvenTheRightPassword()
    {
        var account = Confirmed();
        account.LockedUntil = DateTime.UtcNow.AddMinutes(10);
        Returns(account);

        var ex = await Assert.ThrowsAsync<TooManyRequestsException>(() =>
            _service.SignInAsync(new SignInRequest { Email = account.Email, Password = Password }, default));
        Assert.Contains("10 minutes", ex.Message);
    }

    [Fact]
    public async Task SignIn_AfterLockExpires_Succeeds()
    {
        var account = Confirmed();
        account.LockedUntil = DateTime.UtcNow.AddMinutes(-1);
        Returns(account);

        Assert.NotNull(await _service.SignInAsync(new SignInRequest { Email = account.Email, Password = Password }, default));
        Assert.Null(account.LockedUntil);
    }

    [Fact]
    public async Task SignIn_UnconfirmedWithRightPassword_ThrowsForbidden()
    {
        var account = Confirmed();
        account.EmailConfirmed = false;
        Returns(account);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            _service.SignInAsync(new SignInRequest { Email = account.Email, Password = Password }, default));
    }

    [Fact]
    public async Task SignIn_UnconfirmedWithWrongPassword_DoesNotRevealConfirmationState()
    {
        var account = Confirmed();
        account.EmailConfirmed = false;
        Returns(account);

        Assert.Null(await _service.SignInAsync(new SignInRequest { Email = account.Email, Password = "wrong-password" }, default));
    }

    // ---------- Profile, saved stays, roles ----------

    [Fact]
    public async Task Get_UnknownAccount_ThrowsNotFound()
    {
        Returns(null);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.GetAsync("nobody@portico.test", default));
    }

    [Fact]
    public async Task ListSaved_ReturnsPropertyIdsInSaveOrder()
    {
        var account = Confirmed();
        account.SavedStays.Add(new SavedStay { Id = 2, PropertyId = "riad" });
        account.SavedStays.Add(new SavedStay { Id = 1, PropertyId = "marble" });
        Returns(account, includeSaved: true);

        Assert.Equal(new[] { "marble", "riad" }, await _service.ListSavedAsync(account.Email, default));
    }

    [Fact]
    public async Task Save_NewStay_AddsItOnce()
    {
        var account = Confirmed();
        Returns(account, includeSaved: true);
        _properties.Setup(p => p.ExistsAsync("marble", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await _service.SaveAsync(account.Email, "marble", default);
        var ids = await _service.SaveAsync(account.Email, "marble", default);

        Assert.Equal(new[] { "marble" }, ids);
        _accounts.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Save_UnknownStay_ThrowsNotFound()
    {
        Returns(Confirmed(), includeSaved: true);
        _properties.Setup(p => p.ExistsAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await Assert.ThrowsAsync<NotFoundException>(() => _service.SaveAsync("guest@portico.test", "nowhere", default));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Save_BlankEmail_ThrowsNotFound(string email)
    {
        await Assert.ThrowsAsync<NotFoundException>(() => _service.SaveAsync(email, "marble", default));
    }

    [Fact]
    public async Task Unsave_SavedStay_RemovesIt()
    {
        var account = Confirmed();
        var saved = new SavedStay { Id = 1, PropertyId = "marble" };
        account.SavedStays.Add(saved);
        Returns(account, includeSaved: true);

        await _service.UnsaveAsync(account.Email, "marble", default);

        _accounts.Verify(r => r.RemoveSavedAsync(It.Is<IEnumerable<SavedStay>>(s => s.Single() == saved), It.IsAny<CancellationToken>()), Times.Once);
        _accounts.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Unsave_StayNotSaved_DoesNothing()
    {
        Returns(Confirmed(), includeSaved: true);
        await _service.UnsaveAsync("guest@portico.test", "marble", default);
        _accounts.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CurrentRole_ReturnsRoleOrNullWhenAccountIsGone()
    {
        var account = Confirmed();
        account.Role = AccountRoles.Admin;
        Returns(account);
        Assert.Equal(AccountRoles.Admin, await _service.CurrentRoleAsync(account.Email, default));

        Returns(null);
        Assert.Null(await _service.CurrentRoleAsync(account.Email, default));
    }

    [Fact]
    public async Task EnsureSuperAdmin_NoAccount_CreatesConfirmedSuperAdmin()
    {
        Returns(null);
        Account? added = null;
        _accounts.Setup(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()))
            .Callback<Account, CancellationToken>((a, _) => added = a);

        await _service.EnsureSuperAdminAsync("Boss", " boss@portico.test ", Password, default);

        Assert.NotNull(added);
        Assert.Equal("boss@portico.test", added!.Email);
        Assert.Equal(AccountRoles.SuperAdmin, added.Role);
        Assert.True(added.EmailConfirmed);
        Assert.NotNull(added.PasswordHash);
    }

    [Fact]
    public async Task EnsureSuperAdmin_ExistingAccount_PromotesButKeepsPassword()
    {
        var account = Confirmed("boss@portico.test");
        var hash = account.PasswordHash;
        Returns(account);

        await _service.EnsureSuperAdminAsync("Boss", account.Email, "configured-password", default);

        Assert.Equal(AccountRoles.SuperAdmin, account.Role);
        Assert.Equal(hash, account.PasswordHash);
        _accounts.Verify(r => r.AddAsync(It.IsAny<Account>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
