using Service.Helpers.DTOs.Reservations;

namespace Service.Services.Interfaces;

// The owner email limits a lookup to reservations booked under it; null means staff, who may open any.
public interface IReservationService
{
    Task<ReservationCreated> CreateAsync(StayRequest request, string email, CancellationToken cancellationToken);
    Task<IReadOnlyList<TripDto>> GetAllAsync(string? email, CancellationToken cancellationToken);
    Task<TripDto> GetAsync(string reference, string? ownerEmail, CancellationToken cancellationToken);
    Task<TripDto> CancelAsync(string reference, string? ownerEmail, CancellationToken cancellationToken);
}
