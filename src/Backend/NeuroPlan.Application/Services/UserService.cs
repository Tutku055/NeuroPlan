using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using NeuroPlan.Application.DTOs;
using NeuroPlan.Application.Interfaces;
using NeuroPlan.Domain.Entities;
using NeuroPlan.Domain.Interfaces;

namespace NeuroPlan.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<IEnumerable<UserResponseDto>> GetAllAsync()
    {
        var users = await _userRepository.GetAllWithRolesAsync();
        return users.Select(user => MapToDto(user));
    }

    public async Task<UserResponseDto?> GetByIdAsync(Guid id)
    {
        var user = await _userRepository.GetByIdWithRoleAsync(id);
        return user is null ? null : MapToDto(user);
    }

    public async Task<UserResponseDto> CreateAsync(CreateUserRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            throw new ArgumentException("Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException("Email is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new ArgumentException("Password is required for new users.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existingWithEmail = await _userRepository.FindAsync(user => user.Email == normalizedEmail);
        if (existingWithEmail.Any())
        {
            throw new ArgumentException("A user with this email already exists.");
        }

        var role = await _roleRepository.GetByIdAsync(request.RoleId);
        if (role is null)
        {
            throw new ArgumentException("Invalid role.");
        }

        var user = new User(request.FullName.Trim(), normalizedEmail, request.RoleId);
        user.SetPasswordHash(_passwordHasher.HashPassword(user, request.Password.Trim()));

        await _userRepository.AddAsync(user);

        return MapToDto(user, role.Name, role.Color);
    }

    public async Task UpdateAsync(Guid id, UpdateUserRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            throw new ArgumentException("Full name is required.");
        }

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException("Email is required.");
        }

        var existing = await _userRepository.GetByIdAsync(id);
        if (existing is null)
        {
            throw new KeyNotFoundException();
        }

        var role = await _roleRepository.GetByIdAsync(request.RoleId);
        if (role is null)
        {
            throw new ArgumentException("Invalid role.");
        }

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        if (!string.Equals(existing.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            var otherWithEmail = await _userRepository.FindAsync(user => user.Email == normalizedEmail && user.Id != id);
            if (otherWithEmail.Any())
            {
                throw new ArgumentException("This email is already in use by another user.");
            }
        }

        existing.UpdateProfile(request.FullName.Trim(), normalizedEmail, request.RoleId);

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            existing.SetPasswordHash(_passwordHasher.HashPassword(existing, request.Password.Trim()));
        }

        await _userRepository.UpdateAsync(existing);
    }

    public async Task DeleteAsync(Guid id)
    {
        var existing = await _userRepository.GetByIdAsync(id);
        if (existing is null)
        {
            throw new KeyNotFoundException();
        }

        await _userRepository.DeleteAsync(existing);
    }

    private static UserResponseDto MapToDto(User user, string? roleName = null, string? roleColor = null)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            RoleId = user.RoleId,
            RoleName = roleName ?? user.Role?.Name ?? string.Empty,
            RoleColor = roleColor ?? user.Role?.Color ?? string.Empty
        };
    }
}
