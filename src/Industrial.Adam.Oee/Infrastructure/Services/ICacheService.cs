namespace Industrial.Adam.Oee.Infrastructure.Services;

/// <summary>
/// Cache service interface for OEE data caching
/// Provides high-performance caching for frequently accessed data
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get a cached value by key
    /// </summary>
    /// <typeparam name="T">Type of cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <returns>Cached value or null if not found</returns>
    public T? Get<T>(string key) where T : class;

    /// <summary>
    /// Set a value in cache with expiration
    /// </summary>
    /// <typeparam name="T">Type of value to cache</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="value">Value to cache</param>
    /// <param name="expiration">Cache expiration duration</param>
    public void Set<T>(string key, T value, TimeSpan expiration) where T : class;

    /// <summary>
    /// Get or create a cached value using a factory function
    /// </summary>
    /// <typeparam name="T">Type of cached value</typeparam>
    /// <param name="key">Cache key</param>
    /// <param name="factory">Factory function to create value if not cached</param>
    /// <param name="expiration">Cache expiration duration</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Cached or newly created value</returns>
    public Task<T?> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan expiration,
        CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Remove a value from cache
    /// </summary>
    /// <param name="key">Cache key</param>
    public void Remove(string key);

    /// <summary>
    /// Remove all cached values with keys matching a pattern
    /// </summary>
    /// <param name="pattern">Key pattern (supports wildcards)</param>
    public void RemoveByPattern(string pattern);

    /// <summary>
    /// Clear all cached values
    /// </summary>
    public void Clear();
}
