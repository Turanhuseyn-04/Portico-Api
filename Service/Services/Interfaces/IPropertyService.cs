using Service.Helpers.DTOs.Properties;

namespace Service.Services.Interfaces;

public interface IPropertyService
{
    Task<IReadOnlyList<StayListItem>> GetAllAsync(CancellationToken cancellationToken);
    Task<StayDetail> GetAsync(string id, CancellationToken cancellationToken);
    Task<IReadOnlyList<RoomAvailability>> AvailabilityAsync(string id, DateOnly checkIn, DateOnly checkOut, CancellationToken cancellationToken);
    Task<IReadOnlyList<StayReview>> ReviewsAsync(string id, CancellationToken cancellationToken);
}
