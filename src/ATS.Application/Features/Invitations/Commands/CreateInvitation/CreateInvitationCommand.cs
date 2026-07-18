using MediatR;
using ATS.Application.Common.Models;
using ATS.Application.Common.Interfaces;
using ATS.Domain.Aggregates.Invitations;
using ATS.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;

namespace ATS.Application.Features.Invitations.Commands.CreateInvitation;

public record CreateInvitationCommand(string Email, Guid CompanyId, Guid? DepartmentId, Guid RoleId) : IRequest<Result<Guid>>;

public class CreateInvitationCommandValidator : AbstractValidator<CreateInvitationCommand>
{
    public CreateInvitationCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(256);
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.RoleId).NotEmpty();
    }
}

public class CreateInvitationCommandHandler(IApplicationDbContext dbContext, ITimeProvider timeProvider) : IRequestHandler<CreateInvitationCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateInvitationCommand request, CancellationToken cancellationToken)
    {
        var companyExists = await dbContext.Companies.AnyAsync(c => c.Id == request.CompanyId, cancellationToken);
        if (!companyExists) return Result<Guid>.Failure("Company not found.");

        var roleExists = await dbContext.DomainRoles.AnyAsync(r => r.Id == request.RoleId, cancellationToken);
        if (!roleExists) return Result<Guid>.Failure("Role not found.");

        if (request.DepartmentId.HasValue)
        {
            var deptExists = await dbContext.Departments.AnyAsync(d => d.Id == request.DepartmentId && d.CompanyId == request.CompanyId, cancellationToken);
            if (!deptExists) return Result<Guid>.Failure("Department not found in the specified company.");
        }

        var now = timeProvider.UtcNow;
        var existingActive = await dbContext.Invitations
            .Where(i => i.CompanyId == request.CompanyId && i.Email == request.Email)
            .AnyAsync(i => i.Status == InvitationStatus.Pending && i.ExpirationDate > now, cancellationToken);
        
        if (existingActive)
        {
            return Result<Guid>.Failure("An active invitation already exists for this email in this company.");
        }

        var secureToken = GenerateSecureToken();
        var expiration = now.AddDays(7);

        var invitation = new Invitation(Guid.NewGuid(), request.Email, request.CompanyId, request.DepartmentId, request.RoleId, expiration, secureToken);
        dbContext.Invitations.Add(invitation);

        return Result<Guid>.Success(invitation.Id);
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
