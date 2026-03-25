using Microsoft.Extensions.Diagnostics.HealthChecks;
using RabbitMQ.Client;

namespace Shared.Infrastructure.HealthChecks;

/// <summary>
/// RabbitMQ bağlantısını kontrol eden health check.
/// 12-Factor: Destek servislerinin sağlığı bağımsız olarak izlenir.
/// </summary>
public class RabbitMqHealthCheck : IHealthCheck
{
    private readonly string _connectionString;

    public RabbitMqHealthCheck(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var factory = new ConnectionFactory { Uri = new Uri(_connectionString) };
            using var connection = await factory.CreateConnectionAsync(cancellationToken);
            return HealthCheckResult.Healthy("RabbitMQ is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("RabbitMQ is unreachable.", ex);
        }
    }
}
