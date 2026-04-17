using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NeuroPlan.Application.DTOs;
using NeuroPlan.Infrastructure.Persistence.Context;

namespace NeuroPlan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly NeuroPlanDbContext _dbContext;

    public AuthController(IConfiguration configuration, NeuroPlanDbContext dbContext)
    {
        _configuration = configuration;
        _dbContext = dbContext;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var loginEmail = ResolveLoginEmail(request.Username, request.Password);
        if (loginEmail is null)
        {
            return Unauthorized(new { Message = "Invalid credentials." });
        }

        var user = await _dbContext.Users.AsNoTracking()
            .Include(entity => entity.Role)
            .ThenInclude(role => role.RolePermissions)
            .ThenInclude(rolePermission => rolePermission.Permission)
            .FirstOrDefaultAsync(entity => entity.Email == loginEmail);

        if (user is null)
        {
            return Unauthorized(new { Message = "User is not initialized. Please check seed data." });
        }

        var permissions = user.Role.RolePermissions
            .Select(rolePermission => rolePermission.Permission.SystemName)
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(permission => permission)
            .ToArray();

        var packedPermissions = string.Join(',', permissions);

        var issuer = _configuration["JwtSettings:Issuer"];
        var audience = _configuration["JwtSettings:Audience"];
        var keyParam = _configuration["JwtSettings:Key"];

        // Fallback key if not in config for some reason
        if (string.IsNullOrEmpty(keyParam)) keyParam = "SuperSecretAndLongKeyForJwtTokens123456!!";

        var key = Encoding.UTF8.GetBytes(keyParam);
        var claims = new List<Claim>
        {
            new("Id", user.Id.ToString()),
            new(JwtRegisteredClaimNames.Sub, request.Username.Trim()),
            new(ClaimTypes.Role, user.Role.Name),
            new("Permissions", packedPermissions)
        };

        claims.AddRange(permissions.Select(permission => new Claim("Permission", permission)));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(120),
            Issuer = issuer ?? "NeuroPlanApi",
            Audience = audience ?? "NeuroPlanUsers",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwtToken = tokenHandler.WriteToken(token);

        return Ok(new LoginResponseDto
        {
            Token = jwtToken,
            Role = user.Role.Name,
            Permissions = permissions
        });
    }

    private static string? ResolveLoginEmail(string username, string password)
    {
        var normalizedUsername = username.Trim().ToLowerInvariant();

        if (normalizedUsername == "admin" && password == "admin")
        {
            return "admin@example.com";
        }

        if (normalizedUsername == "worker" && password == "worker")
        {
            return "worker@example.com";
        }

        if (normalizedUsername == "manager" && password == "manager")
        {
            return "manager@example.com";
        }

        return null;
    }
}
