using Microsoft.EntityFrameworkCore;
using Portico.Domain.Properties;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories;

public class AdminPropertyRepository : IAdminPropertyRepository
{
    private readonly PorticoDbContext _db;

    public AdminPropertyRepository(PorticoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Property>> ListAsync(CancellationToken cancellationToken)
    {
        return await _db.Properties.AsNoTracking()
            .Include(property => property.Rooms)
            .OrderBy(property => property.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Property?> FindAsync(string id, CancellationToken cancellationToken)
    {
        return _db.Properties
            .Include(property => property.Rooms)
            .FirstOrDefaultAsync(property => property.Id == id, cancellationToken);
    }

    public Task<bool> ExistsAsync(string id, CancellationToken cancellationToken)
    {
        return _db.Properties.AnyAsync(property => property.Id == id, cancellationToken);
    }

    public Task<bool> HasReservationAsync(string propertyId, CancellationToken cancellationToken)
    {
        return _db.Reservations.AnyAsync(reservation => reservation.PropertyId == propertyId, cancellationToken);
    }

    public Task<bool> RoomHasReservationAsync(string propertyId, string roomId, CancellationToken cancellationToken)
    {
        return _db.Reservations.AnyAsync(reservation => reservation.PropertyId == propertyId && reservation.RoomId == roomId, cancellationToken);
    }

    public async Task AddAsync(Property property, CancellationToken cancellationToken)
    {
        await _db.Properties.AddAsync(property, cancellationToken);
    }

    public async Task RemoveSavedStaysAsync(string propertyId, CancellationToken cancellationToken)
    {
        var saved = await _db.SavedStays.Where(stay => stay.PropertyId == propertyId).ToListAsync(cancellationToken);
        _db.SavedStays.RemoveRange(saved);
    }

    public Task SaveAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);

    public void Remove(Property property) => _db.Properties.Remove(property);
}
