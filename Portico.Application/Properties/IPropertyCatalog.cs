namespace Portico.Application.Properties;

public interface IPropertyCatalog
{
    Task<IReadOnlyList<StayListItem>> ListAsync(CancellationToken cancellationToken);
    Task<StayDetail?> GetAsync(string id, CancellationToken cancellationToken);
    Task<IReadOnlyList<RoomAvailability>?> AvailabilityAsync(string id, DateOnly checkIn, DateOnly checkOut, CancellationToken cancellationToken);
    Task<IReadOnlyList<StayReview>?> ReviewsAsync(string id, CancellationToken cancellationToken);
}
