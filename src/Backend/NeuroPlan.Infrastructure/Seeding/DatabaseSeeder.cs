using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NeuroPlan.Domain.Constants;
using NeuroPlan.Domain.Entities;
using NeuroPlan.Infrastructure.Persistence.Context;

namespace NeuroPlan.Infrastructure.Seeding;

public class DatabaseSeeder : IDatabaseSeeder
{
    private readonly NeuroPlanDbContext _db;
    private readonly IPasswordHasher<User> _passwordHasher;

    public DatabaseSeeder(NeuroPlanDbContext db, IPasswordHasher<User> passwordHasher)
    {
        _db = db;
        _passwordHasher = passwordHasher;
    }

    public async Task SeedAsync(bool isDevelopment, CancellationToken cancellationToken = default)
    {
        if (isDevelopment && RequiresDevelopmentDatabaseRebuild(_db))
        {
            await _db.Database.EnsureDeletedAsync(cancellationToken);
        }

        await _db.Database.MigrateAsync(cancellationToken);

        SeedAuthorizationData(_db, _passwordHasher);
        SeedProjectsAndTasks(_db);
    }

    private static void SeedAuthorizationData(NeuroPlanDbContext db, IPasswordHasher<User> passwordHasher)
    {
        var adminRoleId = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var workerRoleId = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var managerRoleId = Guid.Parse("10000000-0000-0000-0000-000000000003");

        var manageProjectsPermissionId = Guid.Parse("20000000-0000-0000-0000-000000000001");
        var assessRiskPermissionId = Guid.Parse("20000000-0000-0000-0000-000000000002");
        var manageTaskItemsPermissionId = Guid.Parse("20000000-0000-0000-0000-000000000003");
        var trackWorkPermissionId = Guid.Parse("20000000-0000-0000-0000-000000000004");
        var viewStatisticsPermissionId = Guid.Parse("20000000-0000-0000-0000-000000000006");
        var manageUsersPermissionId = Guid.Parse("20000000-0000-0000-0000-000000000008");
        var manageRolesPermissionId = Guid.Parse("20000000-0000-0000-0000-000000000009");

        UpsertRole(db, adminRoleId, "Admin", "#EF4444");
        UpsertRole(db, workerRoleId, "Worker", "#10B981");
        UpsertRole(db, managerRoleId, "Manager", "#0EA5E9");

        UpsertPermission(db, manageProjectsPermissionId, Permissions.ManageProjects, "Manage projects");
        UpsertPermission(db, assessRiskPermissionId, Permissions.AssessRisk, "Assess risk");
        UpsertPermission(db, manageTaskItemsPermissionId, Permissions.ManageTaskItems, "Manage tasks");
        UpsertPermission(db, trackWorkPermissionId, Permissions.TrackWork, "Track work");
        UpsertPermission(db, viewStatisticsPermissionId, Permissions.ViewStatistics, "View statistics");
        UpsertPermission(db, manageUsersPermissionId, Permissions.ManageUsers, "Manage users");
        UpsertPermission(db, manageRolesPermissionId, Permissions.ManageRoles, "Manage roles");

        var expectedPermissionIds = new HashSet<Guid>
        {
            manageProjectsPermissionId,
            assessRiskPermissionId,
            manageTaskItemsPermissionId,
            trackWorkPermissionId,
            viewStatisticsPermissionId,
            manageUsersPermissionId,
            manageRolesPermissionId
        };

        var stalePermissions = db.Permissions.IgnoreQueryFilters().Where(p => !expectedPermissionIds.Contains(p.Id)).ToList();
        foreach (var p in stalePermissions)
        {
            if (!p.IsDeleted)
            {
                p.SoftDelete(DateTime.UtcNow);
            }
        }

        var expectedRolePermissions = new HashSet<(Guid RoleId, Guid PermissionId)>
        {
            (adminRoleId, manageProjectsPermissionId),
            (adminRoleId, assessRiskPermissionId),
            (adminRoleId, manageTaskItemsPermissionId),
            (adminRoleId, trackWorkPermissionId),
            (adminRoleId, viewStatisticsPermissionId),
            (adminRoleId, manageUsersPermissionId),
            (adminRoleId, manageRolesPermissionId),

            (workerRoleId, trackWorkPermissionId),
            (workerRoleId, manageProjectsPermissionId),

            (managerRoleId, manageProjectsPermissionId),
            (managerRoleId, viewStatisticsPermissionId),
            (managerRoleId, manageTaskItemsPermissionId),
            (managerRoleId, assessRiskPermissionId),
            (managerRoleId, manageUsersPermissionId),
            (managerRoleId, manageRolesPermissionId)
        };

        var managedRoleIds = new HashSet<Guid> { adminRoleId, workerRoleId, managerRoleId };
        var existingRolePermissions = db.RolePermissions
            .Where(rolePermission => managedRoleIds.Contains(rolePermission.RoleId))
            .ToList();

        foreach (var staleLink in existingRolePermissions.Where(rolePermission => !expectedRolePermissions.Contains((rolePermission.RoleId, rolePermission.PermissionId))))
        {
            db.RolePermissions.Remove(staleLink);
        }

        var existingRolePermissionSet = existingRolePermissions
            .Select(rolePermission => (rolePermission.RoleId, rolePermission.PermissionId))
            .ToHashSet();

        foreach (var expectedLink in expectedRolePermissions)
        {
            if (!existingRolePermissionSet.Contains(expectedLink))
            {
                db.RolePermissions.Add(new RolePermission(expectedLink.RoleId, expectedLink.PermissionId));
            }
        }

        UpsertUser(
            db,
            userId: Guid.Parse("11111111-1111-1111-1111-111111111111"),
            fullName: "Admin User",
            email: "admin@example.com",
            password: "admin",
            roleId: adminRoleId,
            passwordHasher: passwordHasher);

        UpsertUser(
            db,
            userId: Guid.Parse("22222222-2222-2222-2222-222222222222"),
            fullName: "Worker User",
            email: "worker@example.com",
            password: "worker",
            roleId: workerRoleId,
            passwordHasher: passwordHasher);

        UpsertUser(
            db,
            userId: Guid.Parse("33333333-3333-3333-3333-333333333333"),
            fullName: "Manager User",
            email: "manager@example.com",
            password: "manager",
            roleId: managerRoleId,
            passwordHasher: passwordHasher);

        RemoveUnassignedDemoRoles(db);
        db.SaveChanges();
    }

    private static void SeedProjectsAndTasks(NeuroPlanDbContext db)
    {
        var adminUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var projectId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var existingProject = db.Projects.IgnoreQueryFilters().FirstOrDefault(project => project.Id == projectId);
        if (existingProject is null)
        {
            var demoProject = new Project(
                projectId,
                "Demo Project",
                "DP-001",
                "This is a seeded demo project.",
                DateTime.UtcNow.AddDays(30));
            db.Projects.Add(demoProject);
            db.SaveChanges();

            var taskId1 = Guid.Parse("66666666-6666-6666-6666-666666666661");
            var task1 = new TaskItem(
                taskId1,
                projectId,
                "DP-T01",
                "Initial Setup",
                "Set up the project structure.",
                3);
            task1.MarkCompleted(DateTime.UtcNow.AddDays(-2));
            db.TaskItems.Add(task1);

            var taskId2 = Guid.Parse("66666666-6666-6666-6666-666666666662");
            var task2 = new TaskItem(
                taskId2,
                projectId,
                "DP-T02",
                "Develop Core Features",
                "Implement main features.",
                8);
            task2.ChangeStatus(NeuroPlan.Domain.Enums.EntityStatus.InProgress);
            db.TaskItems.Add(task2);

            db.SaveChanges();

            var worklog1 = new Worklog(
                Guid.NewGuid(),
                taskId1,
                adminUserId,
                DateTime.UtcNow.AddDays(-3));
            worklog1.End(DateTime.UtcNow.AddDays(-3).AddHours(4));
            db.Worklogs.Add(worklog1);

            var worklog2 = new Worklog(
                Guid.NewGuid(),
                taskId2,
                adminUserId,
                DateTime.UtcNow.AddDays(-1));
            worklog2.End(DateTime.UtcNow.AddDays(-1).AddHours(2));
            db.Worklogs.Add(worklog2);

            db.SaveChanges();
        }
    }

    private static void UpsertRole(NeuroPlanDbContext db, Guid roleId, string roleName, string color)
    {
        var existingRole = db.Roles.IgnoreQueryFilters().FirstOrDefault(role => role.Id == roleId);
        if (existingRole is null)
        {
            db.Roles.Add(new Role(roleId, roleName, color));
            return;
        }

        existingRole.UpdateDetails(roleName, color);
        existingRole.Restore();
    }

    private static void UpsertPermission(NeuroPlanDbContext db, Guid permissionId, string systemName, string description)
    {
        var existingPermission = db.Permissions.IgnoreQueryFilters().FirstOrDefault(permission => permission.Id == permissionId);
        if (existingPermission is null)
        {
            db.Permissions.Add(new Permission(permissionId, systemName, description));
            return;
        }

        existingPermission.UpdateDetails(systemName, description);
        existingPermission.Restore();
    }

    private static void UpsertUser(
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
            var newUser = new User(userId, fullName, email, roleId);
            newUser.SetPasswordHash(passwordHasher.HashPassword(newUser, normalizedPassword));
            db.Users.Add(newUser);
            return;
        }

        existingUser.UpdateProfile(fullName, email, roleId);
        existingUser.Restore();

        if (password != null)
        {
            existingUser.SetPasswordHash(passwordHasher.HashPassword(existingUser, normalizedPassword));
        }
    }

    private static void RemoveUnassignedDemoRoles(NeuroPlanDbContext db)
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

            demoRole.SoftDelete(DateTime.UtcNow);
        }
    }

    private static bool RequiresDevelopmentDatabaseRebuild(NeuroPlanDbContext db)
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
}
