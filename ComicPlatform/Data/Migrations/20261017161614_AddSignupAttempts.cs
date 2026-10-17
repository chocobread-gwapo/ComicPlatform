using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ComicPlatform.Migrations
{
    /// <inheritdoc />
    public partial class AddSignupAttempts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SignupAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CodeHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CodeSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CodeExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CodeSendCount = table.Column<int>(type: "int", nullable: false),
                    CodeAttempts = table.Column<int>(type: "int", nullable: false),
                    EmailVerified = table.Column<bool>(type: "bit", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SignupAttempts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SignupAttempts_Email",
                table: "SignupAttempts",
                column: "Email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SignupAttempts");
        }
    }
}
