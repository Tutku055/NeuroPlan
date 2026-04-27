using System.Collections.Generic;

namespace NeuroPlan.Domain.Entities;

public class Role : BaseEntity
{
    private Role()
    {
    }

    public Role(string name, string color)
    {
        UpdateDetails(name, color);
    }

    public Role(Guid id, string name, string color)
        : base(id)
    {
        UpdateDetails(name, color);
    }

    public string Name { get; private set; } = string.Empty;
    public string Color { get; private set; } = "#64748B";

    // Navigation properties
    public ICollection<RolePermission> RolePermissions { get; private set; } = new List<RolePermission>();
    public ICollection<User> Users { get; private set; } = new List<User>();

    public void UpdateDetails(string name, string color)
    {
        Name = name;
        Color = color;
    }
}
