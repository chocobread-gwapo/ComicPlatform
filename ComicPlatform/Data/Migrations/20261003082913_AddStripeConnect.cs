using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComicPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddStripeConnect : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StripeConnectAccountId",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "StripeOnboardingComplete",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "StripeConnectAccountId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "StripeOnboardingComplete",
                table: "AspNetUsers");
        }
    }
}
