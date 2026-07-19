using ATS.Application.Common.Models;
using ATS.Domain.Common;
using MediatR;

namespace ATS.Application.Features.Jobs.Commands.CloseJob;

public record CloseJobCommand(Guid JobId) : IRequest<Result>;

