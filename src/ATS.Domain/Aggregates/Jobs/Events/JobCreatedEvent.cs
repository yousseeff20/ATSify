using ATS.Domain.Common;

namespace ATS.Domain.Aggregates.Jobs.Events;

public record JobCreatedEvent(Guid JobId) : DomainEvent;
