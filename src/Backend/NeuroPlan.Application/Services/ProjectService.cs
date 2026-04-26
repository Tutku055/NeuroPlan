using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NeuroPlan.Application.DTOs;
using NeuroPlan.Application.Interfaces;
using NeuroPlan.Domain.Entities;
using NeuroPlan.Domain.Interfaces;

namespace NeuroPlan.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;

    public ProjectService(IProjectRepository projectRepository)
    {
        _projectRepository = projectRepository;
    }

    public async Task<IEnumerable<ProjectResponseDto>> GetAllAsync()
    {
        var projects = await _projectRepository.GetAllAsync();
        return projects.Select(MapToDto);
    }

    public async Task<ProjectResponseDto?> GetByIdAsync(Guid id)
    {
        var project = await _projectRepository.GetByIdAsync(id);
        return project is null ? null : MapToDto(project);
    }

    public async Task<ProjectResponseDto> CreateAsync(CreateProjectRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required.");
        }

        var project = new Project();
        project.UpdateDetails(
            request.Name.Trim(),
            request.ProjectCode?.Trim() ?? string.Empty,
            request.Description?.Trim() ?? string.Empty,
            request.TargetEndDate);

        await _projectRepository.AddAsync(project);

        return MapToDto(project);
    }

    public async Task UpdateAsync(Guid id, UpdateProjectRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Name is required.");
        }

        var existing = await _projectRepository.GetByIdAsync(id);
        if (existing is null)
        {
            throw new KeyNotFoundException();
        }

        existing.UpdateDetails(
            request.Name.Trim(),
            request.ProjectCode?.Trim() ?? string.Empty,
            request.Description?.Trim() ?? string.Empty,
            request.TargetEndDate);

        await _projectRepository.UpdateAsync(existing);
    }

    public async Task DeleteAsync(Guid id)
    {
        var existing = await _projectRepository.GetByIdAsync(id);
        if (existing is null)
        {
            throw new KeyNotFoundException();
        }

        await _projectRepository.DeleteAsync(existing);
    }

    private static ProjectResponseDto MapToDto(Project project)
    {
        return new ProjectResponseDto
        {
            Id = project.Id,
            Name = project.Name,
            ProjectCode = project.ProjectCode,
            Description = project.Description,
            TargetEndDate = project.TargetEndDate
        };
    }
}
