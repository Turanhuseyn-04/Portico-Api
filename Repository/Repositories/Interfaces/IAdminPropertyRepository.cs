using Portico.Domain.Properties;

namespace Repository.Repositories.Interfaces;

public interface IAdminPropertyRepository
{
    Task<IReadOnlyList<Property>> ListAsync(CancellationToken cancellationToken);
    Task<Property?> FindAsync(string id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken);
    Task<bool> HasReservationAsync(string propertyId, CancellationToken cancellationToken);
    Task<bool> RoomHasReservationAsync(string propertyId, string roomId, CancellationToken cancellationToken);
    Task AddAsync(Property property, CancellationToken cancellationToken);
    Task RemoveSavedStaysAsync(string propertyId, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
    void Remove(Property property);
}
