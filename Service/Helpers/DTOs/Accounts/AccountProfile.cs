namespace Service.Helpers.DTOs.Accounts;

public sealed record AccountProfile(string Name, string Email, string Role);

// What register returns: no sign-in yet, because the email has to be confirmed first.
public sealed record Registered(string Name, string Email, bool ConfirmationRequired, bool EmailSent);

public sealed class AccountRequest
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public sealed class SignInRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}

public sealed class ConfirmEmailRequest
{
    public string Email { get; set; } = "";
    public string Token { get; set; } = "";
}

public sealed class ResendConfirmationRequest
{
    public string Email { get; set; } = "";
}
