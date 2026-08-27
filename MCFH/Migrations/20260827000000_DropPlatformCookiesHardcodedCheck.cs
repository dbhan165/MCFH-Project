using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MCFHBackend.Migrations
{
    /// <inheritdoc />
    public partial class DropPlatformCookiesHardcodedCheck : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_PlatformCookies_Platform",
                table: "PLATFORM_COOKIES");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_PlatformCookies_Platform",
                table: "PLATFORM_COOKIES",
                sql: "CHECK (platform IN ('facebook', 'tiktok', 'threads'))");
        }
    }
}
