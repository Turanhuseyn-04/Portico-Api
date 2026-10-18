using Portico.Domain.Properties;

namespace Repository.Repositories.Interfaces;

public interface IReviewRepository
{
    Task<IReadOnlyList<Review>> ListAsync(CancellationToken cancellationToken);
    Task<Review?> FindAsync(int id, CancellationToken cancellationToken);
    Task AddAsync(Review review, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
    void Remove(Review review);
}
