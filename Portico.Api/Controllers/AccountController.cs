using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portico.Api.Security;
using Portico.Domain.Accounts;
using Service.Helpers.DTOs.Accounts;
using Service.Services.Interfaces;

namespace Portico.Api.Controllers;

// Register and sign-in return a token. Every other call needs it, and a guest can only reach their own account.
[ApiController]
[Route("account")]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accounts;
    private readonly TokenService _tokens;

    public AccountController(IAccountService accounts, TokenService tokens)
    {
        _accounts = accounts;
        _tokens = tokens;
    }

    // No token here: the account signs in after its email is confirmed.
    [HttpPost("register")]
    public async Task<IActionResult> RegisterAsync(AccountRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _accounts.RegisterAsync(request, cancellationToken));
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmAsync(ConfirmEmailRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _accounts.ConfirmEmailAsync(request, cancellationToken));
    }

    [HttpPost("confirm/resend")]
    public async Task<IActionResult> ResendAsync(ResendConfirmationRequest request, CancellationToken cancellationToken)
    {
        await _accounts.ResendConfirmationAsync(request, cancellationToken);
        return Ok(new { sent = true });
    }

    [HttpPost("signin")]
    public async Task<IActionResult> SignInAsync(SignInRequest request, CancellationToken cancellationToken)
    {
        var profile = await _accounts.SignInAsync(request, cancellationToken);
        return profile is null ? Unauthorized() : Ok(_tokens.Issue(profile));
    }

    [Authorize]
    [HttpGet("{email}")]
    public async Task<IActionResult> GetAsync(string email, CancellationToken cancellationToken)
    {
        if (!MayRead(email)) return Forbid();
        return Ok(await _accounts.GetAsync(email, cancellationToken));
    }

    [Authorize]
    [HttpGet("{email}/saved")]
    public async Task<IActionResult> SavedAsync(string email, CancellationToken cancellationToken)
    {
        if (!MayRead(email)) return Forbid();
        return Ok(await _accounts.ListSavedAsync(email, cancellationToken));
    }

    [Authorize]
    [HttpPost("{email}/saved/{id}")]
    public async Task<IActionResult> SaveAsync(string email, string id, CancellationToken cancellationToken)
    {
        if (!User.IsSelf(email)) return Forbid();
        return Ok(await _accounts.SaveAsync(email, id, cancellationToken));
    }

    [Authorize]
    [HttpDelete("{email}/saved/{id}")]
    public async Task<IActionResult> UnsaveAsync(string email, string id, CancellationToken cancellationToken)
    {
        if (!User.IsSelf(email)) return Forbid();
        return Ok(await _accounts.UnsaveAsync(email, id, cancellationToken));
    }

    // Staff may look at a guest's account; only the guest may change it.
    private bool MayRead(string email) => User.IsSelf(email) || AccountRoles.IsStaff(User.RoleOf());
}
