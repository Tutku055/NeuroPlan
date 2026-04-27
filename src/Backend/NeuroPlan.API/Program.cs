using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using NeuroPlan.API.Settings;
using NeuroPlan.Application;
using NeuroPlan.Domain.Constants;
using NeuroPlan.Domain.Entities;
using NeuroPlan.Infrastructure;
using NeuroPlan.Infrastructure.Seeding;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Ensure JWT Key is set
var keyParam = builder.Configuration["JwtSettings:Key"];
if (string.IsNullOrEmpty(keyParam)) keyParam = "SuperSecretAndLongKeyForJwtTokens123456!!";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "NeuroPlanApi",
            ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "NeuroPlanUsers",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyParam))
        };
    });

builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.ProjectsRead, policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context => HasAnyPermission(
                  context.User,
                  Permissions.ManageProjects)));

    options.AddPolicy(AuthorizationPolicies.ProjectsManage, policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context => HasAnyPermission(
                  context.User,
                  Permissions.ManageProjects)));

    options.AddPolicy(AuthorizationPolicies.ForecastAccess, policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context => HasAnyPermission(
                  context.User,
                  Permissions.AssessRisk)));

    options.AddPolicy(AuthorizationPolicies.TasksRead, policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context => HasAnyPermission(
                  context.User,
                  Permissions.TrackWork,
                  Permissions.ManageTaskItems)));

    options.AddPolicy(AuthorizationPolicies.TasksManage, policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context => HasAnyPermission(
                  context.User,
                  Permissions.ManageTaskItems)));

    options.AddPolicy(AuthorizationPolicies.WorklogsTrack, policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context => HasAnyPermission(
                  context.User,
                  Permissions.TrackWork)));

    options.AddPolicy(AuthorizationPolicies.PerformanceRead, policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context => HasAnyPermission(
                  context.User,
                  Permissions.ViewStatistics)));

    options.AddPolicy(AuthorizationPolicies.UsersManage, policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context => HasAnyPermission(
                  context.User,
                  Permissions.ManageUsers)));

    options.AddPolicy(AuthorizationPolicies.RolesManage, policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context => HasAnyPermission(
                  context.User,
                  Permissions.ManageRoles)));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVite",
        policy => policy.WithOrigins(
                            "http://localhost:5173",
                            "http://127.0.0.1:5173",
                            "http://localhost:5174",
                            "http://127.0.0.1:5174")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials());
});


builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register Custom Layers
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);

var app = builder.Build();

// Run migrations and deterministic seed data.
using (var scope = app.Services.CreateScope())
{
    var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
    await seeder.SeedAsync(app.Environment.IsDevelopment());
}


if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors("AllowVite");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static bool HasAnyPermission(ClaimsPrincipal user, params string[] requiredPermissions)
{
    var grantedPermissions = user.FindAll("Permission")
        .Select(claim => claim.Value)
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    if (grantedPermissions.Count == 0)
    {
        var packedClaim = user.FindFirst("Permissions")?.Value;
        if (!string.IsNullOrWhiteSpace(packedClaim))
        {
            foreach (var permission in packedClaim.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                grantedPermissions.Add(permission);
            }
        }
    }

    return requiredPermissions.Any(grantedPermissions.Contains);
}
