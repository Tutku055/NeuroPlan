using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using NeuroPlan.API.Settings;
using NeuroPlan.Application.Interfaces;

namespace NeuroPlan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.WorklogsTrack)]
public class WorklogsController : ControllerBase
{
    private readonly IWorkTrackingService _workTrackingService;

    public WorklogsController(IWorkTrackingService workTrackingService)
    {
        _workTrackingService = workTrackingService;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartWork([FromQuery] string taskCode)
    {
        try
        {
            var userIdClaim = User.FindFirst("Id");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return Unauthorized(new { message = "User ID not found in token." });
            }

            await _workTrackingService.StartWorklogAsync(taskCode, userId);
            return Ok(new { message = "Worklog started effectively." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("stop")]
    public async Task<IActionResult> StopWork([FromQuery] string taskCode, [FromQuery] bool isCompleted = false)
    {
        try
        {
            var userIdClaim = User.FindFirst("Id");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return Unauthorized(new { message = "User ID not found in token." });
            }

            await _workTrackingService.StopWorklogAsync(taskCode, userId, isCompleted);
            return Ok(new { message = "Worklog stopped effectively." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("cancel")]
    public async Task<IActionResult> CancelWork([FromQuery] string taskCode)
    {
        try
        {
            var userIdClaim = User.FindFirst("Id");
            if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out Guid userId))
            {
                return Unauthorized(new { message = "User ID not found in token." });
            }

            await _workTrackingService.CancelTaskWorklogAsync(taskCode, userId);
            return Ok(new { message = "Task cancelled effectively." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
