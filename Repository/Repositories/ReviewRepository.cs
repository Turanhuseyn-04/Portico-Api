using Microsoft.EntityFrameworkCore;
using Portico.Domain.Properties;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories;

public class ReviewRepository : IReviewRepository
{
    private readonly PorticoDbContext _db;

    public ReviewRepository(PorticoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Review>> ListAsync(CancellationToken cancellationToken)
    {
        return await _db.Reviews.AsNoTracking()
            .OrderByDescending(review => review.Date)
            .ThenBy(review => review.Id)
            .ToListAsync(cancellationToken);
    }

    public Task<Review?> FindAsync(int id, CancellationToken cancellationToken)
    {
        return _db.Reviews.FirstOrDefaultAsync(review => review.Id == id, cancellationToken);
    }

    public async Task AddAsync(Review review, CancellationToken cancellationToken)
    {
        await _db.Reviews.AddAsync(review, cancellationToken);
    }

    public Task SaveAsync(CancellationToken cancellationToken) => _db.SaveChangesAsync(cancellationToken);

    public void Remove(Review review) => _db.Reviews.Remove(review);
}
