using ATS.Application.Common.Interfaces;
using ATS.Application.Common.Models;
using ATS.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Jobs.Commands.ArchiveJob;

public class ArchiveJobCommandHandler(IApplicationDbContext context) : IRequestHandler<ArchiveJobCommand, Result>
{
    public async Task<Result> Handle(ArchiveJobCommand request, CancellationToken cancellationToken)
    {
        var job = await context.Jobs
            .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);

        if (job == null)
            return Result.Failure("Job not found.");

        job.Archive();

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

