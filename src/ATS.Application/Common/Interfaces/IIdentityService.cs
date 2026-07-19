using ATS.Application.Common.Models;
using ATS.Domain.Common;

namespace ATS.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<(Result Result, Guid UserId)> CreateUserAsync(string email, string password);
    Task<Result> DeleteUserAsync(Guid userId);
}

