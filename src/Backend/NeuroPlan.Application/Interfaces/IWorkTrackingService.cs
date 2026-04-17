using System;
using System.Threading.Tasks;

namespace NeuroPlan.Application.Interfaces;

public interface IWorkTrackingService
{
    Task StartWorklogAsync(string taskCode, Guid userId);
    Task StopWorklogAsync(string taskCode, Guid userId, bool isTaskCompleted = false);
    Task CancelTaskWorklogAsync(string taskCode, Guid userId);
}
