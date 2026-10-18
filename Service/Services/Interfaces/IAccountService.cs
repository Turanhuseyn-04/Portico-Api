using Service.Helpers.DTOs.Accounts;

namespace Service.Services.Interfaces;

public interface IAccountService
{
    Task<Registered> RegisterAsync(AccountRequest request, CancellationToken cancellationToken);
    Task<AccountProfile> ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken cancellationToken);
    Task ResendConfirmationAsync(ResendConfirmationRequest request, CancellationToken cancellationToken);
    Task<AccountProfile?> SignInAsync(SignInRequest request, CancellationToken cancellationToken);
    Task<AccountProfile> GetAsync(string email, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> ListSavedAsync(string email, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> SaveAsync(string email, string propertyId, CancellationToken cancellationToken);
    Task<IReadOnlyList<string>> UnsaveAsync(string email, string propertyId, CancellationToken cancellationToken);
    Task<string?> CurrentRoleAsync(string email, CancellationToken cancellationToken);
    Task EnsureSuperAdminAsync(string name, string email, string password, CancellationToken cancellationToken);
}
