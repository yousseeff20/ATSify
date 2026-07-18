using ATS.Application.Common.Interfaces;
using ATS.Application.Common.Models;
using ATS.Domain.Aggregates.Jobs.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Jobs.Commands.UpdateJob;

public class UpdateJobCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateJobCommand, Result>
{
    public async Task<Result> Handle(UpdateJobCommand request, CancellationToken cancellationToken)
    {
        var job = await context.Jobs
            .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);

        if (job == null)
            return Result.Failure("Job not found.");

        if (request.DepartmentId.HasValue)
        {
            var department = await context.Departments
                .FirstOrDefaultAsync(d => d.Id == request.DepartmentId.Value && !d.IsDeleted, cancellationToken);

            if (department == null)
                return Result.Failure("Department not found.");

            if (department.CompanyId != job.CompanyId)
                return Result.Failure("Department does not belong to the same company as the job.");
        }

        var salaryRange = new SalaryRange(request.SalaryMin, request.SalaryMax, request.SalaryCurrency);

        try
        {
            job.Update(
                request.Title,
                request.Description,
                request.EmploymentType,
                request.WorkplaceType,
                request.ExperienceLevel,
                request.Location,
                salaryRange,
                request.DepartmentId);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
