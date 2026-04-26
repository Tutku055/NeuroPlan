using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NeuroPlan.API.Settings;
using NeuroPlan.Application.Interfaces;

namespace NeuroPlan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.PerformanceRead)]
public class PerformanceController : ControllerBase
{
    private readonly IPerformanceService _performanceService;

    public PerformanceController(IPerformanceService performanceService)
    {
        _performanceService = performanceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPerformanceData()
    {
        var entries = await _performanceService.GetPerformanceDataAsync();
        return Ok(entries);
    }
}
