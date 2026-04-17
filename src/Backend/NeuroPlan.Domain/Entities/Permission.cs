using System.Collections.Generic;

namespace NeuroPlan.Domain.Entities;

public class Permission : BaseEntity
{
    public string SystemName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
