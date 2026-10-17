using Portico.Domain.Messages;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Messages;
using Service.Services.Interfaces;

namespace Service.Services;

public class MessageService : IMessageService
{
    private readonly IMessageRepository _messages;

    public MessageService(IMessageRepository messages)
    {
        _messages = messages;
    }

    public async Task<IReadOnlyList<GuestNote>> GetAllAsync(CancellationToken cancellationToken)
    {
        var rows = await _messages.ListAsync(cancellationToken);
        return rows.Select(Map).ToArray();
    }

    public async Task<GuestNote> CreateAsync(MessageRequest request, CancellationToken cancellationToken)
    {
        var row = new GuestMessage
        {
            Name = request.Name.Trim(),
            Email = request.Email.Trim(),
            Stay = request.Stay.Trim(),
            Body = request.Message.Trim(),
            At = request.At ?? DateTimeOffset.UtcNow
        };
        await _messages.AddAsync(row, cancellationToken);
        return Map(row);
    }

    private static GuestNote Map(GuestMessage message)
    {
        return new GuestNote(message.Name, message.Email, message.Stay, message.Body, message.At);
    }
}
