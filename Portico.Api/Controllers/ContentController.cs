using Microsoft.AspNetCore.Mvc;
using Service.Services.Interfaces;

namespace Portico.Api.Controllers;

// What the guest site's home page shows: the trending destination slides and the video band, in their order.
// Anyone may read them; changing them is under /admin.
[ApiController]
public class ContentController : ControllerBase
{
    private readonly ISlideService _slides;
    private readonly IVideoService _videos;

    public ContentController(ISlideService slides, IVideoService videos)
    {
        _slides = slides;
        _videos = videos;
    }

    [HttpGet("slides")]
    public async Task<IActionResult> SlidesAsync(CancellationToken cancellationToken)
    {
        return Ok(await _slides.GetAllAsync(cancellationToken));
    }

    [HttpGet("videos")]
    public async Task<IActionResult> VideosAsync(CancellationToken cancellationToken)
    {
        return Ok(await _videos.GetAllAsync(cancellationToken));
    }
}
