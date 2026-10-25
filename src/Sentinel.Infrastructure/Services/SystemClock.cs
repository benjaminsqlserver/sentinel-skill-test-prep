using Sentinel.Application.Abstractions.Ports;

namespace Sentinel.Infrastructure.Services;

internal sealed class SystemClock(TimeProvider timeProvider) : IClock
{
    public DateTime UtcNow => timeProvider.GetUtcNow().UtcDateTime;
}
