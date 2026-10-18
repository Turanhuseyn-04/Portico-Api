using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portico.Domain.Accounts;
using Service.Helpers.DTOs.Admin;
using Service.Services.Interfaces;

namespace Portico.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = AccountRoles.Staff)]
[Route("admin/videos")]
public class VideosController : ControllerBase
{
    private readonly IVideoService _videos;

    public VideosController(IVideoService videos)
    {
        _videos = videos;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        return Ok(await _videos.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAsync(int id, CancellationToken cancellationToken)
    {
        return Ok(await _videos.GetAsync(id, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(VideoRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _videos.CreateAsync(request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> EditAsync(int id, VideoRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _videos.EditAsync(id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await _videos.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
