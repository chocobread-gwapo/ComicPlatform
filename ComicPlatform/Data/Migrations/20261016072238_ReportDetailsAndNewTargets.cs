using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComicPlatform.Migrations
{
    /// <inheritdoc />
    public partial class ReportDetailsAndNewTargets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CreatorPostCommentId",
                table: "Reports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatorPostId",
                table: "Reports",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Details",
                table: "Reports",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReportedCreatorId",
                table: "Reports",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reports_CreatorPostCommentId",
                table: "Reports",
                column: "CreatorPostCommentId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_CreatorPostId",
                table: "Reports",
                column: "CreatorPostId");

            migrationBuilder.CreateIndex(
                name: "IX_Reports_ReportedCreatorId",
                table: "Reports",
                column: "ReportedCreatorId");

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_AspNetUsers_ReportedCreatorId",
                table: "Reports",
                column: "ReportedCreatorId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_CreatorPostComments_CreatorPostCommentId",
                table: "Reports",
                column: "CreatorPostCommentId",
                principalTable: "CreatorPostComments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Reports_CreatorPosts_CreatorPostId",
                table: "Reports",
                column: "CreatorPostId",
                principalTable: "CreatorPosts",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reports_AspNetUsers_ReportedCreatorId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_CreatorPostComments_CreatorPostCommentId",
                table: "Reports");

            migrationBuilder.DropForeignKey(
                name: "FK_Reports_CreatorPosts_CreatorPostId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_CreatorPostCommentId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_CreatorPostId",
                table: "Reports");

            migrationBuilder.DropIndex(
                name: "IX_Reports_ReportedCreatorId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "CreatorPostCommentId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "CreatorPostId",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "Details",
                table: "Reports");

            migrationBuilder.DropColumn(
                name: "ReportedCreatorId",
                table: "Reports");
        }
    }
}
