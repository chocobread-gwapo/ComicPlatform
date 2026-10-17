using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComicPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddPostImageMature : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ImageIsMature",
                table: "CreatorPosts",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageIsMature",
                table: "CreatorPosts");
        }
    }
}
