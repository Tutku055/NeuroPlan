using System;

namespace NeuroPlan.Application.DTOs;

public class RoleResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid[] PermissionIds { get; set; } = Array.Empty<Guid>();
}

public class CreateRoleRequestDto
{
    public string Name { get; set; } = string.Empty;
    public Guid[] PermissionIds { get; set; } = Array.Empty<Guid>();
}

public class UpdateRoleRequestDto
{
    public string Name { get; set; } = string.Empty;
    public Guid[] PermissionIds { get; set; } = Array.Empty<Guid>();
}

public class PermissionResponseDto
{
    public Guid Id { get; set; }
    public string SystemName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
