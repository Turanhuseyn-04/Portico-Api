using Portico.Domain.Properties;
using Portico.Domain.Reservations;

namespace Repository.Repositories.Interfaces;

public interface IPropertyRepository
{
    Task<IReadOnlyList<Property>> ListAsync(CancellationToken cancellationToken);
    Task<Property?> GetWithDetailsAsync(string id, CancellationToken cancellationToken);
    Task<Property?> GetWithRoomsAsync(string id, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Review>> ReviewsAsync(string id, CancellationToken cancellationToken);
    Task<IReadOnlyList<Reservation>> ActiveOverlappingAsync(string propertyId, DateOnly checkIn, DateOnly checkOut, CancellationToken cancellationToken);
}
