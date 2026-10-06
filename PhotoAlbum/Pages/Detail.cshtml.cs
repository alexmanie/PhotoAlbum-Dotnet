using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PhotoAlbum.Services;
using PhotoAlbum.Models;

namespace PhotoAlbum.Pages;

/// <summary>
/// Displays one photo and provides scoped navigation and authenticated deletion.
/// </summary>
public class DetailModel : PageModel
{
    private readonly IPhotoService _photoService;
    private readonly ILogger<DetailModel> _logger;

    /// <summary>
    /// Initializes a photo detail page model.
    /// </summary>
    /// <param name="photoService">The service used to retrieve and delete photos.</param>
    /// <param name="logger">The logger used for operational diagnostics.</param>
    public DetailModel(IPhotoService photoService, ILogger<DetailModel> logger)
    {
        _photoService = photoService;
        _logger = logger;
    }

    /// <summary>
    /// Gets or sets the photo displayed by the page.
    /// </summary>
    public Photo? Photo { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the next older photo, or <see langword="null"/> at the end of the scope.
    /// </summary>
    public int? PreviousPhotoId { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the next newer photo, or <see langword="null"/> at the start of the scope.
    /// </summary>
    public int? NextPhotoId { get; set; }

    /// <summary>
    /// Gets the album identifier that limits navigation, or <see langword="null"/> when navigating all photos.
    /// </summary>
    public int? AlbumId { get; private set; }

    /// <summary>
    /// Loads a photo and computes its older and newer neighbors within the requested scope.
    /// </summary>
    /// <param name="id">The identifier of the photo to display.</param>
    /// <param name="albumId">The optional positive album identifier that limits lookup and navigation.</param>
    /// <returns>The page, status 404 when the photo is absent from the scope, or status 400 for an invalid album identifier.</returns>
    public async Task<IActionResult> OnGetAsync(int? id, int? albumId = null)
    {
        if (id == null)
        {
            return NotFound();
        }

        if (!ModelState.IsValid || albumId <= 0)
        {
            return BadRequest();
        }

        try
        {
            var photos = albumId.HasValue
                ? await _photoService.GetPhotosByAlbumIdAsync(albumId.Value)
                : await _photoService.GetAllPhotosAsync();
            Photo = photos.FirstOrDefault(p => p.Id == id);

            if (Photo == null)
            {
                return NotFound();
            }

            AlbumId = albumId;

            // Find previous and next photos for navigation
            var photoList = photos.ToList();
            var currentIndex = photoList.FindIndex(p => p.Id == id);

            if (currentIndex > 0)
            {
                NextPhotoId = photoList[currentIndex - 1].Id; // Newer photo (previous in chronological order)
            }

            if (currentIndex < photoList.Count - 1)
            {
                PreviousPhotoId = photoList[currentIndex + 1].Id; // Older photo (next in chronological order)
            }

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading photo with ID {PhotoId}", id);
            return NotFound();
        }
    }

    /// <summary>
    /// Deletes a photo for an authenticated caller.
    /// </summary>
    /// <param name="id">The identifier of the photo to delete.</param>
    /// <param name="albumId">The optional album identifier preserved in the redirect.</param>
    /// <returns>A challenge for anonymous callers, a gallery redirect on success, or a detail redirect when deletion throws.</returns>
    public async Task<IActionResult> OnPostDeleteAsync(int id, int? albumId = null)
    {
        // Deleting a photo is a destructive operation and requires authentication
        // (CWE-306). Anonymous callers are redirected to the login page.
        if (User.Identity is null || !User.Identity.IsAuthenticated)
        {
            return Challenge();
        }

        try
        {
            await _photoService.DeletePhotoAsync(id);
            _logger.LogInformation("Photo {PhotoId} deleted successfully", id);
            return RedirectToPage("/Index", new { albumId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting photo {PhotoId}", id);
            TempData["Error"] = "Failed to delete photo. Please try again.";
            return RedirectToPage("/Detail", new { id, albumId });
        }
    }
}
