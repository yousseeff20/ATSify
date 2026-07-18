namespace ATS.Application.Common.Interfaces;

public interface IJwtProvider
{
    string GenerateToken(Guid userId, string email, Guid? companyId);
}
