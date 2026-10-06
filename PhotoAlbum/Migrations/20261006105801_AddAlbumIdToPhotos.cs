using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PhotoAlbum.Migrations
{
    /// <inheritdoc />
    public partial class AddAlbumIdToPhotos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AlbumId",
                table: "Photos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Photos_AlbumId_UploadedAt",
                table: "Photos",
                columns: new[] { "AlbumId", "UploadedAt" },
                descending: new[] { false, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Photos_AlbumId_UploadedAt",
                table: "Photos");

            migrationBuilder.DropColumn(
                name: "AlbumId",
                table: "Photos");
        }
    }
}
