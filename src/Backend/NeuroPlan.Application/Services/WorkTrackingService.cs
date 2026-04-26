using System;
using System.Threading.Tasks;
using NeuroPlan.Application.Interfaces;
using NeuroPlan.Domain.Entities;
using NeuroPlan.Domain.Enums;
using NeuroPlan.Domain.Interfaces;

namespace NeuroPlan.Application.Services;

public class WorkTrackingService : IWorkTrackingService
{
    private readonly ITaskItemRepository _taskItemRepository;
    private readonly IWorklogRepository _worklogRepository;

    public WorkTrackingService(ITaskItemRepository taskItemRepository, IWorklogRepository worklogRepository)
    {
        _taskItemRepository = taskItemRepository;
        _worklogRepository = worklogRepository;
    }

    public async Task StartWorklogAsync(string taskCode, Guid userId)
    {
        var task = await _taskItemRepository.GetByTaskCodeAsync(taskCode);
        if (task == null)
        {
            throw new Exception($"Task with code {taskCode} not found.");
        }

        if (task.Status == EntityStatus.Completed || task.Status == EntityStatus.Planned || task.Status == EntityStatus.Cancelled)
        {
            throw new Exception($"Cannot start worklog for a task with status: {task.Status}. Set it to In Progress first.");
        }

        var activeWorklog = await _worklogRepository.GetActiveWorklogAsync(userId);
        if (activeWorklog != null)
        {
            throw new Exception("User already has an active worklog. Please stop it first.");
        }

        var worklog = new Worklog
        {
            TaskItemId = task.Id,
            UserId = userId,
            StartTime = DateTime.UtcNow
        };

        await _worklogRepository.AddAsync(worklog);
    }

    public async Task StopWorklogAsync(string taskCode, Guid userId, bool isTaskCompleted = false)
    {
        var task = await _taskItemRepository.GetByTaskCodeAsync(taskCode);
        if (task == null)
        {
            throw new Exception($"Task with code {taskCode} not found.");
        }

        var activeWorklog = await _worklogRepository.GetActiveWorklogAsync(userId);
        if (activeWorklog == null || activeWorklog.TaskItemId != task.Id)
        {
            throw new Exception("No active worklog found for this task and user.");
        }

        activeWorklog.End(DateTime.UtcNow);
        await _worklogRepository.UpdateAsync(activeWorklog);

        if (isTaskCompleted)
        {
            task.MarkCompleted(DateTime.UtcNow);
            await _taskItemRepository.UpdateAsync(task);
        }
    }

    public async Task CancelTaskWorklogAsync(string taskCode, Guid userId)
    {
        var task = await _taskItemRepository.GetByTaskCodeAsync(taskCode);
        if (task == null)
        {
            throw new Exception($"Task with code {taskCode} not found.");
        }

        var activeWorklog = await _worklogRepository.GetActiveWorklogAsync(userId);
        if (activeWorklog != null && activeWorklog.TaskItemId == task.Id)
        {
            activeWorklog.End(DateTime.UtcNow);
            await _worklogRepository.UpdateAsync(activeWorklog);
        }

        task.MarkCancelled();
        await _taskItemRepository.UpdateAsync(task);
    }
}
