using Microsoft.EntityFrameworkCore;
using Portico.Domain.Platform;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories;

public class SiteSettingRepository : ISiteSettingRepository
{
    private readonly PorticoDbContext _db;

    public SiteSettingRepository(PorticoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SiteSetting>> ListAsync(CancellationToken cancellationToken)
    {
        return await _db.Settings.AsNoTracking().OrderBy(setting => setting.Key).ToListAsync(cancellationToken);
    }

    public Task<SiteSetting?> FindAsync(string key, CancellationToken cancellationToken)
    {
        return _db.Settings.FirstOrDefaultAsync(setting => setting.Key == key, cancellationToken);
    }

    public async Task AddAsync(SiteSetting setting, CancellationToken cancellationToken)
    {
        await _db.Settings.AddAsync(setting, cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);
}

public class AuditRepository : IAuditRepository
{
    private readonly PorticoDbContext _db;
    private readonly DbContextOptions<PorticoDbContext> _options;

    public AuditRepository(PorticoDbContext db, DbContextOptions<PorticoDbContext> options)
    {
        _db = db;
        _options = options;
    }

    public async Task<IReadOnlyList<AuditEntry>> ListAsync(int take, CancellationToken cancellationToken)
    {
        return await _db.AuditEntries.AsNoTracking()
            .OrderByDescending(entry => entry.At)
            .ThenByDescending(entry => entry.Id)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    // Written through its own context, so recording a failed request never saves that request's half-made changes.
    public async Task AddAsync(AuditEntry entry, CancellationToken cancellationToken)
    {
        await using var db = new PorticoDbContext(_options);
        await db.AuditEntries.AddAsync(entry, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }
}
