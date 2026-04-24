using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
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
using System.Data;
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

    options.AddPolicy(AuthorizationPolicies.PerformanceRead, policy =>
        policy.RequireAuthenticatedUser()
              .RequireAssertion(context => HasAnyPermission(
                  context.User,
                  Permissions.ViewStatistics,
                  Permissions.ManageProjects)));

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

// Ensure database and seed deterministic auth data.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NeuroPlanDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();

    if (app.Environment.IsDevelopment() && RequiresDevelopmentDatabaseRebuild(db))
    {
        db.Database.EnsureDeleted();
    }

    db.Database.Migrate();

    SeedAuthorizationData(db, passwordHasher);
    SeedProjectsAndTasks(db);
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

static void SeedAuthorizationData(NeuroPlanDbContext db, IPasswordHasher<User> passwordHasher)
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
    var manageUsersPermissionId = Guid.Parse("20000000-0000-0000-0000-000000000008");
    var manageRolesPermissionId = Guid.Parse("20000000-0000-0000-0000-000000000009");

    UpsertRole(db, adminRoleId, "Admin", "#EF4444");
    UpsertRole(db, workerRoleId, "Worker", "#10B981");
    UpsertRole(db, managerRoleId, "Manager", "#0EA5E9");

    UpsertPermission(db, manageProjectsPermissionId, Permissions.ManageProjects, "Manage projects");
    UpsertPermission(db, assessRiskPermissionId, Permissions.AssessRisk, "Assess risk");
    UpsertPermission(db, manageTaskItemsPermissionId, Permissions.ManageTaskItems, "Manage tasks");
    UpsertPermission(db, trackWorkPermissionId, Permissions.TrackWork, "Track work");
    UpsertPermission(db, createProjectPermissionId, Permissions.CreateProject, "Create projects");
    UpsertPermission(db, viewStatisticsPermissionId, Permissions.ViewStatistics, "View statistics");
    UpsertPermission(db, readProjectsPermissionId, Permissions.ReadProjects, "Read projects");
    UpsertPermission(db, manageUsersPermissionId, Permissions.ManageUsers, "Manage users");
    UpsertPermission(db, manageRolesPermissionId, Permissions.ManageRoles, "Manage roles");

    var expectedRolePermissions = new HashSet<(Guid RoleId, Guid PermissionId)>
    {
        (adminRoleId, manageProjectsPermissionId),
        (adminRoleId, assessRiskPermissionId),
        (adminRoleId, manageTaskItemsPermissionId),
        (adminRoleId, trackWorkPermissionId),
        (adminRoleId, createProjectPermissionId),
        (adminRoleId, viewStatisticsPermissionId),
        (adminRoleId, readProjectsPermissionId),
        (adminRoleId, manageUsersPermissionId),
        (adminRoleId, manageRolesPermissionId),

        (workerRoleId, trackWorkPermissionId),
        (workerRoleId, readProjectsPermissionId),

        (managerRoleId, createProjectPermissionId),
        (managerRoleId, viewStatisticsPermissionId),
        (managerRoleId, readProjectsPermissionId),
        (managerRoleId, manageTaskItemsPermissionId),
        (managerRoleId, assessRiskPermissionId),
        (managerRoleId, manageUsersPermissionId),
        (managerRoleId, manageRolesPermissionId)
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
        password: "admin",
        roleId: adminRoleId,
        passwordHasher: passwordHasher);

    UpsertUser(db,
        userId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
        fullName: "Worker User",
        email: "worker@example.com",
        password: "worker",
        roleId: workerRoleId,
        passwordHasher: passwordHasher);

    UpsertUser(db,
        userId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
        fullName: "Manager User",
        email: "manager@example.com",
        password: "manager",
        roleId: managerRoleId,
        passwordHasher: passwordHasher);

    RemoveUnassignedDemoRoles(db);
    db.SaveChanges();
}

static void SeedProjectsAndTasks(NeuroPlanDbContext db)
{
    var adminUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    var projectId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    var existingProject = db.Projects.IgnoreQueryFilters().FirstOrDefault(p => p.Id == projectId);
    if (existingProject is null)
    {
        var demoProject = new Project
        {
            Id = projectId,
            Name = "Demo Project",
            ProjectCode = "DP-001",
            Description = "This is a seeded demo project.",
            TargetEndDate = DateTime.UtcNow.AddDays(30)
        };
        db.Projects.Add(demoProject);
        db.SaveChanges(); // save to get valid FK for tasks

        var taskId1 = Guid.Parse("66666666-6666-6666-6666-666666666661");
        var task1 = new TaskItem
        {
            Id = taskId1,
            ProjectId = projectId,
            TaskCode = "DP-T01",
            Title = "Initial Setup",
            Description = "Set up the project structure.",
            ComplexityScore = 3,
            Status = NeuroPlan.Domain.Enums.EntityStatus.Completed,
            CompletedDate = DateTime.UtcNow.AddDays(-2)
        };
        db.TaskItems.Add(task1);

        var taskId2 = Guid.Parse("66666666-6666-6666-6666-666666666662");
        var task2 = new TaskItem
        {
            Id = taskId2,
            ProjectId = projectId,
            TaskCode = "DP-T02",
            Title = "Develop Core Features",
            Description = "Implement main features.",
            ComplexityScore = 8,
            Status = NeuroPlan.Domain.Enums.EntityStatus.InProgress
        };
        db.TaskItems.Add(task2);
        
        db.SaveChanges();

        // Seed some worklogs for performance data
        var worklog1 = new Worklog
        {
            Id = Guid.NewGuid(),
            TaskItemId = taskId1,
            UserId = adminUserId,
            StartTime = DateTime.UtcNow.AddDays(-3),
            EndTime = DateTime.UtcNow.AddDays(-3).AddHours(4) // 4 hours logged
        };
        db.Worklogs.Add(worklog1);

        var worklog2 = new Worklog
        {
            Id = Guid.NewGuid(),
            TaskItemId = taskId2,
            UserId = adminUserId,
            StartTime = DateTime.UtcNow.AddDays(-1),
            EndTime = DateTime.UtcNow.AddDays(-1).AddHours(2) // 2 hours logged
        };
        db.Worklogs.Add(worklog2);
        
        db.SaveChanges();
    }
}

static void UpsertRole(NeuroPlanDbContext db, Guid roleId, string roleName, string color)
{
    var existingRole = db.Roles.IgnoreQueryFilters().FirstOrDefault(role => role.Id == roleId);
    if (existingRole is null)
    {
        db.Roles.Add(new Role
        {
            Id = roleId,
            Name = roleName,
            Color = color
        });
        return;
    }

    existingRole.Name = roleName;
    existingRole.Color = color;
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

static void UpsertUser(
    NeuroPlanDbContext db,
    Guid userId,
    string fullName,
    string email,
    string password,
    Guid roleId,
    IPasswordHasher<User> passwordHasher)
{
    var existingUser = db.Users.IgnoreQueryFilters()
        .FirstOrDefault(user => user.Id == userId || user.Email == email);

    var normalizedPassword = string.IsNullOrWhiteSpace(password) ? "hashed" : password;

    if (existingUser is null)
    {
        var newUser = new User
        {
            Id = userId,
            FullName = fullName,
            Email = email,
            RoleId = roleId
        };

        newUser.PasswordHash = passwordHasher.HashPassword(newUser, normalizedPassword);
        db.Users.Add(newUser);
        return;
    }

    existingUser.FullName = fullName;
    existingUser.Email = email;
    existingUser.RoleId = roleId;
    existingUser.IsDeleted = false;
    existingUser.DeletedAt = null;

    // Only update password if it's not the placeholder or if we explicitly want to force it
    if (password != null)
    {
        existingUser.PasswordHash = passwordHasher.HashPassword(existingUser, normalizedPassword);
    }
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

static bool RequiresDevelopmentDatabaseRebuild(NeuroPlanDbContext db)
{
    using var connection = db.Database.GetDbConnection();
    if (connection.State != ConnectionState.Open)
    {
        connection.Open();
    }

    using var tableCountCommand = connection.CreateCommand();
    tableCountCommand.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';";
    var tableCount = Convert.ToInt32(tableCountCommand.ExecuteScalar() ?? 0);
    if (tableCount == 0)
    {
        return false;
    }

    using var historyCommand = connection.CreateCommand();
    historyCommand.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name='__EFMigrationsHistory';";
    var hasMigrationHistory = Convert.ToInt32(historyCommand.ExecuteScalar() ?? 0) > 0;
    if (!hasMigrationHistory)
    {
        return true;
    }

    using var colorColumnCommand = connection.CreateCommand();
    colorColumnCommand.CommandText = "SELECT COUNT(*) FROM pragma_table_info('Roles') WHERE name='Color';";
    var hasRoleColorColumn = Convert.ToInt32(colorColumnCommand.ExecuteScalar() ?? 0) > 0;

    return !hasRoleColorColumn;
}
