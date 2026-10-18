using Service.Helpers.DTOs.Admin;

namespace Service.Services.Interfaces;

public interface IAdminPropertyService
{
    Task<IReadOnlyList<AdminPropertyDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<AdminPropertyDto> GetAsync(string id, CancellationToken cancellationToken);
    Task<AdminPropertyDto> CreateAsync(AdminPropertyRequest request, CancellationToken cancellationToken);
    Task<AdminPropertyDto> EditAsync(string id, AdminPropertyRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string id, CancellationToken cancellationToken);
}
