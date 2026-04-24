using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NeuroPlan.Application.DTOs;
using NeuroPlan.Domain.Entities;
using NeuroPlan.Infrastructure.Persistence.Context;

namespace NeuroPlan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private static readonly IReadOnlyDictionary<string, string> SeededUserAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["admin"] = "admin@example.com",
        ["worker"] = "worker@example.com",
        ["manager"] = "manager@example.com"
    };

    private readonly IConfiguration _configuration;
    private readonly NeuroPlanDbContext _dbContext;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthController(
        IConfiguration configuration,
        NeuroPlanDbContext dbContext,
        IPasswordHasher<User> passwordHasher)
    {
        _configuration = configuration;
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var loginIdentifier = request.Username?.Trim();
        var providedPassword = request.Password?.Trim();

        if (string.IsNullOrWhiteSpace(loginIdentifier) || string.IsNullOrWhiteSpace(providedPassword))
        {
            return Unauthorized(new { Message = "Invalid credentials." });
        }

        var loginEmail = ResolveLoginEmail(loginIdentifier);

        var user = await _dbContext.Users
            .Include(entity => entity.Role)
            .ThenInclude(role => role.RolePermissions)
            .ThenInclude(rolePermission => rolePermission.Permission)
            .FirstOrDefaultAsync(entity => entity.Email == loginEmail);

        if (user is null)
        {
            return Unauthorized(new { Message = "Invalid credentials." });
        }

        if (!TryVerifyPassword(user, providedPassword, out var shouldRehash))
        {
            return Unauthorized(new { Message = "Invalid credentials." });
        }

        if (shouldRehash)
        {
            user.PasswordHash = _passwordHasher.HashPassword(user, providedPassword);
            await _dbContext.SaveChangesAsync();
        }

        if (user.Role is null)
        {
            return Unauthorized(new { Message = "Invalid credentials." });
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
            new(JwtRegisteredClaimNames.Sub, user.Email),
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

    private bool TryVerifyPassword(User user, string providedPassword, out bool shouldRehash)
    {
        shouldRehash = false;

        var verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, providedPassword);

        if (verificationResult == PasswordVerificationResult.Success)
        {
            return true;
        }

        if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
        {
            shouldRehash = true;
            return true;
        }

        // Backward compatibility for legacy plaintext-era records.
        if (string.Equals(user.PasswordHash, providedPassword, StringComparison.Ordinal))
        {
            shouldRehash = true;
            return true;
        }

        return false;
    }

    private static string ResolveLoginEmail(string usernameOrEmail)
    {
        var normalized = usernameOrEmail.Trim().ToLowerInvariant();
        
        // Only resolve aliases if it's not a full email address (doesn't contain '@')
        if (!normalized.Contains('@') && SeededUserAliases.TryGetValue(normalized, out var seededEmail))
        {
            return seededEmail;
        }
        
        return normalized;
    }
}
