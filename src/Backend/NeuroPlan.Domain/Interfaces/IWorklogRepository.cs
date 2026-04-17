using System;
using System.Threading.Tasks;
using NeuroPlan.Domain.Entities;

namespace NeuroPlan.Domain.Interfaces;

public interface IWorklogRepository : IGenericRepository<Worklog>
{
    Task<Worklog?> GetActiveWorklogAsync(Guid userId);
}
