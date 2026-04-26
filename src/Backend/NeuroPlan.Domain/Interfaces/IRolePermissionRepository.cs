using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeuroPlan.Domain.Entities;

namespace NeuroPlan.Domain.Interfaces;

public interface IRolePermissionRepository
{
    Task<IEnumerable<RolePermission>> GetByRoleIdAsync(Guid roleId);
    Task AddAsync(RolePermission entity);
    Task RemoveAsync(RolePermission entity);
    Task RemoveRangeAsync(IEnumerable<RolePermission> entities);
}
