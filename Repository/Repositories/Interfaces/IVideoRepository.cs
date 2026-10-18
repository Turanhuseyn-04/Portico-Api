using Portico.Domain.Content;

namespace Repository.Repositories.Interfaces;

public interface IVideoRepository
{
    Task<IReadOnlyList<SiteVideo>> ListAsync(CancellationToken cancellationToken);
    Task<SiteVideo?> FindAsync(int id, CancellationToken cancellationToken);
    Task AddAsync(SiteVideo video, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
    void Remove(SiteVideo video);
}
