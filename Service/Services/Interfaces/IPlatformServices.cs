using Service.Helpers.DTOs.Admin;

namespace Service.Services.Interfaces;

public interface IAccountAdminService
{
    Task<IReadOnlyList<AccountRowDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<RoleChanged> SetRoleAsync(string actorEmail, string targetEmail, string role, CancellationToken cancellationToken);
}

public interface ISettingService
{
    Task<IReadOnlyDictionary<string, string>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyDictionary<string, string>> UpdateAsync(IReadOnlyDictionary<string, string> values, CancellationToken cancellationToken);
}

public interface IAuditService
{
    Task<IReadOnlyList<AuditEntryDto>> GetRecentAsync(int take, CancellationToken cancellationToken);
    Task RecordAsync(AuditRecord record, CancellationToken cancellationToken);
}
