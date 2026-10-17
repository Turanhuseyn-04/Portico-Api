namespace Portico.Application.Messages;

public interface IGuestNotes
{
    Task<IReadOnlyList<GuestNote>> ListAsync(CancellationToken cancellationToken);
    Task<GuestNote> CreateAsync(MessageRequest request, CancellationToken cancellationToken);
}
