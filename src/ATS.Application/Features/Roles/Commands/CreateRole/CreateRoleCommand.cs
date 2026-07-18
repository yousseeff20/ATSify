using MediatR;
using ATS.Application.Common.Models;
using ATS.Application.Common.Interfaces;
using ATS.Domain.Aggregates.Users;
using FluentValidation;

namespace ATS.Application.Features.Roles.Commands.CreateRole;

public record CreateRoleCommand(string Name, string? Description, Guid? CompanyId) : IRequest<Result<Guid>>;

public class CreateRoleCommandValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Description).MaximumLength(500);
    }
}

public class CreateRoleCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<CreateRoleCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = new Role(Guid.NewGuid(), request.Name, request.Description, request.CompanyId);
        dbContext.DomainRoles.Add(role);

        // TransactionBehavior handles SaveChangesAsync

        return await Task.FromResult(Result<Guid>.Success(role.Id));
    }
}
