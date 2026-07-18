using ATS.Application.Common.Interfaces;
using ATS.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Jobs.Commands.PublishJob;

public class PublishJobCommandHandler(IApplicationDbContext context, ITimeProvider dateTimeProvider) : IRequestHandler<PublishJobCommand, Result>
{
    public async Task<Result> Handle(PublishJobCommand request, CancellationToken cancellationToken)
    {
        var job = await context.Jobs
            .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);

        if (job == null)
            return Result.Failure("Job not found.");

        if (!job.DepartmentId.HasValue)
            return Result.Failure("DepartmentId must be set before publishing a job.");

        var department = await context.Departments
            .FirstOrDefaultAsync(d => d.Id == job.DepartmentId.Value && !d.IsDeleted, cancellationToken);

        if (department == null)
            return Result.Failure("Department not found.");

        if (department.CompanyId != job.CompanyId)
            return Result.Failure("Department does not belong to the same company as the job.");

        if (!department.IsActive)
            return Result.Failure("Department must be active to publish a job.");

        try
        {
            job.Publish(dateTimeProvider.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
