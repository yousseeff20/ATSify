using Microsoft.AspNetCore.Identity;

namespace ATS.Infrastructure.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    // Identity-specific user entity. Domain User uses the same Guid Id.
}
