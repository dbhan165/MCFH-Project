-- ========================================================
-- MCFH_DB - Mở rộng SCRAPE_PACKAGES + cột liên quan trên SCRAPE_ORDERS.
-- Pattern giống MCFH_DB_Full.sql: DROP giữa script chạy đầu, sau đó CREATE.
-- Chạy trong SSMS: F5 hoặc Execute.
-- Lưu ý: DROP giữa script chỉ chạy khi bạn rebuild DB từ đầu.
--       Nếu DB đã có data, hãy comment block DROP này lại.
-- ========================================================

USE MCFH_DB;
GO

-- ========================================================
-- DROP (theo thứ tự ngược dependency - chỉ chạy khi rebuild DB)
-- ========================================================
IF OBJECT_ID('FK_ScrapeOrders_Package', 'F') IS NOT NULL
    ALTER TABLE SCRAPE_ORDERS DROP CONSTRAINT FK_ScrapeOrders_Package;

IF OBJECT_ID('FK_ScrapePackages_UpdatedBy', 'F') IS NOT NULL
    ALTER TABLE SCRAPE_PACKAGES DROP CONSTRAINT FK_ScrapePackages_UpdatedBy;

IF OBJECT_ID('SCRAPE_PACKAGES', 'U') IS NOT NULL DROP TABLE SCRAPE_PACKAGES;
GO

-- ========================================================
-- CREATE TABLE SCRAPE_PACKAGES
-- ========================================================
CREATE TABLE SCRAPE_PACKAGES (
    package_id    INT IDENTITY(1,1) PRIMARY KEY,
    code          VARCHAR(50)  NOT NULL,
    name          NVARCHAR(255) NOT NULL,
    description   NVARCHAR(MAX) NULL,
    price         DECIMAL(18,2) NOT NULL,
    currency      VARCHAR(10)  NOT NULL DEFAULT 'VND',
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
GO

-- ========================================================
-- CREATE TABLE PROJECT_MENTION_PACKAGES (mỗi lần mua gói = 1 row)
-- ========================================================
IF OBJECT_ID('PROJECT_MENTION_PACKAGES', 'U') IS NULL
BEGIN
    CREATE TABLE PROJECT_MENTION_PACKAGES (
        package_id         INT IDENTITY(1,1) PRIMARY KEY,
        project_id         INT NOT NULL,
        payment_id         INT NOT NULL,
        package_type       VARCHAR(50) NOT NULL,   -- 'PACK_100' | 'PACK_300' | 'PACK_600' | 'FULL_UNLIMITED'
        mentions_included  INT NOT NULL,           -- 100/300/600, -1 nếu FULL_UNLIMITED
        mentions_used      INT NOT NULL DEFAULT 0,
        expires_at         DATETIME NULL,          -- NULL = vĩnh viễn
        status             VARCHAR(20) NOT NULL DEFAULT 'active', -- 'active' | 'exhausted' | 'expired' | 'cancelled'
        created_at         DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_PkgPkg_Project FOREIGN KEY (project_id) REFERENCES PROJECTS(project_id),
        CONSTRAINT FK_PkgPkg_Payment FOREIGN KEY (payment_id) REFERENCES PAYMENTS(payment_id)
    );
    CREATE INDEX IX_PkgPackages_Project ON PROJECT_MENTION_PACKAGES(project_id, status);
    CREATE INDEX IX_PkgPackages_Payment ON PROJECT_MENTION_PACKAGES(payment_id);
END
GO

-- ========================================================
-- Mở rộng SCRAPE_ORDERS: thêm 2 cột nullable
-- ========================================================
IF COL_LENGTH('SCRAPE_ORDERS', 'mentions_package') IS NULL
BEGIN
    ALTER TABLE SCRAPE_ORDERS ADD
        mentions_package    VARCHAR(50) NULL,    -- 'PACK_100' | 'PACK_300' | 'PACK_600' | 'FULL_UNLIMITED'
        mentions_included   INT NULL;             -- snapshot tại thời điểm mua
END
GO

-- ========================================================
-- FK SCRAPE_ORDERS.mentions_package -> SCRAPE_PACKAGES.code
-- ========================================================
IF OBJECT_ID('FK_ScrapeOrders_Package', 'F') IS NULL
BEGIN
    ALTER TABLE SCRAPE_ORDERS
        ADD CONSTRAINT FK_ScrapeOrders_Package
        FOREIGN KEY (mentions_package) REFERENCES SCRAPE_PACKAGES(code);
END
GO

-- ========================================================
-- SEED 4 gói mặc định
-- ========================================================
IF NOT EXISTS (SELECT 1 FROM SCRAPE_PACKAGES WHERE code = 'PACK_100')
BEGIN
    INSERT INTO SCRAPE_PACKAGES (code, name, description, price, currency, duration_days, max_items, max_sources, is_active, sort_order)
    VALUES
        ('PACK_100',       N'Gói Cơ bản',     N'Phù hợp thử nghiệm',                5000.00,  'VND', 3,  100,   1,  1, 1),
        ('PACK_300',       N'Gói Tiêu chuẩn', N'Cho dự án vừa và nhỏ',              10000.00, 'VND', 7,  300,   3,  1, 2),
        ('PACK_600',       N'Gói Nâng cao',   N'Cho dự án cần nhiều dữ liệu',       20000.00, 'VND', 14, 600,   5,  1, 3),
        ('FULL_UNLIMITED', N'Gói Toàn diện',  N'Không giới hạn mentions',            30000.00, 'VND', 30, 9999, 99, 1, 4);
END
GO

-- ========================================================
-- Verify
-- ========================================================
SELECT 'SCRAPE_PACKAGES' AS [table], CAST(COUNT(*) AS varchar(10)) AS [rows] FROM SCRAPE_PACKAGES
UNION ALL
SELECT 'PROJECT_MENTION_PACKAGES', CAST(COUNT(*) AS varchar(10)) FROM PROJECT_MENTION_PACKAGES;
SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'SCRAPE_ORDERS' AND COLUMN_NAME IN ('mentions_package', 'mentions_included');
GO

PRINT '=== Script finished successfully. ===';