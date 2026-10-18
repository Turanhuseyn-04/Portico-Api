using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portico.Domain.Accounts;
using Service.Helpers.DTOs.Admin;
using Service.Services.Interfaces;

namespace Portico.Api.Controllers.Admin;

[ApiController]
[Authorize(Roles = AccountRoles.Staff)]
[Route("admin/properties/{propertyId}/photos")]
public class PhotosController : ControllerBase
{
    private readonly IAdminPhotoService _photos;

    public PhotosController(IAdminPhotoService photos)
    {
        _photos = photos;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync(string propertyId, CancellationToken cancellationToken)
    {
        return Ok(await _photos.GetAllAsync(propertyId, cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAsync(string propertyId, int id, CancellationToken cancellationToken)
    {
        return Ok(await _photos.GetAsync(propertyId, id, cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(string propertyId, PhotoRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _photos.CreateAsync(propertyId, request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> EditAsync(string propertyId, int id, PhotoRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _photos.EditAsync(propertyId, id, request, cancellationToken));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(string propertyId, int id, CancellationToken cancellationToken)
    {
        await _photos.DeleteAsync(propertyId, id, cancellationToken);
        return NoContent();
    }
}
