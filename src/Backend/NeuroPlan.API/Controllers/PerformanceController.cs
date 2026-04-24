using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroPlan.API.Settings;
using NeuroPlan.Application.DTOs;
using NeuroPlan.Domain.Interfaces;

namespace NeuroPlan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.PerformanceRead)]
public class PerformanceController : ControllerBase
{
    private readonly IWorklogRepository _worklogRepository;

    public PerformanceController(IWorklogRepository worklogRepository)
    {
        _worklogRepository = worklogRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetPerformanceData()
    {
        var worklogs = await _worklogRepository.GetAllWithDetailsAsync();

        var entries = worklogs
            .Where(w => w.EndTime != null)
            .GroupBy(w => new
            {
                ProjectId = w.TaskItem.ProjectId,
                ProjectName = w.TaskItem.Project.Name,
                UserFullName = w.User.FullName
            })
            .Select(g => new PerformanceEntryDto
            {
                ProjectId = g.Key.ProjectId,
                ProjectName = g.Key.ProjectName,
                UserFullName = g.Key.UserFullName,
                TotalMinutes = g.Sum(w => (w.EndTime!.Value - w.StartTime).TotalMinutes)
            })
            .OrderBy(e => e.ProjectName)
            .ThenByDescending(e => e.TotalMinutes)
            .ToList();

        return Ok(entries);
    }
}
