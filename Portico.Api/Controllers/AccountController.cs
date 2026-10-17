using Microsoft.AspNetCore.Mvc;
using Portico.Application.Accounts;

namespace Portico.Api.Controllers;

[ApiController]
[Route("account")]
public class AccountController : ControllerBase
{
    public const string CookieName = "portico-account";

    private readonly IAccountDirectory _accounts;

    public AccountController(IAccountDirectory accounts)
    {
        _accounts = accounts;
    }

    [HttpPost("register")]
    public async Task<ActionResult<AccountProfile>> Register(AccountRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email)) return BadRequest();

        var profile = await _accounts.RegisterAsync(request.Name, request.Email, cancellationToken);
        Remember(profile.Email);
        return Ok(profile);
    }

    [HttpPost("signin")]
    public async Task<ActionResult<AccountProfile>> SignIn(SignInRequest request, CancellationToken cancellationToken)
    {
        var profile = await _accounts.SignInAsync(request.Email, cancellationToken);
        if (profile is null) return Unauthorized();

        Remember(profile.Email);
        return Ok(profile);
    }

    [HttpGet]
    public async Task<ActionResult<AccountProfile>> Current(CancellationToken cancellationToken)
    {
        var profile = await _accounts.CurrentAsync(Request.Cookies[CookieName], cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpGet("saved")]
    public async Task<IReadOnlyList<string>> Saved(CancellationToken cancellationToken)
    {
        return await _accounts.ListSavedAsync(Request.Cookies[CookieName], cancellationToken);
    }

    [HttpPost("saved/{id}")]
    public async Task<ActionResult<IReadOnlyList<string>>> Save(string id, CancellationToken cancellationToken)
    {
        var ids = await _accounts.SaveAsync(Request.Cookies[CookieName], id, cancellationToken);
        return ids is null ? NotFound() : Ok(ids);
    }

    [HttpDelete("saved/{id}")]
    public async Task<ActionResult<IReadOnlyList<string>>> Unsave(string id, CancellationToken cancellationToken)
    {
        var ids = await _accounts.UnsaveAsync(Request.Cookies[CookieName], id, cancellationToken);
        return ids is null ? NotFound() : Ok(ids);
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
