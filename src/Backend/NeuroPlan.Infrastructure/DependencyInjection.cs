using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NeuroPlan.Application.Interfaces;
using NeuroPlan.Domain.Interfaces;
using NeuroPlan.Infrastructure.ExternalServices;
using NeuroPlan.Infrastructure.Persistence.Context;
using NeuroPlan.Infrastructure.Persistence.Repositories;

namespace NeuroPlan.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add SQLite DbContext Setup
        services.AddDbContext<NeuroPlanDbContext>(options =>
            options.UseSqlite(configuration.GetConnectionString("DefaultConnection"),
            b => b.MigrationsAssembly(typeof(NeuroPlanDbContext).Assembly.FullName)));

        // Register Repositories
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped<ITaskItemRepository, TaskItemRepository>();
        services.AddScoped<IProjectRepository, ProjectRepository>();
        services.AddScoped<IWorklogRepository, WorklogRepository>();



        // Register AI Prediction Provider HttpClient pointing to Python FastAPI
        services.AddHttpClient<IAiPredictionProvider, AiPredictionProvider>(client =>
        {
            var baseUrl = configuration["AiSettings:BaseUrl"] ?? "http://localhost:8000/";
            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        });

        return services;
    }
}
