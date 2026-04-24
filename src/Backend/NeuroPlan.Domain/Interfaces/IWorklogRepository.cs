using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeuroPlan.Domain.Entities;

namespace NeuroPlan.Domain.Interfaces;

public interface IWorklogRepository : IGenericRepository<Worklog>
{
    Task<Worklog?> GetActiveWorklogAsync(Guid userId);
    Task<IEnumerable<Worklog>> GetAllWithDetailsAsync();
}
