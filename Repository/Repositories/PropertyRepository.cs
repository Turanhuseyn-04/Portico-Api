using Microsoft.EntityFrameworkCore;
using Portico.Domain.Properties;
using Portico.Domain.Reservations;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories;

public class PropertyRepository : IPropertyRepository
{
    private readonly PorticoDbContext _db;

    public PropertyRepository(PorticoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Property>> ListAsync(CancellationToken cancellationToken)
    {
        return await _db.Properties
            .AsNoTracking()
            .Include(property => property.Rooms)
            .Include(property => property.Photos)
            .ToListAsync(cancellationToken);
    }

    public Task<Property?> GetWithDetailsAsync(string id, CancellationToken cancellationToken)
    {
        return _db.Properties
            .AsNoTracking()
            .Include(property => property.Rooms)
            .Include(property => property.Reviews)
            .Include(property => property.Photos)
            .FirstOrDefaultAsync(property => property.Id == id, cancellationToken);
    }

    public Task<Property?> GetWithRoomsAsync(string id, CancellationToken cancellationToken)
    {
        return _db.Properties
            .AsNoTracking()
            .Include(property => property.Rooms)
            .Include(property => property.Photos)
            .FirstOrDefaultAsync(property => property.Id == id, cancellationToken);
    }

    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken)
    {
        return _db.Properties.AsNoTracking().AnyAsync(property => property.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Review>> ReviewsAsync(string id, CancellationToken cancellationToken)
    {
        return await _db.Reviews
            .AsNoTracking()
            .Where(review => review.PropertyId == id)
            .OrderByDescending(review => review.Date)
            .ThenBy(review => review.Author)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reservation>> ActiveOverlappingAsync(string propertyId, DateOnly checkIn, DateOnly checkOut, CancellationToken cancellationToken)
    {
        return await _db.Reservations
            .AsNoTracking()
            .Where(reservation => reservation.PropertyId == propertyId && !reservation.Cancelled && reservation.CheckIn < checkOut && reservation.CheckOut > checkIn)
            .ToListAsync(cancellationToken);
    }
}
