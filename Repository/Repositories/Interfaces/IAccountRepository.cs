using Portico.Domain.Accounts;

namespace Repository.Repositories.Interfaces;

public interface IAccountRepository
{
    Task<Account?> FindByEmailAsync(string email, bool includeSaved, CancellationToken cancellationToken);
    Task AddAsync(Account account, CancellationToken cancellationToken);
    Task RemoveSavedAsync(IEnumerable<SavedStay> stays, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
}
