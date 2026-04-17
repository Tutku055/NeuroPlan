using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using NeuroPlan.API.Settings;
using NeuroPlan.Application;
using NeuroPlan.Domain.Constants;
using NeuroPlan.Domain.Entities;
using NeuroPlan.Infrastructure;
using NeuroPlan.Infrastructure.Persistence.Context;
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

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.ProjectsRead, policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context => HasAnyPermission(
                  context.User,
                  Permissions.ReadProjects,
                  Permissions.ManageProjects,
                  Permissions.CreateProject)));

    options.AddPolicy(AuthorizationPolicies.ProjectsManage, policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context => HasAnyPermission(
                  context.User,
                  Permissions.ManageProjects,
                  Permissions.CreateProject)));

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
                  Permissions.ManageTaskItems,
                  Permissions.ManageProjects,
                  Permissions.ReadProjects)));

    options.AddPolicy(AuthorizationPolicies.TasksManage, policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context => HasAnyPermission(
                  context.User,
                  Permissions.ManageTaskItems,
                  Permissions.ManageProjects)));

    options.AddPolicy(AuthorizationPolicies.WorklogsTrack, policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context => HasAnyPermission(
                  context.User,
                  Permissions.TrackWork,
                  Permissions.ManageTaskItems)));
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

// Add services to the container.
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

// Ensure database and seed deterministic auth data.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NeuroPlanDbContext>();
    db.Database.EnsureCreated();

    SeedAuthorizationData(db);
}

// Configure the HTTP request pipeline.
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

static void SeedAuthorizationData(NeuroPlanDbContext db)
{
    var adminRoleId = Guid.Parse("10000000-0000-0000-0000-000000000001");
    var workerRoleId = Guid.Parse("10000000-0000-0000-0000-000000000002");
    var managerRoleId = Guid.Parse("10000000-0000-0000-0000-000000000003");

    var manageProjectsPermissionId = Guid.Parse("20000000-0000-0000-0000-000000000001");
    var assessRiskPermissionId = Guid.Parse("20000000-0000-0000-0000-000000000002");
    var manageTaskItemsPermissionId = Guid.Parse("20000000-0000-0000-0000-000000000003");
    var trackWorkPermissionId = Guid.Parse("20000000-0000-0000-0000-000000000004");
    var createProjectPermissionId = Guid.Parse("20000000-0000-0000-0000-000000000005");
    var viewStatisticsPermissionId = Guid.Parse("20000000-0000-0000-0000-000000000006");
    var readProjectsPermissionId = Guid.Parse("20000000-0000-0000-0000-000000000007");

    UpsertRole(db, adminRoleId, "Admin");
    UpsertRole(db, workerRoleId, "Worker");
    UpsertRole(db, managerRoleId, "Manager");

    UpsertPermission(db, manageProjectsPermissionId, Permissions.ManageProjects, "Allows full project lifecycle management.");
    UpsertPermission(db, assessRiskPermissionId, Permissions.AssessRisk, "Allows running AI risk and forecast assessment.");
    UpsertPermission(db, manageTaskItemsPermissionId, Permissions.ManageTaskItems, "Allows task create/update/delete and state transitions.");
    UpsertPermission(db, trackWorkPermissionId, Permissions.TrackWork, "Allows starting and stopping worklogs.");
    UpsertPermission(db, createProjectPermissionId, Permissions.CreateProject, "Allows creating and editing projects.");
    UpsertPermission(db, viewStatisticsPermissionId, Permissions.ViewStatistics, "Allows reading project summary and statistics data.");
    UpsertPermission(db, readProjectsPermissionId, Permissions.ReadProjects, "Allows reading project and task listings.");

    var expectedRolePermissions = new HashSet<(Guid RoleId, Guid PermissionId)>
    {
        // Admin permissions (full access)
        (adminRoleId, manageProjectsPermissionId),
        (adminRoleId, assessRiskPermissionId),
        (adminRoleId, manageTaskItemsPermissionId),
        (adminRoleId, trackWorkPermissionId),
        (adminRoleId, createProjectPermissionId),
        (adminRoleId, viewStatisticsPermissionId),
        (adminRoleId, readProjectsPermissionId),

        // Worker permissions
        (workerRoleId, trackWorkPermissionId),
        (workerRoleId, readProjectsPermissionId),

        // Manager permissions
        (managerRoleId, createProjectPermissionId),
        (managerRoleId, viewStatisticsPermissionId),
        (managerRoleId, readProjectsPermissionId),
        (managerRoleId, manageTaskItemsPermissionId),
        (managerRoleId, assessRiskPermissionId)
    };

    var managedRoleIds = new HashSet<Guid> { adminRoleId, workerRoleId, managerRoleId };
    var existingRolePermissions = db.RolePermissions
        .Where(rp => managedRoleIds.Contains(rp.RoleId))
        .ToList();

    foreach (var staleLink in existingRolePermissions.Where(rp => !expectedRolePermissions.Contains((rp.RoleId, rp.PermissionId))))
    {
        db.RolePermissions.Remove(staleLink);
    }

    var existingRolePermissionSet = existingRolePermissions
        .Select(rp => (rp.RoleId, rp.PermissionId))
        .ToHashSet();

    foreach (var expectedLink in expectedRolePermissions)
    {
        if (!existingRolePermissionSet.Contains(expectedLink))
        {
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = expectedLink.RoleId,
                PermissionId = expectedLink.PermissionId
            });
        }
    }

    UpsertUser(db,
        userId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
        fullName: "Admin User",
        email: "admin@example.com",
        roleId: adminRoleId);

    UpsertUser(db,
        userId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
        fullName: "Worker User",
        email: "worker@example.com",
        roleId: workerRoleId);

    UpsertUser(db,
        userId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
        fullName: "Manager User",
        email: "manager@example.com",
        roleId: managerRoleId);

    RemoveUnassignedDemoRoles(db);
    db.SaveChanges();
}

static void UpsertRole(NeuroPlanDbContext db, Guid roleId, string roleName)
{
    var existingRole = db.Roles.IgnoreQueryFilters().FirstOrDefault(role => role.Id == roleId);
    if (existingRole is null)
    {
        db.Roles.Add(new Role
        {
            Id = roleId,
            Name = roleName
        });
        return;
    }

    existingRole.Name = roleName;
    existingRole.IsDeleted = false;
    existingRole.DeletedAt = null;
}

static void UpsertPermission(NeuroPlanDbContext db, Guid permissionId, string systemName, string description)
{
    var existingPermission = db.Permissions.IgnoreQueryFilters().FirstOrDefault(permission => permission.Id == permissionId);
    if (existingPermission is null)
    {
        db.Permissions.Add(new Permission
        {
            Id = permissionId,
            SystemName = systemName,
            Description = description
        });
        return;
    }

    existingPermission.SystemName = systemName;
    existingPermission.Description = description;
    existingPermission.IsDeleted = false;
    existingPermission.DeletedAt = null;
}

static void UpsertUser(NeuroPlanDbContext db, Guid userId, string fullName, string email, Guid roleId)
{
    var existingUser = db.Users.IgnoreQueryFilters()
        .FirstOrDefault(user => user.Id == userId || user.Email == email);

    if (existingUser is null)
    {
        db.Users.Add(new User
        {
            Id = userId,
            FullName = fullName,
            Email = email,
            PasswordHash = "hashed",
            RoleId = roleId
        });
        return;
    }

    existingUser.FullName = fullName;
    existingUser.Email = email;
    existingUser.PasswordHash = "hashed";
    existingUser.RoleId = roleId;
    existingUser.IsDeleted = false;
    existingUser.DeletedAt = null;
}

static void RemoveUnassignedDemoRoles(NeuroPlanDbContext db)
{
    var demoRoles = db.Roles.IgnoreQueryFilters()
        .Where(role => role.Name == "DemoRole")
        .ToList();

    foreach (var demoRole in demoRoles)
    {
        if (demoRole.IsDeleted)
        {
            continue;
        }

        var hasAssignedUsers = db.Users.IgnoreQueryFilters().Any(user => user.RoleId == demoRole.Id);
        if (hasAssignedUsers)
        {
            continue;
        }

        var danglingLinks = db.RolePermissions.Where(link => link.RoleId == demoRole.Id).ToList();
        if (danglingLinks.Count > 0)
        {
            db.RolePermissions.RemoveRange(danglingLinks);
        }

        demoRole.IsDeleted = true;
        demoRole.DeletedAt = DateTime.UtcNow;
    }
}
