using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PhotoAlbum.Data;
using PhotoAlbum.Models;
using PhotoAlbum.Pages;
using PhotoAlbum.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace PhotoAlbum.Tests.Unit.Pages;

/// <summary>
/// Verifies album filtering, navigation, upload assignment, and delete redirects in page handlers.
/// </summary>
public class AlbumPageTests : IDisposable
{
    private readonly PhotoAlbumContext _context;
    private readonly PhotoService _photoService;
    private readonly string _tempUploadPath;

    /// <summary>
    /// Initializes isolated persistence and file storage for a test.
    /// </summary>
    public AlbumPageTests()
    {
        var options = new DbContextOptionsBuilder<PhotoAlbumContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new PhotoAlbumContext(options);

        _tempUploadPath = Path.Combine(Path.GetTempPath(), "PhotoAlbumTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempUploadPath);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["FileUpload:UploadPath"] = _tempUploadPath })
            .Build();

        _photoService = new PhotoService(_context, configuration, NullLogger<PhotoService>.Instance);
    }

    /// <summary>Verifies that the gallery filters an album in newest-first order.</summary>
    [Fact]
    public async Task IndexOnGetAsync_WithAlbumId_ShowsOnlyThatAlbumNewestFirst()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var older = await SeedPhotoAsync("album1-older.jpg", now.AddHours(-2), albumId: 1);
        await SeedPhotoAsync("album2.jpg", now.AddHours(-1), albumId: 2);
        await SeedPhotoAsync("unassigned.jpg", now.AddMinutes(-30), albumId: null);
        var newer = await SeedPhotoAsync("album1-newer.jpg", now, albumId: 1);
        var model = CreateIndexModel();

        // Act
        var result = await model.OnGetAsync(albumId: 1);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(1, model.AlbumId);
        Assert.Equal(new[] { newer.Id, older.Id }, model.Photos.Select(p => p.Id));
    }

    /// <summary>Verifies that an unfiltered gallery displays photos from every album.</summary>
    [Fact]
    public async Task IndexOnGetAsync_WithoutAlbumId_ShowsAllPhotos()
    {
        // Arrange
        await SeedPhotoAsync("album1.jpg", DateTime.UtcNow.AddHours(-1), albumId: 1);
        await SeedPhotoAsync("unassigned.jpg", DateTime.UtcNow, albumId: null);
        var model = CreateIndexModel();

        // Act
        var result = await model.OnGetAsync();

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Null(model.AlbumId);
        Assert.Equal(2, model.Photos.Count);
    }

    /// <summary>Verifies that a nonpositive gallery album identifier returns status 400.</summary>
    /// <param name="albumId">The invalid album identifier under test.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task IndexOnGetAsync_WithNonPositiveAlbumId_ReturnsBadRequestPage(int albumId)
    {
        // Arrange
        await SeedPhotoAsync("photo.jpg", DateTime.UtcNow, albumId: null);
        var model = CreateIndexModel();

        // Act
        var result = await model.OnGetAsync(albumId);

        // Assert
        var page = Assert.IsType<PageResult>(result);
        Assert.Equal(StatusCodes.Status400BadRequest, page.StatusCode);
        Assert.NotNull(model.AlbumIdError);
        Assert.Empty(model.Photos);
    }

    /// <summary>Verifies that a batch upload assigns the requested album to every photo.</summary>
    [Fact]
    public async Task OnPostUploadAsync_WithAlbumId_AssignsAlbumToEveryUploadedPhoto()
    {
        // Arrange
        var model = CreateIndexModel();
        var files = new List<IFormFile> { CreatePngFile("first.png"), CreatePngFile("second.png") };

        // Act
        var result = await model.OnPostUploadAsync(files, albumId: 5);

        // Assert
        var json = JsonSerializer.SerializeToElement(Assert.IsType<JsonResult>(result).Value);
        var uploadedPhotos = json.GetProperty("uploadedPhotos").EnumerateArray().ToList();
        Assert.Equal(2, uploadedPhotos.Count);
        Assert.All(uploadedPhotos, photo => Assert.Equal(5, photo.GetProperty("albumId").GetInt32()));

        var savedPhotos = await _context.Photos.ToListAsync();
        Assert.Equal(2, savedPhotos.Count);
        Assert.All(savedPhotos, photo => Assert.Equal(5, photo.AlbumId));
    }

    /// <summary>Verifies that a nonpositive upload album identifier is rejected before persistence.</summary>
    /// <param name="albumId">The invalid album identifier under test.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task OnPostUploadAsync_WithNonPositiveAlbumId_ReturnsBadRequestWithoutSaving(int albumId)
    {
        // Arrange
        var model = CreateIndexModel();
        var files = new List<IFormFile> { CreatePngFile("photo.png") };

        // Act
        var result = await model.OnPostUploadAsync(files, albumId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await _context.Photos.ToListAsync());
    }

    /// <summary>Verifies that an album model-binding error prevents upload persistence.</summary>
    [Fact]
    public async Task OnPostUploadAsync_WithUnparsableAlbumId_ReturnsBadRequestWithoutSaving()
    {
        // Arrange
        var model = CreateIndexModel();
        // Model binding leaves albumId null and records an error for values such as "abc"
        model.ModelState.AddModelError("albumId", "The value 'abc' is not valid for albumId.");
        var files = new List<IFormFile> { CreatePngFile("photo.png") };

        // Act
        var result = await model.OnPostUploadAsync(files);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await _context.Photos.ToListAsync());
    }

    /// <summary>Verifies that detail navigation remains within the requested album.</summary>
    [Fact]
    public async Task DetailOnGetAsync_WithAlbumId_NavigatesWithinAlbum()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var oldest = await SeedPhotoAsync("album1-oldest.jpg", now.AddHours(-3), albumId: 1);
        await SeedPhotoAsync("unassigned.jpg", now.AddHours(-2), albumId: null);
        var current = await SeedPhotoAsync("album1-current.jpg", now.AddHours(-1), albumId: 1);
        await SeedPhotoAsync("album2.jpg", now.AddMinutes(-30), albumId: 2);
        var newest = await SeedPhotoAsync("album1-newest.jpg", now, albumId: 1);
        var model = CreateDetailModel();

        // Act
        var result = await model.OnGetAsync(current.Id, albumId: 1);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Equal(1, model.AlbumId);
        Assert.Equal(oldest.Id, model.PreviousPhotoId);
        Assert.Equal(newest.Id, model.NextPhotoId);
    }

    /// <summary>Verifies that unscoped detail navigation includes all photos.</summary>
    [Fact]
    public async Task DetailOnGetAsync_WithoutAlbumId_NavigatesAllPhotos()
    {
        // Arrange
        var now = DateTime.UtcNow;
        var older = await SeedPhotoAsync("unassigned.jpg", now.AddHours(-2), albumId: null);
        var current = await SeedPhotoAsync("album1.jpg", now.AddHours(-1), albumId: 1);
        var newer = await SeedPhotoAsync("album2.jpg", now, albumId: 2);
        var model = CreateDetailModel();

        // Act
        var result = await model.OnGetAsync(current.Id);

        // Assert
        Assert.IsType<PageResult>(result);
        Assert.Null(model.AlbumId);
        Assert.Equal(older.Id, model.PreviousPhotoId);
        Assert.Equal(newer.Id, model.NextPhotoId);
    }

    /// <summary>Verifies that a photo outside the requested album is not found.</summary>
    [Fact]
    public async Task DetailOnGetAsync_WithPhotoOutsideAlbum_ReturnsNotFound()
    {
        // Arrange
        var photo = await SeedPhotoAsync("album2.jpg", DateTime.UtcNow, albumId: 2);
        var model = CreateDetailModel();

        // Act
        var result = await model.OnGetAsync(photo.Id, albumId: 1);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    /// <summary>Verifies that a nonpositive detail album identifier returns status 400.</summary>
    /// <param name="albumId">The invalid album identifier under test.</param>
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task DetailOnGetAsync_WithNonPositiveAlbumId_ReturnsBadRequest(int albumId)
    {
        // Arrange
        var photo = await SeedPhotoAsync("photo.jpg", DateTime.UtcNow, albumId: null);
        var model = CreateDetailModel();

        // Act
        var result = await model.OnGetAsync(photo.Id, albumId);

        // Assert
        Assert.IsType<BadRequestResult>(result);
    }

    /// <summary>Verifies that deletion returns an authenticated caller to the scoped gallery.</summary>
    [Fact]
    public async Task OnPostDeleteAsync_WithAlbumId_RedirectsToAlbumGallery()
    {
        // Arrange
        var photo = await SeedPhotoAsync("album3.jpg", DateTime.UtcNow, albumId: 3);
        var model = CreateDetailModel();
        model.PageContext = new PageContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(authenticationType: "Test"))
            }
        };

        // Act
        var result = await model.OnPostDeleteAsync(photo.Id, albumId: 3);

        // Assert
        var redirect = Assert.IsType<RedirectToPageResult>(result);
        Assert.Equal("/Index", redirect.PageName);
        Assert.Equal(3, redirect.RouteValues!["albumId"]);
        Assert.Empty(await _context.Photos.ToListAsync());
    }

    private IndexModel CreateIndexModel() => new(_photoService, NullLogger<IndexModel>.Instance);

    private DetailModel CreateDetailModel() => new(_photoService, NullLogger<DetailModel>.Instance);

    private async Task<Photo> SeedPhotoAsync(string fileName, DateTime uploadedAt, int? albumId)
    {
        var storedFileName = $"{Guid.NewGuid()}.jpg";
        var photo = new Photo
        {
            OriginalFileName = fileName,
            StoredFileName = storedFileName,
            FilePath = $"/uploads/{storedFileName}",
            FileSize = 1024,
            MimeType = "image/jpeg",
            UploadedAt = uploadedAt,
            AlbumId = albumId
        };
        await _context.Photos.AddAsync(photo);
        await _context.SaveChangesAsync();
        return photo;
    }

    // Produces a real (decodable) image so hardened upload validation accepts it.
    private static IFormFile CreatePngFile(string fileName)
    {
        var stream = new MemoryStream();
        using (var image = new Image<Rgba32>(16, 16))
        {
            image.SaveAsPng(stream);
        }
        stream.Position = 0;
        return new FormFile(stream, 0, stream.Length, "files", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png"
        };
    }

    /// <summary>
    /// Releases the database context and deletes the temporary upload directory.
    /// </summary>
    public void Dispose()
    {
        _context.Dispose();
        if (Directory.Exists(_tempUploadPath))
        {
            Directory.Delete(_tempUploadPath, true);
        }
    }
}
