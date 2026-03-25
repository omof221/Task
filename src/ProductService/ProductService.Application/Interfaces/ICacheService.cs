namespace ProductService.Application.Interfaces;

/// <summary>
/// Cache sözleşmesi — Redis implementasyonundan bağımsız.
/// ISP: Generic get/set/remove metotları; başka cache stratejileri
///      bu arayüzü implement ederek sisteme eklenebilir (OCP).
/// DIP: Handler'lar StackExchange.Redis'i değil bu arayüzü bilir.
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class;
    Task RemoveAsync(string key, CancellationToken ct = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken ct = default);
}
