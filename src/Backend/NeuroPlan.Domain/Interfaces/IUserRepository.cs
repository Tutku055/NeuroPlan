using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeuroPlan.Domain.Entities;

namespace NeuroPlan.Domain.Interfaces;

public interface IUserRepository : IGenericRepository<User>
{
    Task<IEnumerable<User>> GetAllWithRolesAsync();
    Task<User?> GetByIdWithRoleAsync(Guid id);
}
