using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Portico.Infrastructure.Persistence;

namespace Portico.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<PorticoDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("Portico")));

        return services;
    }
}
