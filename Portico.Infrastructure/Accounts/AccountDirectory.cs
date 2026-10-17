using Microsoft.EntityFrameworkCore;
using Portico.Application.Accounts;
using Portico.Domain.Accounts;
using Portico.Infrastructure.Persistence;

namespace Portico.Infrastructure.Accounts;

public sealed class AccountDirectory : IAccountDirectory
{
    private readonly PorticoDbContext _db;

    public AccountDirectory(PorticoDbContext db)
    {
        _db = db;
    }

    public async Task<AccountProfile> RegisterAsync(string name, string email, CancellationToken cancellationToken)
    {
        var trimmedName = name.Trim();
        var trimmedEmail = email.Trim();
        var account = await FindAsync(trimmedEmail, cancellationToken);
        if (account is null)
        {
            account = new Account { Name = trimmedName, Email = trimmedEmail };
            _db.Accounts.Add(account);
        }
        else
        {
            account.Name = trimmedName;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Profile(account);
    }

    public async Task<AccountProfile?> SignInAsync(string email, CancellationToken cancellationToken)
    {
        var account = await FindAsync(email.Trim(), cancellationToken);
        return account is null ? null : Profile(account);
    }

    public async Task<AccountProfile?> CurrentAsync(string? email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var account = await FindAsync(email, cancellationToken);
        return account is null ? null : Profile(account);
    }

    public async Task<IReadOnlyList<string>> ListSavedAsync(string? email, CancellationToken cancellationToken)
    {
        var account = await FindWithSavedAsync(email, cancellationToken);
        return account is null ? Array.Empty<string>() : Ids(account);
    }

    public async Task<IReadOnlyList<string>?> SaveAsync(string? email, string propertyId, CancellationToken cancellationToken)
    {
        var account = await FindWithSavedAsync(email, cancellationToken);
        if (account is null) return null;

        var stayExists = await _db.Properties.AnyAsync(property => property.Id == propertyId, cancellationToken);
        if (!stayExists) return null;

        if (account.SavedStays.All(saved => saved.PropertyId != propertyId))
        {
            account.SavedStays.Add(new SavedStay { PropertyId = propertyId });
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Ids(account);
    }

    public async Task<IReadOnlyList<string>?> UnsaveAsync(string? email, string propertyId, CancellationToken cancellationToken)
    {
        var account = await FindWithSavedAsync(email, cancellationToken);
        if (account is null) return null;

        var matches = account.SavedStays.Where(saved => saved.PropertyId == propertyId).ToList();
        if (matches.Count > 0)
        {
            _db.SavedStays.RemoveRange(matches);
            await _db.SaveChangesAsync(cancellationToken);
        }

        return Ids(account);
    }

    private Task<Account?> FindAsync(string email, CancellationToken cancellationToken)
    {
        return _db.Accounts.FirstOrDefaultAsync(account => account.Email == email, cancellationToken);
    }

    private Task<Account?> FindWithSavedAsync(string? email, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(email)) return Task.FromResult<Account?>(null);
        return _db.Accounts
            .Include(account => account.SavedStays)
            .FirstOrDefaultAsync(account => account.Email == email, cancellationToken);
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
