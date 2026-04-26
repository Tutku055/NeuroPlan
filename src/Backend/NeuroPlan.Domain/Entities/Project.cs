using System;
using System.Collections.Generic;
using NeuroPlan.Domain.Enums;

namespace NeuroPlan.Domain.Entities;

public class Project : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? TargetEndDate { get; set; }

    // Navigation properties
    public ICollection<TaskItem> TaskItems { get; set; } = new List<TaskItem>();

    public void UpdateDetails(string name, string projectCode, string description, DateTime? targetEndDate)
    {
        Name = name;
        ProjectCode = projectCode;
        Description = description;
        TargetEndDate = targetEndDate;
    }
}
