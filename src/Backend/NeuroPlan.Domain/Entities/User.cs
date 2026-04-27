using System;
using System.Collections.Generic;

namespace NeuroPlan.Domain.Entities;

public class User : BaseEntity
{
    private User()
    {
    }

    public User(string fullName, string email, Guid roleId)
    {
        UpdateProfile(fullName, email, roleId);
    }

    public User(Guid id, string fullName, string email, Guid roleId)
        : base(id)
    {
        UpdateProfile(fullName, email, roleId);
    }

    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;

    public Guid RoleId { get; private set; }
    public Role Role { get; private set; } = null!;

    // Navigation properties
    public ICollection<Worklog> Worklogs { get; private set; } = new List<Worklog>();

    public void UpdateProfile(string fullName, string email, Guid roleId)
    {
        FullName = fullName;
        Email = email;
        RoleId = roleId;
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
    }
}
