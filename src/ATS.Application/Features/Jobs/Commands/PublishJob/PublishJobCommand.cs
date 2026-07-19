using ATS.Application.Common.Models;
using ATS.Domain.Common;
using MediatR;

namespace ATS.Application.Features.Jobs.Commands.PublishJob;

public record PublishJobCommand(Guid JobId) : IRequest<Result>;

