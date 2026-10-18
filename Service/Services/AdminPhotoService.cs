using Portico.Domain.Properties;
using Repository.Repositories.Interfaces;
using Service.Helpers.DTOs.Admin;
using Service.Helpers.Exceptions;
using Service.Services.Interfaces;

namespace Service.Services;

public class AdminPhotoService : IAdminPhotoService
{
    private readonly IPropertyPhotoRepository _photos;
    private readonly IAdminPropertyRepository _properties;

    public AdminPhotoService(IPropertyPhotoRepository photos, IAdminPropertyRepository properties)
    {
        _photos = photos;
        _properties = properties;
    }

    public async Task<IReadOnlyList<PhotoDto>> GetAllAsync(string propertyId, CancellationToken cancellationToken)
    {
        if (!await _properties.ExistsAsync(propertyId, cancellationToken))
            throw new NotFoundException("That stay does not exist.");
        var rows = await _photos.ListAsync(propertyId, cancellationToken);
        return rows.Select(Map).ToArray();
    }

    public async Task<PhotoDto> GetAsync(string propertyId, int id, CancellationToken cancellationToken)
    {
        var photo = await _photos.FindAsync(propertyId, id, cancellationToken);
        if (photo is null) throw new NotFoundException("That photo does not exist.");
        return Map(photo);
    }

    public async Task<PhotoDto> CreateAsync(string propertyId, PhotoRequest request, CancellationToken cancellationToken)
    {
        if (!await _properties.ExistsAsync(propertyId, cancellationToken))
            throw new NotFoundException("That stay does not exist.");
        var photo = new PropertyPhoto { PropertyId = propertyId };
        Apply(photo, request);
        await _photos.AddAsync(photo, cancellationToken);
        await _photos.SaveAsync(cancellationToken);
        return Map(photo);
    }

    public async Task<PhotoDto> EditAsync(string propertyId, int id, PhotoRequest request, CancellationToken cancellationToken)
    {
        var photo = await _photos.FindAsync(propertyId, id, cancellationToken);
        if (photo is null) throw new NotFoundException("That photo does not exist.");
        Apply(photo, request);
        await _photos.SaveAsync(cancellationToken);
        return Map(photo);
    }

    public async Task DeleteAsync(string propertyId, int id, CancellationToken cancellationToken)
    {
        var photo = await _photos.FindAsync(propertyId, id, cancellationToken);
        if (photo is null) throw new NotFoundException("That photo does not exist.");
        _photos.Remove(photo);
        await _photos.SaveAsync(cancellationToken);
    }

    private static void Apply(PropertyPhoto photo, PhotoRequest request)
    {
        photo.Src = request.Src.Trim();
        photo.Alt = request.Alt.Trim();
        photo.Order = request.Order;
    }

    private static PhotoDto Map(PropertyPhoto photo) => new(photo.Id, photo.Src, photo.Alt, photo.Order);
}
