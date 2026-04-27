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

public class AuthService : IAuthService
{
    private static readonly IReadOnlyDictionary<string, string> SeededUserAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["admin"] = "admin@example.com",
        ["worker"] = "worker@example.com",
        ["manager"] = "manager@example.com"
    };

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthService(IUserRepository userRepository, IPasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<AuthenticatedUserDto?> AuthenticateAsync(LoginRequestDto request)
    {
        var loginIdentifier = request.Username?.Trim();
        var providedPassword = request.Password?.Trim();

        if (string.IsNullOrWhiteSpace(loginIdentifier) || string.IsNullOrWhiteSpace(providedPassword))
        {
            return null;
        }

        var loginEmail = ResolveLoginEmail(loginIdentifier);

        var user = await _userRepository.GetByEmailWithRolePermissionsAsync(loginEmail);
        if (user is null)
        {
            return null;
        }

        if (!TryVerifyPassword(user, providedPassword, out var shouldRehash))
        {
            return null;
        }

        if (shouldRehash)
        {
            user.SetPasswordHash(_passwordHasher.HashPassword(user, providedPassword));
            await _userRepository.UpdateAsync(user);
        }

        if (user.Role is null)
        {
            return null;
        }

        var permissions = user.Role.RolePermissions
            .Select(rolePermission => rolePermission.Permission.SystemName)
            .Where(permission => !string.IsNullOrWhiteSpace(permission))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(permission => permission)
            .ToArray();

        return new AuthenticatedUserDto
        {
            UserId = user.Id,
            Email = user.Email,
            Role = user.Role.Name,
            RoleColor = user.Role.Color,
            Permissions = permissions
        };
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

        if (!normalized.Contains('@') && SeededUserAliases.TryGetValue(normalized, out var seededEmail))
        {
            return seededEmail;
        }

        return normalized;
    }
}
