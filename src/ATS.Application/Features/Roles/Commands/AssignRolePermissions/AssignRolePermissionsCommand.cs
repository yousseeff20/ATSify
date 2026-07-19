using MediatR;
using ATS.Application.Common.Models;
using ATS.Domain.Common;
using ATS.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ATS.Domain.Constants;

namespace ATS.Application.Features.Roles.Commands.AssignRolePermissions;

public record AssignRolePermissionsCommand(Guid RoleId, List<string> Permissions) : IRequest<Result>;

public class AssignRolePermissionsCommandValidator : AbstractValidator<AssignRolePermissionsCommand>
{
    public AssignRolePermissionsCommandValidator()
    {
        RuleFor(x => x.RoleId).NotEmpty();
        RuleFor(x => x.Permissions).NotNull();
        RuleForEach(x => x.Permissions).Must(p => Permissions.GetAll().Contains(p)).WithMessage("Invalid permission provided.");
    }
}

public class AssignRolePermissionsCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<AssignRolePermissionsCommand, Result>
{
    public async Task<Result> Handle(AssignRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        var role = await dbContext.DomainRoles
            .Include(r => r.RolePermissions)
            .FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);

        if (role == null)
            return Result.Failure("Role not found.");

        role.ClearPermissions();

        foreach (var permission in request.Permissions)
        {
            role.AddPermission(permission);
        }

        // TransactionBehavior handles SaveChangesAsync
        return Result.Success();
    }
}

