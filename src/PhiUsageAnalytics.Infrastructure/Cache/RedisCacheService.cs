using System.Text.Json;
using PhiUsageAnalytics.Application.Interfaces;
using StackExchange.Redis;

namespace PhiUsageAnalytics.Infrastructure.Cache;

/// <summary>
/// Redis cache implementation.
/// All values expire at next midnight (12:00 AM).
/// Fresh data is fetched from DB once per day.
/// </summary>
public class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IDatabase _db;

    public RedisCacheService(IConnectionMultiplexer redis)
    {
        _redis = redis;
        _db = redis.GetDatabase();
    }

    public async Task<T?> GetAsync<T>(string key)
    {
        var value = await _db.StringGetAsync(key);
        if (value.IsNullOrEmpty) return default;

        return JsonSerializer.Deserialize<T>(value!);
    }

    public async Task SetAsync<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value);
        var ttl = GetTimeUntilMidnight();
        await _db.StringSetAsync(key, json, ttl);
    }

    public async Task RemoveAsync(string key)
    {
        await _db.KeyDeleteAsync(key);
    }

    public async Task RemoveByPrefixAsync(string prefix)
    {
        var server = _redis.GetServer(_redis.GetEndPoints().First());
        var keys = server.Keys(pattern: $"{prefix}*").ToArray();
        if (keys.Any())
        {
            await _db.KeyDeleteAsync(keys);
        }
    }

    /// <summary>
    /// Calculates time remaining until next cache reset (5:00 AM IST = 11:30 PM UTC).
    /// </summary>
    private static TimeSpan GetTimeUntilMidnight()
    {
        var nowUtc = DateTime.UtcNow;

        // Cache expires at 23:30 UTC (= 5:00 AM IST next day)
        var todayReset = nowUtc.Date.AddHours(23).AddMinutes(30);

        // If we're past today's reset time, next reset is tomorrow
        var nextReset = nowUtc > todayReset ? todayReset.AddDays(1) : todayReset;

        var ttl = nextReset - nowUtc;

        // Minimum 1 minute TTL (edge case)
        return ttl.TotalMinutes < 1 ? TimeSpan.FromMinutes(1) : ttl;
    }
}
