using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroPlan.API.Settings;
using NeuroPlan.Application.DTOs;
using NeuroPlan.Domain.Entities;
using NeuroPlan.Domain.Enums;
using NeuroPlan.Domain.Interfaces;
using NeuroPlan.Infrastructure.Persistence.Context;

namespace NeuroPlan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskItemRepository _taskRepository;

    public TasksController(ITaskItemRepository taskRepository)
    {
        _taskRepository = taskRepository;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.TasksRead)]
    public async Task<IActionResult> GetAll()
    {
        var tasks = await _taskRepository.GetAllAsync();
        return Ok(tasks.Select(MapToDto));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.TasksRead)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null) return NotFound();
        return Ok(MapToDto(task));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.TasksManage)]
    public async Task<IActionResult> Create([FromBody] CreateTaskRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest(new { message = "Title is required." });

        if (dto.ProjectId == Guid.Empty)
            return BadRequest(new { message = "A valid ProjectId is required." });

        if (!TryParseStatus(dto.Status, out var status))
            return BadRequest(new { message = "Status is invalid." });

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            TaskCode = dto.TaskCode ?? string.Empty,
            Title = dto.Title.Trim(),
            Description = dto.Description ?? string.Empty,
            ComplexityScore = dto.ComplexityScore,
            Status = status,
            ProjectId = dto.ProjectId,
        };

        await _taskRepository.AddAsync(task);

        return CreatedAtAction(nameof(GetById), new { id = task.Id }, MapToDto(task));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.TasksManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTaskRequestDto dto, [FromServices] NeuroPlanDbContext db)
    {
        if (id != dto.Id) return BadRequest(new { message = "ID mismatch." });

        if (string.IsNullOrWhiteSpace(dto.Title))
            return BadRequest(new { message = "Title is required." });

        if (!TryParseStatus(dto.Status, out var newStatus))
            return BadRequest(new { message = "Status is invalid." });

        var existing = await _taskRepository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        if (existing.Status == EntityStatus.InProgress && newStatus != EntityStatus.InProgress)
        {
            var activeLogs = db.Worklogs.Where(w => w.TaskItemId == id && w.EndTime == null).ToList();
            foreach (var log in activeLogs)
            {
                log.EndTime = DateTime.UtcNow;
            }
            await db.SaveChangesAsync();
        }

        existing.Title = dto.Title.Trim();
        existing.Description = dto.Description ?? existing.Description;
        existing.Status = newStatus;
        existing.ComplexityScore = dto.ComplexityScore;
        existing.TaskCode = dto.TaskCode ?? existing.TaskCode;

        await _taskRepository.UpdateAsync(existing);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.TasksManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var task = await _taskRepository.GetByIdAsync(id);
        if (task == null) return NotFound();

        await _taskRepository.DeleteAsync(task);
        return NoContent();
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
