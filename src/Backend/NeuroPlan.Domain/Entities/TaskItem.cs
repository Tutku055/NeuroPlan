using System;
using System.Collections.Generic;
using NeuroPlan.Domain.Enums;

namespace NeuroPlan.Domain.Entities;

public class TaskItem : BaseEntity
{
    public string TaskCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ComplexityScore { get; set; }
    public EntityStatus Status { get; set; } = EntityStatus.Planned;
    public DateTime? CompletedDate { get; set; }

    public Guid ProjectId { get; set; }
    public Project Project { get; set; } = null!;

    // Navigation properties
    public ICollection<Worklog> Worklogs { get; set; } = new List<Worklog>();

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
