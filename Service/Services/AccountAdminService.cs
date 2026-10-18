using Portico.Domain.Accounts;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Admin;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services;

// Accounts and roles, for the SuperAdmin only.
public class AccountAdminService : IAccountAdminService
{
    private readonly IAccountRepository _accounts;

    public AccountAdminService(IAccountRepository accounts)
    {
        _accounts = accounts;
    }

    public async Task<IReadOnlyList<AccountRowDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var rows = await _accounts.ListAsync(cancellationToken);
        return rows.Select(Map).ToArray();
    }

    public async Task<RoleChanged> SetRoleAsync(string actorEmail, string targetEmail, string role, CancellationToken cancellationToken)
    {
        var next = AccountRoles.All.FirstOrDefault(known => string.Equals(known, role?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (next is null) throw new BadRequestException($"Role must be one of: {string.Join(", ", AccountRoles.All)}.");

        // Nobody changes their own role, so a SuperAdmin cannot lock themselves out and nobody can promote themselves.
        if (string.Equals(actorEmail.Trim(), targetEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenException("You cannot change your own role.");

        var account = await _accounts.FindByEmailAsync(targetEmail.Trim(), includeSaved: false, cancellationToken);
        if (account is null) throw new NotFoundException("That account does not exist.");

        var previous = account.Role;
        if (previous == next) return new RoleChanged(Map(account), previous);

        if (previous == AccountRoles.SuperAdmin && await _accounts.CountInRoleAsync(AccountRoles.SuperAdmin, cancellationToken) <= 1)
            throw new ConflictException("Portico needs at least one SuperAdmin.");

        account.Role = next;
        await _accounts.SaveAsync(cancellationToken);
        return new RoleChanged(Map(account), previous);
    }

    private static AccountRowDto Map(Account account) => new(account.Name, account.Email, account.Role, account.PasswordHash is not null);
}
