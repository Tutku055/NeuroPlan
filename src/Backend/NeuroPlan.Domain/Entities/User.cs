using System;
using System.Collections.Generic;

namespace NeuroPlan.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;

    // Navigation properties
    public ICollection<Worklog> Worklogs { get; set; } = new List<Worklog>();
}
