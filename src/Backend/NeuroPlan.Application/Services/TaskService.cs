using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NeuroPlan.Application.DTOs;
using NeuroPlan.Application.Interfaces;
using NeuroPlan.Domain.Entities;
using NeuroPlan.Domain.Enums;
using NeuroPlan.Domain.Interfaces;

namespace NeuroPlan.Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskItemRepository _taskRepository;
    private readonly IWorklogRepository _worklogRepository;

    public TaskService(ITaskItemRepository taskRepository, IWorklogRepository worklogRepository)
    {
        _taskRepository = taskRepository;
        _worklogRepository = worklogRepository;
    }

    public async Task<IEnumerable<TaskResponseDto>> GetAllAsync()
    {
        var tasks = await _taskRepository.GetAllAsync();
        return tasks.Select(MapToDto);
    }

    public async Task<TaskResponseDto?> GetByIdAsync(Guid id)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        return task is null ? null : MapToDto(task);
    }

    public async Task<TaskResponseDto> CreateAsync(CreateTaskRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Title is required.");
        }

        if (request.ProjectId == Guid.Empty)
        {
            throw new ArgumentException("A valid ProjectId is required.");
        }

        if (!TryParseStatus(request.Status, out var status))
        {
            throw new ArgumentException("Status is invalid.");
        }

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            ProjectId = request.ProjectId
        };

        task.UpdateDetails(
            request.Title.Trim(),
            request.Description ?? string.Empty,
            request.ComplexityScore,
            request.TaskCode ?? string.Empty);
        task.ChangeStatus(status);

        await _taskRepository.AddAsync(task);

        return MapToDto(task);
    }

    public async Task UpdateAsync(Guid id, UpdateTaskRequestDto request)
    {
        if (id != request.Id)
        {
            throw new ArgumentException("ID mismatch.");
        }

        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new ArgumentException("Title is required.");
        }

        if (!TryParseStatus(request.Status, out var newStatus))
        {
            throw new ArgumentException("Status is invalid.");
        }

        var existing = await _taskRepository.GetByIdAsync(id);
        if (existing is null)
        {
            throw new KeyNotFoundException();
        }

        if (existing.Status == EntityStatus.InProgress && newStatus != EntityStatus.InProgress)
        {
            var activeLogs = await _worklogRepository.FindAsync(worklog => worklog.TaskItemId == id && worklog.EndTime == null);
            foreach (var activeLog in activeLogs)
            {
                activeLog.End(DateTime.UtcNow);
                await _worklogRepository.UpdateAsync(activeLog);
            }
        }

        existing.UpdateDetails(
            request.Title.Trim(),
            request.Description ?? existing.Description,
            request.ComplexityScore,
            request.TaskCode ?? existing.TaskCode);
        existing.ChangeStatus(newStatus);

        await _taskRepository.UpdateAsync(existing);
    }

    public async Task DeleteAsync(Guid id)
    {
        var existing = await _taskRepository.GetByIdAsync(id);
        if (existing is null)
        {
            throw new KeyNotFoundException();
        }

        await _taskRepository.DeleteAsync(existing);
    }

    private static TaskResponseDto MapToDto(TaskItem task)
    {
        return new TaskResponseDto
        {
            Id = task.Id,
            TaskCode = task.TaskCode,
            Title = task.Title,
            Description = task.Description,
            ComplexityScore = task.ComplexityScore,
            Status = (int)task.Status,
            ProjectId = task.ProjectId,
            CompletedDate = task.CompletedDate
        };
    }

    private static bool TryParseStatus(int rawStatus, out EntityStatus status)
    {
        if (Enum.IsDefined(typeof(EntityStatus), rawStatus))
        {
            status = (EntityStatus)rawStatus;
            return true;
        }

        status = EntityStatus.Planned;
        return false;
    }
}
