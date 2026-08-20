namespace PhiUsageAnalytics.Application.Interfaces;

/// <summary>
/// Cache abstraction. Implemented by Redis.
/// All cached data expires at midnight (start of next day).
/// </summary>
public interface ICacheService
{
    /// <summary>Get a cached value. Returns default if not found.</summary>
    Task<T?> GetAsync<T>(string key);

    /// <summary>Store a value in cache with expiry at next midnight.</summary>
    Task SetAsync<T>(string key, T value);

    /// <summary>Remove a specific key from cache.</summary>
    Task RemoveAsync(string key);

    /// <summary>Remove all keys matching a pattern (e.g., "org:123:*").</summary>
    Task RemoveByPrefixAsync(string prefix);
}
