namespace ATS.Domain.Common;

public abstract class SoftDeleteEntity : AuditableEntity
{
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public Guid? DeletedBy { get; set; }

    protected SoftDeleteEntity() { }
    protected SoftDeleteEntity(Guid id) : base(id) { }
}
