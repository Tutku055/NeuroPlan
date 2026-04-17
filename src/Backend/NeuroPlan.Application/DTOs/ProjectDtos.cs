namespace NeuroPlan.Application.DTOs;

public class ProjectResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ProjectCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime? TargetEndDate { get; set; }
}

public class CreateProjectRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? ProjectCode { get; set; }
    public string? Description { get; set; }
    public DateTime? TargetEndDate { get; set; }
}

public class UpdateProjectRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? ProjectCode { get; set; }
    public string? Description { get; set; }
    public DateTime? TargetEndDate { get; set; }
}