namespace ATS.Domain.Common;

public abstract record DomainEvent(Guid EventId, DateTimeOffset OccurredOn)
{
    protected DomainEvent() : this(Guid.NewGuid(), DateTimeOffset.UtcNow)
    {
    }
}
