using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeuroPlan.Domain.Entities;

namespace NeuroPlan.Domain.Interfaces;

public interface IRoleRepository : IGenericRepository<Role>
{
    Task<IEnumerable<Role>> GetAllWithPermissionsAsync();
    Task<Role?> GetByIdWithPermissionsAsync(Guid id);
}
