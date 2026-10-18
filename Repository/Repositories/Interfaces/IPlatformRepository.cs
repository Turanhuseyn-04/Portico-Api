using Portico.Domain.Platform;

namespace Repository.Repositories.Interfaces;

public interface ISiteSettingRepository
{
    Task<IReadOnlyList<SiteSetting>> ListAsync(CancellationToken cancellationToken);
    Task<SiteSetting?> FindAsync(string key, CancellationToken cancellationToken);
    Task AddAsync(SiteSetting setting, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
}

public interface IAuditRepository
{
    Task<IReadOnlyList<AuditEntry>> ListAsync(int take, CancellationToken cancellationToken);
    Task AddAsync(AuditEntry entry, CancellationToken cancellationToken);
}
