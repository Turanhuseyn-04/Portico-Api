using Microsoft.AspNetCore.Mvc;
using Portico.Application.Properties;

namespace Portico.Api.Controllers;

[ApiController]
[Route("properties")]
public class PropertiesController : ControllerBase
{
    private readonly IPropertyCatalog _catalog;

    public PropertiesController(IPropertyCatalog catalog)
    {
        _catalog = catalog;
    }

    [HttpGet]
    public async Task<IReadOnlyList<StayListItem>> List(CancellationToken cancellationToken)
    {
        return await _catalog.ListAsync(cancellationToken);
    }

    [HttpGet("{id}/availability")]
    public async Task<ActionResult<IReadOnlyList<RoomAvailability>>> Availability(string id, [FromQuery] DateOnly checkIn, [FromQuery] DateOnly checkOut, CancellationToken cancellationToken)
    {
        var offers = await _catalog.AvailabilityAsync(id, checkIn, checkOut, cancellationToken);
        return offers is null ? NotFound() : Ok(offers);
    }

    [HttpGet("{id}/reviews")]
    public async Task<ActionResult<IReadOnlyList<StayReview>>> Reviews(string id, CancellationToken cancellationToken)
    {
        var reviews = await _catalog.ReviewsAsync(id, cancellationToken);
        return reviews is null ? NotFound() : Ok(reviews);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<StayDetail>> Get(string id, CancellationToken cancellationToken)
    {
        var stay = await _catalog.GetAsync(id, cancellationToken);
        return stay is null ? NotFound() : Ok(stay);
    }
}
