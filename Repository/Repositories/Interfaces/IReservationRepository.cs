using Portico.Domain.Reservations;

namespace Repository.Repositories.Interfaces;

public interface IReservationRepository
{
    Task<IReadOnlyList<Reservation>> ListAsync(string? email, CancellationToken cancellationToken);
    Task<Reservation?> FindByReferenceAsync(string reference, bool tracking, CancellationToken cancellationToken);
    Task<bool> ReferenceExistsAsync(string reference, CancellationToken cancellationToken);
    Task AddAsync(Reservation reservation, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);

    // Runs the work in one transaction that holds a lock on the property, so two bookings for it cannot both
    // pass the availability check before either is saved.
    Task<T> WithPropertyLockAsync<T>(string propertyId, Func<Task<T>> work, CancellationToken cancellationToken);
}
