using System.Threading.Tasks;
using NeuroPlan.Domain.Entities;

namespace NeuroPlan.Domain.Interfaces;

public interface ITaskItemRepository : IGenericRepository<TaskItem>
{
    Task<TaskItem?> GetByTaskCodeAsync(string taskCode);
}
