using System.Collections.ObjectModel;

namespace FSH.Starter.Shared.Authorization;

/// <summary>
/// Contains application-wide constants for authorization and file handling.
/// </summary>
public static class AppConstants
{
    /// <summary>
    /// Gets the collection of supported image file extensions (including the leading dot).
    /// </summary>
    public static readonly Collection<string> SupportedImageFormats =
    [
        ".jpeg",
        ".jpg",
        ".png"
    ];
    /// <summary>
    /// Gets the standard image MIME type (image/jpeg).
    /// </summary>
    public static readonly string StandardImageFormat = "image/jpeg";
    /// <summary>
    /// Gets the maximum allowed width for images in pixels.
    /// </summary>
    public static readonly int MaxImageWidth = 1500;
    /// <summary>
    /// Gets the maximum allowed height for images in pixels.
    /// </summary>
    public static readonly int MaxImageHeight = 1500;
    /// <summary>
    /// Gets the maximum allowed file size in bytes (1 MB).
    /// </summary>
    public static readonly long MaxAllowedSize = 1000000; // 1 MB
}
