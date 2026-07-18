using ATS.Application.Common.Models;
using MediatR;

namespace ATS.Application.Features.Jobs.Commands.ArchiveJob;

public record ArchiveJobCommand(Guid JobId) : IRequest<Result>;
