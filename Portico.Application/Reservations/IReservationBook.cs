namespace Portico.Application.Reservations;

public interface IReservationBook
{
    Task<BookResult> CreateAsync(StayRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<TripDto>> ListAsync(CancellationToken cancellationToken);
    Task<TripDto?> GetAsync(string reference, CancellationToken cancellationToken);
    Task<TripDto?> CancelAsync(string reference, CancellationToken cancellationToken);
}
