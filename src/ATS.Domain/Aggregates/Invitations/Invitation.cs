using ATS.Domain.Common;
using ATS.Domain.Enums;
using ATS.Domain.Events.Invitations;

namespace ATS.Domain.Aggregates.Invitations;

public class Invitation : AggregateRoot
{
    public string Email { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid? DepartmentId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTimeOffset ExpirationDate { get; private set; }
    public string SecureToken { get; private set; }
    public InvitationStatus Status { get; private set; }

    private Invitation() 
    { 
        Email = null!;
        SecureToken = null!;
    } // EF Core

    public Invitation(Guid id, string email, Guid companyId, Guid? departmentId, Guid roleId, DateTimeOffset expirationDate, string secureToken) : base(id)
    {
        Email = email;
        CompanyId = companyId;
        DepartmentId = departmentId;
        RoleId = roleId;
        ExpirationDate = expirationDate;
        SecureToken = secureToken;
        Status = InvitationStatus.Pending;

        AddDomainEvent(new InvitationCreatedEvent(Id));
    }

    public void Accept()
    {
        Status = InvitationStatus.Accepted;
    }

    public void Cancel()
    {
        if (Status == InvitationStatus.Pending)
        {
            Status = InvitationStatus.Cancelled;
        }
    }

    public void Resend(DateTimeOffset newExpirationDate, string newSecureToken)
    {
        ExpirationDate = newExpirationDate;
        SecureToken = newSecureToken;
        Status = InvitationStatus.Pending;
    }
}
