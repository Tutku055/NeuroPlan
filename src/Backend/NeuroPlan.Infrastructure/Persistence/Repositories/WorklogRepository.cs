using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NeuroPlan.Domain.Entities;
using NeuroPlan.Domain.Interfaces;
using NeuroPlan.Infrastructure.Persistence.Context;

namespace NeuroPlan.Infrastructure.Persistence.Repositories;

public class WorklogRepository : GenericRepository<Worklog>, IWorklogRepository
{
    public WorklogRepository(NeuroPlanDbContext context) : base(context)
    {
    }

    public async Task<Worklog?> GetActiveWorklogAsync(Guid userId)
    {
        return await _dbSet
            .FirstOrDefaultAsync(w => w.UserId == userId && w.EndTime == null);
    }
}
