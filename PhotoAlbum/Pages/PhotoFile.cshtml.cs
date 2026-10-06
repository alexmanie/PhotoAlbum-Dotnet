using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PhotoAlbum.Services;

namespace PhotoAlbum.Pages;

/// <summary>
/// Resolves persisted photo metadata to physical image files for indirect access.
/// </summary>
public class PhotoFileModel : PageModel
{
    private readonly IPhotoService _photoService;
    private readonly ILogger<PhotoFileModel> _logger;
    private readonly IConfiguration _configuration;
    private readonly string _uploadPath;

    /// <summary>
    /// Initializes a photo file page model.
    /// </summary>
    /// <param name="photoService">The service used to resolve photo metadata.</param>
    /// <param name="configuration">Configuration containing the physical upload path.</param>
    /// <param name="logger">The logger used for operational diagnostics.</param>
    public PhotoFileModel(IPhotoService photoService, IConfiguration configuration, ILogger<PhotoFileModel> logger)
    {
        _photoService = photoService;
        _configuration = configuration;
        _logger = logger;

        _uploadPath = _configuration["FileUpload:UploadPath"] ?? "wwwroot/uploads";
    }

    /// <summary>
    /// Serves a photo file with its detected MIME type and long-lived cache headers.
    /// </summary>
    /// <param name="id">The identifier of the photo to serve.</param>
    /// <returns>The image file, status 404 when metadata or content is absent, or status 500 when retrieval fails unexpectedly.</returns>
    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null)
        {
            _logger.LogWarning("Photo file request with null ID");
            return NotFound();
        }

        try
        {
            var photo = await _photoService.GetPhotoByIdAsync(id.Value);

            if (photo == null)
            {
                _logger.LogWarning("Photo with ID {PhotoId} not found", id);
                return NotFound();
            }

            // Construct the physical file path
            // photo.FilePath is stored as "/uploads/filename.jpg"
            // We need to read from "wwwroot/uploads/filename.jpg"
            var fileName = Path.GetFileName(photo.FilePath);
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), _uploadPath, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogError("Physical file not found for photo ID {PhotoId} at path {FilePath}", id, filePath);
                return NotFound();
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);

            _logger.LogDebug("Serving photo ID {PhotoId} ({FileName}, {FileSize} bytes)",
                id, photo.OriginalFileName, fileBytes.Length);

            // Return the file with appropriate content type and enable caching
            Response.Headers.CacheControl = "public,max-age=31536000"; // Cache for 1 year
            Response.Headers.ETag = $"\"{photo.Id}-{photo.UploadedAt.Ticks}\"";

            return File(fileBytes, photo.MimeType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving photo with ID {PhotoId}", id);
            return StatusCode(500);
        }
    }
}
