using Service.Helpers.DTOs.Messages;

namespace Service.Services.Interfaces;

public interface IMessageService
{
    Task<IReadOnlyList<GuestNote>> GetAllAsync(CancellationToken cancellationToken);
    Task<GuestNote> CreateAsync(MessageRequest request, CancellationToken cancellationToken);
}
