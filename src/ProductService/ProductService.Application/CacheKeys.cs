namespace ProductService.Application;

/// <summary>
/// Merkezi cache key tanımları.
/// SRP: Tüm key isimleri tek yerde yönetilir; magic string önlenir.
/// </summary>
public static class CacheKeys
{
    public const string ProductList = "products:all";
    public static string ProductById(Guid id) => $"products:{id}";
}
