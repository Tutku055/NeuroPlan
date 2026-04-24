using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NeuroPlan.API.Settings;
using NeuroPlan.Application.DTOs;
using NeuroPlan.Domain.Entities;
using NeuroPlan.Domain.Interfaces;
using NeuroPlan.Infrastructure.Persistence.Context;

namespace NeuroPlan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.RolesManage)]
public class RolesController : ControllerBase
{
    private const string DefaultRoleColor = "#64748B";
    private static readonly Regex HexColorRegex = new("^#[0-9A-Fa-f]{6}$", RegexOptions.Compiled);

    private readonly IRoleRepository _roleRepository;
    private readonly IPermissionRepository _permissionRepository;
    private readonly NeuroPlanDbContext _dbContext;

    public RolesController(IRoleRepository roleRepository, IPermissionRepository permissionRepository, NeuroPlanDbContext dbContext)
    {
        _roleRepository = roleRepository;
        _permissionRepository = permissionRepository;
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var roles = await _roleRepository.GetAllWithPermissionsAsync();
        return Ok(roles.Select(MapToDto));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var role = await _roleRepository.GetByIdWithPermissionsAsync(id);
        if (role == null) return NotFound();
        return Ok(MapToDto(role));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRoleRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "Name is required." });

        var normalizedColor = NormalizeRoleColor(dto.Color);
        if (normalizedColor is null)
            return BadRequest(new { message = "Color must be a valid hex value like #3B82F6." });

        var role = new Role
        {
            Name = dto.Name.Trim(),
            Color = normalizedColor
        };

        await _roleRepository.AddAsync(role);

        if (dto.PermissionIds.Length > 0)
        {
            foreach (var permId in dto.PermissionIds.Distinct())
            {
                _dbContext.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permId
                });
            }
            await _dbContext.SaveChangesAsync();
        }

        return CreatedAtAction(nameof(GetById), new { id = role.Id }, MapToDto(role));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRoleRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            return BadRequest(new { message = "Name is required." });

        var normalizedColor = NormalizeRoleColor(dto.Color);
        if (normalizedColor is null)
            return BadRequest(new { message = "Color must be a valid hex value like #3B82F6." });

        var existing = await _roleRepository.GetByIdWithPermissionsAsync(id);
        if (existing == null) return NotFound();

        existing.Name = dto.Name.Trim();
        existing.Color = normalizedColor;

        var currentPermIds = existing.RolePermissions.Select(rp => rp.PermissionId).ToHashSet();
        var newPermIds = dto.PermissionIds.Distinct().ToHashSet();

        var toRemove = existing.RolePermissions.Where(rp => !newPermIds.Contains(rp.PermissionId)).ToList();
        foreach (var rp in toRemove)
        {
            _dbContext.RolePermissions.Remove(rp);
        }

        foreach (var permId in newPermIds.Where(pid => !currentPermIds.Contains(pid)))
        {
            _dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = id,
                PermissionId = permId
            });
        }

        await _roleRepository.UpdateAsync(existing);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var role = await _roleRepository.GetByIdWithPermissionsAsync(id);
        if (role == null) return NotFound();

        var hasUsers = await _dbContext.Users.AsNoTracking().AnyAsync(u => u.RoleId == id);
        if (hasUsers)
            return Conflict(new { message = "Cannot delete this role because it is assigned to active users." });

        var links = await _dbContext.RolePermissions.Where(rp => rp.RoleId == id).ToListAsync();
        _dbContext.RolePermissions.RemoveRange(links);

        await _roleRepository.DeleteAsync(role);
        return NoContent();
    }

    [HttpGet("/api/permissions")]
    public async Task<IActionResult> GetAllPermissions()
    {
        var permissions = await _permissionRepository.GetAllAsync();
        return Ok(permissions.Select(p => new PermissionResponseDto
        {
            Id = p.Id,
            SystemName = p.SystemName,
            Label = string.IsNullOrWhiteSpace(p.Description) ? p.SystemName : p.Description,
            Description = p.Description
        }));
    }

    private static RoleResponseDto MapToDto(Role role)
    {
        return new RoleResponseDto
        {
            Id = role.Id,
            Name = role.Name,
            Color = string.IsNullOrWhiteSpace(role.Color) ? DefaultRoleColor : role.Color,
            PermissionIds = role.RolePermissions.Select(rp => rp.PermissionId).ToArray()
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
