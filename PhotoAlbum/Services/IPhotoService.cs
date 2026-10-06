using PhotoAlbum.Models;

namespace PhotoAlbum.Services;

/// <summary>
/// Defines storage and retrieval operations for uploaded photos and their metadata.
/// </summary>
public interface IPhotoService
{
    /// <summary>
    /// Retrieves all photos in descending upload-time order.
    /// </summary>
    /// <returns>A list containing all persisted photos, with the newest photo first.</returns>
    Task<List<Photo>> GetAllPhotosAsync();

    /// <summary>
    /// Retrieves a photo by its database identifier.
    /// </summary>
    /// <param name="id">The database identifier of the photo.</param>
    /// <returns>The matching photo, or <see langword="null"/> when no photo has the identifier.</returns>
    Task<Photo?> GetPhotoByIdAsync(int id);

    /// <summary>
    /// Validates and stores an uploaded raster image and persists its metadata.
    /// </summary>
    /// <param name="file">The uploaded file whose content is inspected to determine its image format.</param>
    /// <param name="albumId">The album identifier to assign, or <see langword="null"/> to leave the photo unassigned.</param>
    /// <returns>An outcome containing the persisted photo identifier on success or a user-facing error on failure.</returns>
    /// <remarks>Supported formats and the maximum file size are controlled by application configuration.</remarks>
    Task<UploadResult> UploadPhotoAsync(IFormFile file, int? albumId = null);

    /// <summary>
    /// Deletes a photo's stored file and persisted metadata.
    /// </summary>
    /// <param name="id">The database identifier of the photo to delete.</param>
    /// <returns><see langword="true"/> when the photo existed and its metadata was deleted; otherwise, <see langword="false"/>.</returns>
    /// <remarks>Metadata deletion continues when deletion of the physical file fails.</remarks>
    Task<bool> DeletePhotoAsync(int id);

    /// <summary>
    /// Deletes all persisted photo metadata and attempts to delete every corresponding stored file.
    /// </summary>
    /// <remarks>Failure to delete an individual physical file does not stop metadata deletion.</remarks>
    Task DeleteAllAsync();

    /// <summary>
    /// Replaces the stored width and height for an existing photo.
    /// </summary>
    /// <param name="photo">A photo carrying the target identifier and replacement dimensions.</param>
    /// <returns><see langword="true"/> when the photo existed and was updated; otherwise, <see langword="false"/>.</returns>
    /// <remarks>File metadata, upload time, and album assignment are not changed.</remarks>
    Task<bool> UpdatePhotoAsync(Photo photo);

    /// <summary>
    /// Retrieves the photos assigned to an album in descending upload-time order.
    /// </summary>
    /// <param name="albumId">The album identifier to match.</param>
    /// <returns>The album's photos with the newest first, or an empty list when none match.</returns>
    Task<List<Photo>> GetPhotosByAlbumIdAsync(int albumId);
}
