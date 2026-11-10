using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portico.Domain.Accounts;
using Service.Helpers.DTOs.Admin;
using Service.Services.Interfaces;

namespace Portico.Api.Controllers.Admin;

[ApiController]
// Staff (Admin or SuperAdmin) may read and edit. Creating and deleting is for a SuperAdmin only.
[Authorize(Roles = AccountRoles.Staff)]
[Route("admin/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly IAdminReviewService _reviews;

    public ReviewsController(IAdminReviewService reviews)
    {
        _reviews = reviews;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        return Ok(await _reviews.GetAllAsync(cancellationToken));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetAsync(int id, CancellationToken cancellationToken)
    {
        return Ok(await _reviews.GetAsync(id, cancellationToken));
    }

    [Authorize(Roles = AccountRoles.SuperAdmin)]
    [HttpPost]
    public async Task<IActionResult> CreateAsync(ReviewRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _reviews.CreateAsync(request, cancellationToken));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> EditAsync(int id, ReviewRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _reviews.EditAsync(id, request, cancellationToken));
    }

    [Authorize(Roles = AccountRoles.SuperAdmin)]
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        await _reviews.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
