using MediatR;
using ATS.Application.Common.Models;
using ATS.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Users.Commands.AssignUserRole;

public record AssignUserRoleCommand(Guid UserId, Guid RoleId) : IRequest<Result>;

public class AssignUserRoleCommandValidator : AbstractValidator<AssignUserRoleCommand>
{
    public AssignUserRoleCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.RoleId).NotEmpty();
    }
}

public class AssignUserRoleCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<AssignUserRoleCommand, Result>
{
    public async Task<Result> Handle(AssignUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await dbContext.DomainUsers
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user == null)
            return Result.Failure("User not found.");

        var role = await dbContext.DomainRoles.FirstOrDefaultAsync(r => r.Id == request.RoleId, cancellationToken);
        if (role == null)
            return Result.Failure("Role not found.");

        user.AssignRole(role);

        // TransactionBehavior handles SaveChangesAsync
        return Result.Success();
    }
}
