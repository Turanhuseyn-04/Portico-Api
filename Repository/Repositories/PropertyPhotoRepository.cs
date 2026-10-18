using Microsoft.EntityFrameworkCore;
using Portico.Domain.Properties;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories;

public class PropertyPhotoRepository : IPropertyPhotoRepository
{
    private readonly PorticoDbContext _db;

    public PropertyPhotoRepository(PorticoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<PropertyPhoto>> ListAsync(string propertyId, CancellationToken cancellationToken)
    {
        return await _db.Photos.AsNoTracking()
            .Where(photo => photo.PropertyId == propertyId)
            .OrderBy(photo => photo.Order)
            .ThenBy(photo => photo.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<PropertyPhoto?> FindAsync(string propertyId, int id, CancellationToken cancellationToken)
    {
        return _db.Photos.FirstOrDefaultAsync(photo => photo.PropertyId == propertyId && photo.Id == id, cancellationToken);
    }

    public async Task AddAsync(PropertyPhoto photo, CancellationToken cancellationToken)
    {
        await _db.Photos.AddAsync(photo, cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);

    public void Remove(PropertyPhoto photo) => _db.Photos.Remove(photo);
}
