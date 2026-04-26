using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NeuroPlan.Domain.Entities;
using NeuroPlan.Domain.Interfaces;
using NeuroPlan.Infrastructure.Persistence.Context;

namespace NeuroPlan.Infrastructure.Persistence.Repositories;

public class UserRepository : GenericRepository<User>, IUserRepository
{
    public UserRepository(NeuroPlanDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<User>> GetAllWithRolesAsync()
    {
        return await _dbSet
            .Include(u => u.Role)
            .ToListAsync();
    }

    public async Task<User?> GetByIdWithRoleAsync(Guid id)
    {
        return await _dbSet
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task<User?> GetByEmailWithRolePermissionsAsync(string email)
    {
        return await _dbSet
            .Include(user => user.Role)
                .ThenInclude(role => role.RolePermissions)
                    .ThenInclude(link => link.Permission)
            .FirstOrDefaultAsync(user => user.Email == email);
    }
}
