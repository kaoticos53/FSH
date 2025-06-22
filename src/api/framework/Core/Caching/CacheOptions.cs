namespace FSH.Framework.Core.Caching;

/// <summary>
/// Represents the configuration options for the caching system.
/// This class contains settings that control how caching is configured in the application.
/// </summary>
public class CacheOptions
{
    /// <summary>
    /// Gets or sets the connection string for the Redis server.
    /// If this is not set, an in-memory cache will be used instead of Redis.
    /// </summary>
    public string Redis { get; set; } = string.Empty;
}
