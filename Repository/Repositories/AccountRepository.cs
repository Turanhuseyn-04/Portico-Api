using Microsoft.EntityFrameworkCore;
using Portico.Domain.Accounts;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories;

public class AccountRepository : IAccountRepository
{
    private readonly PorticoDbContext _db;

    public AccountRepository(PorticoDbContext db)
    {
        _db = db;
    }

    public Task<Account?> FindByEmailAsync(string email, bool includeSaved, CancellationToken cancellationToken)
    {
        var query = includeSaved ? _db.Accounts.Include(account => account.SavedStays) : _db.Accounts.AsQueryable();
        return query.FirstOrDefaultAsync(account => account.Email == email, cancellationToken);
    }

    public async Task<IReadOnlyList<Account>> ListAsync(CancellationToken cancellationToken)
    {
        return await _db.Accounts.AsNoTracking()
            .OrderBy(account => account.Email)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountInRoleAsync(string role, CancellationToken cancellationToken)
    {
        return _db.Accounts.CountAsync(account => account.Role == role, cancellationToken);
    }

    public async Task AddAsync(Account account, CancellationToken cancellationToken)
    {
        await _db.Accounts.AddAsync(account, cancellationToken);
    }

    public Task RemoveSavedAsync(IEnumerable<SavedStay> stays, CancellationToken cancellationToken)
    {
        _db.SavedStays.RemoveRange(stays);
        return Task.CompletedTask;
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
