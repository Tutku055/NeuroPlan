using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NeuroPlan.API.Settings;
using NeuroPlan.Application.DTOs;
using NeuroPlan.Domain.Entities;
using NeuroPlan.Domain.Interfaces;

namespace NeuroPlan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = AuthorizationPolicies.UsersManage)]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UsersController(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userRepository.GetAllWithRolesAsync();
        return Ok(users.Select(u => MapToDto(u)));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userRepository.GetByIdWithRoleAsync(id);
        if (user == null) return NotFound();
        return Ok(MapToDto(user));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            return BadRequest(new { message = "Full name is required." });

        if (string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest(new { message = "Email is required." });

        if (string.IsNullOrWhiteSpace(dto.Password))
            return BadRequest(new { message = "Password is required for new users." });

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

        // Check for uniqueness
        var existingWithEmail = await _userRepository.FindAsync(u => u.Email == normalizedEmail);
        if (existingWithEmail.Any())
            return BadRequest(new { message = "A user with this email already exists." });

        var role = await _roleRepository.GetByIdAsync(dto.RoleId);
        if (role == null)
            return BadRequest(new { message = "Invalid role." });

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = normalizedEmail,
            RoleId = dto.RoleId
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password.Trim());

        await _userRepository.AddAsync(user);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, MapToDto(user, role.Name, role.Color));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateUserRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName))
            return BadRequest(new { message = "Full name is required." });

        if (string.IsNullOrWhiteSpace(dto.Email))
            return BadRequest(new { message = "Email is required." });

        var existing = await _userRepository.GetByIdAsync(id);
        if (existing == null) return NotFound();

        var role = await _roleRepository.GetByIdAsync(dto.RoleId);
        if (role == null)
            return BadRequest(new { message = "Invalid role." });

        var normalizedEmail = dto.Email.Trim().ToLowerInvariant();

        // Check for uniqueness if email changed
        if (existing.Email != normalizedEmail)
        {
            var otherWithEmail = await _userRepository.FindAsync(u => u.Email == normalizedEmail && u.Id != id);
            if (otherWithEmail.Any())
                return BadRequest(new { message = "This email is already in use by another user." });
        }

        existing.FullName = dto.FullName.Trim();
        existing.Email = normalizedEmail;
        existing.RoleId = dto.RoleId;

        // Update password if provided
        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            existing.PasswordHash = _passwordHasher.HashPassword(existing, dto.Password.Trim());
        }

        await _userRepository.UpdateAsync(existing);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user == null) return NotFound();

        await _userRepository.DeleteAsync(user);
        return NoContent();
    }

    private static UserResponseDto MapToDto(User user, string? roleName = null, string? roleColor = null)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            RoleId = user.RoleId,
            RoleName = roleName ?? user.Role?.Name ?? "",
            RoleColor = roleColor ?? user.Role?.Color ?? ""
        };
    }
}
