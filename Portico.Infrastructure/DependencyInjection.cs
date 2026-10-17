using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portico.Application.Properties;
using Portico.Infrastructure.Persistence;
using Portico.Infrastructure.Properties;

namespace Portico.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PorticoDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Portico")));
        services.AddScoped<IPropertyCatalog, PropertyCatalog>();

        return services;
    }
}
