using MediatR;
using ATS.Application.Common.Models;
using ATS.Domain.Common;
using ATS.Application.Common.Interfaces;
using ATS.Domain.Aggregates.Companies;
using ATS.Domain.Aggregates.Departments;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Departments.Commands.CreateDepartment;

public record CreateDepartmentCommand(Guid CompanyId, string Name, string? Description) : IRequest<Result<Guid>>;

public class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(x => x.CompanyId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(1000);
    }
}

public class CreateDepartmentCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<CreateDepartmentCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var companyExists = await dbContext.Companies.AnyAsync(c => c.Id == request.CompanyId, cancellationToken);
        if (!companyExists) return Result<Guid>.Failure("Company not found.");

        var nameExists = await dbContext.Departments.AnyAsync(d => d.CompanyId == request.CompanyId && d.Name == request.Name, cancellationToken);
        if (nameExists) return Result<Guid>.Failure("A department with this name already exists in the company.");

        var department = new Department(Guid.NewGuid(), request.Name, request.Description, request.CompanyId);
        dbContext.Departments.Add(department);

        return Result<Guid>.Success(department.Id);
    }
}

