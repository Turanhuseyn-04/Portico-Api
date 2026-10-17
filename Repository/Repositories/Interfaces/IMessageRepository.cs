using Portico.Domain.Messages;

namespace Repository.Repositories.Interfaces;

public interface IMessageRepository
{
    Task<IReadOnlyList<GuestMessage>> ListAsync(CancellationToken cancellationToken);
    Task AddAsync(GuestMessage message, CancellationToken cancellationToken);
}
