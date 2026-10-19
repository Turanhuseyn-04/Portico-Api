namespace Portico.Domain.Accounts;

public class Account
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";

    // Null for accounts made before passwords existed; registering again with the same email sets one.
    public string? PasswordHash { get; set; }
    public string Role { get; set; } = AccountRoles.Guest;

    // Sign-in waits until the guest opens the link sent to this address. Only a hash of the link's token is kept.
    public bool EmailConfirmed { get; set; }
    public string? ConfirmTokenHash { get; set; }
    public DateTime? ConfirmTokenExpires { get; set; }

    // Wrong passwords in a row, and when sign-in opens again after too many of them.
    public int FailedSignIns { get; set; }
    public DateTime? LockedUntil { get; set; }

    public ICollection<SavedStay> SavedStays { get; set; } = new List<SavedStay>();
}

public static class AccountRoles
{
    public const string Guest = "Guest";
    public const string Admin = "Admin";
    public const string SuperAdmin = "SuperAdmin";

    // Staff run the houses. Only a SuperAdmin manages accounts, roles, settings, and the audit log.
    public const string Staff = Admin + "," + SuperAdmin;

    public static readonly IReadOnlyList<string> All = new[] { Guest, Admin, SuperAdmin };

    public static bool IsStaff(string? role) => role is Admin or SuperAdmin;
}
