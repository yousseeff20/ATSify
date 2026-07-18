namespace ATS.Domain.Aggregates.Users;

public class RolePermission
{
    public Guid RoleId { get; private set; }
    public string PermissionName { get; private set; }

    private RolePermission() 
    { 
        PermissionName = null!;
    } // EF Core

    internal RolePermission(Guid roleId, string permissionName)
    {
        RoleId = roleId;
        PermissionName = permissionName;
    }
}
