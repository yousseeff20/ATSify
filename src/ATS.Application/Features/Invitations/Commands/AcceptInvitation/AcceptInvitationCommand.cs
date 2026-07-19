using MediatR;
using ATS.Application.Common.Models;
using ATS.Domain.Common;
using ATS.Application.Common.Interfaces;
using ATS.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace ATS.Application.Features.Invitations.Commands.AcceptInvitation;

public record AcceptInvitationCommand(string SecureToken) : IRequest<Result>;

public class AcceptInvitationCommandValidator : AbstractValidator<AcceptInvitationCommand>
{
    public AcceptInvitationCommandValidator()
    {
        RuleFor(x => x.SecureToken).NotEmpty();
    }
}

public class AcceptInvitationCommandHandler(IApplicationDbContext dbContext, ITimeProvider timeProvider) : IRequestHandler<AcceptInvitationCommand, Result>
{
    public async Task<Result> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        var invitation = await dbContext.Invitations
            .FirstOrDefaultAsync(i => i.SecureToken == request.SecureToken, cancellationToken);

        if (invitation == null)
            return Result.Failure("Invalid invitation token.");

        if (invitation.ExpirationDate < timeProvider.UtcNow && invitation.Status == InvitationStatus.Pending)
        {
            return Result.Failure("This invitation has expired.");
        }

        if (invitation.Status == InvitationStatus.Accepted)
            return Result.Failure("This invitation has already been accepted.");

        if (invitation.Status == InvitationStatus.Cancelled)
            return Result.Failure("This invitation has been cancelled.");

        invitation.Accept();

        return Result.Success();
    }
}

