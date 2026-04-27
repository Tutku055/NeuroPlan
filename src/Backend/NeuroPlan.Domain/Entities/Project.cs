using System;
using System.Collections.Generic;
using NeuroPlan.Domain.Enums;

namespace NeuroPlan.Domain.Entities;

public class Project : BaseEntity
{
    private Project()
    {
    }

    public Project(string name, string projectCode, string description, DateTime? targetEndDate)
    {
        UpdateDetails(name, projectCode, description, targetEndDate);
    }

    public Project(Guid id, string name, string projectCode, string description, DateTime? targetEndDate)
        : base(id)
    {
        UpdateDetails(name, projectCode, description, targetEndDate);
    }

    public string Name { get; private set; } = string.Empty;
    public string ProjectCode { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public DateTime? TargetEndDate { get; private set; }

    // Navigation properties
    public ICollection<TaskItem> TaskItems { get; private set; } = new List<TaskItem>();

    public void UpdateDetails(string name, string projectCode, string description, DateTime? targetEndDate)
    {
        Name = name;
        ProjectCode = projectCode;
        Description = description;
        TargetEndDate = targetEndDate;
    }
}
