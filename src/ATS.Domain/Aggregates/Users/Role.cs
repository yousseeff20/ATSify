using ATS.Domain.Common;

namespace ATS.Domain.Aggregates.Users;

public class Role : AggregateRoot
{
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public Guid? CompanyId { get; private set; }

    private readonly List<RolePermission> _rolePermissions = [];
    public IReadOnlyCollection<RolePermission> RolePermissions => _rolePermissions.AsReadOnly();

    private readonly List<UserRole> _userRoles = [];
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private Role() 
    {
        Name = null!;
    } // EF Core

    public Role(Guid id, string name, string? description, Guid? companyId)
        : base(id)
    {
        Name = name;
        Description = description;
        CompanyId = companyId;
    }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public void AddPermission(string permissionName)
    {
        if (_rolePermissions.Any(rp => rp.PermissionName == permissionName))
            return;

        _rolePermissions.Add(new RolePermission(Id, permissionName));
    }

    public void RemovePermission(string permissionName)
    {
        var permission = _rolePermissions.FirstOrDefault(rp => rp.PermissionName == permissionName);
        if (permission != null)
        {
            _rolePermissions.Remove(permission);
        }
    }
    
    public void ClearPermissions()
    {
        _rolePermissions.Clear();
    }
}
