using ATS.Application.Common.Interfaces;
using ATS.Application.Common.Models;
using Microsoft.AspNetCore.Identity;

namespace ATS.Infrastructure.Identity;

public class IdentityService(UserManager<ApplicationUser> userManager) : IIdentityService
{
    public async Task<(Result Result, Guid UserId)> CreateUserAsync(string email, string password)
    {
        var user = new ApplicationUser
        {
            UserName = email,
            Email = email
        };

        var result = await userManager.CreateAsync(user, password);

        return result.Succeeded
            ? (Result.Success(), user.Id)
            : (Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description))), Guid.Empty);
    }

    public async Task<Result> DeleteUserAsync(Guid userId)
    {
        var user = await userManager.FindByIdAsync(userId.ToString());
        
        if (user == null)
            return Result.Success();

        var result = await userManager.DeleteAsync(user);

        return result.Succeeded
            ? Result.Success()
            : Result.Failure(string.Join("; ", result.Errors.Select(e => e.Description)));
    }
}
