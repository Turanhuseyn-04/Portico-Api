using Microsoft.EntityFrameworkCore;
using Portico.Application.Messages;
using Portico.Domain.Messages;
using Portico.Infrastructure.Persistence;

namespace Portico.Infrastructure.Messages;

public sealed class GuestNotes : IGuestNotes
{
    private readonly PorticoDbContext _db;

    public GuestNotes(PorticoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<GuestNote>> ListAsync(CancellationToken cancellationToken)
    {
        var rows = await _db.Messages
            .AsNoTracking()
            .OrderBy(message => message.At)
            .ThenBy(message => message.Id)
            .ToListAsync(cancellationToken);
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
        _db.Messages.Add(row);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(row);
    }

    private static GuestNote Map(GuestMessage message)
    {
        return new GuestNote(message.Name, message.Email, message.Stay, message.Body, message.At);
    }
}
