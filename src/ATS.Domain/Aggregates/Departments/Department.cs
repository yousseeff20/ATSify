using ATS.Domain.Common;
using ATS.Domain.Events.Departments;

namespace ATS.Domain.Aggregates.Departments;

public class Department : AggregateRoot, ISoftDelete
{
    public string Name { get; private set; }
    public string? Description { get; private set; }
    public Guid CompanyId { get; private set; }
    public bool IsActive { get; private set; }

    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    private Department() 
    { 
        Name = null!;
    } // EF Core

    public Department(Guid id, string name, string? description, Guid companyId) : base(id)
    {
        Name = name;
        Description = description;
        CompanyId = companyId;
        IsActive = true;

        AddDomainEvent(new DepartmentCreatedEvent(Id));
    }

    public void Update(string name, string? description)
    {
        Name = name;
        Description = description;
    }

    public void SetActiveStatus(bool isActive)
    {
        IsActive = isActive;
    }
}
