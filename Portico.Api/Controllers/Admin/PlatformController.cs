using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portico.Api.Middlewares;
using Portico.Domain.Accounts;
using Service.Services.Interfaces;

namespace Portico.Api.Controllers.Admin;

// Site settings: anyone may read them (the footer shows them); only a SuperAdmin changes them.
[ApiController]
public class SettingsController : ControllerBase
{
    private readonly ISettingService _settings;

    public SettingsController(ISettingService settings)
    {
        _settings = settings;
    }

    [HttpGet("settings")]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        return Ok(await _settings.GetAllAsync(cancellationToken));
    }

    [Authorize(Roles = AccountRoles.SuperAdmin)]
    [HttpPut("admin/settings")]
    public async Task<IActionResult> UpdateAsync(Dictionary<string, string> values, CancellationToken cancellationToken)
    {
        HttpContext.Items[AuditMiddleware.DetailKey] = string.Join(", ", values.Keys);
        return Ok(await _settings.UpdateAsync(values, cancellationToken));
    }
}

[ApiController]
[Authorize(Roles = AccountRoles.SuperAdmin)]
[Route("admin/audit")]
public class AuditController : ControllerBase
{
    private readonly IAuditService _audit;

    public AuditController(IAuditService audit)
    {
        _audit = audit;
    }

    [HttpGet]
    public async Task<IActionResult> GetRecentAsync([FromQuery] int take = 200, CancellationToken cancellationToken = default)
    {
        return Ok(await _audit.GetRecentAsync(take, cancellationToken));
    }
}
