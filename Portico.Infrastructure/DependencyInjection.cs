using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portico.Application.Properties;
using Portico.Application.Reservations;
using Portico.Infrastructure.Persistence;
using Portico.Infrastructure.Properties;
using Portico.Infrastructure.Reservations;

namespace Portico.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PorticoDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Portico")));
        services.AddScoped<IPropertyCatalog, PropertyCatalog>();
        services.AddScoped<IReservationBook, ReservationBook>();

        return services;
    }
}
