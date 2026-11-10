using Service.Helpers.DTOs.Admin;

namespace Service.Services.Interfaces;

public interface IAdminPropertyService
{
    Task<IReadOnlyList<AdminPropertyDto>> GetAllAsync(CancellationToken cancellationToken);
    Task<AdminPropertyDto> GetAsync(string id, CancellationToken cancellationToken);
    Task<AdminPropertyDto> CreateAsync(AdminPropertyRequest request, CancellationToken cancellationToken);
    // canAddOrRemoveRooms is false for an Admin: they may change the rooms a stay has, but only a SuperAdmin adds or removes one.
    Task<AdminPropertyDto> EditAsync(string id, AdminPropertyRequest request, bool canAddOrRemoveRooms, CancellationToken cancellationToken);
    Task DeleteAsync(string id, CancellationToken cancellationToken);
}
