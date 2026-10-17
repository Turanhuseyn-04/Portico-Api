namespace Service.Helpers.DTOs.Accounts;

public sealed record AccountProfile(string Name, string Email);

public sealed class AccountRequest
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

public sealed class SignInRequest
{
    public string Email { get; set; } = "";
}
