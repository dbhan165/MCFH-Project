using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MCFHBackend.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMentionsColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_PayOsKeys",
                table: "PAYOS_KEYS");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BrevoKeys",
                table: "BREVO_KEYS");

            migrationBuilder.RenameIndex(
                name: "IX_SCRAPE_ORDERS_user_id",
                table: "SCRAPE_ORDERS",
                newName: "IX_ScrapeOrders_User");

            migrationBuilder.RenameIndex(
                name: "IX_SCRAPE_ORDERS_project_id",
                table: "SCRAPE_ORDERS",
                newName: "IX_ScrapeOrders_Project");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "SYSTEM_SETTINGS",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true,
                oldDefaultValueSql: "(getdate())");

            migrationBuilder.AddColumn<int>(
                name: "import_file_id",
                table: "SCRAPED_FEEDBACKS",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "mentions_included",
                table: "SCRAPE_ORDERS",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "mentions_package",
                table: "SCRAPE_ORDERS",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "mentions_expires_at",
                table: "PROJECTS",
                type: "datetime",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "mentions_full_unlimited",
                table: "PROJECTS",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "mentions_quota_total",
                table: "PROJECTS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "mentions_quota_used",
                table: "PROJECTS",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "cookie_count",
                table: "PLATFORM_COOKIES",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "PAYOS_KEYS",
                type: "datetime",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "BREVO_KEYS",
                type: "datetime",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_PAYOS_KEYS",
                table: "PAYOS_KEYS",
                column: "payos_key_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BREVO_KEYS",
                table: "BREVO_KEYS",
                column: "brevo_key_id");

            migrationBuilder.CreateTable(
                name: "PROJECT_MENTION_PACKAGES",
                columns: table => new
                {
                    package_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    project_id = table.Column<int>(type: "int", nullable: false),
                    payment_id = table.Column<int>(type: "int", nullable: false),
                    package_type = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    mentions_included = table.Column<int>(type: "int", nullable: false),
                    mentions_used = table.Column<int>(type: "int", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    status = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROJECT_MENTION_PACKAGES", x => x.package_id);
                    table.ForeignKey(
                        name: "FK_PkgPkg_Payment",
                        column: x => x.payment_id,
                        principalTable: "PAYMENTS",
                        principalColumn: "payment_id");
                    table.ForeignKey(
                        name: "FK_PkgPkg_Project",
                        column: x => x.project_id,
                        principalTable: "PROJECTS",
                        principalColumn: "project_id");
                });

            migrationBuilder.CreateTable(
                name: "SCRAPE_PACKAGES",
                columns: table => new
                {
                    package_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    code = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    currency = table.Column<string>(type: "varchar(10)", unicode: false, maxLength: 10, nullable: false, defaultValue: "VND"),
                    duration_days = table.Column<int>(type: "int", nullable: false),
                    max_items = table.Column<int>(type: "int", nullable: true),
                    max_sources = table.Column<int>(type: "int", nullable: true),
                    is_active = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SCRAPE_P__B66AD3F4E5A8B6C2", x => x.package_id);
                    table.UniqueConstraint("AK_SCRAPE_PACKAGES_code", x => x.code);
                    table.ForeignKey(
                        name: "FK_ScrapePackages_UpdatedBy",
                        column: x => x.updated_by,
                        principalTable: "USERS",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SCRAPE_ORDERS_mentions_package",
                table: "SCRAPE_ORDERS",
                column: "mentions_package");

            migrationBuilder.CreateIndex(
                name: "IX_PAYOS_KEYS_updated_by",
                table: "PAYOS_KEYS",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_BREVO_KEYS_updated_by",
                table: "BREVO_KEYS",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "IX_PkgPackages_Project_Status",
                table: "PROJECT_MENTION_PACKAGES",
                columns: new[] { "project_id", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_PROJECT_MENTION_PACKAGES_payment_id",
                table: "PROJECT_MENTION_PACKAGES",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_SCRAPE_PACKAGES_updated_by",
                table: "SCRAPE_PACKAGES",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "UQ_ScrapePackages_Code",
                table: "SCRAPE_PACKAGES",
                column: "code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ScrapeOrders_Package",
                table: "SCRAPE_ORDERS",
                column: "mentions_package",
                principalTable: "SCRAPE_PACKAGES",
                principalColumn: "code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ScrapeOrders_Package",
                table: "SCRAPE_ORDERS");

            migrationBuilder.DropTable(
                name: "PROJECT_MENTION_PACKAGES");

            migrationBuilder.DropTable(
                name: "SCRAPE_PACKAGES");

            migrationBuilder.DropIndex(
                name: "IX_SCRAPE_ORDERS_mentions_package",
                table: "SCRAPE_ORDERS");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PAYOS_KEYS",
                table: "PAYOS_KEYS");

            migrationBuilder.DropIndex(
                name: "IX_PAYOS_KEYS_updated_by",
                table: "PAYOS_KEYS");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BREVO_KEYS",
                table: "BREVO_KEYS");

            migrationBuilder.DropIndex(
                name: "IX_BREVO_KEYS_updated_by",
                table: "BREVO_KEYS");

            migrationBuilder.DropColumn(
                name: "import_file_id",
                table: "SCRAPED_FEEDBACKS");

            migrationBuilder.DropColumn(
                name: "mentions_included",
                table: "SCRAPE_ORDERS");

            migrationBuilder.DropColumn(
                name: "mentions_package",
                table: "SCRAPE_ORDERS");

            migrationBuilder.DropColumn(
                name: "mentions_expires_at",
                table: "PROJECTS");

            migrationBuilder.DropColumn(
                name: "mentions_full_unlimited",
                table: "PROJECTS");

            migrationBuilder.DropColumn(
                name: "mentions_quota_total",
                table: "PROJECTS");

            migrationBuilder.DropColumn(
                name: "mentions_quota_used",
                table: "PROJECTS");

            migrationBuilder.RenameIndex(
                name: "IX_ScrapeOrders_User",
                table: "SCRAPE_ORDERS",
                newName: "IX_SCRAPE_ORDERS_user_id");

            migrationBuilder.RenameIndex(
                name: "IX_ScrapeOrders_Project",
                table: "SCRAPE_ORDERS",
                newName: "IX_SCRAPE_ORDERS_project_id");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "SYSTEM_SETTINGS",
                type: "datetime",
                nullable: true,
                defaultValueSql: "(getdate())",
                oldClrType: typeof(DateTime),
                oldType: "datetime",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "cookie_count",
                table: "PLATFORM_COOKIES",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "PAYOS_KEYS",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime");

            migrationBuilder.AlterColumn<DateTime>(
                name: "updated_at",
                table: "BREVO_KEYS",
                type: "datetime",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PayOsKeys",
                table: "PAYOS_KEYS",
                column: "payos_key_id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BrevoKeys",
                table: "BREVO_KEYS",
                column: "brevo_key_id");
        }
    }
}
