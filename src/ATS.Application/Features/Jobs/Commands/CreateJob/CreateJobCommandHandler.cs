using ATS.Application.Common.Interfaces;
using ATS.Application.Common.Models;
using ATS.Domain.Aggregates.Jobs;
using ATS.Domain.Aggregates.Jobs.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Jobs.Commands.CreateJob;

public class CreateJobCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateJobCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateJobCommand request, CancellationToken cancellationToken)
    {
        var companyExists = await context.Companies
            .AnyAsync(c => c.Id == request.CompanyId && c.IsActive && !c.IsDeleted, cancellationToken);
            
        if (!companyExists)
            return Result<Guid>.Failure("Company not found or inactive.");

        if (request.DepartmentId.HasValue)
            {
                var department = await context.Departments
                    .FirstOrDefaultAsync(d => d.Id == request.DepartmentId.Value && !d.IsDeleted, cancellationToken);

                if (department == null)
                    return Result<Guid>.Failure("Department not found.");
                    
                if (department.CompanyId != request.CompanyId)
                    return Result<Guid>.Failure("Department does not belong to the specified company.");
            }

        var salaryRange = new SalaryRange(request.SalaryMin, request.SalaryMax, request.SalaryCurrency);

        var job = new Job(
            Guid.NewGuid(),
            request.CompanyId,
            request.Title,
            request.Description,
            request.EmploymentType,
            request.WorkplaceType,
            request.ExperienceLevel,
            request.Location,
            salaryRange,
            request.DepartmentId);

        context.Jobs.Add(job);
        await context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(job.Id);
    }
}
