using Portico.Domain.Content;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Admin;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services;

public class SlideService : ISlideService
{
    private readonly ISlideRepository _slides;

    public SlideService(ISlideRepository slides)
    {
        _slides = slides;
    }

    public async Task<IReadOnlyList<SlideDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var rows = await _slides.ListAsync(cancellationToken);
        return rows.Select(Map).ToArray();
    }

    public async Task<SlideDto> GetAsync(int id, CancellationToken cancellationToken)
    {
        var slide = await _slides.FindAsync(id, cancellationToken);
        if (slide is null) throw new NotFoundException("That slide does not exist.");
        return Map(slide);
    }

    public async Task<SlideDto> CreateAsync(SlideRequest request, CancellationToken cancellationToken)
    {
        var slide = new Slide();
        Apply(slide, request);
        await _slides.AddAsync(slide, cancellationToken);
        await _slides.SaveAsync(cancellationToken);
        return Map(slide);
    }

    public async Task<SlideDto> EditAsync(int id, SlideRequest request, CancellationToken cancellationToken)
    {
        var slide = await _slides.FindAsync(id, cancellationToken);
        if (slide is null) throw new NotFoundException("That slide does not exist.");
        Apply(slide, request);
        await _slides.SaveAsync(cancellationToken);
        return Map(slide);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var slide = await _slides.FindAsync(id, cancellationToken);
        if (slide is null) throw new NotFoundException("That slide does not exist.");
        _slides.Remove(slide);
        await _slides.SaveAsync(cancellationToken);
    }

    private static void Apply(Slide slide, SlideRequest request)
    {
        slide.City = request.City.Trim();
        slide.Country = request.Country.Trim();
        slide.Image = request.Image.Trim();
        slide.Stays = request.Stays;
        slide.Order = request.Order;
    }

    private static SlideDto Map(Slide slide) => new(slide.Id, slide.City, slide.Country, slide.Image, slide.Stays, slide.Order);
}
