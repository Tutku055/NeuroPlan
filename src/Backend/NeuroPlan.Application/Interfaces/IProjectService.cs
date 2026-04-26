using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeuroPlan.Application.DTOs;

namespace NeuroPlan.Application.Interfaces;

public interface IProjectService
{
    Task<IEnumerable<ProjectResponseDto>> GetAllAsync();
    Task<ProjectResponseDto?> GetByIdAsync(Guid id);
    Task<ProjectResponseDto> CreateAsync(CreateProjectRequestDto request);
    Task UpdateAsync(Guid id, UpdateProjectRequestDto request);
    Task DeleteAsync(Guid id);
}
