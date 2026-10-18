using Service.Helpers.DTOs.Admin;

namespace Service.Services.Interfaces;

public interface IAdminReviewService
{
    Task<IReadOnlyList<ReviewDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<ReviewDto> GetAsync(int id, CancellationToken cancellationToken);
    Task<ReviewDto> CreateAsync(ReviewRequest request, CancellationToken cancellationToken);
    Task<ReviewDto> EditAsync(int id, ReviewRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(int id, CancellationToken cancellationToken);
}
