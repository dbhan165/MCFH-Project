-- Tạo bảng BREVO_KEYS — multi-secret vault cho Brevo API key và SMTP login.
-- EmailService chỉ đọc 1 row active + IsDefault=1; admin có thể rotate key mà
-- không cần deploy lại (cập nhật row mới → set IsDefault=1 → row cũ IsDefault=0).
-- KeyType phân biệt "api" (Brevo REST API, keyPrefix xkeysib-) và "smtp" (smtp-relay.brevo.com login).

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
