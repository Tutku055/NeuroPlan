using NeuroPlan.Domain.Entities;
using NeuroPlan.Domain.Interfaces;
using NeuroPlan.Infrastructure.Persistence.Context;

namespace NeuroPlan.Infrastructure.Persistence.Repositories;

public class PermissionRepository : GenericRepository<Permission>, IPermissionRepository
{
    public PermissionRepository(NeuroPlanDbContext context) : base(context)
    {
    }
}
