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

    public async Task<IReadOnlyList<Reservation>> ListAsync(string? email, CancellationToken cancellationToken)
    {
        var query = _db.Reservations.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(email)) query = query.Where(reservation => reservation.Email == email);
        return await query
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

    // An exclusive SQL Server application lock named after the property, released when the transaction ends.
    // If the work throws, disposing the transaction rolls it back.
    public async Task<T> WithPropertyLockAsync<T>(string propertyId, Func<Task<T>> work, CancellationToken cancellationToken)
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
        var resource = $"portico:reservations:{propertyId}";
        await _db.Database.ExecuteSqlInterpolatedAsync($"""
            DECLARE @result int;
            EXEC @result = sp_getapplock @Resource = {resource}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 10000;
            IF @result < 0 THROW 51000, 'The property lock could not be taken.', 1;
            """, cancellationToken);

        var result = await work();
        await transaction.CommitAsync(cancellationToken);
        return result;
    }
}
