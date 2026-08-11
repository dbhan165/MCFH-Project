-- Tạo bảng PAYOS_KEYS — multi-secret vault cho PayOS credentials (ClientId + ApiKey + ChecksumKey).
-- PayOsService đọc row IsDefault=1 active để khởi tạo PayOSClient.
-- Key được encrypted bằng EncryptionService (AES-256-CBC). Admin rotate key bằng cách
-- thêm row mới + set IsDefault=1 → row cũ được clear IsDefault.

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
