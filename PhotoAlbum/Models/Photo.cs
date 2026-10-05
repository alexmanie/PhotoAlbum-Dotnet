using System.ComponentModel.DataAnnotations;

namespace PhotoAlbum.Models;

/// <summary>
/// Stores the persisted metadata used to locate, display, and manage an uploaded photo.
/// </summary>
public class Photo
{
    /// <summary>
    /// Gets or sets the database identifier for the photo.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the base file name supplied for the upload, without directory information.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the generated file name, including its detected image-format extension, used for storage.
    /// </summary>
    [Required]
    [MaxLength(255)]
    public string StoredFileName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the root-relative URL path of the stored image under <c>/uploads</c>.
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the size of the uploaded file, in bytes.
    /// </summary>
    [Required]
    [Range(1, long.MaxValue)]
    public long FileSize { get; set; }

    /// <summary>
    /// Gets or sets the MIME type determined from the decoded image content.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string MimeType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC date and time when the photo was uploaded.
    /// </summary>
    [Required]
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// Gets or sets the decoded image width, in pixels, or <see langword="null"/> when unavailable.
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Gets or sets the decoded image height, in pixels, or <see langword="null"/> when unavailable.
    /// </summary>
    public int? Height { get; set; }
}
