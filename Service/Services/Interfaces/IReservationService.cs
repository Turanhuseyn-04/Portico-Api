using Service.Helpers.DTOs.Reservations;

namespace Service.Services.Interfaces;

public interface IReservationService
{
    Task<ReservationCreated> CreateAsync(StayRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<TripDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<TripDto> GetAsync(string reference, CancellationToken cancellationToken);
    Task<TripDto> CancelAsync(string reference, CancellationToken cancellationToken);
}
