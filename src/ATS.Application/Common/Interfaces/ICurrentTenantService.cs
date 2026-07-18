namespace ATS.Application.Common.Interfaces;

public interface ICurrentTenantService
{
    Guid? CompanyId { get; }
}
