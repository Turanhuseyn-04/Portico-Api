using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Portico.Domain.Accounts;
using Service.Services;
using Service.Services.Interfaces;

namespace Service;

public static class DependencyInjection
{
    public static IServiceCollection AddServiceLayer(this IServiceCollection services)
    {
        services.AddScoped<IPropertyService, PropertyService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IPasswordHasher<Account>, PasswordHasher<Account>>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<ISlideService, SlideService>();
        services.AddScoped<IVideoService, VideoService>();
        services.AddScoped<IAdminReviewService, AdminReviewService>();
        services.AddScoped<IAdminPropertyService, AdminPropertyService>();
        services.AddScoped<IAdminPhotoService, AdminPhotoService>();
        services.AddScoped<IAccountAdminService, AccountAdminService>();
        services.AddScoped<ISettingService, SettingService>();
        services.AddScoped<IAuditService, AuditService>();
        return services;
    }
}
