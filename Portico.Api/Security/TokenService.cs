using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Service.Helpers.DTOs.Accounts;

namespace Portico.Api.Security;

public sealed class JwtOptions
{
    public string Issuer { get; set; } = "Portico.Api";
    public string Audience { get; set; } = "Portico.Web";
    public string Key { get; set; } = "";
    public int Days { get; set; } = 7;

    public SymmetricSecurityKey SigningKey() => new(Encoding.UTF8.GetBytes(Key));
}

// What register and sign-in return: the account and the token Portico.Web sends with each call for that guest.
public sealed record SignedIn(string Name, string Email, string Role, string Token, DateTime ExpiresAt);

public static class PorticoClaims
{
    public const string Email = "email";
    public const string Name = "name";
    public const string Role = "role";

    public static string? EmailOf(this ClaimsPrincipal user) => user.FindFirstValue(Email);

    public static string RoleOf(this ClaimsPrincipal user) => user.FindFirstValue(Role) ?? "";

    public static bool IsSelf(this ClaimsPrincipal user, string email) =>
        string.Equals(user.EmailOf(), email.Trim(), StringComparison.OrdinalIgnoreCase);
}

public class TokenService
{
    private readonly JwtOptions _options;

    public TokenService(JwtOptions options)
    {
        _options = options;
    }

    // The role in the token is only a hint; every request reads the current role from the database.
    public SignedIn Issue(AccountProfile profile)
    {
        var expires = DateTime.UtcNow.AddDays(_options.Days);
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, profile.Email),
                new Claim(PorticoClaims.Email, profile.Email),
                new Claim(PorticoClaims.Name, profile.Name),
                new Claim(PorticoClaims.Role, profile.Role)
            },
            expires: expires,
            signingCredentials: new SigningCredentials(_options.SigningKey(), SecurityAlgorithms.HmacSha256));
        return new SignedIn(profile.Name, profile.Email, profile.Role, new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
