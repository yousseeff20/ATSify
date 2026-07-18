using ATS.Domain.Common;

namespace ATS.Domain.Events.Departments;

public record DepartmentCreatedEvent(Guid DepartmentId) : DomainEvent;
