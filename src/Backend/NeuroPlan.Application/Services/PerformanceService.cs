using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NeuroPlan.Application.DTOs;
using NeuroPlan.Application.Interfaces;
using NeuroPlan.Domain.Interfaces;

namespace NeuroPlan.Application.Services;

public class PerformanceService : IPerformanceService
{
    private readonly IWorklogRepository _worklogRepository;

    public PerformanceService(IWorklogRepository worklogRepository)
    {
        _worklogRepository = worklogRepository;
    }

    public async Task<IEnumerable<PerformanceEntryDto>> GetPerformanceDataAsync()
    {
        var worklogs = await _worklogRepository.GetAllWithDetailsAsync();

        return worklogs
            .Where(worklog => worklog.EndTime != null)
            .GroupBy(worklog => new
            {
                ProjectId = worklog.TaskItem.ProjectId,
                ProjectName = worklog.TaskItem.Project.Name,
                UserFullName = worklog.User.FullName
            })
            .Select(group => new PerformanceEntryDto
            {
                ProjectId = group.Key.ProjectId,
                ProjectName = group.Key.ProjectName,
                UserFullName = group.Key.UserFullName,
                TotalMinutes = group.Sum(worklog => (worklog.EndTime!.Value - worklog.StartTime).TotalMinutes)
            })
            .OrderBy(entry => entry.ProjectName)
            .ThenByDescending(entry => entry.TotalMinutes)
            .ToList();
    }
}
