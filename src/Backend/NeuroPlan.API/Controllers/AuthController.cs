using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using NeuroPlan.Application.DTOs;
using NeuroPlan.Application.Interfaces;

namespace NeuroPlan.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IAuthService _authService;

    public AuthController(
        IConfiguration configuration,
        IAuthService authService)
    {
        _configuration = configuration;
        _authService = authService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var authenticatedUser = await _authService.AuthenticateAsync(request);
        if (authenticatedUser is null)
        {
            return Unauthorized(new { Message = "Invalid credentials." });
        }

        var permissions = authenticatedUser.Permissions;

        var packedPermissions = string.Join(',', permissions);

        var issuer = _configuration["JwtSettings:Issuer"];
        var audience = _configuration["JwtSettings:Audience"];
        var keyParam = _configuration["JwtSettings:Key"];

        // Fallback key if not in config for some reason
        if (string.IsNullOrEmpty(keyParam)) keyParam = "SuperSecretAndLongKeyForJwtTokens123456!!";

        var key = Encoding.UTF8.GetBytes(keyParam);
        var claims = new List<Claim>
        {
            new("Id", authenticatedUser.UserId.ToString()),
            new(JwtRegisteredClaimNames.Sub, authenticatedUser.Email),
            new(ClaimTypes.Role, authenticatedUser.Role),
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
            Role = authenticatedUser.Role,
            Permissions = permissions
        });
    }
}
