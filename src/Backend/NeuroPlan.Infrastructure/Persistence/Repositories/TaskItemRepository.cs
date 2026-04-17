using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NeuroPlan.Domain.Entities;
using NeuroPlan.Domain.Interfaces;
using NeuroPlan.Infrastructure.Persistence.Context;

namespace NeuroPlan.Infrastructure.Persistence.Repositories;

public class TaskItemRepository : GenericRepository<TaskItem>, ITaskItemRepository
{
    public TaskItemRepository(NeuroPlanDbContext context) : base(context)
    {
    }

    public async Task<TaskItem?> GetByTaskCodeAsync(string taskCode)
    {
        return await _dbSet.FirstOrDefaultAsync(t => t.TaskCode == taskCode);
    }
}
