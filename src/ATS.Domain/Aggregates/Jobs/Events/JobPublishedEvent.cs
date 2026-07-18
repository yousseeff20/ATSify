using ATS.Domain.Common;

namespace ATS.Domain.Aggregates.Jobs.Events;

public record JobPublishedEvent(Guid JobId) : DomainEvent;
