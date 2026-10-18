using Service.Helpers.DTOs.Admin;

namespace Service.Services.Interfaces;

public interface IAdminPhotoService
{
    Task<IReadOnlyList<PhotoDto>> GetAllAsync(string propertyId, CancellationToken cancellationToken);
    Task<PhotoDto> GetAsync(string propertyId, int id, CancellationToken cancellationToken);
    Task<PhotoDto> CreateAsync(string propertyId, PhotoRequest request, CancellationToken cancellationToken);
    Task<PhotoDto> EditAsync(string propertyId, int id, PhotoRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string propertyId, int id, CancellationToken cancellationToken);
}
