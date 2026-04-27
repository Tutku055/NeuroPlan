namespace NeuroPlan.Application.DTOs;

public class LoginRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string Token { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string RoleColor { get; set; } = "#64748B";
    public string[] Permissions { get; set; } = Array.Empty<string>();
}

public class AuthenticatedUserDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string RoleColor { get; set; } = "#64748B";
    public string[] Permissions { get; set; } = Array.Empty<string>();
}