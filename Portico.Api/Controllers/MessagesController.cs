using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portico.Domain.Accounts;
using Service.Helpers.DTOs.Messages;
using Service.Services.Interfaces;

namespace Portico.Api.Controllers;

[ApiController]
[Route("messages")]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messages;

    public MessagesController(IMessageService messages)
    {
        _messages = messages;
    }

    // Anyone can send a note from Help; only staff read them.
    [Authorize(Roles = AccountRoles.Staff)]
    [HttpGet]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        return Ok(await _messages.GetAllAsync(cancellationToken));
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(MessageRequest request, CancellationToken cancellationToken)
    {
        return Ok(await _messages.CreateAsync(request, cancellationToken));
    }
}
