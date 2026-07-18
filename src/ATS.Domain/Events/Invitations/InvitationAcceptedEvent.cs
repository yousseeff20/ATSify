using ATS.Domain.Common;

namespace ATS.Domain.Events.Invitations;

public record InvitationAcceptedEvent(Guid InvitationId, Guid UserId) : DomainEvent;
