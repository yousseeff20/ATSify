using System.Security.Claims;
using ATS.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ATS.Infrastructure.Authorization;

public class PermissionAuthorizationHandler(AtsDbContext dbContext) 
    : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(userIdStr, out var userId))
            return;

        var hasPermission = await dbContext.DomainUserRoles
            .Where(ur => ur.UserId == userId)
            .Join(dbContext.RolePermissions, ur => ur.RoleId, rp => rp.RoleId, (ur, rp) => rp)
            .AnyAsync(rp => rp.PermissionName == requirement.Permission);

        if (hasPermission)
        {
            context.Succeed(requirement);
        }
    }
}
