using System;

namespace NeuroPlan.Domain.Entities;

public class Worklog : BaseEntity
{
    private Worklog()
    {
    }

    public Worklog(Guid taskItemId, Guid userId, DateTime startTime)
    {
        TaskItemId = taskItemId;
        UserId = userId;
        StartTime = startTime;
    }

    public Worklog(Guid id, Guid taskItemId, Guid userId, DateTime startTime)
        : base(id)
    {
        TaskItemId = taskItemId;
        UserId = userId;
        StartTime = startTime;
    }

    public Guid TaskItemId { get; private set; }
    public TaskItem TaskItem { get; private set; } = null!;

    public Guid UserId { get; private set; }
    public User User { get; private set; } = null!;

    public DateTime StartTime { get; private set; }
    public DateTime? EndTime { get; private set; }

    public void End(DateTime endTime)
    {
        if (endTime < StartTime)
        {
            throw new InvalidOperationException("End time cannot be earlier than start time.");
        }

        EndTime = endTime;
    }
}
