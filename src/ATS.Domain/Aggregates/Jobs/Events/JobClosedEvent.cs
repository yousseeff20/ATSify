using ATS.Domain.Common;

namespace ATS.Domain.Aggregates.Jobs.Events;

public record JobClosedEvent(Guid JobId) : DomainEvent;
