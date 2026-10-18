using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Accounts;
using Service.Services.Interfaces;

namespace Portico.Api.Controllers;

// Portico.Web signs guests in with its own cookie and names the account in the address of each call.
[ApiController]
[Route("account")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accounts;

    public AccountController(IAccountService accounts)
    {
        _accounts = accounts;
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync(AccountRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _accounts.RegisterAsync(request, cancellationToken));
    }

    [HttpPost("signin")]
    public async Task<IActionResult> SignInAsync(SignInRequest request, CancellationToken cancellationToken)
    {
        var profile = await _accounts.SignInAsync(request, cancellationToken);
        return profile is null ? Unauthorized() : Ok(profile);
    }

    [HttpGet("{email}")]
    public async Task<IActionResult> GetAsync(string email, CancellationToken cancellationToken)
    {
        return Ok(await _accounts.GetAsync(email, cancellationToken));
    }

    [HttpGet("{email}/saved")]
    public async Task<IActionResult> SavedAsync(string email, CancellationToken cancellationToken)
    {
        return Ok(await _accounts.ListSavedAsync(email, cancellationToken));
    }

    [HttpPost("{email}/saved/{id}")]
    public async Task<IActionResult> SaveAsync(string email, string id, CancellationToken cancellationToken)
    {
        return Ok(await _accounts.SaveAsync(email, id, cancellationToken));
    }

    [HttpDelete("{email}/saved/{id}")]
    public async Task<IActionResult> UnsaveAsync(string email, string id, CancellationToken cancellationToken)
    {
        return Ok(await _accounts.UnsaveAsync(email, id, cancellationToken));
    }
}
