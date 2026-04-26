using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NeuroPlan.Application.DTOs;
using NeuroPlan.Application.Interfaces;
using NeuroPlan.Domain.Entities;
using NeuroPlan.Domain.Interfaces;

namespace NeuroPlan.Application.Services;

public class RoleService : IRoleService
{
    private const string DefaultRoleColor = "#64748B";
    private static readonly Regex HexColorRegex = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

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

        var role = new Role();
        role.UpdateDetails(request.Name.Trim(), normalizedColor);

        await _roleRepository.AddAsync(role);

        if (request.PermissionIds.Length > 0)
        {
            foreach (var permissionId in request.PermissionIds.Distinct())
            {
                await _rolePermissionRepository.AddAsync(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permissionId
                });
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
            await _rolePermissionRepository.AddAsync(new RolePermission
            {
                RoleId = id,
                PermissionId = permissionId
            });
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
            Label = string.IsNullOrWhiteSpace(permission.Description) ? permission.SystemName : permission.Description,
            Description = permission.Description
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
}
