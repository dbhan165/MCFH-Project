using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MCFHBackend.Migrations
{
    /// <inheritdoc />
    public partial class AddProviderKeys : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BREVO_KEYS",
                columns: table => new
                {
                    brevo_key_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    key_type = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "api"),
                    api_key_encrypted = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    smtp_login = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    from_address = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    from_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, defaultValue: "active"),
                    is_default = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    last_used_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrevoKeys", x => x.brevo_key_id);
                    table.CheckConstraint("CK_BrevoKeys_KeyType", "[key_type] IN ('api', 'smtp')");
                    table.CheckConstraint("CK_BrevoKeys_Status", "[status] IN ('active', 'disabled')");
                    table.ForeignKey(
                        name: "FK_BrevoKeys_UpdatedBy",
                        column: x => x.updated_by,
                        principalTable: "USERS",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BrevoKeys_Default_Status",
                table: "BREVO_KEYS",
                columns: new[] { "is_default", "status" });

            migrationBuilder.CreateTable(
                name: "PAYOS_KEYS",
                columns: table => new
                {
                    payos_key_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    client_id = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false),
                    api_key_encrypted = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    checksum_key_encrypted = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    environment = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: false, defaultValue: "live"),
                    status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, defaultValue: "active"),
                    is_default = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    last_used_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())"),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    updated_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayOsKeys", x => x.payos_key_id);
                    table.CheckConstraint("CK_PayOsKeys_Environment", "[environment] IN ('sandbox', 'live')");
                    table.CheckConstraint("CK_PayOsKeys_Status", "[status] IN ('active', 'disabled')");
                    table.ForeignKey(
                        name: "FK_PayOsKeys_UpdatedBy",
                        column: x => x.updated_by,
                        principalTable: "USERS",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayOsKeys_Default_Status",
                table: "PAYOS_KEYS",
                columns: new[] { "is_default", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "BREVO_KEYS");
            migrationBuilder.DropTable(name: "PAYOS_KEYS");
        }
    }
}
