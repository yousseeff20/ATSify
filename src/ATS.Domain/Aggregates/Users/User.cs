using ATS.Domain.Common;

namespace ATS.Domain.Aggregates.Users;

public class User : AggregateRoot
{
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }
    public bool IsActive { get; private set; }
    public Guid? CompanyId { get; private set; }

    private readonly List<UserRole> _userRoles = [];
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    private User() 
    { 
        FirstName = null!;
        LastName = null!;
        Email = null!;
    } // EF Core

    public User(Guid id, string firstName, string lastName, string email, Guid? companyId)
        : base(id)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        CompanyId = companyId;
        IsActive = true;
    }

    public void UpdateProfile(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void AssignRole(Role role)
    {
        if (_userRoles.Any(ur => ur.RoleId == role.Id))
            return;

        _userRoles.Add(new UserRole(Id, role.Id));
    }

    public void RemoveRole(Role role)
    {
        var userRole = _userRoles.FirstOrDefault(ur => ur.RoleId == role.Id);
        if (userRole != null)
        {
            _userRoles.Remove(userRole);
        }
    }
}
