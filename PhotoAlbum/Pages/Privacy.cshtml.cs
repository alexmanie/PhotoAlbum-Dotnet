using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PhotoAlbum.Pages;

/// <summary>
/// Displays the application's privacy information page.
/// </summary>
public class PrivacyModel : PageModel
{
    private readonly ILogger<PrivacyModel> _logger;

    /// <summary>
    /// Initializes a privacy page model.
    /// </summary>
    /// <param name="logger">The logger available to the privacy page.</param>
    public PrivacyModel(ILogger<PrivacyModel> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Displays the privacy page.
    /// </summary>
    public void OnGet()
    {
    }
}

