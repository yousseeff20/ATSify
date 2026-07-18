namespace ATS.Domain.Common;

public abstract class AggregateRoot : AuditableEntity
{
    protected AggregateRoot() { }
    protected AggregateRoot(Guid id) : base(id) { }

    private readonly List<DomainEvent> _domainEvents = [];

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(DomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
