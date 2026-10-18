using Portico.Domain.Properties;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Admin;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services;

public class AdminReviewService : IAdminReviewService
{
    private readonly IReviewRepository _reviews;
    private readonly IAdminPropertyRepository _properties;

    public AdminReviewService(IReviewRepository reviews, IAdminPropertyRepository properties)
    {
        _reviews = reviews;
        _properties = properties;
    }

    public async Task<IReadOnlyList<ReviewDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var rows = await _reviews.ListAsync(cancellationToken);
        return rows.Select(Map).ToArray();
    }

    public async Task<ReviewDto> GetAsync(int id, CancellationToken cancellationToken)
    {
        var review = await _reviews.FindAsync(id, cancellationToken);
        if (review is null) throw new NotFoundException("That review does not exist.");
        return Map(review);
    }

    public async Task<ReviewDto> CreateAsync(ReviewRequest request, CancellationToken cancellationToken)
    {
        if (!await _properties.ExistsAsync(request.PropertyId.Trim(), cancellationToken))
            throw new NotFoundException("That stay does not exist.");

        var review = new Review();
        Apply(review, request);
        await _reviews.AddAsync(review, cancellationToken);
        await _reviews.SaveAsync(cancellationToken);
        return Map(review);
    }

    public async Task<ReviewDto> EditAsync(int id, ReviewRequest request, CancellationToken cancellationToken)
    {
        var review = await _reviews.FindAsync(id, cancellationToken);
        if (review is null) throw new NotFoundException("That review does not exist.");
        if (!await _properties.ExistsAsync(request.PropertyId.Trim(), cancellationToken))
            throw new NotFoundException("That stay does not exist.");
        Apply(review, request);
        await _reviews.SaveAsync(cancellationToken);
        return Map(review);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var review = await _reviews.FindAsync(id, cancellationToken);
        if (review is null) throw new NotFoundException("That review does not exist.");
        _reviews.Remove(review);
        await _reviews.SaveAsync(cancellationToken);
    }

    private static void Apply(Review review, ReviewRequest request)
    {
        review.PropertyId = request.PropertyId.Trim();
        review.RoomId = string.IsNullOrWhiteSpace(request.RoomId) ? null : request.RoomId.Trim();
        review.Author = request.Author.Trim();
        review.Country = request.Country.Trim();
        review.Traveller = request.Traveller.Trim();
        review.Nights = request.Nights;
        review.Date = request.Date;
        review.Score = request.Score;
        review.Title = request.Title.Trim();
        review.Liked = request.Liked.Trim();
        review.Disliked = request.Disliked.Trim();
    }

    private static ReviewDto Map(Review review)
    {
        return new ReviewDto(
            review.Id,
            review.PropertyId,
            review.RoomId,
            review.Author,
            review.Country,
            review.Traveller,
            review.Nights,
            review.Date,
            review.Score,
            review.Title,
            review.Liked,
            review.Disliked);
    }
}
