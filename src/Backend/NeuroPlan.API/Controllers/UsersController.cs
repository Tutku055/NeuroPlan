using System;
using Microsoft.AspNetCore.Authorization;
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

    public UsersController(IUserRepository userRepository, IRoleRepository roleRepository)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
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

        var role = await _roleRepository.GetByIdAsync(dto.RoleId);
        if (role == null)
            return BadRequest(new { message = "Invalid role." });

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Email = dto.Email.Trim(),
            PasswordHash = dto.Password ?? "hashed",
            RoleId = dto.RoleId
        };

        await _userRepository.AddAsync(user);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, MapToDto(user, role.Name));
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

        existing.FullName = dto.FullName.Trim();
        existing.Email = dto.Email.Trim();
        existing.RoleId = dto.RoleId;

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

    private static UserResponseDto MapToDto(User user, string? roleName = null)
    {
        return new UserResponseDto
        {
            Id = user.Id,
            FullName = user.FullName,
            Email = user.Email,
            RoleId = user.RoleId,
            RoleName = roleName ?? user.Role?.Name ?? ""
        };
    }
}
