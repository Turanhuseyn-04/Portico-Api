using Microsoft.EntityFrameworkCore;
using Portico.Domain.Content;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories;

public class VideoRepository : IVideoRepository
{
    private readonly PorticoDbContext _db;

    public VideoRepository(PorticoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SiteVideo>> ListAsync(CancellationToken cancellationToken)
    {
        return await _db.Videos.AsNoTracking()
            .OrderBy(video => video.Order)
            .ThenBy(video => video.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<SiteVideo?> FindAsync(int id, CancellationToken cancellationToken)
    {
        return _db.Videos.FirstOrDefaultAsync(video => video.Id == id, cancellationToken);
    }

    public async Task AddAsync(SiteVideo video, CancellationToken cancellationToken)
    {
        await _db.Videos.AddAsync(video, cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);

    public void Remove(SiteVideo video) => _db.Videos.Remove(video);
}
