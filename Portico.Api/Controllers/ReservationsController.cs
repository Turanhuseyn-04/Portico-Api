using Microsoft.AspNetCore.Mvc;
using Portico.Application.Reservations;

namespace Portico.Api.Controllers;

[ApiController]
[Route("reservations")]
public class ReservationsController : ControllerBase
{
    private readonly IReservationBook _book;

    public ReservationsController(IReservationBook book)
    {
        _book = book;
    }

    [HttpPost]
    public async Task<ActionResult<ReservationCreated>> Create(StayRequest request, CancellationToken cancellationToken)
    {
        var result = await _book.CreateAsync(request, cancellationToken);
        return result switch
        {
            Booked booked => Ok(booked.Reservation),
            BookTaken => Conflict(),
            _ => NotFound()
        };
    }

    [HttpGet]
    public async Task<IReadOnlyList<TripDto>> List(CancellationToken cancellationToken)
    {
        return await _book.ListAsync(cancellationToken);
    }

    [HttpGet("{reference}")]
    public async Task<ActionResult<TripDto>> Get(string reference, CancellationToken cancellationToken)
    {
        var trip = await _book.GetAsync(reference, cancellationToken);
        return trip is null ? NotFound() : Ok(trip);
    }

    [HttpPost("{reference}/cancel")]
    public async Task<ActionResult<TripDto>> Cancel(string reference, CancellationToken cancellationToken)
    {
        var trip = await _book.CancelAsync(reference, cancellationToken);
        return trip is null ? NotFound() : Ok(trip);
    }
}
