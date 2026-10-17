using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Properties;
using Service.Services.Interfaces;

namespace Portico.Api.Controllers;

[ApiController]
[Route("properties")]
public class PropertiesController : ControllerBase
{
    private readonly IPropertyService _properties;

    public PropertiesController(IPropertyService properties)
    {
        _properties = properties;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        return Ok(await _properties.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id}/availability")]
    public async Task<IActionResult> AvailabilityAsync(string id, [FromQuery] DateOnly checkIn, [FromQuery] DateOnly checkOut, CancellationToken cancellationToken)
    {
        return Ok(await _properties.AvailabilityAsync(id, checkIn, checkOut, cancellationToken));
    }

    [HttpGet("{id}/reviews")]
    public async Task<IActionResult> ReviewsAsync(string id, CancellationToken cancellationToken)
    {
        return Ok(await _properties.ReviewsAsync(id, cancellationToken));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync(string id, CancellationToken cancellationToken)
    {
        return Ok(await _properties.GetAsync(id, cancellationToken));
    }
}
