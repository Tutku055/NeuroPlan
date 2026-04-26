using Microsoft.Extensions.DependencyInjection;
using NeuroPlan.Application.Interfaces;
using NeuroPlan.Application.Services;

namespace NeuroPlan.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IForecastService, ForecastService>();
        services.AddScoped<IProjectService, ProjectService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<ITaskService, TaskService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IPerformanceService, PerformanceService>();
        services.AddScoped<IWorkTrackingService, WorkTrackingService>();

        return services;
    }
}
