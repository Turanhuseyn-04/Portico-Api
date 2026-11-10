using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portico.Api.Security;
using Portico.Domain.Accounts;
using Service.Helpers.DTOs.Admin;
using Service.Services.Interfaces;

namespace Portico.Api.Controllers.Admin;

[ApiController]
// Staff (Admin or SuperAdmin) may read and edit. Creating and deleting is for a SuperAdmin only.
[Authorize(Roles = AccountRoles.Staff)]
[Route("admin/properties")]
public class PropertiesController : ControllerBase
{
    private readonly IAdminPropertyService _properties;

    public PropertiesController(IAdminPropertyService properties)
    {
        _properties = properties;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        return Ok(await _properties.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync(string id, CancellationToken cancellationToken)
    {
        return Ok(await _properties.GetAsync(id, cancellationToken));
    }

    [Authorize(Roles = AccountRoles.SuperAdmin)]
    [HttpPost]
    public async Task<IActionResult> CreateAsync(AdminPropertyRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _properties.CreateAsync(request, cancellationToken));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> EditAsync(string id, AdminPropertyRequest request, CancellationToken cancellationToken)
    {
        var superAdmin = User.RoleOf() == AccountRoles.SuperAdmin;
        return Ok(await _properties.EditAsync(id, request, superAdmin, cancellationToken));
    }

    [Authorize(Roles = AccountRoles.SuperAdmin)]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        await _properties.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
