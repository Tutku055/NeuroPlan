using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeuroPlan.Application.DTOs;

namespace NeuroPlan.Application.Interfaces;

public interface IRoleService
{
    Task<IEnumerable<RoleResponseDto>> GetAllAsync();
    Task<RoleResponseDto?> GetByIdAsync(Guid id);
    Task<RoleResponseDto> CreateAsync(CreateRoleRequestDto request);
    Task UpdateAsync(Guid id, UpdateRoleRequestDto request);
    Task DeleteAsync(Guid id);
    Task<IEnumerable<PermissionResponseDto>> GetAllPermissionsAsync();
}
