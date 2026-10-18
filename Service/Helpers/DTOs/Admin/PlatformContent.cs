namespace Service.Helpers.DTOs.Admin;

public sealed record AccountRowDto(string Name, string Email, string Role, bool HasPassword, bool EmailConfirmed);

public sealed class RoleRequest
{
    public string Role { get; set; } = "";
}

public sealed record RoleChanged(AccountRowDto Account, string PreviousRole);

public sealed record AuditEntryDto(int Id, DateTime At, string ActorEmail, string ActorRole, string Method, string Path, int StatusCode, string? Detail);

public sealed record AuditRecord(string ActorEmail, string ActorRole, string Method, string Path, int StatusCode, string? Detail);
