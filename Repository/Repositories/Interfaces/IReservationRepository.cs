using Portico.Domain.Reservations;

namespace Repository.Repositories.Interfaces;

public interface IReservationRepository
{
    Task<IReadOnlyList<Reservation>> ListAsync(CancellationToken cancellationToken);
    Task<Reservation?> FindByReferenceAsync(string reference, bool tracking, CancellationToken cancellationToken);
    Task<bool> ReferenceExistsAsync(string reference, CancellationToken cancellationToken);
    Task AddAsync(Reservation reservation, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
}
