using System;
using System.Threading.Tasks;

namespace NeuroPlan.Application.Interfaces;

public interface IAiPredictionProvider
{
    Task<DateTime> PredictCompletionDateAsync(DateTime startDate, double velocity, int remainingComplexity);
}
