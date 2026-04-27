using System;
using System.Collections.Generic;
using NeuroPlan.Domain.Enums;

namespace NeuroPlan.Domain.Entities;

public class TaskItem : BaseEntity
{
    private TaskItem()
    {
    }

    public TaskItem(Guid projectId, string taskCode, string title, string description, int complexityScore)
    {
        ProjectId = projectId;
        UpdateDetails(title, description, complexityScore, taskCode);
    }

    public TaskItem(Guid id, Guid projectId, string taskCode, string title, string description, int complexityScore)
        : base(id)
    {
        ProjectId = projectId;
        UpdateDetails(title, description, complexityScore, taskCode);
    }

    public string TaskCode { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int ComplexityScore { get; private set; }
    public EntityStatus Status { get; private set; } = EntityStatus.Planned;
    public DateTime? CompletedDate { get; private set; }

    public Guid ProjectId { get; private set; }
    public Project Project { get; private set; } = null!;

    // Navigation properties
    public ICollection<Worklog> Worklogs { get; private set; } = new List<Worklog>();

    public void UpdateDetails(string title, string description, int complexityScore, string taskCode)
    {
        Title = title;
        Description = description;
        ComplexityScore = complexityScore;
        TaskCode = taskCode;
    }

    public void ChangeStatus(EntityStatus status)
    {
        Status = status;
    }

    public void MarkCompleted(DateTime completedAt)
    {
        Status = EntityStatus.Completed;
        CompletedDate = completedAt;
    }

    public void MarkCancelled()
    {
        Status = EntityStatus.Cancelled;
    }
}
