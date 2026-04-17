using System;
using System.Threading.Tasks;
using NeuroPlan.Application.DTOs;

namespace NeuroPlan.Application.Interfaces;

public interface IForecastService
{
    Task<ForecastResultDto> AssessProjectRiskAsync(Guid projectId);
}
