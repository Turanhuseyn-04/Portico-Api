using Microsoft.Extensions.DependencyInjection;
using Repository.Repositories;
using Repository.Repositories.Interfaces;

namespace Repository;

public static class DependencyInjection
{
    public static IServiceCollection AddRepositoryLayer(this IServiceCollection services)
    {
        services.AddScoped<IPropertyRepository, PropertyRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();
        services.AddScoped<IAccountRepository, AccountRepository>();
        services.AddScoped<IMessageRepository, MessageRepository>();
        services.AddScoped<ISlideRepository, SlideRepository>();
        services.AddScoped<IVideoRepository, VideoRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IPropertyPhotoRepository, PropertyPhotoRepository>();
        services.AddScoped<IAdminPropertyRepository, AdminPropertyRepository>();
        services.AddScoped<ISiteSettingRepository, SiteSettingRepository>();
        services.AddScoped<IAuditRepository, AuditRepository>();
        return services;
    }
}
