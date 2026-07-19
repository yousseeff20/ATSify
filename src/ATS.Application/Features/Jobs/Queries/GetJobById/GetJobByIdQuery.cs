using ATS.Application.Common.Models;
using ATS.Domain.Common;
using MediatR;

namespace ATS.Application.Features.Jobs.Queries.GetJobById;

public record GetJobByIdQuery(Guid JobId) : IRequest<Result<JobDto>>;

