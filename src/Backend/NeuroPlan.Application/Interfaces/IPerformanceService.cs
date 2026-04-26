using System.Collections.Generic;
using System.Threading.Tasks;
using NeuroPlan.Application.DTOs;

namespace NeuroPlan.Application.Interfaces;

public interface IPerformanceService
{
    Task<IEnumerable<PerformanceEntryDto>> GetPerformanceDataAsync();
}
