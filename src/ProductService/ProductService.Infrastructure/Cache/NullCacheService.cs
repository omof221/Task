using ProductService.Application.Interfaces;

namespace ProductService.Infrastructure.Cache;

/// <summary>
/// Test ortamı için no-op ICacheService implementasyonu.
/// Redis bağlantısı olmadan integration test çalışabilir.
/// Her zaman null döner (cache miss) → handler DB'ye gider.
/// </summary>
public sealed class NullCacheService : ICacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
        => Task.FromResult<T?>(null);

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class
        => Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task RemoveByPatternAsync(string pattern, CancellationToken ct = default)
        => Task.CompletedTask;
}
