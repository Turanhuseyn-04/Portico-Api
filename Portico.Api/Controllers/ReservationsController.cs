using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portico.Api.Security;
using Portico.Domain.Accounts;
using Service.Helpers.DTOs.Reservations;
using Service.Services.Interfaces;

namespace Portico.Api.Controllers;

// Every reservation belongs to the account that booked it. A guest reaches only their own; staff reach them all.
[ApiController]
[Authorize]
[Route("reservations")]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservations;

    public ReservationsController(IReservationService reservations)
    {
        _reservations = reservations;
    }

    // The reservation is booked under the signed-in account's email, whatever the request says.
    [HttpPost]
    public async Task<IActionResult> CreateAsync(StayRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _reservations.CreateAsync(request, User.EmailOf() ?? "", cancellationToken));
    }

    // A guest lists their own trips. Every reservation, or another guest's, is for staff.
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
        return Ok(await _reservations.GetAsync(reference, OwnerFilter(), cancellationToken));
    }

    [HttpPost("{reference}/cancel")]
    public async Task<IActionResult> CancelAsync(string reference, CancellationToken cancellationToken)
    {
        return Ok(await _reservations.CancelAsync(reference, OwnerFilter(), cancellationToken));
    }

    // Null for staff, who may open any reservation; otherwise the email a reservation must be booked under.
    private string? OwnerFilter() => AccountRoles.IsStaff(User.RoleOf()) ? null : User.EmailOf() ?? "";
}
