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
}
