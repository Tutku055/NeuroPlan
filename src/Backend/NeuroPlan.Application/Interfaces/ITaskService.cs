using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeuroPlan.Application.DTOs;

namespace NeuroPlan.Application.Interfaces;

public interface ITaskService
{
    Task<IEnumerable<TaskResponseDto>> GetAllAsync();
    Task<TaskResponseDto?> GetByIdAsync(Guid id);
    Task<TaskResponseDto> CreateAsync(CreateTaskRequestDto request);
    Task UpdateAsync(Guid id, UpdateTaskRequestDto request);
    Task DeleteAsync(Guid id);
}
