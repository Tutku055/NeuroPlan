using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroPlan.API.Settings;
using NeuroPlan.Application.DTOs;
using NeuroPlan.Application.Interfaces;


namespace NeuroPlan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IForecastService _forecastService;

    public ProjectsController(IProjectService projectService, IForecastService forecastService)
    {
        _projectService = projectService;
        _forecastService = forecastService;
    }

    [HttpGet]
    [Authorize(Policy = AuthorizationPolicies.ProjectsRead)]
    public async Task<IActionResult> GetAll()
    {
        var projects = await _projectService.GetAllAsync();
        return Ok(projects);
    }

    [HttpGet("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ProjectsRead)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var project = await _projectService.GetByIdAsync(id);
        if (project == null) return NotFound();
        return Ok(project);
    }

    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.ProjectsManage)]
    public async Task<IActionResult> Create([FromBody] CreateProjectRequestDto request)
    {
        try
        {
            var project = await _projectService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = project.Id }, project);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ProjectsManage)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProjectRequestDto request)
    {
        try
        {
            await _projectService.UpdateAsync(id, request);
            return NoContent();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = AuthorizationPolicies.ProjectsManage)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            await _projectService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
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
}
