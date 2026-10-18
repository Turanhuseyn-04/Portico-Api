using Service.Helpers.DTOs.Admin;

namespace Service.Services.Interfaces;

public interface ISlideService
{
    Task<IReadOnlyList<SlideDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<SlideDto> GetAsync(int id, CancellationToken cancellationToken);
    Task<SlideDto> CreateAsync(SlideRequest request, CancellationToken cancellationToken);
    Task<SlideDto> EditAsync(int id, SlideRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
