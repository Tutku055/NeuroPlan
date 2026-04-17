using System;
using System.Linq;
using System.Threading.Tasks;
using NeuroPlan.Application.DTOs;
using NeuroPlan.Application.Interfaces;
using NeuroPlan.Domain.Enums;
using NeuroPlan.Domain.Interfaces;

namespace NeuroPlan.Application.Services;

public class ForecastService : IForecastService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IAiPredictionProvider _aiPredictionProvider;

    public ForecastService(IProjectRepository projectRepository, IAiPredictionProvider aiPredictionProvider)
    {
        _projectRepository = projectRepository;
        _aiPredictionProvider = aiPredictionProvider;
    }

    public async Task<ForecastResultDto> AssessProjectRiskAsync(Guid projectId)
    {
        var project = await _projectRepository.GetProjectWithTasksAsync(projectId);
        if (project == null)
        {
            throw new Exception("Project not found.");
        }

        if (project.TaskItems == null || !project.TaskItems.Any())
        {
            throw new Exception("Project has no tasks assigned.");
        }

        var totalComplexity = project.TaskItems.Sum(t => t.ComplexityScore);

        var completedTasks = project.TaskItems.Where(t => t.Status == EntityStatus.Completed).ToList();
        var completedComplexity = completedTasks.Sum(t => t.ComplexityScore);

        var remainingComplexity = totalComplexity - completedComplexity;

        // Calculate elapsed days
        // Finding the earliest task completion or log. For simplicity, we just use a baseline of 1 minimum.
        double daysElapsed = 1;
        if (completedTasks.Any(t => t.CompletedDate.HasValue))
        {
            var earliestCompletion = completedTasks.Min(t => t.CompletedDate!.Value);
            daysElapsed = Math.Max(1, (DateTime.UtcNow - earliestCompletion).TotalDays);
        }

        // Current velocity (points per day)
        double velocity = (double)completedComplexity / daysElapsed;
        if (velocity <= 0) velocity = 1.0; // Assume minimum velocity of 1 point/day if no data yet

        // Fetch prediction from the hypothetical external Python FastAPI ML model
        var predictedDate = await _aiPredictionProvider.PredictCompletionDateAsync(DateTime.UtcNow, velocity, remainingComplexity);

        var riskStatus = ProjectRiskStatus.OnTrack;
        if (project.TargetEndDate.HasValue)
        {
            var daysDifference = (predictedDate - project.TargetEndDate.Value).TotalDays;

            if (daysDifference > 5)
            {
                // Predicted to finish more than 5 days late
                riskStatus = ProjectRiskStatus.Risky;
            }
            else if (daysDifference < 0)
            {
                // Predicted to finish early
                riskStatus = ProjectRiskStatus.Safe;
            }
            else
            {
                // Predicted to finish within a 5-day grace period
                riskStatus = ProjectRiskStatus.OnTrack;
            }
        }

        return new ForecastResultDto
        {
            PredictedCompletionDate = predictedDate,
            RiskStatus = riskStatus,
            CurrentVelocity = velocity,
            RemainingComplexity = remainingComplexity
        };
    }
}
