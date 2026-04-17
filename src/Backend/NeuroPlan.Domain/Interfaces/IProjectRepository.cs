using System;
using System.Threading.Tasks;
using NeuroPlan.Domain.Entities;

namespace NeuroPlan.Domain.Interfaces;

public interface IProjectRepository : IGenericRepository<Project>
{
    Task<Project?> GetProjectWithTasksAsync(Guid projectId);
}
