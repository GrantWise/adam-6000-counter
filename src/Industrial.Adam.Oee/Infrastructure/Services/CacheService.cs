using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Industrial.Adam.Oee.Infrastructure.Services;

/// <summary>
/// In-memory cache service implementation for OEE data
/// Provides high-performance caching with pattern-based invalidation
/// </summary>
public sealed class CacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<CacheService> _logger;
    private readonly ConcurrentDictionary<string, byte> _keyTracker;

    /// <summary>
    /// Constructor for cache service
    /// </summary>
    /// <param name="memoryCache">Memory cache instance</param>
    /// <param name="logger">Logger instance</param>
    public CacheService(IMemoryCache memoryCache, ILogger<CacheService> logger)
    {
        _memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _keyTracker = new ConcurrentDictionary<string, byte>();
    }

    /// <summary>
    /// Get a cached value by key
    /// </summary>
    /// <typeparam name="T">Type of cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <returns>Cached value or null if not found</returns>
    public T? Get<T>(string key) where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        try
        {
            var value = _memoryCache.Get<T>(key);

            if (value != null)
            {
                _logger.LogDebug("Cache hit for key: {CacheKey}", key);
            }
            else
            {
                _logger.LogDebug("Cache miss for key: {CacheKey}", key);
            }

            return value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get cached value for key: {CacheKey}", key);
            return null;
        }
    }

    /// <summary>
    /// Set a value in cache with expiration
    /// </summary>
    /// <typeparam name="T">Type of value to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="expiration">Cache expiration duration</param>
    public void Set<T>(string key, T value, TimeSpan expiration) where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        if (value == null)
            throw new ArgumentNullException(nameof(value));

        if (expiration <= TimeSpan.Zero)
            throw new ArgumentException("Expiration must be positive", nameof(expiration));

        try
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration,
                Priority = CacheItemPriority.Normal,
                PostEvictionCallbacks =
                {
                    new PostEvictionCallbackRegistration
                    {
                        EvictionCallback = OnCacheEntryEvicted,
                        State = key
                    }
                }
            };

            _memoryCache.Set(key, value, options);
            _keyTracker.TryAdd(key, 0);

            _logger.LogDebug("Cached value for key: {CacheKey} with expiration: {Expiration}",
                key, expiration);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cache value for key: {CacheKey}", key);
        }
    }

    /// <summary>
    /// Get or create a cached value using a factory function
    /// </summary>
    /// <typeparam name="T">Type of cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="factory">Factory function to create value if not cached</param>
    /// <param name="expiration">Cache expiration duration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cached or newly created value</returns>
    public async Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan expiration,
        CancellationToken cancellationToken = default) where T : class
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        if (expiration <= TimeSpan.Zero)
            throw new ArgumentException("Expiration must be positive", nameof(expiration));

        // Try to get from cache first
        var cachedValue = Get<T>(key);
        if (cachedValue != null)
        {
            return cachedValue;
        }

        try
        {
            _logger.LogDebug("Creating new cached value for key: {CacheKey}", key);

            // Create new value using factory
            var newValue = await factory();

            if (newValue != null)
            {
                Set(key, newValue, expiration);
                _logger.LogDebug("Created and cached new value for key: {CacheKey}", key);
            }

            return newValue;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create cached value for key: {CacheKey}", key);
            throw;
        }
    }

    /// <summary>
    /// Remove a value from cache
    /// </summary>
    /// <param name="key">Cache key</param>
    public void Remove(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));

        try
        {
            _memoryCache.Remove(key);
            _keyTracker.TryRemove(key, out _);

            _logger.LogDebug("Removed cached value for key: {CacheKey}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove cached value for key: {CacheKey}", key);
        }
    }

    /// <summary>
    /// Remove all cached values with keys matching a pattern
    /// </summary>
    /// <param name="pattern">Key pattern (supports wildcards)</param>
    public void RemoveByPattern(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
            throw new ArgumentException("Pattern cannot be null or empty", nameof(pattern));

        try
        {
            // Convert wildcard pattern to regex
            var regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*") + "$";
            var regex = new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);

            var keysToRemove = _keyTracker.Keys
                .Where(key => regex.IsMatch(key))
                .ToList();

            foreach (var key in keysToRemove)
            {
                Remove(key);
            }

            _logger.LogInformation("Removed {Count} cached values matching pattern: {Pattern}",
                keysToRemove.Count, pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to remove cached values by pattern: {Pattern}", pattern);
        }
    }

    /// <summary>
    /// Clear all cached values
    /// </summary>
    public void Clear()
    {
        try
        {
            var keysToRemove = _keyTracker.Keys.ToList();

            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
            }

            _keyTracker.Clear();

            _logger.LogInformation("Cleared all cached values ({Count} items)", keysToRemove.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear cache");
        }
    }

    /// <summary>
    /// Callback when cache entry is evicted
    /// </summary>
    /// <param name="key">Cache key</param>
    /// <param name="value">Cached value</param>
    /// <param name="reason">Eviction reason</param>
    /// <param name="state">Callback state</param>
    private void OnCacheEntryEvicted(object key, object? value, EvictionReason reason, object? state)
    {
        if (state is string cacheKey)
        {
            _keyTracker.TryRemove(cacheKey, out _);

            _logger.LogDebug("Cache entry evicted - Key: {CacheKey}, Reason: {Reason}",
                cacheKey, reason);
        }
    }
}

/// <summary>
/// Cache key constants for OEE data
/// </summary>
public static class CacheKeys
{
    /// <summary>
    /// Cache key template for active work order data
    /// </summary>
    public const string ActiveWorkOrder = "work_order:active:{0}";
    /// <summary>
    /// Cache key template for work order data by ID
    /// </summary>
    public const string WorkOrderById = "work_order:id:{0}";
    /// <summary>
    /// Cache key template for device configuration data
    /// </summary>
    public const string DeviceConfiguration = "device_config:{0}";
    /// <summary>
    /// Cache key template for current OEE metrics
    /// </summary>
    public const string CurrentOeeMetrics = "oee:current:{0}";
    /// <summary>
    /// Cache key template for counter aggregate data
    /// </summary>
    public const string CounterAggregates = "counter:aggregates:{0}:{1}:{2}:{3}";
    /// <summary>
    /// Cache key template for current production rate
    /// </summary>
    public const string CurrentRate = "counter:rate:{0}:{1}";

    /// <summary>
    /// Format cache key with parameters
    /// </summary>
    /// <param name="template">Cache key template</param>
    /// <param name="args">Arguments to format into template</param>
    /// <returns>Formatted cache key</returns>
    public static string Format(string template, params object[] args)
    {
        return string.Format(template, args);
    }
}
