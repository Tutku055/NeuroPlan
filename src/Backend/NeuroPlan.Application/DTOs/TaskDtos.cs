namespace NeuroPlan.Application.DTOs;

public class TaskResponseDto
{
    public Guid Id { get; set; }
    public string TaskCode { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int ComplexityScore { get; set; }
    public int Status { get; set; }
    public Guid ProjectId { get; set; }
    public DateTime? CompletedDate { get; set; }
}

public class CreateTaskRequestDto
{
    public string? TaskCode { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ComplexityScore { get; set; }
    public int Status { get; set; }
    public Guid ProjectId { get; set; }
}

public class UpdateTaskRequestDto
{
    public Guid Id { get; set; }
    public string? TaskCode { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ComplexityScore { get; set; }
    public int Status { get; set; }
}