using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComicPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddMainGenre : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MainGenreId",
                table: "Series",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Series_MainGenreId",
                table: "Series",
                column: "MainGenreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Series_Genres_MainGenreId",
                table: "Series",
                column: "MainGenreId",
                principalTable: "Genres",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Series_Genres_MainGenreId",
                table: "Series");

            migrationBuilder.DropIndex(
                name: "IX_Series_MainGenreId",
                table: "Series");

            migrationBuilder.DropColumn(
                name: "MainGenreId",
                table: "Series");
        }
    }
}
