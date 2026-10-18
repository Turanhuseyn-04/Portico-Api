using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portico.Api.Security;
using Portico.Domain.Accounts;
using Service.Helpers.DTOs.Reservations;
using Service.Services.Interfaces;

namespace Portico.Api.Controllers;

[ApiController]
[Route("reservations")]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservations;

    public ReservationsController(IReservationService reservations)
    {
        _reservations = reservations;
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(StayRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _reservations.CreateAsync(request, cancellationToken));
    }

    // A guest lists their own trips. Every reservation, or another guest's, is for staff.
    [Authorize]
    [HttpGet]
    public async Task<IActionResult> GetAllAsync([FromQuery] string? email, CancellationToken cancellationToken)
    {
        var staff = AccountRoles.IsStaff(User.RoleOf());
        if (string.IsNullOrWhiteSpace(email) ? !staff : !(staff || User.IsSelf(email))) return Forbid();
        return Ok(await _reservations.GetAllAsync(email, cancellationToken));
    }

    [HttpGet("{reference}")]
    public async Task<IActionResult> GetAsync(string reference, CancellationToken cancellationToken)
    {
        return Ok(await _reservations.GetAsync(reference, cancellationToken));
    }

    [HttpPost("{reference}/cancel")]
    public async Task<IActionResult> CancelAsync(string reference, CancellationToken cancellationToken)
    {
        return Ok(await _reservations.CancelAsync(reference, cancellationToken));
    }
}
