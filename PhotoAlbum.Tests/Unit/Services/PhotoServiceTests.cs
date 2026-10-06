using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PhotoAlbum.Data;
using PhotoAlbum.Models;
using PhotoAlbum.Services;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text;

namespace PhotoAlbum.Tests.Unit.Services;

/// <summary>
/// Verifies photo storage, retrieval, update, and deletion behavior against isolated storage.
/// </summary>
public class PhotoServiceTests : IDisposable
{
    private readonly PhotoAlbumContext _context;
    private readonly IPhotoService _photoService;
    private readonly string _tempUploadPath;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PhotoService> _logger;

    /// <summary>
    /// Initializes an isolated in-memory database and temporary upload directory for a test.
    /// </summary>
    public PhotoServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<PhotoAlbumContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new PhotoAlbumContext(options);

        // Setup temp upload directory
        _tempUploadPath = Path.Combine(Path.GetTempPath(), "PhotoAlbumTests", Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempUploadPath);

        // Setup configuration
        var inMemorySettings = new Dictionary<string, string>
        {
            {"FileUpload:MaxFileSizeBytes", "10485760"},
            {"FileUpload:AllowedMimeTypes:0", "image/jpeg"},
            {"FileUpload:AllowedMimeTypes:1", "image/png"},
            {"FileUpload:AllowedMimeTypes:2", "image/gif"},
            {"FileUpload:AllowedMimeTypes:3", "image/webp"},
            {"FileUpload:UploadPath", _tempUploadPath}
        };
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        // Setup logger
        _logger = new LoggerFactory().CreateLogger<PhotoService>();

        // Create PhotoService instance
        _photoService = new PhotoService(_context, _configuration, _logger);
    }

    /// <summary>Verifies that a valid image is stored successfully.</summary>
    [Fact]
    public async Task UploadPhotoAsync_WithValidImage_ReturnsSuccess()
    {
        // Arrange
        var file = CreateImageFormFile("test.jpg", "image/jpeg");

        // Act
        var result = await _photoService.UploadPhotoAsync(file);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.PhotoId);
        Assert.Equal("test.jpg", result.FileName);
        Assert.Null(result.ErrorMessage);

        // Verify photo was saved to database
        var photo = await _context.Photos.FindAsync(result.PhotoId);
        Assert.NotNull(photo);
        Assert.Equal("test.jpg", photo.OriginalFileName);
        Assert.Equal("image/jpeg", photo.MimeType);
        Assert.True(photo.FileSize > 0);
    }

    /// <summary>Verifies that content which cannot be decoded as an image is rejected.</summary>
    [Fact]
    public async Task UploadPhotoAsync_WithInvalidMimeType_ReturnsError()
    {
        // Arrange
        var file = CreateMockFormFile("document.pdf", "application/pdf", 1024);

        // Act
        var result = await _photoService.UploadPhotoAsync(file);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.PhotoId);
        Assert.Contains("not supported", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that a file exceeding the configured size limit is rejected.</summary>
    [Fact]
    public async Task UploadPhotoAsync_WithOversizedFile_ReturnsError()
    {
        // Arrange
        var file = CreateMockFormFile("huge.jpg", "image/jpeg", 11 * 1024 * 1024); // 11MB

        // Act
        var result = await _photoService.UploadPhotoAsync(file);

        // Assert
        Assert.False(result.Success);
        Assert.Null(result.PhotoId);
        Assert.Contains("exceeds", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Verifies that a successful upload creates a physical file.</summary>
    [Fact]
    public async Task UploadPhotoAsync_CreatesFileInUploadsDirectory()
    {
        // Arrange
        var file = CreateImageFormFile("test.png", "image/png");

        // Act
        var result = await _photoService.UploadPhotoAsync(file);

        // Assert
        Assert.True(result.Success);

        // Verify file exists
        var photo = await _context.Photos.FindAsync(result.PhotoId);
        Assert.NotNull(photo);
        var fullPath = Path.Combine(_tempUploadPath, photo.StoredFileName);
        Assert.True(File.Exists(fullPath));
    }

    /// <summary>Verifies that a successful upload persists its metadata.</summary>
    [Fact]
    public async Task UploadPhotoAsync_SavesMetadataToDatabase()
    {
        // Arrange
        var file = CreateImageFormFile("photo.jpg", "image/jpeg");

        // Act
        var result = await _photoService.UploadPhotoAsync(file);

        // Assert
        Assert.True(result.Success);

        var photo = await _context.Photos.FindAsync(result.PhotoId);
        Assert.NotNull(photo);
        Assert.Equal("photo.jpg", photo.OriginalFileName);
        Assert.NotEmpty(photo.StoredFileName);
        Assert.NotEmpty(photo.FilePath);
        Assert.True(photo.UploadedAt <= DateTime.UtcNow);
        Assert.True(photo.UploadedAt > DateTime.UtcNow.AddMinutes(-1));
    }

    /// <summary>Verifies that an upload can be assigned to an album.</summary>
    [Fact]
    public async Task UploadPhotoAsync_WithAlbumId_SavesAlbumId()
    {
        // Arrange
        var file = CreateImageFormFile("album.jpg", "image/jpeg");

        // Act
        var result = await _photoService.UploadPhotoAsync(file, 7);

        // Assert
        Assert.True(result.Success);
        var photo = await _context.Photos.FindAsync(result.PhotoId);
        Assert.NotNull(photo);
        Assert.Equal(7, photo.AlbumId);
    }

    /// <summary>Verifies that an upload without an album remains unassigned.</summary>
    [Fact]
    public async Task UploadPhotoAsync_WithoutAlbumId_LeavesPhotoUnassigned()
    {
        // Arrange
        var file = CreateImageFormFile("unassigned.jpg", "image/jpeg");

        // Act
        var result = await _photoService.UploadPhotoAsync(file);

        // Assert
        Assert.True(result.Success);
        var photo = await _context.Photos.FindAsync(result.PhotoId);
        Assert.NotNull(photo);
        Assert.Null(photo.AlbumId);
    }

    /// <summary>Verifies that all photos are returned in newest-first order.</summary>
    [Fact]
    public async Task GetAllPhotosAsync_ReturnsPhotosOrderedByDate()
    {
        // Arrange
        var photo1 = new Photo
        {
            OriginalFileName = "first.jpg",
            StoredFileName = "guid1.jpg",
            FilePath = "/uploads/guid1.jpg",
            FileSize = 1024,
            MimeType = "image/jpeg",
            UploadedAt = DateTime.UtcNow.AddHours(-2)
        };
        var photo2 = new Photo
        {
            OriginalFileName = "second.jpg",
            StoredFileName = "guid2.jpg",
            FilePath = "/uploads/guid2.jpg",
            FileSize = 2048,
            MimeType = "image/jpeg",
            UploadedAt = DateTime.UtcNow.AddHours(-1)
        };
        var photo3 = new Photo
        {
            OriginalFileName = "third.jpg",
            StoredFileName = "guid3.jpg",
            FilePath = "/uploads/guid3.jpg",
            FileSize = 3072,
            MimeType = "image/jpeg",
            UploadedAt = DateTime.UtcNow
        };

        await _context.Photos.AddRangeAsync(photo1, photo2, photo3);
        await _context.SaveChangesAsync();

        // Act
        var photos = await _photoService.GetAllPhotosAsync();

        // Assert
        Assert.Equal(3, photos.Count);
        Assert.Equal("third.jpg", photos[0].OriginalFileName); // Most recent first
        Assert.Equal("second.jpg", photos[1].OriginalFileName);
        Assert.Equal("first.jpg", photos[2].OriginalFileName);
    }

    /// <summary>Verifies that deleting a photo removes both its file and metadata.</summary>
    [Fact]
    public async Task DeletePhotoAsync_RemovesFileAndDatabaseRecord()
    {
        // Arrange
        var file = CreateImageFormFile("todelete.jpg", "image/jpeg");
        var uploadResult = await _photoService.UploadPhotoAsync(file);
        var photoId = uploadResult.PhotoId!.Value;

        var photo = await _context.Photos.FindAsync(photoId);
        var fullPath = Path.Combine(_tempUploadPath, photo!.StoredFileName);

        // Act
        var result = await _photoService.DeletePhotoAsync(photoId);

        // Assert
        Assert.True(result);
        Assert.Null(await _context.Photos.FindAsync(photoId));
        Assert.False(File.Exists(fullPath));
    }

    /// <summary>Verifies that deleting all photos removes every file and metadata record.</summary>
    [Fact]
    public async Task DeleteAllAsync_RemovesAllFilesAndDatabaseRecords()
    {
        // Arrange
        var firstUpload = await _photoService.UploadPhotoAsync(CreateImageFormFile("first.jpg", "image/jpeg"));
        var secondUpload = await _photoService.UploadPhotoAsync(CreateImageFormFile("second.jpg", "image/jpeg"));
        Assert.True(firstUpload.Success);
        Assert.True(secondUpload.Success);

        var photos = await _context.Photos.ToListAsync();
        var photoPaths = photos
            .Select(photo => Path.Combine(_tempUploadPath, photo.StoredFileName))
            .ToList();

        // Act
        await _photoService.DeleteAllAsync();

        // Assert
        Assert.Empty(await _context.Photos.ToListAsync());
        Assert.All(photoPaths, path => Assert.False(File.Exists(path)));
    }

    /// <summary>Verifies that updating an existing photo replaces its dimensions.</summary>
    [Fact]
    public async Task UpdatePhotoAsync_WithExistingPhoto_UpdatesDimensions()
    {
        // Arrange
        var existingPhoto = new Photo
        {
            OriginalFileName = "original.jpg",
            StoredFileName = "stored.jpg",
            FilePath = "/uploads/stored.jpg",
            FileSize = 1024,
            MimeType = "image/jpeg",
            UploadedAt = DateTime.UtcNow,
            Width = 640,
            Height = 480
        };
        await _context.Photos.AddAsync(existingPhoto);
        await _context.SaveChangesAsync();

        var updatedPhoto = new Photo
        {
            Id = existingPhoto.Id,
            Width = 1920,
            Height = 1080
        };

        // Act
        var result = await _photoService.UpdatePhotoAsync(updatedPhoto);

        // Assert
        Assert.True(result);
        Assert.Equal(1920, existingPhoto.Width);
        Assert.Equal(1080, existingPhoto.Height);
    }

    /// <summary>Verifies that updating an unknown photo reports no match.</summary>
    [Fact]
    public async Task UpdatePhotoAsync_WithUnknownId_ReturnsFalse()
    {
        // Arrange
        var photo = new Photo { Id = 999, Width = 100, Height = 100 };

        // Act
        var result = await _photoService.UpdatePhotoAsync(photo);

        // Assert
        Assert.False(result);
    }

    /// <summary>Verifies that a dimension update preserves file and upload metadata.</summary>
    [Fact]
    public async Task UpdatePhotoAsync_DoesNotChangeFileOrUploadMetadata()
    {
        // Arrange
        var uploadedAt = DateTime.UtcNow.AddDays(-1);
        var existingPhoto = new Photo
        {
            OriginalFileName = "original.jpg",
            StoredFileName = "stored.jpg",
            FilePath = "/uploads/stored.jpg",
            FileSize = 1024,
            MimeType = "image/jpeg",
            UploadedAt = uploadedAt
        };
        await _context.Photos.AddAsync(existingPhoto);
        await _context.SaveChangesAsync();

        var updatedPhoto = new Photo
        {
            Id = existingPhoto.Id,
            OriginalFileName = "changed.png",
            StoredFileName = "changed.png",
            FilePath = "/uploads/changed.png",
            FileSize = 2048,
            MimeType = "image/png",
            UploadedAt = DateTime.UtcNow,
            Width = 800,
            Height = 600
        };

        // Act
        var result = await _photoService.UpdatePhotoAsync(updatedPhoto);

        // Assert
        Assert.True(result);
        Assert.Equal("original.jpg", existingPhoto.OriginalFileName);
        Assert.Equal("stored.jpg", existingPhoto.StoredFileName);
        Assert.Equal("/uploads/stored.jpg", existingPhoto.FilePath);
        Assert.Equal(1024, existingPhoto.FileSize);
        Assert.Equal("image/jpeg", existingPhoto.MimeType);
        Assert.Equal(uploadedAt, existingPhoto.UploadedAt);
        Assert.Equal(800, existingPhoto.Width);
        Assert.Equal(600, existingPhoto.Height);
    }

    /// <summary>Verifies that null replacement dimensions clear existing dimensions.</summary>
    [Fact]
    public async Task UpdatePhotoAsync_WithNullDimensions_ClearsExistingDimensions()
    {
        // Arrange
        var existingPhoto = new Photo
        {
            OriginalFileName = "photo.jpg",
            StoredFileName = "stored.jpg",
            FilePath = "/uploads/stored.jpg",
            FileSize = 1024,
            MimeType = "image/jpeg",
            UploadedAt = DateTime.UtcNow,
            Width = 640,
            Height = 480
        };
        await _context.Photos.AddAsync(existingPhoto);
        await _context.SaveChangesAsync();

        var updatedPhoto = new Photo
        {
            Id = existingPhoto.Id,
            Width = null,
            Height = null
        };

        // Act
        var result = await _photoService.UpdatePhotoAsync(updatedPhoto);

        // Assert
        Assert.True(result);
        Assert.Null(existingPhoto.Width);
        Assert.Null(existingPhoto.Height);
    }

    /// <summary>Verifies album filtering and newest-first ordering.</summary>
    [Fact]
    public async Task GetPhotosByAlbumIdAsync_ReturnsOnlyAlbumPhotosNewestFirst()
    {
        // Arrange
        var now = DateTime.UtcNow;
        await _context.Photos.AddRangeAsync(
            CreatePhoto("album1-older.jpg", now.AddHours(-2), albumId: 1),
            CreatePhoto("album2.jpg", now.AddHours(-1), albumId: 2),
            CreatePhoto("unassigned.jpg", now.AddMinutes(-30), albumId: null),
            CreatePhoto("album1-newer.jpg", now, albumId: 1));
        await _context.SaveChangesAsync();

        // Act
        var photos = await _photoService.GetPhotosByAlbumIdAsync(1);

        // Assert
        Assert.Equal(new[] { "album1-newer.jpg", "album1-older.jpg" }, photos.Select(p => p.OriginalFileName));
    }

    /// <summary>Verifies that an album with no matching photos produces an empty list.</summary>
    [Fact]
    public async Task GetPhotosByAlbumIdAsync_WithNoMatchingPhotos_ReturnsEmptyList()
    {
        // Arrange
        await _context.Photos.AddRangeAsync(
            CreatePhoto("album1.jpg", DateTime.UtcNow, albumId: 1),
            CreatePhoto("unassigned.jpg", DateTime.UtcNow, albumId: null));
        await _context.SaveChangesAsync();

        // Act
        var photos = await _photoService.GetPhotosByAlbumIdAsync(99);

        // Assert
        Assert.Empty(photos);
    }

    private static Photo CreatePhoto(string fileName, DateTime uploadedAt, int? albumId)
    {
        var storedFileName = $"{Guid.NewGuid()}.jpg";
        return new Photo
        {
            OriginalFileName = fileName,
            StoredFileName = storedFileName,
            FilePath = $"/uploads/{storedFileName}",
            FileSize = 1024,
            MimeType = "image/jpeg",
            UploadedAt = uploadedAt,
            AlbumId = albumId
        };
    }

    private IFormFile CreateMockFormFile(string fileName, string contentType, long size)
    {
        var content = new byte[size];
        var stream = new MemoryStream(content);
        return new FormFile(stream, 0, size, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    // Produces a real (decodable) image so hardened upload validation accepts it.
    private static IFormFile CreateImageFormFile(string fileName, string contentType)
    {
        using var image = new Image<Rgba32>(16, 16);
        var stream = new MemoryStream();
        switch (Path.GetExtension(fileName).ToLowerInvariant())
        {
            case ".png": image.SaveAsPng(stream); break;
            case ".gif": image.SaveAsGif(stream); break;
            case ".webp": image.SaveAsWebp(stream); break;
            default: image.SaveAsJpeg(stream); break;
        }
        stream.Position = 0;
        return new FormFile(stream, 0, stream.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
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
