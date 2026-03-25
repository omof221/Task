using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace Shared.Infrastructure.HealthChecks;

/// <summary>
/// Redis bağlantısını kontrol eden health check.
/// 12-Factor: Bağımlı servislerin sağlığı merkezi olarak izlenir.
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _redis;

    public RedisHealthCheck(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.PingAsync();
            return HealthCheckResult.Healthy("Redis is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis is unreachable.", ex);
        }
    }
}
