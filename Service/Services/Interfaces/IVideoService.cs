using Service.Helpers.DTOs.Admin;

namespace Service.Services.Interfaces;

public interface IVideoService
{
    Task<IReadOnlyList<VideoDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<VideoDto> GetAsync(int id, CancellationToken cancellationToken);
    Task<VideoDto> CreateAsync(VideoRequest request, CancellationToken cancellationToken);
    Task<VideoDto> EditAsync(int id, VideoRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
