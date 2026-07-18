using ATS.Application.Common.Models;
using MediatR;

namespace ATS.Application.Features.Jobs.Commands.PublishJob;

public record PublishJobCommand(Guid JobId) : IRequest<Result>;
