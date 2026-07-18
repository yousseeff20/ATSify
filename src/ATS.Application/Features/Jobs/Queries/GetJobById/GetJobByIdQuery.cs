using ATS.Application.Common.Models;
using MediatR;

namespace ATS.Application.Features.Jobs.Queries.GetJobById;

public record GetJobByIdQuery(Guid JobId) : IRequest<Result<JobDto>>;
