using ATS.Domain.Common;

namespace ATS.Domain.Events.Companies;

public record CompanyCreatedEvent(Guid CompanyId) : DomainEvent;
