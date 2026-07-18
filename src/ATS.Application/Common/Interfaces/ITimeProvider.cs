namespace ATS.Application.Common.Interfaces;

public interface ITimeProvider
{
    DateTimeOffset UtcNow { get; }
}
