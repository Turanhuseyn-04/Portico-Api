using Portico.Domain.Properties;

namespace Repository.Repositories.Interfaces;

public interface IPropertyPhotoRepository
{
    Task<IReadOnlyList<PropertyPhoto>> ListAsync(string propertyId, CancellationToken cancellationToken);
    Task<PropertyPhoto?> FindAsync(string propertyId, int id, CancellationToken cancellationToken);
    Task AddAsync(PropertyPhoto photo, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
    void Remove(PropertyPhoto photo);
}
