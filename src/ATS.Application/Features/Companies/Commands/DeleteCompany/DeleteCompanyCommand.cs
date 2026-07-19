using MediatR;
using ATS.Application.Common.Models;
using ATS.Domain.Common;
using ATS.Application.Common.Interfaces;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Companies.Commands.DeleteCompany;

public record DeleteCompanyCommand(Guid CompanyId) : IRequest<Result>;

public class DeleteCompanyCommandValidator : AbstractValidator<DeleteCompanyCommand>
{
    public DeleteCompanyCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
    }
}

public class DeleteCompanyCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<DeleteCompanyCommand, Result>
{
    public async Task<Result> Handle(DeleteCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = await dbContext.Companies.FirstOrDefaultAsync(c => c.Id == request.CompanyId, cancellationToken);
        if (company == null) return Result.Failure("Company not found.");

        bool hasDepartments = await dbContext.Departments.AnyAsync(d => d.CompanyId == request.CompanyId, cancellationToken);
        bool hasInvitations = await dbContext.Invitations.AnyAsync(i => i.CompanyId == request.CompanyId, cancellationToken);
        bool hasUsers = await dbContext.DomainUsers.AnyAsync(u => u.CompanyId == request.CompanyId, cancellationToken);

        if (hasDepartments || hasInvitations || hasUsers)
        {
            return Result.Failure("Cannot delete company because it contains business data (Departments, Invitations, or Users).");
        }

        company.IsDeleted = true;
        company.DeletedAt = DateTimeOffset.UtcNow;
        
        return Result.Success();
    }
}

