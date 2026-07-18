using ATS.Application.Common.Models;
using MediatR;

namespace ATS.Application.Features.Jobs.Commands.CloseJob;

public record CloseJobCommand(Guid JobId) : IRequest<Result>;
