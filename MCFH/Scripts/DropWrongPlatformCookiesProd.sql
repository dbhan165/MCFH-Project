-- =============================================================
-- MỤC ĐÍCH: drop bảng sai tên "PlatformCookies" nếu tồn tại
-- AN TOÀN: chỉ drop khi bảng ĐÚNG "PLATFORM_COOKIES" đã có data.
--           Nếu cả 2 bảng cùng trống, sẽ cảnh báo trước khi drop.
-- =============================================================

SET NOCOUNT ON;

DECLARE @CorrectExists BIT = 0;
DECLARE @WrongExists BIT = 0;
DECLARE @CorrectCount INT = 0;
DECLARE @WrongCount INT = 0;

IF OBJECT_ID('dbo.PLATFORM_COOKIES', 'U') IS NOT NULL
    SET @CorrectExists = 1;
IF OBJECT_ID('dbo.PlatformCookies', 'U') IS NOT NULL
    SET @WrongExists = 1;

PRINT '=== Summary ===';
PRINT 'PLATFORM_COOKIES (correct): ' + CASE WHEN @CorrectExists = 1 THEN 'EXISTS' ELSE 'NOT EXISTS' END;
PRINT 'PlatformCookies  (wrong):   ' + CASE WHEN @WrongExists = 1 THEN 'EXISTS' ELSE 'NOT EXISTS' END;

IF @WrongExists = 1
    SELECT @WrongCount = COUNT(*) FROM [PlatformCookies];
IF @CorrectExists = 1
    SELECT @CorrectCount = COUNT(*) FROM PLATFORM_COOKIES;

PRINT 'PLATFORM_COOKIES row count: ' + CAST(@CorrectCount AS VARCHAR(10));
PRINT 'PlatformCookies  row count: ' + CAST(@WrongCount  AS VARCHAR(10));

IF @WrongExists = 0
BEGIN
    PRINT 'Nothing to drop. Exiting.';
    RETURN;
END

-- Nếu bảng đúng chưa có → cảnh báo
IF @CorrectExists = 0
BEGIN
    PRINT 'ERROR: PLATFORM_COOKIES does NOT exist.';
    PRINT 'Cannot drop the wrong table without the correct one in place.';
    PRINT 'Run MCFH/Scripts/CreatePlatformCookiesTable.sql FIRST, then rerun this script.';
    RETURN;
END

-- Nếu bảng sai có data nhưng bảng đúng rỗng → copy data sang trước khi drop
IF @WrongCount > 0 AND @CorrectCount = 0
BEGIN
    PRINT '=== Migrating data from PlatformCookies to PLATFORM_COOKIES ===';
    INSERT INTO PLATFORM_COOKIES (
        platform_cookie_id,
        platform, file_path, status, note,
        cookie_count, expires_at, uploaded_at, last_used_at, created_at
    )
    SELECT
        platform_cookie_id,
        platform, file_path, status, note,
        cookie_count, expires_at, uploaded_at, last_used_at, created_at
    FROM [PlatformCookies];

    PRINT 'Migrated ' + CAST(@@ROWCOUNT AS VARCHAR(10)) + ' rows.';
END

-- Nếu bảng sai có data nhưng bảng đúng CŨNG có data → cảnh báo, KHÔNG drop
IF @WrongCount > 0 AND @CorrectCount > 0
BEGIN
    PRINT 'WARNING: Both tables have data.';
    PRINT 'Please manually migrate any unique rows before dropping.';
    PRINT 'NOT dropping anything. Review and rerun after manual migration.';
    RETURN;
END

-- Nếu cả 2 rỗng → drop an toàn
-- Nếu bảng sai rỗng, bảng đúng có data → drop bảng sai
PRINT '=== Dropping wrong table [PlatformCookies] ===';
DROP TABLE [PlatformCookies];
PRINT 'Dropped [PlatformCookies] successfully.';

-- Kiểm tra lại
SELECT name FROM sys.tables WHERE name LIKE '%Platform%' OR name LIKE '%PLATFORM%';