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
        mentions_package    NVARCHAR(20) NULL,    -- 'PACK_100' | 'PACK_300' | 'PACK_600' | 'FULL_UNLIMITED'
        mentions_included   INT NULL;              -- snapshot tại thời điểm mua
END
GO
