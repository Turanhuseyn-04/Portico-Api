using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using Portico.Domain.Accounts;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Accounts;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services;

public class AccountService : IAccountService
{
    public const int MinPasswordLength = 8;

    private readonly IAccountRepository _accounts;
    private readonly IPropertyRepository _properties;
    private readonly IPasswordHasher<Account> _hasher;

    public AccountService(IAccountRepository accounts, IPropertyRepository properties, IPasswordHasher<Account> hasher)
    {
        _accounts = accounts;
        _properties = properties;
        _hasher = hasher;
    }

    public async Task<AccountProfile> RegisterAsync(AccountRequest request, CancellationToken cancellationToken)
    {
        var name = request.Name.Trim();
        var email = request.Email.Trim();
        if (name.Length < 2) throw new BadRequestException("A name of at least 2 letters is required.");
        if (!IsEmail(email)) throw new BadRequestException("A valid email is required.");
        if (request.Password.Length < MinPasswordLength) throw new BadRequestException($"A password of at least {MinPasswordLength} characters is required.");

        var account = await _accounts.FindByEmailAsync(email, includeSaved: false, cancellationToken);
        if (account?.PasswordHash is not null) throw new ConflictException("An account with this email already exists. Sign in instead.");

        if (account is null)
        {
            account = new Account { Email = email, Role = AccountRoles.Guest };
            await _accounts.AddAsync(account, cancellationToken);
        }

        // An account from before passwords existed is claimed by the first person to register its email.
        account.Name = name;
        account.PasswordHash = _hasher.HashPassword(account, request.Password);
        await _accounts.SaveAsync(cancellationToken);
        return Profile(account);
    }

    public async Task<AccountProfile?> SignInAsync(SignInRequest request, CancellationToken cancellationToken)
    {
        var account = await _accounts.FindByEmailAsync(request.Email.Trim(), includeSaved: false, cancellationToken);
        if (account?.PasswordHash is null) return null;

        var result = _hasher.VerifyHashedPassword(account, account.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed) return null;

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            account.PasswordHash = _hasher.HashPassword(account, request.Password);
            await _accounts.SaveAsync(cancellationToken);
        }

        return Profile(account);
    }

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
