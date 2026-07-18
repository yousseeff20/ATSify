using ATS.Domain.Common;
using ATS.Domain.Events.Companies;

namespace ATS.Domain.Aggregates.Companies;

public class Company : AggregateRoot, ISoftDelete
{
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public bool IsActive { get; private set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    private Company() 
    { 
        Name = null!;
        Slug = null!;
    } // EF Core

    public Company(Guid id, string name, string slug) : base(id)
    {
        Name = name;
        Slug = slug;
        IsActive = true;
        
        AddDomainEvent(new CompanyCreatedEvent(Id));
    }

    public void Update(string name, string slug)
    {
        Name = name;
        Slug = slug;
    }

    public void SetActiveStatus(bool isActive)
    {
        IsActive = isActive;
    }
}
