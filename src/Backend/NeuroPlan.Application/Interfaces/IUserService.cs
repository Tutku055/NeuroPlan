using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NeuroPlan.Application.DTOs;

namespace NeuroPlan.Application.Interfaces;

public interface IUserService
{
    Task<IEnumerable<UserResponseDto>> GetAllAsync();
    Task<UserResponseDto?> GetByIdAsync(Guid id);
    Task<UserResponseDto> CreateAsync(CreateUserRequestDto request);
    Task UpdateAsync(Guid id, UpdateUserRequestDto request);
    Task DeleteAsync(Guid id);
}
