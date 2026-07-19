using ATS.Domain.Common;
using MediatR;

namespace ATS.Application.Features.Jobs.Queries.GetPublicJobById;

public record GetPublicJobByIdQuery(Guid JobId) : IRequest<Result<JobDto>>;
