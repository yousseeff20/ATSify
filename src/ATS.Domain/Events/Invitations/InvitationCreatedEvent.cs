using ATS.Domain.Common;

namespace ATS.Domain.Events.Invitations;

public record InvitationCreatedEvent(Guid InvitationId) : DomainEvent;
