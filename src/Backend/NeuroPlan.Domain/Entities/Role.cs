using System.Collections.Generic;

namespace NeuroPlan.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#64748B";

    // Navigation properties
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<User> Users { get; set; } = new List<User>();

    public void UpdateDetails(string name, string color)
    {
        Name = name;
        Color = color;
    }
}
