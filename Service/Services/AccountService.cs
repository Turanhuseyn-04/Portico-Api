using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Portico.Domain.Accounts;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Accounts;
using Service.Helpers.Email;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services;

public class AccountService : IAccountService
{
    public const int MinPasswordLength = 8;
    public static readonly TimeSpan ConfirmLinkLifetime = TimeSpan.FromHours(24);
    private static readonly TimeSpan ResendGap = TimeSpan.FromMinutes(1);

    private readonly IAccountRepository _accounts;
    private readonly IPropertyRepository _properties;
    private readonly IPasswordHasher<Account> _hasher;
    private readonly IEmailSender _email;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IAccountRepository accounts,
        IPropertyRepository properties,
        IPasswordHasher<Account> hasher,
        IEmailSender email,
        EmailOptions emailOptions,
        ILogger<AccountService> logger)
    {
        _accounts = accounts;
        _properties = properties;
        _hasher = hasher;
        _email = email;
        _emailOptions = emailOptions;
        _logger = logger;
    }

    // Creates the account unconfirmed and sends the confirmation link. Sign-in waits until the link is opened.
    public async Task<Registered> RegisterAsync(AccountRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var email = request.Email.Trim();
        if (name.Length < 2) throw new BadRequestException("A name of at least 2 letters is required.");
        if (!IsEmail(email)) throw new BadRequestException("A valid email is required.");
        if (request.Password.Length < MinPasswordLength) throw new BadRequestException($"A password of at least {MinPasswordLength} characters is required.");

        var account = await _accounts.FindByEmailAsync(email, includeSaved: false, cancellationToken);
        if (account?.PasswordHash is not null)
        {
            throw new ConflictException(account.EmailConfirmed
                ? "An account with this email already exists. Sign in instead."
                : "This email is registered but not confirmed yet. Use the link in the email, or ask for a new one.");
        }

        if (account is null)
        {
            account = new Account { Email = email, Role = AccountRoles.Guest };
            await _accounts.AddAsync(account, cancellationToken);
        }

        // An account from before passwords existed is claimed by registering its email; the confirmation link
        // makes sure only the owner of that inbox can finish the claim.
        account.Name = name;
        account.PasswordHash = _hasher.HashPassword(account, request.Password);
        account.EmailConfirmed = false;
        var token = IssueConfirmToken(account);
        await _accounts.SaveAsync(cancellationToken);

        var sent = await TrySendConfirmationAsync(account, token, cancellationToken);
        return new Registered(account.Name, account.Email, ConfirmationRequired: true, EmailSent: sent);
    }

    public async Task<AccountProfile?> SignInAsync(SignInRequest request, CancellationToken cancellationToken)
    {
        var account = await _accounts.FindByEmailAsync(request.Email.Trim(), includeSaved: false, cancellationToken);
        if (account?.PasswordHash is null) return null;

        var result = _hasher.VerifyHashedPassword(account, account.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed) return null;

        // Checked after the password, so an unconfirmed account is only revealed to someone who knows it.
        if (!account.EmailConfirmed) throw new ForbiddenException("Confirm your email first. We sent a link when you registered.");

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            account.PasswordHash = _hasher.HashPassword(account, request.Password);
            await _accounts.SaveAsync(cancellationToken);
        }

        return Profile(account);
    }

    // The link carries the token once; only its hash is stored, it works once, and it ends after 24 hours.
    public async Task<AccountProfile> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        var account = await _accounts.FindByEmailAsync(request.Email.Trim(), includeSaved: false, cancellationToken);
        if (account is null) throw new BadRequestException("That confirmation link is not valid.");
        if (account.EmailConfirmed) return Profile(account);

        var valid = account.ConfirmTokenHash is not null
            && account.ConfirmTokenExpires > DateTime.UtcNow
            && CryptographicOperations.FixedTimeEquals(
                Encoding.ASCII.GetBytes(HashToken(request.Token ?? "")),
                Encoding.ASCII.GetBytes(account.ConfirmTokenHash));
        if (!valid) throw new BadRequestException("That confirmation link is not valid or has expired. Ask for a new one.");

        account.EmailConfirmed = true;
        account.ConfirmTokenHash = null;
        account.ConfirmTokenExpires = null;
        await _accounts.SaveAsync(cancellationToken);
        return Profile(account);
    }

    // Answers the same whether or not the email is registered, so the form cannot be used to find accounts.
    // A new link replaces the old one, at most once a minute.
    public async Task ResendConfirmationAsync(ResendConfirmationRequest request, CancellationToken cancellationToken)
    {
        var account = await _accounts.FindByEmailAsync(request.Email.Trim(), includeSaved: false, cancellationToken);
        if (account is null || account.EmailConfirmed || account.PasswordHash is null) return;

        var lastSent = account.ConfirmTokenExpires - ConfirmLinkLifetime;
        if (lastSent > DateTime.UtcNow - ResendGap) return;

        var token = IssueConfirmToken(account);
        await _accounts.SaveAsync(cancellationToken);
        await TrySendConfirmationAsync(account, token, cancellationToken);
    }

    private string IssueConfirmToken(Account account)
    {
        var token = Base64Url(RandomNumberGenerator.GetBytes(32));
        account.ConfirmTokenHash = HashToken(token);
        account.ConfirmTokenExpires = DateTime.UtcNow.Add(ConfirmLinkLifetime);
        return token;
    }

    // A mail server that is down does not undo the registration; the guest can ask for a new link.
    private async Task<bool> TrySendConfirmationAsync(Account account, string token, CancellationToken cancellationToken)
    {
        var link = $"{_emailOptions.ConfirmUrl}?email={Uri.EscapeDataString(account.Email)}&token={Uri.EscapeDataString(token)}";
        var name = WebUtility.HtmlEncode(account.Name);
        var href = WebUtility.HtmlEncode(link);
        var message = new EmailMessage(
            account.Email,
            "Confirm your Portico email",
            $"Hello {account.Name},\n\nConfirm your email to start using Portico:\n{link}\n\nThe link works once and ends in 24 hours. If you did not register, ignore this email.\n\nPortico",
            $"<p>Hello {name},</p><p>Confirm your email to start using Portico.</p><p><a href=\"{href}\">Confirm my email</a></p><p>The link works once and ends in 24 hours. If you did not register, ignore this email.</p><p>Portico</p>");
        try
        {
            await _email.SendAsync(message, cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is SmtpException or InvalidOperationException or IOException or UnauthorizedAccessException)
        {
            _logger.LogError(ex, "The confirmation email for {Email} could not be sent.", account.Email);
            return false;
        }
    }

    private static string HashToken(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static string Base64Url(byte[] bytes) => Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    public async Task<AccountProfile> GetAsync(string email, CancellationToken cancellationToken)
    {
        var account = await _accounts.FindByEmailAsync(email, includeSaved: false, cancellationToken);
        if (account is null) throw new NotFoundException("That account does not exist.");
        return Profile(account);
    }

    public async Task<IReadOnlyList<string>> ListSavedAsync(string email, CancellationToken cancellationToken)
    {
        var account = await FindWithSavedAsync(email, cancellationToken);
        return Ids(account);
    }

    public async Task<IReadOnlyList<string>> SaveAsync(string email, string propertyId, CancellationToken cancellationToken)
    {
        var account = await FindWithSavedAsync(email, cancellationToken);
        if (!await _properties.ExistsAsync(propertyId, cancellationToken)) throw new NotFoundException("That stay does not exist.");

        if (account.SavedStays.All(saved => saved.PropertyId != propertyId))
        {
            account.SavedStays.Add(new SavedStay { PropertyId = propertyId });
            await _accounts.SaveAsync(cancellationToken);
        }

        return Ids(account);
    }

    public async Task<IReadOnlyList<string>> UnsaveAsync(string email, string propertyId, CancellationToken cancellationToken)
    {
        var account = await FindWithSavedAsync(email, cancellationToken);
        var matches = account.SavedStays.Where(saved => saved.PropertyId == propertyId).ToList();
        if (matches.Count > 0)
        {
            await _accounts.RemoveSavedAsync(matches, cancellationToken);
            await _accounts.SaveAsync(cancellationToken);
        }

        return Ids(account);
    }

    // The current role, read on every API request so a role change applies at once. Null when the account is gone.
    public async Task<string?> CurrentRoleAsync(string email, CancellationToken cancellationToken)
    {
        var account = await _accounts.FindByEmailAsync(email.Trim(), includeSaved: false, cancellationToken);
        return account?.Role;
    }

    // The SuperAdmin from configuration always exists with that role. Its password is set only when it has none,
    // so a password changed later is not reset on every start.
    public async Task EnsureSuperAdminAsync(string name, string email, string password, CancellationToken cancellationToken)
    {
        var account = await _accounts.FindByEmailAsync(email.Trim(), includeSaved: false, cancellationToken);
        if (account is null)
        {
            account = new Account { Email = email.Trim(), Name = name.Trim() };
            await _accounts.AddAsync(account, cancellationToken);
        }

        account.Role = AccountRoles.SuperAdmin;
        account.EmailConfirmed = true;
        if (string.IsNullOrWhiteSpace(account.Name)) account.Name = name.Trim();
        account.PasswordHash ??= _hasher.HashPassword(account, password);
        await _accounts.SaveAsync(cancellationToken);
    }

    private async Task<Account> FindWithSavedAsync(string email, CancellationToken cancellationToken)
    {
        var account = string.IsNullOrWhiteSpace(email) ? null : await _accounts.FindByEmailAsync(email.Trim(), includeSaved: true, cancellationToken);
        return account ?? throw new NotFoundException("That account does not exist.");
    }

    private static bool IsEmail(string email) =>
        MailAddress.TryCreate(email, out var address) && address.Address == email;

    private static AccountProfile Profile(Account account) => new(account.Name, account.Email, account.Role);

    private static string[] Ids(Account account)
    {
        return account.SavedStays
            .OrderBy(saved => saved.Id)
            .Select(saved => saved.PropertyId)
            .ToArray();
    }
}
