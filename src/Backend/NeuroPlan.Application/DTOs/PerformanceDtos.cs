using System;

namespace NeuroPlan.Application.DTOs;

public class PerformanceEntryDto
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string UserFullName { get; set; } = string.Empty;
    public double TotalMinutes { get; set; }
}
