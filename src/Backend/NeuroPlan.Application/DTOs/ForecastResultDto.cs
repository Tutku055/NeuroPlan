using System;
using NeuroPlan.Domain.Enums;

namespace NeuroPlan.Application.DTOs;

public class ForecastResultDto
{
    public DateTime PredictedCompletionDate { get; set; }
    public ProjectRiskStatus RiskStatus { get; set; }
    public double CurrentVelocity { get; set; }
    public int RemainingComplexity { get; set; }
}
