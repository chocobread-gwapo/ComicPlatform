using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComicPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddFollowLastViewed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastViewedAt",
                table: "Follows",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastViewedAt",
                table: "Follows");
        }
    }
}
