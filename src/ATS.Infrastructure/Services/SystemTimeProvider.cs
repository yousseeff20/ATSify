using ATS.Application.Common.Interfaces;

namespace ATS.Infrastructure.Services;

public class SystemTimeProvider : ITimeProvider
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
