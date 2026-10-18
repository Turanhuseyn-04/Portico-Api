using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portico.Api.Middlewares;
using Portico.Api.Security;
using Portico.Domain.Accounts;
using Service.Helpers.DTOs.Admin;
using Service.Services.Interfaces;

namespace Portico.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = AccountRoles.SuperAdmin)]
[Route("admin/accounts")]
public class AccountsController : ControllerBase
{
    private readonly IAccountAdminService _accounts;

    public AccountsController(IAccountAdminService accounts)
    {
        _accounts = accounts;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        return Ok(await _accounts.GetAllAsync(cancellationToken));
    }

    [HttpPut("{email}/role")]
    public async Task<IActionResult> SetRoleAsync(string email, RoleRequest request, CancellationToken cancellationToken)
    {
        HttpContext.Items[AuditMiddleware.DetailKey] = $"{email}: role → {request.Role}";
        var changed = await _accounts.SetRoleAsync(User.EmailOf() ?? "", email, request.Role, cancellationToken);
        HttpContext.Items[AuditMiddleware.DetailKey] = $"{changed.Account.Email}: {changed.PreviousRole} → {changed.Account.Role}";
        return Ok(changed.Account);
    }
}
