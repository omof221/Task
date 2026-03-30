using Microsoft.Extensions.Logging;
using ProductService.Application.Interfaces;
using StackExchange.Redis;
using System.Text.Json;

namespace ProductService.Infrastructure.Cache;

/// ICacheService'in Redis implementasyonu (StackExchange.Redis).
/// DIP: Application katmanı ICacheService'e bağımlıdır, bu sınıfa değil.
/// Cache-Aside pattern'ı destekler: Get → miss → DB → Set.
/// Redis erişim hatalarında loglayıp null döner (graceful degradation).
public sealed class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisCacheService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
    {
        _redis  = redis;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        try
        {
            var db    = _redis.GetDatabase();
            var value = await db.StringGetAsync(key);

            if (value.IsNullOrEmpty)
                return null;

            return JsonSerializer.Deserialize<T>(value!, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cache] GET hatası: {Key}", key);
            return null; // Graceful degradation — Redis yoksa DB'den devam et
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class
    {
        try
        {
            var db   = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(value, JsonOptions);
            await db.StringSetAsync(key, json, expiry ?? TimeSpan.FromMinutes(5));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cache] SET hatası: {Key}", key);
        }
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var db = _redis.GetDatabase();
            await db.KeyDeleteAsync(key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cache] REMOVE hatası: {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken ct = default)
    {
        try
        {
            var server  = _redis.GetServer(_redis.GetEndPoints().First());
            var keys    = server.Keys(pattern: pattern).ToArray();
            var db      = _redis.GetDatabase();
            if (keys.Length > 0)
                await db.KeyDeleteAsync(keys);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[Cache] REMOVE PATTERN hatası: {Pattern}", pattern);
        }
    }
}
