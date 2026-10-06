using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace PhotoAlbum.Pages;

/// <summary>
/// Authenticates the administrator account used for state-changing operations.
/// </summary>
/// <remarks>Credentials are read from the <c>Admin:Username</c> and <c>Admin:Password</c> configuration values.</remarks>
public class LoginModel : PageModel
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Initializes an administrator login page model.
    /// </summary>
    /// <param name="configuration">Configuration containing the administrator credentials.</param>
    public LoginModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Gets or sets the submitted administrator user name.
    /// </summary>
    [BindProperty]
    public string? Username { get; set; }

    /// <summary>
    /// Gets or sets the submitted administrator password.
    /// </summary>
    [BindProperty]
    public string? Password { get; set; }

    /// <summary>
    /// Gets or sets the local URL to return to after successful authentication.
    /// </summary>
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    /// <summary>
    /// Gets or sets the user-facing authentication or configuration error.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Displays the login page.
    /// </summary>
    public void OnGet()
    {
    }

    /// <summary>
    /// Validates configured administrator credentials and creates an authentication cookie.
    /// </summary>
    /// <returns>The login page on failure, the local return URL when valid, or the gallery after successful login.</returns>
    public async Task<IActionResult> OnPostAsync()
    {
        var adminUser = _configuration["Admin:Username"] ?? "admin";
        var adminPassword = _configuration["Admin:Password"];

        // Fail closed if no admin password has been configured.
        if (string.IsNullOrEmpty(adminPassword))
        {
            ErrorMessage = "Admin account is not configured.";
            return Page();
        }

        var userMatches = string.Equals(Username, adminUser, StringComparison.Ordinal);
        var passwordMatches = FixedTimeEquals(Password, adminPassword);
        if (!userMatches || !passwordMatches)
        {
            ErrorMessage = "Invalid username or password.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, adminUser),
            new(ClaimTypes.Role, "Admin")
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));

        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
        {
            return LocalRedirect(ReturnUrl);
        }

        return RedirectToPage("/Index");
    }

    private static bool FixedTimeEquals(string? a, string? b)
    {
        var bytesA = Encoding.UTF8.GetBytes(a ?? string.Empty);
        var bytesB = Encoding.UTF8.GetBytes(b ?? string.Empty);
        return CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }
}
