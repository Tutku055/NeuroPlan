using Microsoft.Extensions.DependencyInjection;
using NeuroPlan.Application.Interfaces;
using NeuroPlan.Application.Services;

namespace NeuroPlan.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IForecastService, ForecastService>();
        services.AddScoped<IWorkTrackingService, WorkTrackingService>();

        return services;
    }
}
