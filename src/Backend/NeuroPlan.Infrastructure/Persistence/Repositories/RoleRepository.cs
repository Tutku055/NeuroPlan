using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NeuroPlan.Domain.Entities;
using NeuroPlan.Domain.Interfaces;
using NeuroPlan.Infrastructure.Persistence.Context;

namespace NeuroPlan.Infrastructure.Persistence.Repositories;

public class RoleRepository : GenericRepository<Role>, IRoleRepository
{
    public RoleRepository(NeuroPlanDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Role>> GetAllWithPermissionsAsync()
    {
        return await _dbSet
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .ToListAsync();
    }

    public async Task<Role?> GetByIdWithPermissionsAsync(Guid id)
    {
        return await _dbSet
            .Include(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(r => r.Id == id);
    }
}
