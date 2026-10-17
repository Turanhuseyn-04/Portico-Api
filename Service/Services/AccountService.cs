using Portico.Domain.Accounts;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Accounts;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services;

public class AccountService : IAccountService
{
    private readonly IAccountRepository _accounts;
    private readonly IPropertyRepository _properties;

    public AccountService(IAccountRepository accounts, IPropertyRepository properties)
    {
        _accounts = accounts;
        _properties = properties;
    }

    public async Task<AccountProfile> RegisterAsync(string name, string email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new BadRequestException("An email is required.");

        var trimmedName = name.Trim();
        var trimmedEmail = email.Trim();
        var account = await _accounts.FindByEmailAsync(trimmedEmail, includeSaved: false, cancellationToken);
        if (account is null)
        {
            account = new Account { Name = trimmedName, Email = trimmedEmail };
            await _accounts.AddAsync(account, cancellationToken);
        }
        else
        {
            account.Name = trimmedName;
        }

        await _accounts.SaveAsync(cancellationToken);
        return Profile(account);
    }

    public async Task<AccountProfile?> SignInAsync(string email, CancellationToken cancellationToken)
    {
        var account = await _accounts.FindByEmailAsync(email.Trim(), includeSaved: false, cancellationToken);
        return account is null ? null : Profile(account);
    }

    public async Task<AccountProfile?> CurrentAsync(string? email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var account = await _accounts.FindByEmailAsync(email, includeSaved: false, cancellationToken);
        return account is null ? null : Profile(account);
    }

    public async Task<IReadOnlyList<string>> ListSavedAsync(string? email, CancellationToken cancellationToken)
    {
        var account = await FindWithSavedAsync(email, cancellationToken);
        return account is null ? Array.Empty<string>() : Ids(account);
    }

    public async Task<IReadOnlyList<string>> SaveAsync(string? email, string propertyId, CancellationToken cancellationToken)
    {
        var account = await FindWithSavedAsync(email, cancellationToken);
        if (account is null) throw new NotFoundException("That account does not exist.");
        if (!await _properties.ExistsAsync(propertyId, cancellationToken)) throw new NotFoundException("That stay does not exist.");

        if (account.SavedStays.All(saved => saved.PropertyId != propertyId))
        {
            account.SavedStays.Add(new SavedStay { PropertyId = propertyId });
            await _accounts.SaveAsync(cancellationToken);
        }

        return Ids(account);
    }

    public async Task<IReadOnlyList<string>> UnsaveAsync(string? email, string propertyId, CancellationToken cancellationToken)
    {
        var account = await FindWithSavedAsync(email, cancellationToken);
        if (account is null) throw new NotFoundException("That account does not exist.");

        var matches = account.SavedStays.Where(saved => saved.PropertyId == propertyId).ToList();
        if (matches.Count > 0)
        {
            await _accounts.RemoveSavedAsync(matches, cancellationToken);
            await _accounts.SaveAsync(cancellationToken);
        }

        return Ids(account);
    }

    private Task<Account?> FindWithSavedAsync(string? email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email)) return Task.FromResult<Account?>(null);
        return _accounts.FindByEmailAsync(email, includeSaved: true, cancellationToken);
    }

    private static AccountProfile Profile(Account account) => new(account.Name, account.Email);

    private static string[] Ids(Account account)
    {
        return account.SavedStays
            .OrderBy(saved => saved.Id)
            .Select(saved => saved.PropertyId)
            .ToArray();
    }
}
