using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BeloteEngine.Services.Services;

public class CachingService(
      IMemoryCache cache
    , ILogger<CachingService> logger)
{
    private readonly IMemoryCache _cache = cache;
    private readonly ILogger<CachingService> _logger = logger;

    /// <summary>
    /// Get or create a cached item with specified expiration
    /// </summary>
    public T GetOrCreate<T>(string key, Func<T> factory,
        TimeSpan? absoluteExpiration = null, TimeSpan? slidingExpiration = null)
    {
        if (_cache.TryGetValue(key, out T cachedValue))
        {
            _logger.LogDebug("Cache HIT:  {CacheKey}", key);
            return cachedValue;
        }

        _logger.LogDebug("Cache MISS: {CacheKey}", key);
        var value = factory();

        if (value != null)
        {
            var options = new MemoryCacheEntryOptions
            {
                // Absolute expiration - cache expires after this time no matter what
                AbsoluteExpirationRelativeToNow = absoluteExpiration ?? TimeSpan.FromMinutes(30),

                // Sliding expiration - resets timer if accessed
                SlidingExpiration = slidingExpiration ?? TimeSpan.FromMinutes(10),

                // Priority - determines which items get evicted first when memory is low
                Priority = CacheItemPriority.Normal,

                // Size - helps enforce memory limits (1 unit per lobby)
                Size = 1
            };

            // Log when cache entry is removed
            options.RegisterPostEvictionCallback((key, value, reason, state) =>
            {
                _logger.LogDebug("Cache entry removed: {Key}, Reason: {Reason}", key, reason);
            });

            _cache.Set(key, value, options);
        }

        return value;
    }

    /// <summary>
    /// Remove a specific cache entry
    /// </summary>
    public void Remove(string key)
    {
        _cache.Remove(key);
        _logger.LogDebug("Cache removed: {CacheKey}", key);
    }

    /// <summary>
    /// Check if key exists in cache
    /// </summary>
    public bool Exists(string key)
    {
        return _cache.TryGetValue(key, out _);
    }
}