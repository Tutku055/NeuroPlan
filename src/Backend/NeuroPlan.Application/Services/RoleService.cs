using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NeuroPlan.Application.DTOs;
using NeuroPlan.Application.Interfaces;
using NeuroPlan.Domain.Constants;
using NeuroPlan.Domain.Entities;
using NeuroPlan.Domain.Interfaces;

namespace NeuroPlan.Application.Services;

public class RoleService : IRoleService
{
    private const string DefaultRoleColor = "#64748B";
    private static readonly Regex HexColorRegex = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);
    private static readonly IReadOnlyDictionary<string, (string Label, string Description)> PermissionDisplayMetadata =
        new Dictionary<string, (string Label, string Description)>(StringComparer.OrdinalIgnoreCase)
        {
            [Permissions.ManageProjects] = (
                "Manage Projects",
                "View, create, update, and delete projects."),
            [Permissions.AssessRisk] = (
                "Assess Risk",
                "Run AI forecast and risk analysis for projects."),
            [Permissions.ManageTaskItems] = (
                "Manage Tasks",
                "Create, update, delete, and change the status of tasks."),
            [Permissions.TrackWork] = (
                "Track Worklogs",
                "Start and stop worklogs for in-progress tasks."),
            [Permissions.ViewStatistics] = (
                "View Performance",
                "Access project and team performance analytics."),
            [Permissions.ManageUsers] = (
                "Manage Users",
                "Create, update, and deactivate user accounts."),
            [Permissions.ManageRoles] = (
                "Manage Roles",
                "Create roles and assign permissions to roles.")
        };

    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly IRolePermissionRepository _rolePermissionRepository;
    private readonly IUserRepository _userRepository;

    public RoleService(
        IRoleRepository roleRepository,
        IPermissionRepository permissionRepository,
        IRolePermissionRepository rolePermissionRepository,
        IUserRepository userRepository)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _rolePermissionRepository = rolePermissionRepository;
        _userRepository = userRepository;
    }

    public async Task<IEnumerable<RoleResponseDto>> GetAllAsync()
    {
        var roles = await _roleRepository.GetAllWithPermissionsAsync();
        return roles.Select(MapToDto);
    }

    public async Task<RoleResponseDto?> GetByIdAsync(Guid id)
    {
        var role = await _roleRepository.GetByIdWithPermissionsAsync(id);
        return role is null ? null : MapToDto(role);
    }

    public async Task<RoleResponseDto> CreateAsync(CreateRoleRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required.");
        }

        var normalizedColor = NormalizeRoleColor(request.Color);
        if (normalizedColor is null)
        {
            throw new ArgumentException("Color must be a valid hex value like #3B82F6.");
        }

        var role = new Role(request.Name.Trim(), normalizedColor);

        await _roleRepository.AddAsync(role);

        if (request.PermissionIds.Length > 0)
        {
            foreach (var permissionId in request.PermissionIds.Distinct())
            {
                await _rolePermissionRepository.AddAsync(new RolePermission(role.Id, permissionId));
            }
        }

        var createdRole = await _roleRepository.GetByIdWithPermissionsAsync(role.Id) ?? role;
        return MapToDto(createdRole);
    }

    public async Task UpdateAsync(Guid id, UpdateRoleRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required.");
        }

        var normalizedColor = NormalizeRoleColor(request.Color);
        if (normalizedColor is null)
        {
            throw new ArgumentException("Color must be a valid hex value like #3B82F6.");
        }

        var existing = await _roleRepository.GetByIdWithPermissionsAsync(id);
        if (existing is null)
        {
            throw new KeyNotFoundException();
        }

        existing.UpdateDetails(request.Name.Trim(), normalizedColor);

        var currentPermissionIds = existing.RolePermissions.Select(link => link.PermissionId).ToHashSet();
        var newPermissionIds = request.PermissionIds.Distinct().ToHashSet();

        var toRemove = existing.RolePermissions.Where(link => !newPermissionIds.Contains(link.PermissionId)).ToList();
        foreach (var link in toRemove)
        {
            await _rolePermissionRepository.RemoveAsync(link);
        }

        foreach (var permissionId in newPermissionIds.Where(permissionId => !currentPermissionIds.Contains(permissionId)))
        {
            await _rolePermissionRepository.AddAsync(new RolePermission(id, permissionId));
        }

        await _roleRepository.UpdateAsync(existing);
    }

    public async Task DeleteAsync(Guid id)
    {
        var existing = await _roleRepository.GetByIdWithPermissionsAsync(id);
        if (existing is null)
        {
            throw new KeyNotFoundException();
        }

        var hasUsers = (await _userRepository.FindAsync(user => user.RoleId == id)).Any();
        if (hasUsers)
        {
            throw new InvalidOperationException("Cannot delete this role because it is assigned to active users.");
        }

        var links = await _rolePermissionRepository.GetByRoleIdAsync(id);
        await _rolePermissionRepository.RemoveRangeAsync(links);

        await _roleRepository.DeleteAsync(existing);
    }

    public async Task<IEnumerable<PermissionResponseDto>> GetAllPermissionsAsync()
    {
        var permissions = await _permissionRepository.GetAllAsync();
        return permissions.Select(permission => new PermissionResponseDto
        {
            Id = permission.Id,
            SystemName = permission.SystemName,
            Label = GetPermissionLabel(permission.SystemName, permission.Description),
            Description = GetPermissionDescription(permission.SystemName, permission.Description)
        });
    }

    private static RoleResponseDto MapToDto(Role role)
    {
        return new RoleResponseDto
        {
            Id = role.Id,
            Name = role.Name,
            Color = string.IsNullOrWhiteSpace(role.Color) ? DefaultRoleColor : role.Color,
            PermissionIds = role.RolePermissions.Select(link => link.PermissionId).ToArray()
        };
    }

    private static string? NormalizeRoleColor(string? color)
    {
        if (string.IsNullOrWhiteSpace(color))
        {
            return DefaultRoleColor;
        }

        var normalized = color.Trim().ToUpperInvariant();
        return HexColorRegex.IsMatch(normalized) ? normalized : null;
    }

    private static string GetPermissionLabel(string systemName, string? fallbackDescription)
    {
        if (PermissionDisplayMetadata.TryGetValue(systemName, out var metadata))
        {
            return metadata.Label;
        }

        return string.IsNullOrWhiteSpace(fallbackDescription) ? systemName : fallbackDescription;
    }

    private static string GetPermissionDescription(string systemName, string? fallbackDescription)
    {
        if (PermissionDisplayMetadata.TryGetValue(systemName, out var metadata))
        {
            return metadata.Description;
        }

        return string.IsNullOrWhiteSpace(fallbackDescription)
            ? "No description available."
            : fallbackDescription;
    }
}
