namespace FSH.Framework.Core.Caching;

/// <summary>
/// Defines the contract for a generic caching service.
/// This interface provides methods for storing, retrieving, and managing cached data.
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Gets the value associated with the specified key.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve.</typeparam>
    /// <param name="key">The key of the value to get.</param>
    /// <returns>The value associated with the specified key, or the default value if the key is not found.</returns>
    T? Get<T>(string key);

    /// <summary>
    /// Asynchronously gets the value associated with the specified key.
    /// </summary>
    /// <typeparam name="T">The type of the value to retrieve.</typeparam>
    /// <param name="key">The key of the value to get.</param>
    /// <param name="token">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the value associated with the specified key, or the default value if the key is not found.</returns>
    Task<T?> GetAsync<T>(string key, CancellationToken token = default);


    /// <summary>
    /// Refreshes the value with the specified key by resetting its sliding expiration timeout (if any).
    /// </summary>
    /// <param name="key">The key of the value to refresh.</param>
    void Refresh(string key);

    /// <summary>
    /// Asynchronously refreshes the value with the specified key by resetting its sliding expiration timeout (if any).
    /// </summary>
    /// <param name="key">The key of the value to refresh.</param>
    /// <param name="token">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RefreshAsync(string key, CancellationToken token = default);


    /// <summary>
    /// Removes the value with the specified key from the cache.
    /// </summary>
    /// <param name="key">The key of the value to remove.</param>
    void Remove(string key);

    /// <summary>
    /// Asynchronously removes the value with the specified key from the cache.
    /// </summary>
    /// <param name="key">The key of the value to remove.</param>
    /// <param name="token">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task RemoveAsync(string key, CancellationToken token = default);


    /// <summary>
    /// Sets the value associated with the specified key in the cache.
    /// </summary>
    /// <typeparam name="T">The type of the value to store.</typeparam>
    /// <param name="key">The key of the value to store.</param>
    /// <param name="value">The value to store in the cache.</param>
    /// <param name="slidingExpiration">The sliding expiration time. If not specified, the default expiration is used.</param>
    void Set<T>(string key, T value, TimeSpan? slidingExpiration = null);

    /// <summary>
    /// Asynchronously sets the value associated with the specified key in the cache.
    /// </summary>
    /// <typeparam name="T">The type of the value to store.</typeparam>
    /// <param name="key">The key of the value to store.</param>
    /// <param name="value">The value to store in the cache.</param>
    /// <param name="slidingExpiration">The sliding expiration time. If not specified, the default expiration is used.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    Task SetAsync<T>(string key, T value, TimeSpan? slidingExpiration = null, CancellationToken cancellationToken = default);
}