using Microsoft.AspNetCore.Mvc;
using Service.Helpers.DTOs.Admin;
using Service.Services.Interfaces;

namespace Portico.Api.Controllers.Admin;

[ApiController]
[Route("admin/slides")]
public class SlidesController : ControllerBase
{
    private readonly ISlideService _slides;

    public SlidesController(ISlideService slides)
    {
        _slides = slides;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        return Ok(await _slides.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAsync(int id, CancellationToken cancellationToken)
    {
        return Ok(await _slides.GetAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(SlideRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _slides.CreateAsync(request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> EditAsync(int id, SlideRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _slides.EditAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await _slides.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
