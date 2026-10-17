using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Accounts;
using Service.Services.Interfaces;

namespace Portico.Api.Controllers;

[ApiController]
[Route("account")]
public class AccountController : ControllerBase
{
    public const string CookieName = "portico-account";

    private readonly IAccountService _accounts;

    public AccountController(IAccountService accounts)
    {
        _accounts = accounts;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync(AccountRequest request, CancellationToken cancellationToken)
    {
        var profile = await _accounts.RegisterAsync(request.Name, request.Email, cancellationToken);
        Remember(profile.Email);
        return Ok(profile);
    }

    [HttpPost("signin")]
    public async Task<IActionResult> SignInAsync(SignInRequest request, CancellationToken cancellationToken)
    {
        var profile = await _accounts.SignInAsync(request.Email, cancellationToken);
        if (profile is null) return Unauthorized();

        Remember(profile.Email);
        return Ok(profile);
    }

    [HttpGet]
    public async Task<IActionResult> CurrentAsync(CancellationToken cancellationToken)
    {
        var profile = await _accounts.CurrentAsync(Request.Cookies[CookieName], cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpGet("saved")]
    public async Task<IActionResult> SavedAsync(CancellationToken cancellationToken)
    {
        return Ok(await _accounts.ListSavedAsync(Request.Cookies[CookieName], cancellationToken));
    }

    [HttpPost("saved/{id}")]
    public async Task<IActionResult> SaveAsync(string id, CancellationToken cancellationToken)
    {
        return Ok(await _accounts.SaveAsync(Request.Cookies[CookieName], id, cancellationToken));
    }

    [HttpDelete("saved/{id}")]
    public async Task<IActionResult> UnsaveAsync(string id, CancellationToken cancellationToken)
    {
        return Ok(await _accounts.UnsaveAsync(Request.Cookies[CookieName], id, cancellationToken));
    }

    private void Remember(string email)
    {
        Response.Cookies.Append(CookieName, email, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            Path = "/",
            IsEssential = true,
            MaxAge = TimeSpan.FromDays(30)
        });
    }
}
