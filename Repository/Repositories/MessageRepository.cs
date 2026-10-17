using Microsoft.EntityFrameworkCore;
using Portico.Domain.Messages;
using Repository.Data;
using Repository.Repositories.Interfaces;

namespace Repository.Repositories;

public class MessageRepository : IMessageRepository
{
    private readonly PorticoDbContext _db;

    public MessageRepository(PorticoDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<GuestMessage>> ListAsync(CancellationToken cancellationToken)
    {
        return await _db.Messages
            .AsNoTracking()
            .OrderBy(message => message.At)
            .ThenBy(message => message.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(GuestMessage message, CancellationToken cancellationToken)
    {
        await _db.Messages.AddAsync(message, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
