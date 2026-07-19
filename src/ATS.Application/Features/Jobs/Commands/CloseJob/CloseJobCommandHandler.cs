using ATS.Application.Common.Interfaces;
using ATS.Application.Common.Models;
using ATS.Domain.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Jobs.Commands.CloseJob;

public class CloseJobCommandHandler(IApplicationDbContext context, ITimeProvider dateTimeProvider) : IRequestHandler<CloseJobCommand, Result>
{
    public async Task<Result> Handle(CloseJobCommand request, CancellationToken cancellationToken)
    {
        var job = await context.Jobs
            .FirstOrDefaultAsync(j => j.Id == request.JobId, cancellationToken);

        if (job == null)
            return Result.Failure("Job not found.");

        try
        {
            job.Close(dateTimeProvider.UtcNow);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Failure(ex.Message);
        }

        await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}

