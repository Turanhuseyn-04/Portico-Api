using Microsoft.AspNetCore.Mvc;
using Portico.Application.Messages;

namespace Portico.Api.Controllers;

[ApiController]
[Route("messages")]
public class MessagesController : ControllerBase
{
    private readonly IGuestNotes _notes;

    public MessagesController(IGuestNotes notes)
    {
        _notes = notes;
    }

    [HttpGet]
    public async Task<IReadOnlyList<GuestNote>> List(CancellationToken cancellationToken)
    {
        return await _notes.ListAsync(cancellationToken);
    }

    [HttpPost]
    public async Task<GuestNote> Create(MessageRequest request, CancellationToken cancellationToken)
    {
        return await _notes.CreateAsync(request, cancellationToken);
    }
}
