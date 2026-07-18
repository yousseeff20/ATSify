using ATS.Domain.Common;

namespace ATS.Domain.Aggregates.Jobs.Events;

public record JobArchivedEvent(Guid JobId) : DomainEvent;
