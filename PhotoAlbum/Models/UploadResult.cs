namespace PhotoAlbum.Models;

/// <summary>
/// Describes the outcome of an attempted photo upload.
/// </summary>
public class UploadResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the photo was stored and its metadata was persisted.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the persisted <see cref="Photo"/>, or <see langword="null"/> when the upload fails.
    /// </summary>
    public int? PhotoId { get; set; }

    /// <summary>
    /// Gets or sets the file name supplied with the upload request.
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the user-facing failure message, or <see langword="null"/> when the upload succeeds.
    /// </summary>
    public string? ErrorMessage { get; set; }
}
