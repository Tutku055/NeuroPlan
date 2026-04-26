using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NeuroPlan.Domain.Entities;
using NeuroPlan.Domain.Interfaces;
using NeuroPlan.Infrastructure.Persistence.Context;

namespace NeuroPlan.Infrastructure.Persistence.Repositories;

public class RolePermissionRepository : IRolePermissionRepository
{
    private readonly NeuroPlanDbContext _context;

    public RolePermissionRepository(NeuroPlanDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<RolePermission>> GetByRoleIdAsync(Guid roleId)
    {
        return await _context.RolePermissions
            .Where(link => link.RoleId == roleId)
            .ToListAsync();
    }

    public async Task AddAsync(RolePermission entity)
    {
        await _context.RolePermissions.AddAsync(entity);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveAsync(RolePermission entity)
    {
        _context.RolePermissions.Remove(entity);
        await _context.SaveChangesAsync();
    }

    public async Task RemoveRangeAsync(IEnumerable<RolePermission> entities)
    {
        var list = entities.ToList();
        if (list.Count == 0)
        {
            return;
        }

        _context.RolePermissions.RemoveRange(list);
        await _context.SaveChangesAsync();
    }
}
