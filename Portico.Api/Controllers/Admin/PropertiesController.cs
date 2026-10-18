using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Admin;
using Service.Services.Interfaces;

namespace Portico.Api.Controllers.Admin;

[ApiController]
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

    [HttpPost]
    public async Task<IActionResult> CreateAsync(AdminPropertyRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _properties.CreateAsync(request, cancellationToken));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> EditAsync(string id, AdminPropertyRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _properties.EditAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(string id, CancellationToken cancellationToken)
    {
        await _properties.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
