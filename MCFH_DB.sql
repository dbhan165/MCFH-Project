-- ========================================================
-- MCFH_DB.sql
-- Social Listening Platform — Complete Database Script
-- Đáp ứng đầy đủ 83 Use Cases (UC V2.0) + Cập nhật Scraping Architecture
-- Chạy trong SSMS: F5 hoặc Execute
-- ========================================================

USE master;
GO

-- Tạo DB nếu chưa có
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'MCFH_DB')
BEGIN
    CREATE DATABASE MCFH_DB;
END
GO

USE MCFH_DB;
GO

-- ========================================================
-- DROP TABLES (thứ tự ngược dependency để tránh FK error)
-- ========================================================
IF OBJECT_ID('PAYOS_KEYS',               'U') IS NOT NULL DROP TABLE PAYOS_KEYS;
IF OBJECT_ID('BREVO_KEYS',               'U') IS NOT NULL DROP TABLE BREVO_KEYS;
IF OBJECT_ID('PLATFORM_COOKIES',         'U') IS NOT NULL DROP TABLE PLATFORM_COOKIES;
IF OBJECT_ID('FEEDBACK_ASPECTS',         'U') IS NOT NULL DROP TABLE FEEDBACK_ASPECTS;
IF OBJECT_ID('MENTION_TAGS',             'U') IS NOT NULL DROP TABLE MENTION_TAGS;
IF OBJECT_ID('AI_ANALYSIS',              'U') IS NOT NULL DROP TABLE AI_ANALYSIS;
IF OBJECT_ID('IMPORT_FILES',             'U') IS NOT NULL DROP TABLE IMPORT_FILES;
IF OBJECT_ID('SCRAPING_JOBS',            'U') IS NOT NULL DROP TABLE SCRAPING_JOBS;
IF OBJECT_ID('SCRAPED_FEEDBACKS',        'U') IS NOT NULL DROP TABLE SCRAPED_FEEDBACKS;
IF OBJECT_ID('BESPOKE_REPORTS',          'U') IS NOT NULL DROP TABLE BESPOKE_REPORTS;
IF OBJECT_ID('PROJECT_MENTION_PACKAGES', 'U') IS NOT NULL DROP TABLE PROJECT_MENTION_PACKAGES;
IF OBJECT_ID('SCRAPE_ORDERS',            'U') IS NOT NULL DROP TABLE SCRAPE_ORDERS;
IF OBJECT_ID('SCRAPE_PACKAGES',          'U') IS NOT NULL DROP TABLE SCRAPE_PACKAGES;
IF OBJECT_ID('PAYMENTS',                 'U') IS NOT NULL DROP TABLE PAYMENTS;
IF OBJECT_ID('INFLUENCERS',              'U') IS NOT NULL DROP TABLE INFLUENCERS;
IF OBJECT_ID('MUTED_ENTITIES',           'U') IS NOT NULL DROP TABLE MUTED_ENTITIES;
IF OBJECT_ID('TAGS',                     'U') IS NOT NULL DROP TABLE TAGS;
IF OBJECT_ID('SAVED_FILTERS',            'U') IS NOT NULL DROP TABLE SAVED_FILTERS;
IF OBJECT_ID('NSR_SNAPSHOTS',            'U') IS NOT NULL DROP TABLE NSR_SNAPSHOTS;
IF OBJECT_ID('FB_SOURCES',               'U') IS NOT NULL DROP TABLE FB_SOURCES;
IF OBJECT_ID('DATA_SOURCES',             'U') IS NOT NULL DROP TABLE DATA_SOURCES;
IF OBJECT_ID('NOTIFICATIONS',            'U') IS NOT NULL DROP TABLE NOTIFICATIONS;
IF OBJECT_ID('BESPOKE_REQUESTS',         'U') IS NOT NULL DROP TABLE BESPOKE_REQUESTS;
IF OBJECT_ID('PROJECTS',                 'U') IS NOT NULL DROP TABLE PROJECTS;
IF OBJECT_ID('SUBSCRIPTIONS',            'U') IS NOT NULL DROP TABLE SUBSCRIPTIONS;
IF OBJECT_ID('WORKSPACE_ACTIVITY_LOGS',  'U') IS NOT NULL DROP TABLE WORKSPACE_ACTIVITY_LOGS;
IF OBJECT_ID('WORKSPACE_INVITATIONS',    'U') IS NOT NULL DROP TABLE WORKSPACE_INVITATIONS;
IF OBJECT_ID('WORKSPACE_MEMBERS',        'U') IS NOT NULL DROP TABLE WORKSPACE_MEMBERS;
IF OBJECT_ID('WORKSPACE_CREDITS',        'U') IS NOT NULL DROP TABLE WORKSPACE_CREDITS;
IF OBJECT_ID('WORKSPACES',               'U') IS NOT NULL DROP TABLE WORKSPACES;
IF OBJECT_ID('EMAIL_VERIFICATIONS',      'U') IS NOT NULL DROP TABLE EMAIL_VERIFICATIONS;
IF OBJECT_ID('PASSWORD_RESET_TOKENS',    'U') IS NOT NULL DROP TABLE PASSWORD_RESET_TOKENS;
IF OBJECT_ID('SYSTEM_SETTINGS',          'U') IS NOT NULL DROP TABLE SYSTEM_SETTINGS;
IF OBJECT_ID('SYSTEM_PROXIES',           'U') IS NOT NULL DROP TABLE SYSTEM_PROXIES;
IF OBJECT_ID('SUBSCRIPTION_PLANS',       'U') IS NOT NULL DROP TABLE SUBSCRIPTION_PLANS;
IF OBJECT_ID('WORKSPACE_ROLES',          'U') IS NOT NULL DROP TABLE WORKSPACE_ROLES;
IF OBJECT_ID('USERS',                    'U') IS NOT NULL DROP TABLE USERS;
GO

-- ========================================================
-- LEVEL 0 — Bảng không phụ thuộc
-- ========================================================

-- UC-03, 05, 06, 09, 10, 11, 44, 45, 46, 68
CREATE TABLE USERS (
    user_id        INT           IDENTITY(1,1) PRIMARY KEY,
    email          VARCHAR(255)  NOT NULL CONSTRAINT UQ_Users_Email UNIQUE,
    password_hash  VARCHAR(255)  NULL,           -- NULL nếu đăng ký qua Google SSO (UC-06)
    full_name      NVARCHAR(100) NOT NULL,
    phone          VARCHAR(20)   NULL,            -- UC-10
    avatar_url     NVARCHAR(MAX) NULL,            -- UC-10: Cloud URL
    auth_provider  VARCHAR(50)   NOT NULL DEFAULT 'local', -- UC-06: 'local' | 'google'
    google_id      VARCHAR(255)  NULL,            -- UC-06: Google OAuth sub ID
    system_role    VARCHAR(50)   NOT NULL,        -- 'Admin' | 'Client' | 'Reporter'
    is_verified    BIT           DEFAULT 0,       -- UC-04: Đã xác thực email chưa
    verified_at    DATETIME      NULL,            -- UC-04: Thời điểm xác thực
    is_banned      BIT           DEFAULT 0,       -- UC-68: Khóa tài khoản
    banned_at      DATETIME      NULL,            -- UC-68: Thời điểm bị khóa
    created_at     DATETIME      DEFAULT GETDATE()
);
GO

-- UC-16: Từ điển role trong Workspace
CREATE TABLE WORKSPACE_ROLES (
    role_id    INT         IDENTITY(1,1) PRIMARY KEY,
    role_name  VARCHAR(50) NOT NULL       -- 'Owner' | 'Editor' | 'Viewer'
);
GO

-- UC-02, 55, 69: Danh mục gói cước
CREATE TABLE SUBSCRIPTION_PLANS (
    plan_id          INT           IDENTITY(1,1) PRIMARY KEY,
    name             NVARCHAR(100) NOT NULL,
    price            DECIMAL(18,2) NOT NULL,
    ai_credit_limit  INT           NOT NULL
);
GO

-- UC-48, 49, 50, 73, 78: Kho địa chỉ IP proxy cho Bot
CREATE TABLE SYSTEM_PROXIES (
    proxy_id     INT          IDENTITY(1,1) PRIMARY KEY,
    ip_address   VARCHAR(50)  NOT NULL,
    port         INT          NOT NULL,
    auth_user    VARCHAR(100) NULL,
    auth_pass    VARCHAR(100) NULL,
    status       VARCHAR(50)  DEFAULT 'active', -- 'active' | 'dead'
    fail_count   INT          DEFAULT 0,
    last_used_at DATETIME     NULL
);
GO

-- UC-75: Cấu hình hệ thống toàn cục (Gemini Key, VNPay Secret, ...)
CREATE TABLE SYSTEM_SETTINGS (
    setting_id    INT           IDENTITY(1,1) PRIMARY KEY,
    setting_key   VARCHAR(100)  NOT NULL CONSTRAINT UQ_SettingKey UNIQUE,
    setting_value NVARCHAR(MAX) NULL,            -- Giá trị (mã hóa AES nếu is_encrypted = 1)
    is_encrypted  BIT           DEFAULT 0,       -- Service tự quyết định có decrypt không
    updated_at    DATETIME      DEFAULT GETDATE(),
    updated_by    INT           NULL             -- FK → USERS, thêm sau khi USERS tồn tại
);
GO

-- UC-04: OTP / Verification Link xác thực email
CREATE TABLE EMAIL_VERIFICATIONS (
    verification_id    INT          IDENTITY(1,1) PRIMARY KEY,
    user_id            INT          NOT NULL,
    otp_code           VARCHAR(20)  NULL,         -- Mã 6 số gửi qua email
    verification_token VARCHAR(255) NULL,         -- Token dùng cho link click
    expired_at         DATETIME     NOT NULL,
    is_used            BIT          DEFAULT 0,    -- Chặn reuse OTP cũ
    created_at         DATETIME     DEFAULT GETDATE()
);
GO

-- UC-07, 08: Token đặt lại mật khẩu
CREATE TABLE PASSWORD_RESET_TOKENS (
    token_id    INT          IDENTITY(1,1) PRIMARY KEY,
    user_id     INT          NOT NULL,
    reset_token VARCHAR(255) NOT NULL CONSTRAINT UQ_ResetToken UNIQUE,
    expired_at  DATETIME     NOT NULL,
    is_used     BIT          DEFAULT 0,
    created_at  DATETIME     DEFAULT GETDATE()
);
GO

-- ========================================================
-- LEVEL 1 — Phụ thuộc USERS
-- ========================================================

-- UC-12, 13, 14: Không gian làm việc (Workspace)
CREATE TABLE WORKSPACES (
    workspace_id INT           IDENTITY(1,1) PRIMARY KEY,
    owner_id     INT           NOT NULL,
    name         NVARCHAR(255) NOT NULL,
    is_deleted   BIT           DEFAULT 0,    -- UC-14: Soft-delete
    deleted_at   DATETIME      NULL,         -- UC-14
    created_at   DATETIME      DEFAULT GETDATE(),
    CONSTRAINT FK_Workspace_Owner FOREIGN KEY (owner_id) REFERENCES USERS(user_id)
);
GO

-- ========================================================
-- LEVEL 2 — Phụ thuộc WORKSPACES
-- ========================================================

-- UC-15, 16, 17: Thành viên trong Workspace
CREATE TABLE WORKSPACE_MEMBERS (
    workspace_id INT NOT NULL,
    user_id      INT NOT NULL,
    role_id      INT NOT NULL,
    PRIMARY KEY (workspace_id, user_id),
    CONSTRAINT FK_Member_Workspace FOREIGN KEY (workspace_id) REFERENCES WORKSPACES(workspace_id),
    CONSTRAINT FK_Member_User      FOREIGN KEY (user_id)      REFERENCES USERS(user_id),
    CONSTRAINT FK_Member_Role      FOREIGN KEY (role_id)      REFERENCES WORKSPACE_ROLES(role_id)
);
GO

-- UC-15: Lời mời tham gia Workspace
CREATE TABLE WORKSPACE_INVITATIONS (
    invitation_id INT          IDENTITY(1,1) PRIMARY KEY,
    workspace_id  INT          NOT NULL,
    invited_email VARCHAR(255) NOT NULL,
    invited_by    INT          NOT NULL,
    status        VARCHAR(50)  DEFAULT 'pending', -- 'pending' | 'accepted' | 'rejected'
    created_at    DATETIME     DEFAULT GETDATE(),
    CONSTRAINT FK_Invitation_Workspace FOREIGN KEY (workspace_id) REFERENCES WORKSPACES(workspace_id),
    CONSTRAINT FK_Invitation_InvitedBy FOREIGN KEY (invited_by)   REFERENCES USERS(user_id)
);
GO

-- UC-56: Đăng ký gói cước cho Workspace
CREATE TABLE SUBSCRIPTIONS (
    subscription_id INT         IDENTITY(1,1) PRIMARY KEY,
    workspace_id    INT         NOT NULL,
    plan_id         INT         NOT NULL,
    start_date      DATETIME    NOT NULL,
    expiry_date     DATETIME    NOT NULL,
    status          VARCHAR(50) DEFAULT 'active', -- 'active' | 'expired' | 'cancelled'
    CONSTRAINT FK_Subscription_Workspace FOREIGN KEY (workspace_id) REFERENCES WORKSPACES(workspace_id),
    CONSTRAINT FK_Subscription_Plan      FOREIGN KEY (plan_id)      REFERENCES SUBSCRIPTION_PLANS(plan_id)
);
GO

-- UC-55: Theo dõi AI Credit usage thực tế
-- remaining = total_credits - used_credits (tính ở service, không lưu DB tránh race condition)
CREATE TABLE WORKSPACE_CREDITS (
    workspace_id  INT      NOT NULL,
    total_credits INT      NOT NULL,     -- Copy từ SUBSCRIPTION_PLANS.ai_credit_limit khi mua gói
    used_credits  INT      DEFAULT 0,   -- Cộng dồn mỗi lần gọi Gemini API
    last_updated  DATETIME DEFAULT GETDATE(),
    CONSTRAINT PK_WorkspaceCredits     PRIMARY KEY (workspace_id),
    CONSTRAINT FK_Credits_Workspace    FOREIGN KEY (workspace_id) REFERENCES WORKSPACES(workspace_id)
);
GO

-- Log hoạt động trong Workspace (audit trail)
CREATE TABLE WORKSPACE_ACTIVITY_LOGS (
    log_id       INT           IDENTITY(1,1) PRIMARY KEY,
    workspace_id INT           NOT NULL,
    user_id      INT           NOT NULL,
    action_type  VARCHAR(50)   NOT NULL,
    target_type  VARCHAR(50)   NULL,
    target_id    INT           NULL,
    target_name  NVARCHAR(255) NULL,
    description  NVARCHAR(MAX) NULL,
    created_at   DATETIME      DEFAULT GETDATE(),
    CONSTRAINT FK_ActivityLog_Workspace FOREIGN KEY (workspace_id) REFERENCES WORKSPACES(workspace_id),
    CONSTRAINT FK_ActivityLog_User      FOREIGN KEY (user_id)      REFERENCES USERS(user_id)
);
GO

CREATE INDEX IX_ActivityLog_WorkspaceId_CreatedAt
    ON WORKSPACE_ACTIVITY_LOGS(workspace_id, created_at DESC);
GO

-- UC-18, 19, 20, 21: Project (tên cũ: Campaign)
-- enable_facebook/tiktok/youtube/maps: Cờ bật/tắt nền tảng cho Project (UI checkbox, User tick chọn)
CREATE TABLE PROJECTS (
    project_id      INT           IDENTITY(1,1) PRIMARY KEY,
    workspace_id    INT           NOT NULL,
    name            NVARCHAR(255) NOT NULL,
    description     NVARCHAR(MAX) NULL,
    search_query    NVARCHAR(MAX) NULL,    -- Từ khóa tổng theo dõi thương hiệu (UC-19), dùng chung mọi platform
    enable_facebook BIT           DEFAULT 0,
    enable_tiktok   BIT           DEFAULT 0,
    enable_youtube  BIT           DEFAULT 0,
    enable_maps     BIT           DEFAULT 0,
    is_deleted      BIT           DEFAULT 0, -- UC-21: Soft-delete
    deleted_at      DATETIME      NULL,      -- UC-21
    created_at      DATETIME      DEFAULT GETDATE(),
    CONSTRAINT FK_Project_Workspace FOREIGN KEY (workspace_id) REFERENCES WORKSPACES(workspace_id)
);
GO

-- UC-37, 38, 58, 39, 40, 41, 42: Yêu cầu báo cáo Bespoke
CREATE TABLE BESPOKE_REQUESTS (
    request_id     INT           IDENTITY(1,1) PRIMARY KEY,
    client_id      INT           NOT NULL,
    reporter_id    INT           NULL,
    assigned_by    INT           NULL,         -- Admin giao việc
    title          NVARCHAR(255) NOT NULL,
    requirements   NVARCHAR(MAX) NULL,         -- Mô tả yêu cầu chung
    custom_metrics NVARCHAR(MAX) NULL,         -- UC-58 V2.0: JSON tiêu chí đo lường riêng
                                               -- VD: {"aspects":["Giá","Ship"],"competitors":["BrandX"]}
    agreed_price   DECIMAL(18,2) NULL,         -- UC-61: Giá sau khi Reporter báo giá
    deadline       DATETIME      NULL,         -- UC-61
    assigned_at    DATETIME      NULL,
    submitted_at   DATETIME      NULL,
    status         VARCHAR(50)   DEFAULT 'pending',
    -- Vòng đời status: pending → quoted → paid → in_progress → completed | cancelled
    CONSTRAINT FK_Bespoke_Client   FOREIGN KEY (client_id)   REFERENCES USERS(user_id),
    CONSTRAINT FK_Bespoke_Reporter FOREIGN KEY (reporter_id) REFERENCES USERS(user_id),
    CONSTRAINT FK_Bespoke_Admin    FOREIGN KEY (assigned_by) REFERENCES USERS(user_id)
);
GO

-- ========================================================
-- LEVEL 3 — Phụ thuộc PROJECTS / BESPOKE_REQUESTS
-- ========================================================

-- UC-51, 82: Thông báo & Cảnh báo khủng hoảng real-time (SignalR)
CREATE TABLE NOTIFICATIONS (
    notification_id INT           IDENTITY(1,1) PRIMARY KEY,
    user_id         INT           NOT NULL,
    project_id      INT           NULL,          -- Gán cảnh báo đúng project (UC-82)
    title           NVARCHAR(255) NOT NULL,
    message         NVARCHAR(MAX) NULL,
    type            VARCHAR(50)   NULL,           -- 'CrisisAlert' | 'System' | 'Billing'
    related_id      INT           NULL,
    related_type    VARCHAR(50)   NULL,           -- 'payment' | 'bespoke' | 'mention'
    is_read         BIT           DEFAULT 0,
    created_at      DATETIME      DEFAULT GETDATE(),
    CONSTRAINT FK_Notification_User    FOREIGN KEY (user_id)    REFERENCES USERS(user_id),
    CONSTRAINT FK_Notification_Project FOREIGN KEY (project_id) REFERENCES PROJECTS(project_id)
);
GO

-- UC-22, 23, 24: Nguồn dữ liệu cào (URL / Hashtag) — hiện không dùng cho Facebook/YouTube/TikTok
-- (xem FB_SOURCES bên dưới); giữ lại cho mục đích Import CSV/Excel (UC-25) và mở rộng sau.
CREATE TABLE DATA_SOURCES (
    source_id    INT           IDENTITY(1,1) PRIMARY KEY,
    project_id   INT           NOT NULL,
    platform     VARCHAR(50)   NOT NULL,    -- 'facebook' | 'tiktok' | 'maps'
    source_type  VARCHAR(50)   NOT NULL,    -- 'Crawl' | 'Import'
    target_url   NVARCHAR(MAX) NULL,        -- Link trang/kênh cần cào
    search_query NVARCHAR(MAX) NULL,        -- Hashtag hoặc keyword tìm kiếm
    status       VARCHAR(50)   DEFAULT 'active', -- 'active' | 'paused'
    CONSTRAINT FK_Source_Project FOREIGN KEY (project_id) REFERENCES PROJECTS(project_id)
);
GO

-- Nguồn Facebook Group cấp HỆ THỐNG (Admin quản lý, dùng CHUNG cho mọi Project có enable_facebook = 1)
-- Lý do tách riêng khỏi DATA_SOURCES: tài khoản Facebook dùng để scrape phải LÀ THÀNH VIÊN của group
-- trước thì mới cào được — nên không thể để User tự nhập URL group tùy ý (UI chỉ cho tick chọn platform,
-- không cho nhập URL). Admin chịu trách nhiệm join group + thêm vào đây trước khi User dùng được.
CREATE TABLE FB_SOURCES (
    fb_source_id INT IDENTITY(1,1) PRIMARY KEY,
    group_url    NVARCHAR(MAX) NOT NULL,
    group_name   NVARCHAR(255) NULL,
    status       VARCHAR(50) DEFAULT 'active',  -- 'active' | 'paused' | 'broken'
    added_by     INT NOT NULL,
    created_at   DATETIME DEFAULT GETDATE(),
    CONSTRAINT FK_FbSource_AddedBy FOREIGN KEY (added_by) REFERENCES USERS(user_id)
);
GO

-- UC-29, 40, 41, 42, 43, 46, 47, 58, 81: Cache snapshot NSR hàng đêm
CREATE TABLE NSR_SNAPSHOTS (
    snapshot_id    INT         IDENTITY(1,1) PRIMARY KEY,
    project_id     INT         NOT NULL,
    platform       VARCHAR(50) NULL,        -- 'facebook' | 'tiktok' | 'maps' | 'all'
    snapshot_date  DATE        NOT NULL,
    total_positive INT         DEFAULT 0,
    total_negative INT         DEFAULT 0,
    total_neutral  INT         DEFAULT 0,
    total_reach    INT         DEFAULT 0,   -- UC-40: Tổng reach cho Dashboard Overview
    nsr_score      FLOAT       NULL,
    presence_score FLOAT       NULL,        -- UC-40: Chỉ số hiện diện thương hiệu
    calculated_at  DATETIME    DEFAULT GETDATE(),
    CONSTRAINT FK_Snapshot_Project FOREIGN KEY (project_id) REFERENCES PROJECTS(project_id)
);
GO

-- UC-28, 29, 30: Bộ lọc tùy chỉnh của User
CREATE TABLE SAVED_FILTERS (
    filter_id     INT           IDENTITY(1,1) PRIMARY KEY,
    project_id    INT           NOT NULL,
    name          NVARCHAR(255) NOT NULL,
    filter_config NVARCHAR(MAX) NOT NULL,   -- JSON: { platform, sentiment, dateRange, ... }
    created_by    INT           NOT NULL,
    created_at    DATETIME      DEFAULT GETDATE(),
    CONSTRAINT FK_Filter_Project  FOREIGN KEY (project_id) REFERENCES PROJECTS(project_id),
    CONSTRAINT FK_Filter_Creator  FOREIGN KEY (created_by) REFERENCES USERS(user_id)
);
GO

-- UC-33, 34: Thẻ phân loại Mention
CREATE TABLE TAGS (
    tag_id     INT           IDENTITY(1,1) PRIMARY KEY,
    project_id INT           NOT NULL,
    name       NVARCHAR(100) NOT NULL,
    color      VARCHAR(20)   NULL,          -- Mã màu HEX, ví dụ: #FF5733
    created_by INT           NOT NULL,
    CONSTRAINT FK_Tag_Project  FOREIGN KEY (project_id) REFERENCES PROJECTS(project_id),
    CONSTRAINT FK_Tag_Creator  FOREIGN KEY (created_by) REFERENCES USERS(user_id)
);
GO

-- UC-37, 38: Chặn tác giả / nguồn rác
CREATE TABLE MUTED_ENTITIES (
    mute_id      INT           IDENTITY(1,1) PRIMARY KEY,
    project_id   INT           NOT NULL,
    entity_type  VARCHAR(50)   NOT NULL,    -- 'author' | 'source'
    entity_value NVARCHAR(255) NOT NULL,    -- Tên tác giả hoặc URL nguồn
    muted_by     INT           NOT NULL,
    created_at   DATETIME      DEFAULT GETDATE(),
    CONSTRAINT FK_Muted_Project FOREIGN KEY (project_id) REFERENCES PROJECTS(project_id),
    CONSTRAINT FK_Muted_User    FOREIGN KEY (muted_by)   REFERENCES USERS(user_id)
);
GO

-- UC-52, 53, 54: Người ảnh hưởng (Influencer)
CREATE TABLE INFLUENCERS (
    influencer_id   INT           IDENTITY(1,1) PRIMARY KEY,
    project_id      INT           NOT NULL,
    name            NVARCHAR(255) NOT NULL,
    platform        VARCHAR(50)   NOT NULL,
    handle_url      NVARCHAR(MAX) NULL,
    followers       INT           DEFAULT 0,
    influence_score FLOAT         NULL,
    reach           INT           DEFAULT 0,
    created_at      DATETIME      DEFAULT GETDATE(),
    CONSTRAINT FK_Influencer_Project FOREIGN KEY (project_id) REFERENCES PROJECTS(project_id)
);
GO

-- ========================================================
-- LEVEL 4 — Phụ thuộc SUBSCRIPTION_PLANS / BESPOKE_REQUESTS / PROJECTS
-- ========================================================

-- UC-56, 57, 62: Lịch sử thanh toán (Subscription + Bespoke)
CREATE TABLE PAYMENTS (
    payment_id      INT           IDENTITY(1,1) PRIMARY KEY,
    transaction_ref VARCHAR(100)  NULL CONSTRAINT UQ_TxRef UNIQUE, -- Mã giao dịch VNPay
    amount          DECIMAL(18,2) NOT NULL,
    status          VARCHAR(50)   NULL,   -- 'Paid' | 'Pending' | 'Failed'
    type            VARCHAR(50)   NULL,   -- 'subscription' | 'bespoke'
    plan_id         INT           NULL,   -- FK nếu type = subscription
    request_id      INT           NULL,   -- FK nếu type = bespoke
    created_by      INT           NULL,
    created_at      DATETIME      DEFAULT GETDATE(),
    CONSTRAINT FK_Payment_Plan    FOREIGN KEY (plan_id)    REFERENCES SUBSCRIPTION_PLANS(plan_id),
    CONSTRAINT FK_Payment_Request FOREIGN KEY (request_id) REFERENCES BESPOKE_REQUESTS(request_id),
    CONSTRAINT FK_Payment_Creator FOREIGN KEY (created_by) REFERENCES USERS(user_id)
);
GO

-- UC-64, 65: File PDF thành phẩm của Reporter
CREATE TABLE BESPOKE_REPORTS (
    report_id   INT           IDENTITY(1,1) PRIMARY KEY,
    request_id  INT           NOT NULL,
    file_url    NVARCHAR(MAX) NOT NULL,
    version     VARCHAR(50)   NULL,
    uploaded_at DATETIME      DEFAULT GETDATE(),
    CONSTRAINT FK_Report_Request FOREIGN KEY (request_id) REFERENCES BESPOKE_REQUESTS(request_id)
);
GO

-- UC-26, 27, 31, 32, 35, 36, 39, 44, 45: Kho bình luận thô cào từ MXH
-- source_id: nullable — KHÔNG dùng cho Facebook/YouTube/TikTok (xem project_id/platform bên dưới);
--            giữ lại nullable cho mục đích Import file (UC-25) hoặc mở rộng sau.
-- project_id, platform: cột mới — biết feedback thuộc Project nào và từ nền tảng nào,
--            thay thế vai trò cũ của source_id sau khi Facebook/YouTube/TikTok không còn gắn DATA_SOURCES.
-- comments_file_url, comments_count: Comment KHÔNG lưu theo row DB (tránh phình bảng khi scale lớn),
--            lưu vào file JSON local (StorageData/comments/{feedback_id}.json), cột này chỉ lưu path + count.
CREATE TABLE SCRAPED_FEEDBACKS (
    feedback_id       INT           IDENTITY(1,1) PRIMARY KEY,  -- Tương đương mention_id
    source_id         INT           NULL,
    project_id        INT           NULL,
    import_file_id    INT           NULL,
    platform          VARCHAR(50)   NULL,    -- 'facebook' | 'youtube' | 'tiktok'
    content           NVARCHAR(MAX) NOT NULL,
    author_name       NVARCHAR(255) NULL,
    original_url      NVARCHAR(MAX) NULL,    -- UC-32: URL bài viết/comment gốc trên MXH
    posted_at         DATETIME      NULL,
    reach             INT           DEFAULT 0, -- UC-44, 45: Tầm tiếp cận
    engagement_count  INT           DEFAULT 0, -- UC-45: Tổng tương tác (Like/Share/Cmt)
    comments_file_url NVARCHAR(MAX) NULL,      -- Path file JSON chứa toàn bộ comment của bài viết
    comments_count    INT           DEFAULT 0, -- Tổng số comment, tránh phải đọc file để biết count
    pinned_for_report BIT           DEFAULT 0, -- UC-36: Đưa vào báo cáo PDF
    is_deleted        BIT           DEFAULT 0, -- UC-39: Soft-delete rác/spam
    deleted_at        DATETIME      NULL,       -- UC-39
    scraped_at        DATETIME      DEFAULT GETDATE(),
    CONSTRAINT FK_Feedback_Source  FOREIGN KEY (source_id)  REFERENCES DATA_SOURCES(source_id),
    CONSTRAINT FK_Feedback_Project FOREIGN KEY (project_id) REFERENCES PROJECTS(project_id)
);
GO

-- UC-25: Lịch sử Import CSV/Excel
CREATE TABLE IMPORT_FILES (
    file_id       INT           IDENTITY(1,1) PRIMARY KEY,
    project_id    INT           NOT NULL,
    source_id     INT           NULL,           -- FK → DATA_SOURCES (gán sau khi tạo source)
    uploaded_by   INT           NOT NULL,
    file_name     NVARCHAR(255) NOT NULL,
    file_url      NVARCHAR(MAX) NOT NULL,       -- Cloud Storage URL
    total_rows    INT           NULL,            -- Số dòng đọc được từ file
    imported_rows INT           NULL,            -- Số dòng import thành công
    status        VARCHAR(50)   DEFAULT 'processing', -- 'processing' | 'completed' | 'failed'
    imported_at   DATETIME      DEFAULT GETDATE(),
    CONSTRAINT FK_Import_Project  FOREIGN KEY (project_id)  REFERENCES PROJECTS(project_id),
    CONSTRAINT FK_Import_Source   FOREIGN KEY (source_id)   REFERENCES DATA_SOURCES(source_id),
    CONSTRAINT FK_Import_Uploader FOREIGN KEY (uploaded_by) REFERENCES USERS(user_id)
);
GO

-- UC-74, 83: Giám sát tiến trình cào của Bot Python
CREATE TABLE SCRAPING_JOBS (
    job_id        VARCHAR(100)  PRIMARY KEY,    -- Hangfire Job ID
    source_id     INT           NOT NULL,
    project_id    INT           NOT NULL,
    proxy_id      INT           NULL,
    status        VARCHAR(50)   NULL,           -- 'running' | 'completed' | 'failed'
    total_scraped INT           DEFAULT 0,
    error_log     NVARCHAR(MAX) NULL,
    started_at    DATETIME      NULL,
    finished_at   DATETIME      NULL,
    CONSTRAINT FK_Job_Source  FOREIGN KEY (source_id)  REFERENCES DATA_SOURCES(source_id),
    CONSTRAINT FK_Job_Project FOREIGN KEY (project_id) REFERENCES PROJECTS(project_id),
    CONSTRAINT FK_Job_Proxy   FOREIGN KEY (proxy_id)   REFERENCES SYSTEM_PROXIES(proxy_id)
);
GO

-- ========================================================
-- LEVEL 5 — AI Pipeline (phụ thuộc SCRAPED_FEEDBACKS)
-- ========================================================

-- UC-27, 31, 57, 80, 82: Kết quả phân tích cảm xúc của Gemini
-- agreement_rate: % đồng tình của comment với nội dung bài viết — AI đọc toàn bộ file JSON
--                  (comments_file_url ở SCRAPED_FEEDBACKS) để tính, không lưu từng comment riêng.
CREATE TABLE AI_ANALYSIS (
    analysis_id           INT         IDENTITY(1,1) PRIMARY KEY,
    feedback_id           INT         NOT NULL CONSTRAINT UQ_Analysis_Feedback UNIQUE,
    main_sentiment        VARCHAR(50) NULL,    -- 'Positive' | 'Negative' | 'Neutral'
    confidence_score      FLOAT       NULL,
    agreement_rate        FLOAT       NULL,    -- % đồng tình của comment với bài viết
    is_crisis_alert       BIT         DEFAULT 0, -- UC-82: Trigger cảnh báo khủng hoảng
    sentiment_override_by INT         NULL,    -- UC-31: FK → USERS (nhân viên sửa nhãn thủ công)
    processed_at          DATETIME    DEFAULT GETDATE(),
    CONSTRAINT FK_Analysis_Feedback     FOREIGN KEY (feedback_id)           REFERENCES SCRAPED_FEEDBACKS(feedback_id),
    CONSTRAINT FK_Analysis_OverrideUser FOREIGN KEY (sentiment_override_by) REFERENCES USERS(user_id)
);
GO

-- UC-35: Gán Tag vào Mention — quan hệ nhiều-nhiều
CREATE TABLE MENTION_TAGS (
    feedback_id INT NOT NULL,
    tag_id      INT NOT NULL,
    PRIMARY KEY (feedback_id, tag_id),
    CONSTRAINT FK_MentionTag_Feedback FOREIGN KEY (feedback_id) REFERENCES SCRAPED_FEEDBACKS(feedback_id),
    CONSTRAINT FK_MentionTag_Tag      FOREIGN KEY (tag_id)      REFERENCES TAGS(tag_id)
);
GO

-- ========================================================
-- LEVEL 6 — Chi tiết khía cạnh AI (phụ thuộc AI_ANALYSIS)
-- ========================================================

-- UC-43: Phân tích khía cạnh (Giá, Dịch vụ, Chất lượng, ...)
CREATE TABLE FEEDBACK_ASPECTS (
    aspect_id        INT           IDENTITY(1,1) PRIMARY KEY,
    analysis_id      INT           NOT NULL,
    category         NVARCHAR(100) NOT NULL,   -- 'Giá' | 'Dịch vụ' | 'Chất lượng' | ...
    sentiment        VARCHAR(50)   NULL,        -- 'Positive' | 'Negative' | 'Neutral'
    confidence_score FLOAT         NULL,
    CONSTRAINT FK_Aspect_Analysis FOREIGN KEY (analysis_id) REFERENCES AI_ANALYSIS(analysis_id)
);
GO

-- ========================================================
-- FOREIGN KEY DEFERREDS
-- (Các FK vòng: SYSTEM_SETTINGS → USERS — thêm sau khi cả 2 bảng đã tồn tại)
-- ========================================================

ALTER TABLE SYSTEM_SETTINGS
    ADD CONSTRAINT FK_Setting_UpdatedBy FOREIGN KEY (updated_by) REFERENCES USERS(user_id);
GO

ALTER TABLE EMAIL_VERIFICATIONS
    ADD CONSTRAINT FK_EmailVerif_User FOREIGN KEY (user_id) REFERENCES USERS(user_id);
GO

ALTER TABLE PASSWORD_RESET_TOKENS
    ADD CONSTRAINT FK_PwdReset_User FOREIGN KEY (user_id) REFERENCES USERS(user_id);
GO

-- ========================================================
-- SEED DATA — Dữ liệu khởi tạo tối thiểu
-- ========================================================

-- Workspace Roles
INSERT INTO WORKSPACE_ROLES (role_name) VALUES ('Owner'), ('Editor'), ('Viewer');

-- Subscription Plans
INSERT INTO SUBSCRIPTION_PLANS (name, price, ai_credit_limit) VALUES
    ('Basic',      199000,   500),
    ('Premium',    499000,  2000),
    ('Enterprise', 999000, 10000);

-- System Settings (API Keys — để trống, Admin điền qua UC-75)
INSERT INTO SYSTEM_SETTINGS (setting_key, setting_value, is_encrypted) VALUES
    ('GEMINI_API_KEY',    NULL, 1),
    ('VNPAY_SECRET_KEY',  NULL, 1),
    ('VNPAY_TMN_CODE',    NULL, 0),
    ('SMTP_HOST',         NULL, 0),
    ('SMTP_PORT',         NULL, 0),
    ('SMTP_PASSWORD',     NULL, 1),
    ('CRISIS_THRESHOLD',  '30', 0);  -- UC-82: % Tiêu cực tăng > 30% → bắn cảnh báo

GO

-- ========================================================
-- DỮ LIỆU TEST — Workspace / Project / FB_SOURCES mẫu cho demo FPT
-- (Có thể bỏ qua đoạn này nếu không cần seed sẵn data test)
-- ========================================================

-- Tạo user test
INSERT INTO USERS (email, full_name, system_role, is_verified)
VALUES ('test@mcfh.com', N'Test User', 'Client', 1);

-- Tạo workspace (owner_id = user_id vừa tạo, mặc định = 1 nếu DB rỗng)
INSERT INTO WORKSPACES (owner_id, name)
VALUES (1, N'FPT Workspace');

-- Tạo project kèm keyword theo dõi + bật cả 3 nền tảng (workspace_id = 1 nếu DB rỗng)
INSERT INTO PROJECTS (workspace_id, name, search_query, enable_facebook, enable_youtube, enable_tiktok)
VALUES (1, N'FPT Reputation Tracking', N'FPT', 1, 1, 1);

-- Thêm nguồn Facebook Group mẫu (Admin setup sẵn — added_by = user_id Admin, mặc định = 1)
INSERT INTO FB_SOURCES (group_url, group_name, added_by)
VALUES ('https://www.facebook.com/groups/465885632447300', N'IT Tuyển Dụng - Tìm Việc Làm IT', 1);

GO

-- ========================================================
-- XÁC NHẬN TẠO THÀNH CÔNG
-- ========================================================

SELECT
    t.name          AS [Table],
    p.rows          AS [Rows],
    CAST(ROUND((SUM(a.total_pages) * 8) / 1024.0, 2) AS NVARCHAR) + ' KB' AS [Size]
FROM sys.tables t
JOIN sys.indexes i     ON t.object_id = i.object_id
JOIN sys.partitions p  ON i.object_id = p.object_id AND i.index_id = p.index_id
JOIN sys.allocation_units a ON p.partition_id = a.container_id
WHERE t.is_ms_shipped = 0
GROUP BY t.name, p.rows
ORDER BY t.name;

DECLARE @tableCount INT = (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0);
PRINT '========================================================';
PRINT 'MCFH_DB tao thanh cong!';
PRINT 'Tong so bang: ' + CAST(@tableCount AS VARCHAR);
PRINT '========================================================';
GO

-- Chạy trên MCFH_DB trước khi dùng luồng báo giá / thanh toán cào dữ liệu.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SCRAPE_ORDERS')
BEGIN
    CREATE TABLE SCRAPE_ORDERS (
        order_id            INT IDENTITY(1,1) PRIMARY KEY,
        workspace_id        INT NOT NULL,
        project_id          INT NOT NULL,
        user_id             INT NOT NULL,
        keyword             NVARCHAR(500) NOT NULL,
        posted_since_days   INT NOT NULL DEFAULT 30,
        quoted_price        DECIMAL(18,2) NOT NULL,
        status              VARCHAR(50) NOT NULL DEFAULT 'quoted',
        payment_id          INT NULL,
        scrape_job_id       VARCHAR(100) NULL,
        progress_percent    INT NOT NULL DEFAULT 0,
        status_message      NVARCHAR(500) NULL,
        estimated_report_at DATETIME NULL,
        report_ready_at     DATETIME NULL,
        created_at          DATETIME NOT NULL DEFAULT GETDATE(),
        paid_at             DATETIME NULL,
        completed_at        DATETIME NULL,
        CONSTRAINT FK_ScrapeOrder_Workspace FOREIGN KEY (workspace_id) REFERENCES WORKSPACES(workspace_id),
        CONSTRAINT FK_ScrapeOrder_Project FOREIGN KEY (project_id) REFERENCES PROJECTS(project_id),
        CONSTRAINT FK_ScrapeOrder_User FOREIGN KEY (user_id) REFERENCES USERS(user_id),
        CONSTRAINT FK_ScrapeOrder_Payment FOREIGN KEY (payment_id) REFERENCES PAYMENTS(payment_id)
    );
    CREATE INDEX IX_ScrapeOrders_User ON SCRAPE_ORDERS(user_id, created_at DESC);
    CREATE INDEX IX_ScrapeOrders_Project ON SCRAPE_ORDERS(project_id);
END
GO

ALTER TABLE SCRAPING_JOBS ALTER COLUMN source_id INT NULL;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PLATFORM_COOKIES')
BEGIN
    CREATE TABLE PLATFORM_COOKIES (
        platform_cookie_id  INT IDENTITY(1,1) NOT NULL,
        platform            VARCHAR(50)  NOT NULL,   -- 'facebook' | 'tiktok'
        file_path           NVARCHAR(500) NOT NULL, -- relative ContentRoot, vd: cookies/fb_cookie.json
        status              VARCHAR(50)  NOT NULL DEFAULT 'active', -- active | disabled | expired
        note                NVARCHAR(1000) NULL,
        cookie_count        INT NOT NULL DEFAULT 0,
        expires_at          DATETIME NULL,          -- expirationDate sớm nhất trong JSON
        uploaded_at         DATETIME NULL,          -- admin upload cookie lần cuối
        last_used_at        DATETIME NULL,          -- scraper đọc/ghi file lần cuối
        created_at          DATETIME NOT NULL DEFAULT GETDATE(),

        CONSTRAINT PK_PlatformCookies PRIMARY KEY (platform_cookie_id),
        CONSTRAINT UQ_PlatformCookies_Platform UNIQUE (platform),
        CONSTRAINT CK_PlatformCookies_Platform
            CHECK (platform IN ('facebook', 'tiktok')),
        CONSTRAINT CK_PlatformCookies_Status
            CHECK (status IN ('active', 'disabled', 'expired'))
    );

    CREATE INDEX IX_PlatformCookies_Status ON PLATFORM_COOKIES(status);
END
GO

-- 1. Đổi tên bảng (không mất data, chỉ rename)
-- EXEC sp_rename 'PlatformCookies', 'PLATFORM_COOKIES';--

-- 2. Đổi tên PK constraint cho đẹp (optional)
-- EXEC sp_rename 'PK_PlatformCookies', 'PK_PlatformCookies', 'OBJECT'; --

-- Seed 2 platform mặc định (idempotent)
IF NOT EXISTS (SELECT 1 FROM PLATFORM_COOKIES WHERE platform = 'facebook')
BEGIN
    INSERT INTO PLATFORM_COOKIES (platform, file_path, status, note)
    VALUES (N'facebook', N'cookies/fb_cookie.json', N'active', N'Cookie Facebook — export Cookie Editor');
END

IF NOT EXISTS (SELECT 1 FROM PLATFORM_COOKIES WHERE platform = 'tiktok')
BEGIN
    INSERT INTO PLATFORM_COOKIES (platform, file_path, status, note)
    VALUES (N'tiktok', N'cookies/tiktok_cookie.json', N'active', N'Cookie TikTok — export Cookie Editor');
END
GO

-- Chạy script này trên MCFH_DB trước khi dùng thanh toán PayOS cho đơn cào dữ liệu.
-- Thêm các cột gateway PayOS vào bảng PAYMENTS (order_code, payment_link_id, checkout_url, paid_at).
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('PAYMENTS') AND name = 'order_code')
BEGIN
    ALTER TABLE PAYMENTS ADD order_code BIGINT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('PAYMENTS') AND name = 'payment_link_id')
BEGIN
    ALTER TABLE PAYMENTS ADD payment_link_id VARCHAR(100) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('PAYMENTS') AND name = 'checkout_url')
BEGIN
    ALTER TABLE PAYMENTS ADD checkout_url VARCHAR(500) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('PAYMENTS') AND name = 'paid_at')
BEGIN
    ALTER TABLE PAYMENTS ADD paid_at DATETIME NULL;
END
GO

-- Webhook tra cứu payment theo order_code — unique (filtered) để chống trùng.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('PAYMENTS') AND name = 'UQ_Payments_OrderCode')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Payments_OrderCode
        ON PAYMENTS(order_code)
        WHERE order_code IS NOT NULL;
END
GO

-- ====================================================================
-- Migration: Thêm quota mentions cho Project + bảng PROJECT_MENTION_PACKAGES
-- Chạy sau khi đã có bảng PROJECTS và PAYMENTS.
--
-- Thay đổi:
--   1. PROJECTS: thêm các cột quota (mentions_total/used/expires_at/full_unlimited)
--   2. PROJECT_MENTION_PACKAGES: bảng mới — mỗi lần mua gói tạo 1 row
--   3. SCRAPE_ORDERS: thêm cột MentionsPackage + MentionsIncluded (nullable, không breaking)
-- ====================================================================

-- 1) Mở rộng PROJECTS
IF COL_LENGTH('PROJECTS', 'mentions_quota_total') IS NULL
BEGIN
    ALTER TABLE PROJECTS ADD
        mentions_quota_total   INT NOT NULL DEFAULT 0,   -- tổng mentions được mua (tính Full = -1 nếu muốn)
        mentions_quota_used    INT NOT NULL DEFAULT 0,   -- đã dùng
        mentions_expires_at    DATETIME NULL,            -- hết hạn quota (NULL = vĩnh viễn)
        mentions_full_unlimited BIT  NOT NULL DEFAULT 0;  -- 1 = mua gói Full, không giới hạn
END
GO

-- 2) Bảng PROJECT_MENTION_PACKAGES — mỗi lần user mua gói thì 1 row
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PROJECT_MENTION_PACKAGES')
BEGIN
    CREATE TABLE PROJECT_MENTION_PACKAGES (
        package_id         INT IDENTITY(1,1) PRIMARY KEY,
        project_id         INT NOT NULL,
        payment_id         INT NOT NULL,
        package_type       NVARCHAR(20) NOT NULL,   -- 'PACK_100' | 'PACK_300' | 'PACK_600' | 'FULL_UNLIMITED'
        mentions_included  INT NOT NULL,            -- 100/300/600, -1 nếu FULL_UNLIMITED
        mentions_used      INT NOT NULL DEFAULT 0,
        expires_at         DATETIME NULL,           -- NULL = vĩnh viễn
        status             NVARCHAR(20) NOT NULL DEFAULT 'active', -- 'active' | 'exhausted' | 'expired' | 'cancelled'
        created_at         DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_PkgPkg_Project FOREIGN KEY (project_id) REFERENCES PROJECTS(project_id),
        CONSTRAINT FK_PkgPkg_Payment FOREIGN KEY (payment_id) REFERENCES PAYMENTS(payment_id)
    );
    CREATE INDEX IX_PkgPackages_Project ON PROJECT_MENTION_PACKAGES(project_id, status);
    CREATE INDEX IX_PkgPackages_Payment ON PROJECT_MENTION_PACKAGES(payment_id);
END
GO

-- 3) Mở rộng SCRAPE_ORDERS (nullable, không breaking với order cũ)
IF COL_LENGTH('SCRAPE_ORDERS', 'mentions_package') IS NULL
BEGIN
    ALTER TABLE SCRAPE_ORDERS ADD
        mentions_package    VARCHAR(50) NULL,    -- 'PACK_100' | 'PACK_300' | 'PACK_600' | 'FULL_UNLIMITED'
        mentions_included   INT NULL;              -- snapshot tại thời điểm mua
END
GO

-- Chạy script này trên MCFH_DB để tạo bảng cấu hình các gói scrape (admin CRUD).
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SCRAPE_PACKAGES')
BEGIN
    CREATE TABLE SCRAPE_PACKAGES (
        package_id    INT IDENTITY(1,1) PRIMARY KEY,
        code          VARCHAR(50) NOT NULL,
        name          NVARCHAR(255) NOT NULL,
        description   NVARCHAR(MAX) NULL,
        price         DECIMAL(18,2) NOT NULL,
        currency      VARCHAR(10) NOT NULL DEFAULT 'VND',
        duration_days INT NOT NULL,
        max_items     INT NOT NULL,
        max_sources   INT NULL,
        is_active     BIT NOT NULL DEFAULT 1,
        sort_order    INT NOT NULL DEFAULT 0,
        created_at    DATETIME NOT NULL DEFAULT GETDATE(),
        updated_at    DATETIME NOT NULL DEFAULT GETDATE(),
        updated_by    INT NULL,
        CONSTRAINT UQ_ScrapePackages_Code UNIQUE (code),
        CONSTRAINT FK_ScrapePackages_UpdatedBy FOREIGN KEY (updated_by) REFERENCES USERS(user_id)
    );
    CREATE INDEX IX_ScrapePackages_Active ON SCRAPE_PACKAGES(is_active, sort_order);
END
GO
-- Thêm cột MentionsPackage vào SCRAPE_ORDERS nếu thiếu (cho order cũ nullable).
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('SCRAPE_ORDERS') AND name = 'mentions_package')
BEGIN
    ALTER TABLE SCRAPE_ORDERS ADD mentions_package VARCHAR(50) NULL;
END
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('SCRAPE_ORDERS') AND name = 'mentions_included')
BEGIN
    ALTER TABLE SCRAPE_ORDERS ADD mentions_included INT NULL;
END
GO
-- FK từ SCRAPE_ORDERS.mentions_package -> SCRAPE_PACKAGES.code (optional, không ràng buộc cứng).
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_ScrapeOrders_Package')
BEGIN
    ALTER TABLE SCRAPE_ORDERS
        ADD CONSTRAINT FK_ScrapeOrders_Package
        FOREIGN KEY (mentions_package) REFERENCES SCRAPE_PACKAGES(code);
END
GO
-- Seed gói mặc định (chạy 1 lần, có thể tắt sau khi admin CRUD xong).
IF NOT EXISTS (SELECT 1 FROM SCRAPE_PACKAGES WHERE code = 'PACK_100')
BEGIN
    INSERT INTO SCRAPE_PACKAGES (code, name, description, price, currency, duration_days, max_items, max_sources, is_active, sort_order)
    VALUES
        ('PACK_100',  N'Gói Cơ bản',    N'Phù hợp thử nghiệm',         5000.00,    'VND', 3, 100,   1, 1, 1),
        ('PACK_300',  N'Gói Tiêu chuẩn', N'Cho dự án vừa và nhỏ',     10000.00,   'VND', 7, 300,   3, 1, 2),
        ('PACK_600',  N'Gói Nâng cao',   N'Cho dự án cần nhiều dữ liệu', 20000.00, 'VND', 14, 600,  5, 1, 3),
        ('FULL_UNLIMITED', N'Gói Toàn diện', N'Không giới hạn mentions', 30000.00, 'VND', 30, 9999, 10, 1, 4);
END
GO

UPDATE SCRAPE_PACKAGES SET max_sources = 99 WHERE code = 'FULL_UNLIMITED';

IF NOT EXISTS (SELECT 1 FROM USERS WHERE email = N'admin@gmail.com')
BEGIN
    INSERT INTO USERS (
        email,
        password_hash,
        full_name,
        auth_provider,
        system_role,
        is_verified,
        verified_at,
        is_banned,
        created_at
    )
    VALUES (
        N'admin@gmail.com',
        N'$2a$11$EhXIn/jDaJnWw.OMzzsWLu.nzZ2E/I8ZpPI/UewoSYZk0pR7AXoKa',
        N'System Admin',
        N'local',
        N'Admin',
        1,
        GETDATE(),
        0,
        GETDATE()
    );

    PRINT N'Đã tạo admin@gmail.com (mật khẩu: 123)';
END
ELSE
BEGIN
    PRINT N'admin@gmail.com đã tồn tại — bỏ qua INSERT.';
END

SELECT user_id, email, full_name, system_role, is_verified
FROM USERS
WHERE email = N'admin@gmail.com';

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BREVO_KEYS')
BEGIN
    CREATE TABLE BREVO_KEYS (
        brevo_key_id        INT IDENTITY(1,1) NOT NULL,
        key_type            VARCHAR(20)  NOT NULL DEFAULT 'api',
        api_key_encrypted   NVARCHAR(MAX) NOT NULL,                    -- encrypted by EncryptionService (AES-256-CBC)
        smtp_login          VARCHAR(255) NULL,                          -- chỉ cần khi key_type='smtp'
        from_address        VARCHAR(255) NULL,                          -- sender đã verify trên Brevo
        from_name           NVARCHAR(100) NULL,
        status              VARCHAR(50)  NOT NULL DEFAULT 'active',     -- active | disabled
        is_default          BIT          NOT NULL DEFAULT 0,
        note                NVARCHAR(1000) NULL,
        last_used_at        DATETIME NULL,
        created_at          DATETIME NOT NULL DEFAULT GETDATE(),
        updated_at          DATETIME NULL,
        updated_by          INT NULL,

        CONSTRAINT PK_BrevoKeys PRIMARY KEY (brevo_key_id),
        CONSTRAINT CK_BrevoKeys_KeyType
            CHECK (key_type IN ('api', 'smtp')),
        CONSTRAINT CK_BrevoKeys_Status
            CHECK (status IN ('active', 'disabled')),
        CONSTRAINT FK_BrevoKeys_UpdatedBy
            FOREIGN KEY (updated_by) REFERENCES USERS(user_id)
            ON DELETE SET NULL
    );

    CREATE INDEX IX_BrevoKeys_Default_Status ON BREVO_KEYS(is_default, status);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PAYOS_KEYS')
BEGIN
    CREATE TABLE PAYOS_KEYS (
        payos_key_id            INT IDENTITY(1,1) NOT NULL,
        client_id               VARCHAR(64) NOT NULL,                  -- PayOS ClientId (UUID không có dấu gạch)
        api_key_encrypted       NVARCHAR(MAX) NOT NULL,                -- encrypted
        checksum_key_encrypted  NVARCHAR(MAX) NOT NULL,                -- encrypted
        environment             VARCHAR(20) NOT NULL DEFAULT 'live',   -- sandbox | live
        status                  VARCHAR(50) NOT NULL DEFAULT 'active', -- active | disabled
        is_default              BIT         NOT NULL DEFAULT 0,
        note                    NVARCHAR(1000) NULL,
        last_used_at            DATETIME NULL,
        created_at              DATETIME NOT NULL DEFAULT GETDATE(),
        updated_at              DATETIME NULL,
        updated_by              INT NULL,

        CONSTRAINT PK_PayOsKeys PRIMARY KEY (payos_key_id),
        CONSTRAINT CK_PayOsKeys_Environment
            CHECK (environment IN ('sandbox', 'live')),
        CONSTRAINT CK_PayOsKeys_Status
            CHECK (status IN ('active', 'disabled')),
        CONSTRAINT FK_PayOsKeys_UpdatedBy
            FOREIGN KEY (updated_by) REFERENCES USERS(user_id)
            ON DELETE SET NULL
    );

    CREATE INDEX IX_PayOsKeys_Default_Status ON PAYOS_KEYS(is_default, status);
END
GO
