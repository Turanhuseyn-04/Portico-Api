using Portico.Domain.Content;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Admin;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services;

public class VideoService : IVideoService
{
    private readonly IVideoRepository _videos;

    public VideoService(IVideoRepository videos)
    {
        _videos = videos;
    }

    public async Task<IReadOnlyList<VideoDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var rows = await _videos.ListAsync(cancellationToken);
        return rows.Select(Map).ToArray();
    }

    public async Task<VideoDto> GetAsync(int id, CancellationToken cancellationToken)
    {
        var video = await _videos.FindAsync(id, cancellationToken);
        if (video is null) throw new NotFoundException("That video does not exist.");
        return Map(video);
    }

    public async Task<VideoDto> CreateAsync(VideoRequest request, CancellationToken cancellationToken)
    {
        var video = new SiteVideo();
        Apply(video, request);
        await _videos.AddAsync(video, cancellationToken);
        await _videos.SaveAsync(cancellationToken);
        return Map(video);
    }

    public async Task<VideoDto> EditAsync(int id, VideoRequest request, CancellationToken cancellationToken)
    {
        var video = await _videos.FindAsync(id, cancellationToken);
        if (video is null) throw new NotFoundException("That video does not exist.");
        Apply(video, request);
        await _videos.SaveAsync(cancellationToken);
        return Map(video);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var video = await _videos.FindAsync(id, cancellationToken);
        if (video is null) throw new NotFoundException("That video does not exist.");
        _videos.Remove(video);
        await _videos.SaveAsync(cancellationToken);
    }

    private static void Apply(SiteVideo video, VideoRequest request)
    {
        video.Src = request.Src.Trim();
        video.Order = request.Order;
    }

    private static VideoDto Map(SiteVideo video) => new(video.Id, video.Src, video.Order);
}
