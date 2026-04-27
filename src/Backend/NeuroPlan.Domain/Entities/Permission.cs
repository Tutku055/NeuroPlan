using System.Collections.Generic;

namespace NeuroPlan.Domain.Entities;

public class Permission : BaseEntity
{
    private Permission()
    {
    }

    public Permission(string systemName, string description)
    {
        UpdateDetails(systemName, description);
    }

    public Permission(Guid id, string systemName, string description)
        : base(id)
    {
        UpdateDetails(systemName, description);
    }

    public string SystemName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;

    // Navigation properties
    public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();

    public void UpdateDetails(string systemName, string description)
    {
        SystemName = systemName;
        Description = description;
    }
}
