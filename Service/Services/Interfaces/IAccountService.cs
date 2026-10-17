using Service.Helpers.DTOs.Accounts;

namespace Service.Services.Interfaces;

public interface IAccountService
{
    Task<AccountProfile> RegisterAsync(string name, string email, CancellationToken cancellationToken);
    Task<AccountProfile?> SignInAsync(string email, CancellationToken cancellationToken);
    Task<AccountProfile?> CurrentAsync(string? email, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> ListSavedAsync(string? email, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> SaveAsync(string? email, string propertyId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> UnsaveAsync(string? email, string propertyId, CancellationToken cancellationToken);
}
