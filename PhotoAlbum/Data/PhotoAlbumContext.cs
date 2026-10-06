using Microsoft.EntityFrameworkCore;
using PhotoAlbum.Models;

namespace PhotoAlbum.Data;

/// <summary>
/// Provides database access to persisted photo metadata.
/// </summary>
public class PhotoAlbumContext : DbContext
{
    /// <summary>
    /// Initializes a new context with the specified database options.
    /// </summary>
    /// <param name="options">The options that configure the database provider and connection.</param>
    public PhotoAlbumContext(DbContextOptions<PhotoAlbumContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the photos tracked by the context.
    /// </summary>
    public DbSet<Photo> Photos { get; set; } = null!;

    /// <inheritdoc/>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Photo entity
        modelBuilder.Entity<Photo>(entity =>
        {
            // Primary key
            entity.HasKey(p => p.Id);

            // Index on UploadedAt for chronological queries
            entity.HasIndex(p => p.UploadedAt)
                .HasDatabaseName("IX_Photos_UploadedAt")
                .IsDescending();

            // Index on AlbumId and UploadedAt for album queries ordered newest first
            entity.HasIndex(p => new { p.AlbumId, p.UploadedAt })
                .HasDatabaseName("IX_Photos_AlbumId_UploadedAt")
                .IsDescending(false, true);

            // Property configurations
            entity.Property(p => p.OriginalFileName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(p => p.StoredFileName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(p => p.FilePath)
                .IsRequired()
                .HasMaxLength(500);

            entity.Property(p => p.MimeType)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(p => p.FileSize)
                .IsRequired();

            entity.Property(p => p.UploadedAt)
                .IsRequired();
        });
    }
}
