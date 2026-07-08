-- Metadata registry cho cookie scraper (nội dung cookie nằm trong file JSON trên server).
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PLATFORM_COOKIES')
BEGIN
    CREATE TABLE PLATFORM_COOKIES (
        platform_cookie_id  INT IDENTITY(1,1) NOT NULL,
        platform            VARCHAR(50)  NOT NULL,
        file_path           NVARCHAR(500) NOT NULL,
        status              VARCHAR(50)  NOT NULL DEFAULT 'active',
        note                NVARCHAR(1000) NULL,
        cookie_count        INT NOT NULL DEFAULT 0,
        expires_at          DATETIME NULL,
        uploaded_at         DATETIME NULL,
        last_used_at        DATETIME NULL,
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
