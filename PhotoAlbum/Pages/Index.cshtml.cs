using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PhotoAlbum.Models;
using PhotoAlbum.Services;

namespace PhotoAlbum.Pages;

/// <summary>
/// Displays the photo gallery and handles multi-file image uploads.
/// </summary>
public class IndexModel : PageModel
{
    private const string InvalidAlbumIdMessage = "Album ID must be a whole number between 1 and 2147483647.";

    private readonly IPhotoService _photoService;
    private readonly ILogger<IndexModel> _logger;

    /// <summary>
    /// Initializes a gallery page model.
    /// </summary>
    /// <param name="photoService">The service used to query and upload photos.</param>
    /// <param name="logger">The logger used for operational diagnostics.</param>
    public IndexModel(IPhotoService photoService, ILogger<IndexModel> logger)
    {
        _photoService = photoService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the photos displayed in the gallery, ordered newest first.
    /// </summary>
    public List<Photo> Photos { get; set; } = new();

    /// <summary>
    /// Gets the album identifier used to filter the gallery, or <see langword="null"/> when all photos are shown.
    /// </summary>
    public int? AlbumId { get; private set; }

    /// <summary>
    /// Gets the validation message for an invalid album filter, or <see langword="null"/> when the filter is valid.
    /// </summary>
    public string? AlbumIdError { get; private set; }

    /// <summary>
    /// Loads all photos or the photos assigned to a requested album.
    /// </summary>
    /// <param name="albumId">The optional positive album identifier used to filter the gallery.</param>
    /// <returns>The gallery page, with status 400 when the album identifier is invalid.</returns>
    public async Task<IActionResult> OnGetAsync(int? albumId = null)
    {
        if (!ModelState.IsValid || albumId <= 0)
        {
            AlbumIdError = InvalidAlbumIdMessage;
            return new PageResult { StatusCode = StatusCodes.Status400BadRequest };
        }

        AlbumId = albumId;

        try
        {
            Photos = albumId.HasValue
                ? await _photoService.GetPhotosByAlbumIdAsync(albumId.Value)
                : await _photoService.GetAllPhotosAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading photos");
            Photos = new List<Photo>();
        }

        return Page();
    }

    /// <summary>
    /// Uploads one or more photo files, optionally assigning all of them to an album.
    /// </summary>
    /// <param name="files">The files to validate and upload.</param>
    /// <param name="albumId">The optional positive album identifier assigned to every successful upload.</param>
    /// <returns>A JSON result separating successfully uploaded photos from failed uploads, or status 400 for invalid input.</returns>
    public async Task<IActionResult> OnPostUploadAsync(List<IFormFile> files, int? albumId = null)
    {
        if (files == null || files.Count == 0)
        {
            return BadRequest(new { success = false, error = "No files provided" });
        }

        if (!ModelState.IsValid || albumId <= 0)
        {
            return BadRequest(new { success = false, error = InvalidAlbumIdMessage });
        }

        var uploadedPhotos = new List<object>();
        var failedUploads = new List<object>();

        foreach (var file in files)
        {
            var result = await _photoService.UploadPhotoAsync(file, albumId);

            if (result.Success)
            {
                var photo = await _photoService.GetAllPhotosAsync();
                var uploadedPhoto = photo.FirstOrDefault(p => p.Id == result.PhotoId);

                if (uploadedPhoto != null)
                {
                    uploadedPhotos.Add(new
                    {
                        id = uploadedPhoto.Id,
                        originalFileName = uploadedPhoto.OriginalFileName,
                        filePath = uploadedPhoto.FilePath,
                        uploadedAt = uploadedPhoto.UploadedAt,
                        fileSize = uploadedPhoto.FileSize,
                        width = uploadedPhoto.Width,
                        height = uploadedPhoto.Height,
                        albumId = uploadedPhoto.AlbumId
                    });
                }
            }
            else
            {
                failedUploads.Add(new
                {
                    fileName = result.FileName,
                    error = result.ErrorMessage
                });
            }
        }

        return new JsonResult(new
        {
            success = uploadedPhotos.Count > 0,
            uploadedPhotos,
            failedUploads
        });
    }
}
