namespace Portico.Application.Properties;

public interface IPropertyCatalog
{
    Task<IReadOnlyList<StayListItem>> ListAsync(CancellationToken cancellationToken);
}
