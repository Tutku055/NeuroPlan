using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NeuroPlan.Domain.Entities;
using NeuroPlan.Domain.Interfaces;
using NeuroPlan.Infrastructure.Persistence.Context;

namespace NeuroPlan.Infrastructure.Persistence.Repositories;

public class ProjectRepository : GenericRepository<Project>, IProjectRepository
{
    public ProjectRepository(NeuroPlanDbContext context) : base(context)
    {
    }

    public async Task<Project?> GetProjectWithTasksAsync(Guid projectId)
    {
        return await _dbSet
            .Include(p => p.TaskItems)
            .FirstOrDefaultAsync(p => p.Id == projectId);
    }
}
