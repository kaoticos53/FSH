namespace FSH.Framework.Core.Caching;

/// <summary>
/// Provides extension methods for the <see cref="ICacheService"/> interface.
/// These extensions add convenient methods for common caching patterns.
/// </summary>
public static class CacheServiceExtensions
{
    /// <summary>
    /// Gets a value from the cache with the specified key. If the value is not found,
    /// it invokes the provided callback to retrieve the value, caches it, and returns it.
    /// </summary>
    /// <typeparam name="T">The type of the value to get or set.</typeparam>
    /// <param name="cache">The cache service instance.</param>
    /// <param name="key">The cache key.</param>
    /// <param name="getItemCallback">The callback to retrieve the value if it's not in the cache.</param>
    /// <param name="slidingExpiration">The sliding expiration time. If not specified, the default expiration is used.</param>
    /// <returns>The cached value or the value returned by the callback if not found in cache.</returns>
    public static T? GetOrSet<T>(this ICacheService cache, string key, Func<T?> getItemCallback, TimeSpan? slidingExpiration = null)
    {
        T? value = cache.Get<T>(key);

        if (value is not null)
        {
            return value;
        }

        value = getItemCallback();

        if (value is not null)
        {
            cache.Set(key, value, slidingExpiration);
        }

        return value;
    }

    /// <summary>
    /// Asynchronously gets a value from the cache with the specified key. If the value is not found,
    /// it invokes the provided asynchronous task to retrieve the value, caches it, and returns it.
    /// </summary>
    /// <typeparam name="T">The type of the value to get or set.</typeparam>
    /// <param name="cache">The cache service instance.</param>
    /// <param name="key">The cache key.</param>
    /// <param name="task">The asynchronous task to retrieve the value if it's not in the cache.</param>
    /// <param name="slidingExpiration">The sliding expiration time. If not specified, the default expiration is used.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the cached value or the value returned by the task if not found in cache.</returns>
    public static async Task<T?> GetOrSetAsync<T>(this ICacheService cache, string key, Func<Task<T>> task, TimeSpan? slidingExpiration = null, CancellationToken cancellationToken = default)
    {
        T? value = await cache.GetAsync<T>(key, cancellationToken);

        if (value is not null)
        {
            return value;
        }

        value = await task();

        if (value is not null)
        {
            await cache.SetAsync(key, value, slidingExpiration, cancellationToken);
        }

        return value;
    }
}
