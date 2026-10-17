using Microsoft.EntityFrameworkCore;
using Portico.Domain.Reservations;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories;

public class ReservationRepository : IReservationRepository
{
    private readonly PorticoDbContext _db;

    public ReservationRepository(PorticoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Reservation>> ListAsync(CancellationToken cancellationToken)
    {
        return await _db.Reservations
            .AsNoTracking()
            .OrderBy(reservation => reservation.CheckIn)
            .ThenBy(reservation => reservation.Reference)
            .ToListAsync(cancellationToken);
    }

    public Task<Reservation?> FindByReferenceAsync(string reference, bool tracking, CancellationToken cancellationToken)
    {
        var query = tracking ? _db.Reservations : _db.Reservations.AsNoTracking();
        return query.FirstOrDefaultAsync(reservation => reservation.Reference == reference, cancellationToken);
    }

    public Task<bool> ReferenceExistsAsync(string reference, CancellationToken cancellationToken)
    {
        return _db.Reservations.AnyAsync(reservation => reservation.Reference == reference, cancellationToken);
    }

    public async Task AddAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        await _db.Reservations.AddAsync(reservation, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken)
    {
        return _db.SaveChangesAsync(cancellationToken);
    }
}
