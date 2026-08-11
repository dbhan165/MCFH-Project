-- =============================================================
-- Mục đích: kiểm tra prod DB có bảng sai tên không
-- Lỗi gốc: EF Core trên prod đang trỏ tên "PlatformCookies"
--           (PascalCase, có chữ 's') thay vì "PLATFORM_COOKIES".
-- An toàn: chỉ in thông tin, KHÔNG drop gì cả.
-- =============================================================

-- 1) Liệt kê tất cả bảng liên quan (đúng + sai)
SELECT
    name              AS TableName,
    schema_id         AS SchemaId,
    SCHEMA_NAME(schema_id) AS SchemaName,
    create_date       AS CreatedAt,
    modify_date       AS ModifiedAt,
    CASE
        WHEN name = 'PLATFORM_COOKIES' THEN 'CORRECT'
        WHEN name = 'PlatformCookies' THEN 'WRONG (PascalCase, will cause EF error)'
        ELSE 'OTHER'
    END               AS Status
FROM sys.tables
WHERE name LIKE '%PlatformCookie%'
   OR name LIKE '%PLATFORM_COOKIE%'
ORDER BY name;

-- 2) So sánh cấu trúc 2 bảng (nếu có)
IF OBJECT_ID('dbo.PLATFORM_COOKIES', 'U') IS NOT NULL
BEGIN
    PRINT '--- Columns of PLATFORM_COOKIES (CORRECT) ---';
    SELECT column_name, data_type, character_maximum_length, is_nullable
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'PLATFORM_COOKIES'
    ORDER BY ordinal_position;
END
ELSE
    PRINT 'Table PLATFORM_COOKIES does NOT exist!';

IF OBJECT_ID('dbo.PlatformCookies', 'U') IS NOT NULL
BEGIN
    PRINT '--- Columns of PlatformCookies (WRONG) ---';
    SELECT column_name, data_type, character_maximum_length, is_nullable
    FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'PlatformCookies'
    ORDER BY ordinal_position;
END
ELSE
    PRINT 'Table PlatformCookies does NOT exist.';

-- 3) Đếm row trong mỗi bảng
SELECT 'PLATFORM_COOKIES' AS tbl, COUNT(*) AS row_count FROM PLATFORM_COOKIES
UNION ALL
SELECT 'PlatformCookies'  AS tbl, COUNT(*) AS row_count FROM [PlatformCookies];

-- 4) Kiểm tra __EFMigrationsHistory có migration cho PLATFORM_COOKIES chưa
SELECT
    MigrationId,
    ProductVersion
FROM __EFMigrationsHistory
WHERE MigrationId LIKE '%Platform%'
   OR MigrationId LIKE '%InitialCreate%'
ORDER BY MigrationId;