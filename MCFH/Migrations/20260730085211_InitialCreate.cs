using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MCFHBackend.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PLATFORM_COOKIES",
                columns: table => new
                {
                    platform_cookie_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    platform = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    file_path = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, defaultValue: "active"),
                    note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    cookie_count = table.Column<int>(type: "int", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    uploaded_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PLATFORM_COOKIES", x => x.platform_cookie_id);
                });

            migrationBuilder.CreateTable(
                name: "SUBSCRIPTION_PLANS",
                columns: table => new
                {
                    plan_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ai_credit_limit = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SUBSCRIP__BE9F8F1DB8FCAA99", x => x.plan_id);
                });

            migrationBuilder.CreateTable(
                name: "SYSTEM_PROXIES",
                columns: table => new
                {
                    proxy_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ip_address = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    port = table.Column<int>(type: "int", nullable: false),
                    auth_user = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    auth_pass = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, defaultValue: "active"),
                    fail_count = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    last_used_at = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SYSTEM_P__9FE1B4A8A86BA5B9", x => x.proxy_id);
                });

            migrationBuilder.CreateTable(
                name: "USERS",
                columns: table => new
                {
                    user_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    email = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    full_name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    phone = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    avatar_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    auth_provider = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false, defaultValue: "local"),
                    google_id = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    system_role = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    is_verified = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    verified_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    is_banned = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    banned_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__USERS__B9BE370FA534CBB4", x => x.user_id);
                });

            migrationBuilder.CreateTable(
                name: "WORKSPACE_ROLES",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    role_name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__WORKSPAC__760965CC1A537511", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "BESPOKE_REQUESTS",
                columns: table => new
                {
                    request_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    client_id = table.Column<int>(type: "int", nullable: false),
                    reporter_id = table.Column<int>(type: "int", nullable: true),
                    assigned_by = table.Column<int>(type: "int", nullable: true),
                    title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    requirements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    custom_metrics = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    agreed_price = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    deadline = table.Column<DateTime>(type: "datetime", nullable: true),
                    assigned_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    submitted_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, defaultValue: "pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__BESPOKE___18D3B90FFEEC8ABE", x => x.request_id);
                    table.ForeignKey(
                        name: "FK_Bespoke_Admin",
                        column: x => x.assigned_by,
                        principalTable: "USERS",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_Bespoke_Client",
                        column: x => x.client_id,
                        principalTable: "USERS",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_Bespoke_Reporter",
                        column: x => x.reporter_id,
                        principalTable: "USERS",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "EMAIL_VERIFICATIONS",
                columns: table => new
                {
                    verification_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    otp_code = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    verification_token = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: true),
                    expired_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    is_used = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__EMAIL_VE__24F17969176A7C24", x => x.verification_id);
                    table.ForeignKey(
                        name: "FK_EmailVerif_User",
                        column: x => x.user_id,
                        principalTable: "USERS",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "FB_SOURCES",
                columns: table => new
                {
                    fb_source_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    group_url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    group_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, defaultValue: "active"),
                    added_by = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FB_SOURCES", x => x.fb_source_id);
                    table.ForeignKey(
                        name: "FK_FbSource_AddedBy",
                        column: x => x.added_by,
                        principalTable: "USERS",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "PASSWORD_RESET_TOKENS",
                columns: table => new
                {
                    token_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    reset_token = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    expired_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    is_used = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PASSWORD__CB3C9E177983BC6B", x => x.token_id);
                    table.ForeignKey(
                        name: "FK_PwdReset_User",
                        column: x => x.user_id,
                        principalTable: "USERS",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "SYSTEM_SETTINGS",
                columns: table => new
                {
                    setting_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    setting_key = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    setting_value = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_encrypted = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    updated_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    updated_by = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SYSTEM_S__256E1E32D347203C", x => x.setting_id);
                    table.ForeignKey(
                        name: "FK_Setting_UpdatedBy",
                        column: x => x.updated_by,
                        principalTable: "USERS",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "WORKSPACES",
                columns: table => new
                {
                    workspace_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    owner_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    is_deleted = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__WORKSPAC__7C58AC0B19C3C21F", x => x.workspace_id);
                    table.ForeignKey(
                        name: "FK_Workspace_Owner",
                        column: x => x.owner_id,
                        principalTable: "USERS",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "BESPOKE_REPORTS",
                columns: table => new
                {
                    report_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    request_id = table.Column<int>(type: "int", nullable: false),
                    file_url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    version = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    uploaded_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__BESPOKE___779B7C581A85FC78", x => x.report_id);
                    table.ForeignKey(
                        name: "FK_Report_Request",
                        column: x => x.request_id,
                        principalTable: "BESPOKE_REQUESTS",
                        principalColumn: "request_id");
                });

            migrationBuilder.CreateTable(
                name: "PAYMENTS",
                columns: table => new
                {
                    payment_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    transaction_ref = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    type = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    plan_id = table.Column<int>(type: "int", nullable: true),
                    request_id = table.Column<int>(type: "int", nullable: true),
                    created_by = table.Column<int>(type: "int", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    order_code = table.Column<long>(type: "bigint", nullable: true),
                    payment_link_id = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    checkout_url = table.Column<string>(type: "varchar(500)", unicode: false, maxLength: 500, nullable: true),
                    paid_at = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PAYMENTS__ED1FC9EA8603306C", x => x.payment_id);
                    table.ForeignKey(
                        name: "FK_Payment_Creator",
                        column: x => x.created_by,
                        principalTable: "USERS",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_Payment_Plan",
                        column: x => x.plan_id,
                        principalTable: "SUBSCRIPTION_PLANS",
                        principalColumn: "plan_id");
                    table.ForeignKey(
                        name: "FK_Payment_Request",
                        column: x => x.request_id,
                        principalTable: "BESPOKE_REQUESTS",
                        principalColumn: "request_id");
                });

            migrationBuilder.CreateTable(
                name: "PROJECTS",
                columns: table => new
                {
                    project_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    workspace_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    search_query = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    enable_facebook = table.Column<bool>(type: "bit", nullable: true),
                    enable_tiktok = table.Column<bool>(type: "bit", nullable: true),
                    enable_youtube = table.Column<bool>(type: "bit", nullable: true),
                    enable_maps = table.Column<bool>(type: "bit", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__PROJECTS__BC799E1F6713875E", x => x.project_id);
                    table.ForeignKey(
                        name: "FK_Project_Workspace",
                        column: x => x.workspace_id,
                        principalTable: "WORKSPACES",
                        principalColumn: "workspace_id");
                });

            migrationBuilder.CreateTable(
                name: "SUBSCRIPTIONS",
                columns: table => new
                {
                    subscription_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    workspace_id = table.Column<int>(type: "int", nullable: false),
                    plan_id = table.Column<int>(type: "int", nullable: false),
                    start_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    expiry_date = table.Column<DateTime>(type: "datetime", nullable: false),
                    status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, defaultValue: "active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SUBSCRIP__863A7EC1BCAA53D3", x => x.subscription_id);
                    table.ForeignKey(
                        name: "FK_Subscription_Plan",
                        column: x => x.plan_id,
                        principalTable: "SUBSCRIPTION_PLANS",
                        principalColumn: "plan_id");
                    table.ForeignKey(
                        name: "FK_Subscription_Workspace",
                        column: x => x.workspace_id,
                        principalTable: "WORKSPACES",
                        principalColumn: "workspace_id");
                });

            migrationBuilder.CreateTable(
                name: "WORKSPACE_ACTIVITY_LOGS",
                columns: table => new
                {
                    log_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    workspace_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    action_type = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    target_type = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    target_id = table.Column<int>(type: "int", nullable: true),
                    target_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceActivityLog", x => x.log_id);
                    table.ForeignKey(
                        name: "FK_ActivityLog_User",
                        column: x => x.user_id,
                        principalTable: "USERS",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_ActivityLog_Workspace",
                        column: x => x.workspace_id,
                        principalTable: "WORKSPACES",
                        principalColumn: "workspace_id");
                });

            migrationBuilder.CreateTable(
                name: "WORKSPACE_CREDITS",
                columns: table => new
                {
                    workspace_id = table.Column<int>(type: "int", nullable: false),
                    total_credits = table.Column<int>(type: "int", nullable: false),
                    used_credits = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    last_updated = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceCredits", x => x.workspace_id);
                    table.ForeignKey(
                        name: "FK_Credits_Workspace",
                        column: x => x.workspace_id,
                        principalTable: "WORKSPACES",
                        principalColumn: "workspace_id");
                });

            migrationBuilder.CreateTable(
                name: "WORKSPACE_INVITATIONS",
                columns: table => new
                {
                    invitation_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    workspace_id = table.Column<int>(type: "int", nullable: false),
                    invited_email = table.Column<string>(type: "varchar(255)", unicode: false, maxLength: 255, nullable: false),
                    invited_by = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, defaultValue: "pending"),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__WORKSPAC__94B74D7CFE0C2630", x => x.invitation_id);
                    table.ForeignKey(
                        name: "FK_Invitation_InvitedBy",
                        column: x => x.invited_by,
                        principalTable: "USERS",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_Invitation_Workspace",
                        column: x => x.workspace_id,
                        principalTable: "WORKSPACES",
                        principalColumn: "workspace_id");
                });

            migrationBuilder.CreateTable(
                name: "WORKSPACE_MEMBERS",
                columns: table => new
                {
                    workspace_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    role_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__WORKSPAC__97C34F7B6C37E4F5", x => new { x.workspace_id, x.user_id });
                    table.ForeignKey(
                        name: "FK_Member_Role",
                        column: x => x.role_id,
                        principalTable: "WORKSPACE_ROLES",
                        principalColumn: "role_id");
                    table.ForeignKey(
                        name: "FK_Member_User",
                        column: x => x.user_id,
                        principalTable: "USERS",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_Member_Workspace",
                        column: x => x.workspace_id,
                        principalTable: "WORKSPACES",
                        principalColumn: "workspace_id");
                });

            migrationBuilder.CreateTable(
                name: "DATA_SOURCES",
                columns: table => new
                {
                    source_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    project_id = table.Column<int>(type: "int", nullable: false),
                    platform = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    source_type = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    target_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    search_query = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, defaultValue: "active")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__DATA_SOU__3035A9B6D208F19F", x => x.source_id);
                    table.ForeignKey(
                        name: "FK_Source_Project",
                        column: x => x.project_id,
                        principalTable: "PROJECTS",
                        principalColumn: "project_id");
                });

            migrationBuilder.CreateTable(
                name: "INFLUENCERS",
                columns: table => new
                {
                    influencer_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    project_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    platform = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    handle_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    followers = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    influence_score = table.Column<double>(type: "float", nullable: true),
                    reach = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__INFLUENC__D0669ABDE8CA1A89", x => x.influencer_id);
                    table.ForeignKey(
                        name: "FK_Influencer_Project",
                        column: x => x.project_id,
                        principalTable: "PROJECTS",
                        principalColumn: "project_id");
                });

            migrationBuilder.CreateTable(
                name: "MUTED_ENTITIES",
                columns: table => new
                {
                    mute_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    project_id = table.Column<int>(type: "int", nullable: false),
                    entity_type = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    entity_value = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    muted_by = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__MUTED_EN__84EE96EBD9B518BA", x => x.mute_id);
                    table.ForeignKey(
                        name: "FK_Muted_Project",
                        column: x => x.project_id,
                        principalTable: "PROJECTS",
                        principalColumn: "project_id");
                    table.ForeignKey(
                        name: "FK_Muted_User",
                        column: x => x.muted_by,
                        principalTable: "USERS",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "NOTIFICATIONS",
                columns: table => new
                {
                    notification_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    project_id = table.Column<int>(type: "int", nullable: true),
                    title = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    type = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    related_id = table.Column<int>(type: "int", nullable: true),
                    related_type = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    is_read = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__NOTIFICA__E059842F9AC3F414", x => x.notification_id);
                    table.ForeignKey(
                        name: "FK_Notification_Project",
                        column: x => x.project_id,
                        principalTable: "PROJECTS",
                        principalColumn: "project_id");
                    table.ForeignKey(
                        name: "FK_Notification_User",
                        column: x => x.user_id,
                        principalTable: "USERS",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "NSR_SNAPSHOTS",
                columns: table => new
                {
                    snapshot_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    project_id = table.Column<int>(type: "int", nullable: false),
                    platform = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    snapshot_date = table.Column<DateOnly>(type: "date", nullable: false),
                    total_positive = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    total_negative = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    total_neutral = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    total_reach = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    nsr_score = table.Column<double>(type: "float", nullable: true),
                    presence_score = table.Column<double>(type: "float", nullable: true),
                    calculated_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__NSR_SNAP__C27CFBF7D0092DAD", x => x.snapshot_id);
                    table.ForeignKey(
                        name: "FK_Snapshot_Project",
                        column: x => x.project_id,
                        principalTable: "PROJECTS",
                        principalColumn: "project_id");
                });

            migrationBuilder.CreateTable(
                name: "SAVED_FILTERS",
                columns: table => new
                {
                    filter_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    project_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    filter_config = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_by = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SAVED_FI__833C443F9E93686D", x => x.filter_id);
                    table.ForeignKey(
                        name: "FK_Filter_Creator",
                        column: x => x.created_by,
                        principalTable: "USERS",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_Filter_Project",
                        column: x => x.project_id,
                        principalTable: "PROJECTS",
                        principalColumn: "project_id");
                });

            migrationBuilder.CreateTable(
                name: "SCRAPE_ORDERS",
                columns: table => new
                {
                    order_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    workspace_id = table.Column<int>(type: "int", nullable: false),
                    project_id = table.Column<int>(type: "int", nullable: false),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    keyword = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    posted_since_days = table.Column<int>(type: "int", nullable: false),
                    quoted_price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    payment_id = table.Column<int>(type: "int", nullable: true),
                    scrape_job_id = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    progress_percent = table.Column<int>(type: "int", nullable: false),
                    status_message = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    estimated_report_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    report_ready_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime", nullable: false),
                    paid_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    completed_at = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SCRAPE_ORDERS", x => x.order_id);
                    table.ForeignKey(
                        name: "FK_SCRAPE_ORDERS_PAYMENTS_payment_id",
                        column: x => x.payment_id,
                        principalTable: "PAYMENTS",
                        principalColumn: "payment_id");
                    table.ForeignKey(
                        name: "FK_SCRAPE_ORDERS_PROJECTS_project_id",
                        column: x => x.project_id,
                        principalTable: "PROJECTS",
                        principalColumn: "project_id");
                    table.ForeignKey(
                        name: "FK_SCRAPE_ORDERS_USERS_user_id",
                        column: x => x.user_id,
                        principalTable: "USERS",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_SCRAPE_ORDERS_WORKSPACES_workspace_id",
                        column: x => x.workspace_id,
                        principalTable: "WORKSPACES",
                        principalColumn: "workspace_id");
                });

            migrationBuilder.CreateTable(
                name: "TAGS",
                columns: table => new
                {
                    tag_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    project_id = table.Column<int>(type: "int", nullable: false),
                    name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    color = table.Column<string>(type: "varchar(20)", unicode: false, maxLength: 20, nullable: true),
                    created_by = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TAGS__4296A2B674FA44A3", x => x.tag_id);
                    table.ForeignKey(
                        name: "FK_Tag_Creator",
                        column: x => x.created_by,
                        principalTable: "USERS",
                        principalColumn: "user_id");
                    table.ForeignKey(
                        name: "FK_Tag_Project",
                        column: x => x.project_id,
                        principalTable: "PROJECTS",
                        principalColumn: "project_id");
                });

            migrationBuilder.CreateTable(
                name: "IMPORT_FILES",
                columns: table => new
                {
                    file_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    project_id = table.Column<int>(type: "int", nullable: false),
                    source_id = table.Column<int>(type: "int", nullable: true),
                    uploaded_by = table.Column<int>(type: "int", nullable: false),
                    file_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    file_url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    total_rows = table.Column<int>(type: "int", nullable: true),
                    imported_rows = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true, defaultValue: "processing"),
                    imported_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__IMPORT_F__07D884C6A37E7BEA", x => x.file_id);
                    table.ForeignKey(
                        name: "FK_Import_Project",
                        column: x => x.project_id,
                        principalTable: "PROJECTS",
                        principalColumn: "project_id");
                    table.ForeignKey(
                        name: "FK_Import_Source",
                        column: x => x.source_id,
                        principalTable: "DATA_SOURCES",
                        principalColumn: "source_id");
                    table.ForeignKey(
                        name: "FK_Import_Uploader",
                        column: x => x.uploaded_by,
                        principalTable: "USERS",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "SCRAPED_FEEDBACKS",
                columns: table => new
                {
                    feedback_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    source_id = table.Column<int>(type: "int", nullable: true),
                    content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    author_name = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    original_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    posted_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    reach = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    engagement_count = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    pinned_for_report = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    is_deleted = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    deleted_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    scraped_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    comments_file_url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    comments_count = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    project_id = table.Column<int>(type: "int", nullable: true),
                    platform = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SCRAPED___7A6B2B8C16F9AF60", x => x.feedback_id);
                    table.ForeignKey(
                        name: "FK_Feedback_Project",
                        column: x => x.project_id,
                        principalTable: "PROJECTS",
                        principalColumn: "project_id");
                    table.ForeignKey(
                        name: "FK_Feedback_Source",
                        column: x => x.source_id,
                        principalTable: "DATA_SOURCES",
                        principalColumn: "source_id");
                });

            migrationBuilder.CreateTable(
                name: "SCRAPING_JOBS",
                columns: table => new
                {
                    job_id = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    source_id = table.Column<int>(type: "int", nullable: true),
                    project_id = table.Column<int>(type: "int", nullable: false),
                    proxy_id = table.Column<int>(type: "int", nullable: true),
                    status = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    total_scraped = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    error_log = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    started_at = table.Column<DateTime>(type: "datetime", nullable: true),
                    finished_at = table.Column<DateTime>(type: "datetime", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__SCRAPING__6E32B6A5112EB2DB", x => x.job_id);
                    table.ForeignKey(
                        name: "FK_Job_Project",
                        column: x => x.project_id,
                        principalTable: "PROJECTS",
                        principalColumn: "project_id");
                    table.ForeignKey(
                        name: "FK_Job_Proxy",
                        column: x => x.proxy_id,
                        principalTable: "SYSTEM_PROXIES",
                        principalColumn: "proxy_id");
                    table.ForeignKey(
                        name: "FK_Job_Source",
                        column: x => x.source_id,
                        principalTable: "DATA_SOURCES",
                        principalColumn: "source_id");
                });

            migrationBuilder.CreateTable(
                name: "AI_ANALYSIS",
                columns: table => new
                {
                    analysis_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    feedback_id = table.Column<int>(type: "int", nullable: false),
                    main_sentiment = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    confidence_score = table.Column<double>(type: "float", nullable: true),
                    is_crisis_alert = table.Column<bool>(type: "bit", nullable: true, defaultValue: false),
                    sentiment_override_by = table.Column<int>(type: "int", nullable: true),
                    processed_at = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())"),
                    agreement_rate = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__AI_ANALY__5B14DE5AFC9853C5", x => x.analysis_id);
                    table.ForeignKey(
                        name: "FK_Analysis_Feedback",
                        column: x => x.feedback_id,
                        principalTable: "SCRAPED_FEEDBACKS",
                        principalColumn: "feedback_id");
                    table.ForeignKey(
                        name: "FK_Analysis_OverrideUser",
                        column: x => x.sentiment_override_by,
                        principalTable: "USERS",
                        principalColumn: "user_id");
                });

            migrationBuilder.CreateTable(
                name: "MENTION_TAGS",
                columns: table => new
                {
                    feedback_id = table.Column<int>(type: "int", nullable: false),
                    tag_id = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__MENTION___0E4241A77F852E4C", x => new { x.feedback_id, x.tag_id });
                    table.ForeignKey(
                        name: "FK_MentionTag_Feedback",
                        column: x => x.feedback_id,
                        principalTable: "SCRAPED_FEEDBACKS",
                        principalColumn: "feedback_id");
                    table.ForeignKey(
                        name: "FK_MentionTag_Tag",
                        column: x => x.tag_id,
                        principalTable: "TAGS",
                        principalColumn: "tag_id");
                });

            migrationBuilder.CreateTable(
                name: "FEEDBACK_ASPECTS",
                columns: table => new
                {
                    aspect_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    analysis_id = table.Column<int>(type: "int", nullable: false),
                    category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    sentiment = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    confidence_score = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__FEEDBACK__F50CDEFE89E30CC0", x => x.aspect_id);
                    table.ForeignKey(
                        name: "FK_Aspect_Analysis",
                        column: x => x.analysis_id,
                        principalTable: "AI_ANALYSIS",
                        principalColumn: "analysis_id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AI_ANALYSIS_sentiment_override_by",
                table: "AI_ANALYSIS",
                column: "sentiment_override_by");

            migrationBuilder.CreateIndex(
                name: "UQ_Analysis_Feedback",
                table: "AI_ANALYSIS",
                column: "feedback_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BESPOKE_REPORTS_request_id",
                table: "BESPOKE_REPORTS",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "IX_BESPOKE_REQUESTS_assigned_by",
                table: "BESPOKE_REQUESTS",
                column: "assigned_by");

            migrationBuilder.CreateIndex(
                name: "IX_BESPOKE_REQUESTS_client_id",
                table: "BESPOKE_REQUESTS",
                column: "client_id");

            migrationBuilder.CreateIndex(
                name: "IX_BESPOKE_REQUESTS_reporter_id",
                table: "BESPOKE_REQUESTS",
                column: "reporter_id");

            migrationBuilder.CreateIndex(
                name: "IX_DATA_SOURCES_project_id",
                table: "DATA_SOURCES",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_EMAIL_VERIFICATIONS_user_id",
                table: "EMAIL_VERIFICATIONS",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_FB_SOURCES_added_by",
                table: "FB_SOURCES",
                column: "added_by");

            migrationBuilder.CreateIndex(
                name: "IX_FEEDBACK_ASPECTS_analysis_id",
                table: "FEEDBACK_ASPECTS",
                column: "analysis_id");

            migrationBuilder.CreateIndex(
                name: "IX_IMPORT_FILES_project_id",
                table: "IMPORT_FILES",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_IMPORT_FILES_source_id",
                table: "IMPORT_FILES",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "IX_IMPORT_FILES_uploaded_by",
                table: "IMPORT_FILES",
                column: "uploaded_by");

            migrationBuilder.CreateIndex(
                name: "IX_INFLUENCERS_project_id",
                table: "INFLUENCERS",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_MENTION_TAGS_tag_id",
                table: "MENTION_TAGS",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_MUTED_ENTITIES_muted_by",
                table: "MUTED_ENTITIES",
                column: "muted_by");

            migrationBuilder.CreateIndex(
                name: "IX_MUTED_ENTITIES_project_id",
                table: "MUTED_ENTITIES",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATIONS_project_id",
                table: "NOTIFICATIONS",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATIONS_user_id",
                table: "NOTIFICATIONS",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_NSR_SNAPSHOTS_project_id",
                table: "NSR_SNAPSHOTS",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_PASSWORD_RESET_TOKENS_user_id",
                table: "PASSWORD_RESET_TOKENS",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "UQ_ResetToken",
                table: "PASSWORD_RESET_TOKENS",
                column: "reset_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENTS_created_by",
                table: "PAYMENTS",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENTS_plan_id",
                table: "PAYMENTS",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENTS_request_id",
                table: "PAYMENTS",
                column: "request_id");

            migrationBuilder.CreateIndex(
                name: "UQ_Payments_OrderCode",
                table: "PAYMENTS",
                column: "order_code",
                unique: true,
                filter: "[order_code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_TxRef",
                table: "PAYMENTS",
                column: "transaction_ref",
                unique: true,
                filter: "[transaction_ref] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "UQ_PlatformCookies_Platform",
                table: "PLATFORM_COOKIES",
                column: "platform",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PROJECTS_workspace_id",
                table: "PROJECTS",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_SAVED_FILTERS_created_by",
                table: "SAVED_FILTERS",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_SAVED_FILTERS_project_id",
                table: "SAVED_FILTERS",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_SCRAPE_ORDERS_payment_id",
                table: "SCRAPE_ORDERS",
                column: "payment_id");

            migrationBuilder.CreateIndex(
                name: "IX_SCRAPE_ORDERS_project_id",
                table: "SCRAPE_ORDERS",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_SCRAPE_ORDERS_user_id",
                table: "SCRAPE_ORDERS",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_SCRAPE_ORDERS_workspace_id",
                table: "SCRAPE_ORDERS",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_SCRAPED_FEEDBACKS_project_id",
                table: "SCRAPED_FEEDBACKS",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_SCRAPED_FEEDBACKS_source_id",
                table: "SCRAPED_FEEDBACKS",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "IX_SCRAPING_JOBS_project_id",
                table: "SCRAPING_JOBS",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "IX_SCRAPING_JOBS_proxy_id",
                table: "SCRAPING_JOBS",
                column: "proxy_id");

            migrationBuilder.CreateIndex(
                name: "IX_SCRAPING_JOBS_source_id",
                table: "SCRAPING_JOBS",
                column: "source_id");

            migrationBuilder.CreateIndex(
                name: "IX_SUBSCRIPTIONS_plan_id",
                table: "SUBSCRIPTIONS",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_SUBSCRIPTIONS_workspace_id",
                table: "SUBSCRIPTIONS",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_SYSTEM_SETTINGS_updated_by",
                table: "SYSTEM_SETTINGS",
                column: "updated_by");

            migrationBuilder.CreateIndex(
                name: "UQ_SettingKey",
                table: "SYSTEM_SETTINGS",
                column: "setting_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TAGS_created_by",
                table: "TAGS",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_TAGS_project_id",
                table: "TAGS",
                column: "project_id");

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Email",
                table: "USERS",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WORKSPACE_ACTIVITY_LOGS_user_id",
                table: "WORKSPACE_ACTIVITY_LOGS",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_WORKSPACE_ACTIVITY_LOGS_workspace_id",
                table: "WORKSPACE_ACTIVITY_LOGS",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_WORKSPACE_INVITATIONS_invited_by",
                table: "WORKSPACE_INVITATIONS",
                column: "invited_by");

            migrationBuilder.CreateIndex(
                name: "IX_WORKSPACE_INVITATIONS_workspace_id",
                table: "WORKSPACE_INVITATIONS",
                column: "workspace_id");

            migrationBuilder.CreateIndex(
                name: "IX_WORKSPACE_MEMBERS_role_id",
                table: "WORKSPACE_MEMBERS",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "IX_WORKSPACE_MEMBERS_user_id",
                table: "WORKSPACE_MEMBERS",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_WORKSPACES_owner_id",
                table: "WORKSPACES",
                column: "owner_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BESPOKE_REPORTS");

            migrationBuilder.DropTable(
                name: "EMAIL_VERIFICATIONS");

            migrationBuilder.DropTable(
                name: "FB_SOURCES");

            migrationBuilder.DropTable(
                name: "FEEDBACK_ASPECTS");

            migrationBuilder.DropTable(
                name: "IMPORT_FILES");

            migrationBuilder.DropTable(
                name: "INFLUENCERS");

            migrationBuilder.DropTable(
                name: "MENTION_TAGS");

            migrationBuilder.DropTable(
                name: "MUTED_ENTITIES");

            migrationBuilder.DropTable(
                name: "NOTIFICATIONS");

            migrationBuilder.DropTable(
                name: "NSR_SNAPSHOTS");

            migrationBuilder.DropTable(
                name: "PASSWORD_RESET_TOKENS");

            migrationBuilder.DropTable(
                name: "PLATFORM_COOKIES");

            migrationBuilder.DropTable(
                name: "SAVED_FILTERS");

            migrationBuilder.DropTable(
                name: "SCRAPE_ORDERS");

            migrationBuilder.DropTable(
                name: "SCRAPING_JOBS");

            migrationBuilder.DropTable(
                name: "SUBSCRIPTIONS");

            migrationBuilder.DropTable(
                name: "SYSTEM_SETTINGS");

            migrationBuilder.DropTable(
                name: "WORKSPACE_ACTIVITY_LOGS");

            migrationBuilder.DropTable(
                name: "WORKSPACE_CREDITS");

            migrationBuilder.DropTable(
                name: "WORKSPACE_INVITATIONS");

            migrationBuilder.DropTable(
                name: "WORKSPACE_MEMBERS");

            migrationBuilder.DropTable(
                name: "AI_ANALYSIS");

            migrationBuilder.DropTable(
                name: "TAGS");

            migrationBuilder.DropTable(
                name: "PAYMENTS");

            migrationBuilder.DropTable(
                name: "SYSTEM_PROXIES");

            migrationBuilder.DropTable(
                name: "WORKSPACE_ROLES");

            migrationBuilder.DropTable(
                name: "SCRAPED_FEEDBACKS");

            migrationBuilder.DropTable(
                name: "SUBSCRIPTION_PLANS");

            migrationBuilder.DropTable(
                name: "BESPOKE_REQUESTS");

            migrationBuilder.DropTable(
                name: "DATA_SOURCES");

            migrationBuilder.DropTable(
                name: "PROJECTS");

            migrationBuilder.DropTable(
                name: "WORKSPACES");

            migrationBuilder.DropTable(
                name: "USERS");
        }
    }
}
