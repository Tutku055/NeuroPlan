using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroPlan.API.Settings;
using NeuroPlan.Application.DTOs;
using NeuroPlan.Application.Interfaces;
using NeuroPlan.Domain.Entities;
using NeuroPlan.Domain.Interfaces;


namespace NeuroPlan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectRepository _projectRepository;
    private readonly IForecastService _forecastService;

    public ProjectsController(IProjectRepository projectRepository, IForecastService forecastService)
    {
        _projectRepository = projectRepository;
        _forecastService = forecastService;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ProjectsRead)]
    public async Task<IActionResult> GetAll()
    {
        var projects = await _projectRepository.GetAllAsync();
        return Ok(projects.Select(MapToDto));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ProjectsRead)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null) return NotFound();
        return Ok(MapToDto(project));
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ProjectsManage)]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Name is required." });
        }

        var project = new Project
        {
            Name = request.Name.Trim(),
            ProjectCode = request.ProjectCode?.Trim() ?? string.Empty,
            Description = request.Description?.Trim() ?? string.Empty,
            TargetEndDate = request.TargetEndDate
        };

        await _projectRepository.AddAsync(project);
        return CreatedAtAction(nameof(GetById), new { id = project.Id }, MapToDto(project));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ProjectsManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return BadRequest(new { message = "Name is required." });
        }

        var existing = await _projectRepository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        existing.Name = request.Name.Trim();
        existing.ProjectCode = request.ProjectCode?.Trim() ?? string.Empty;
        existing.Description = request.Description?.Trim() ?? string.Empty;
        existing.TargetEndDate = request.TargetEndDate;

        _projectRepository.Update(existing);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ProjectsManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        if (project == null) return NotFound();

        _projectRepository.Delete(project);
        return NoContent();
    }

    [HttpGet("{id:guid}/forecast")]
    [Authorize(Policy = AuthorizationPolicies.ForecastAccess)]
    public async Task<IActionResult> AssessRisk(Guid id)
    {
        try
        {
            var result = await _forecastService.AssessProjectRiskAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }



    private static ProjectResponseDto MapToDto(Project project)
    {
        return new ProjectResponseDto
        {
            Id = project.Id,
            Name = project.Name,
            ProjectCode = project.ProjectCode,
            Description = project.Description,
            TargetEndDate = project.TargetEndDate
        };
    }
}
