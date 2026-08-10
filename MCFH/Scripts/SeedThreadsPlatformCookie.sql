-- Add Threads platform cookie support
-- Run this script to enable Threads in the Platform Cookie admin panel

-- 1. Update CHECK constraint to include 'threads' platform
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PLATFORM_COOKIES')
BEGIN
    -- Drop old constraint
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PlatformCookies_Platform')
    BEGIN
        ALTER TABLE PLATFORM_COOKIES DROP CONSTRAINT CK_PlatformCookies_Platform;
    END

    -- Add new constraint with threads support
    ALTER TABLE PLATFORM_COOKIES
    ADD CONSTRAINT CK_PlatformCookies_Platform
        CHECK (platform IN ('facebook', 'tiktok', 'threads'));
END
GO

-- 2. Insert Threads platform record if not exists
IF NOT EXISTS (SELECT 1 FROM PLATFORM_COOKIES WHERE platform = 'threads')
BEGIN
    INSERT INTO PLATFORM_COOKIES (platform, file_path, status, note)
    VALUES (N'threads', N'cookies/threads_cookie.json', N'disabled', N'Cookie Threads — export Cookie Editor (chưa cung cấp)');
END
GO

-- 3. Verify
SELECT platform_cookie_id, platform, file_path, status, note, cookie_count FROM PLATFORM_COOKIES ORDER BY platform;
