using Portico.Domain.Content;

namespace Repository.Repositories.Interfaces;

public interface ISlideRepository
{
    Task<IReadOnlyList<Slide>> ListAsync(CancellationToken cancellationToken);
    Task<Slide?> FindAsync(int id, CancellationToken cancellationToken);
    Task AddAsync(Slide slide, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
    void Remove(Slide slide);
}
