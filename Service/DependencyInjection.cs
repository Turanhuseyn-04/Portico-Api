using Microsoft.Extensions.DependencyInjection;
using Service.Services;
using Service.Services.Interfaces;

namespace Service;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IMessageService, MessageService>();
        return services;
    }
}
