using MediatR;
using ATS.Application.Common.Models;
using ATS.Domain.Common;
using ATS.Application.Common.Interfaces;
using ATS.Domain.Aggregates.Users;
using FluentValidation;

namespace ATS.Application.Features.Users.Commands.CreateUser;

public record CreateUserCommand(string FirstName, string LastName, string Email, string Password, Guid? CompanyId) : IRequest<Result<Guid>>;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public class CreateUserCommandHandler(IIdentityService identityService, IApplicationDbContext dbContext) : IRequestHandler<CreateUserCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var (identityResult, userId) = await identityService.CreateUserAsync(request.Email, request.Password);
        
        if (!identityResult.IsSuccess)
        {
            return Result<Guid>.Failure(identityResult.ErrorMessage!);
        }

        var user = new User(userId, request.FirstName, request.LastName, request.Email, request.CompanyId);
        dbContext.DomainUsers.Add(user);
        
        // TransactionBehavior handles SaveChangesAsync

        return Result<Guid>.Success(user.Id);
    }
}

