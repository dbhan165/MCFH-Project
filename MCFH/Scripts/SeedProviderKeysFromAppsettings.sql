-- =========================================================================
-- SEED Brevo + PayOS keys từ appsettings vào DB.
--
-- ⚠️  QUAN TRỌNG:
-- - Cột api_key_encrypted / api_key_encrypted đang lưu dạng AES-256-CBC với
--   IV ngẫu nhiên, nên KHÔNG thể hardcode giá trị đã encrypt từ đây.
-- - Cách dùng:
--     (a) Vào Admin UI → Cài đặt hệ thống → Brevo Email Key / PayOS Key → Thêm key
--         (paste plain text, server tự encrypt).
--     (b) Hoặc dùng helper Program.cs SeedProviderKeysFromConfig:
--         dotnet run --project MCFH --seed-provider-keys
--
-- File SQL này chỉ là TEMPLATE để bạn copy-paste và chạy khi KHÔNG dùng UI
-- và KHÔNG chạy được helper — bạn sẽ tự dán plain text và cột api_key_encrypted
-- sẽ chứa PLAIN TEXT (KHÔNG AN TOÀN). Hãy đổi sang cơ chế encrypt sau.
--
-- Sau khi chạy SQL, services sẽ tự dùng row default trong DB,
-- không còn đọc appsettings.
-- =========================================================================

SET NOCOUNT ON;

-- =====================================================================
-- 1) BREVO_KEYS
-- =====================================================================
-- Xác định keyType:
--   api  → Brevo REST API (key bắt đầu bằng xkeysib-...)
--   smtp → SMTP login (key bắt đầu bằng xsmtpsib-...)

IF NOT EXISTS (SELECT 1 FROM BREVO_KEYS WHERE is_default = 1)
BEGIN
    PRINT 'Inserting default BrevoKey from appsettings...';

    INSERT INTO BREVO_KEYS (
        key_type, api_key_encrypted, smtp_login, from_address, from_name,
        status, is_default, note, created_at, updated_at
    )
    VALUES (
        N'api',                                                  -- key_type: 'api' | 'smtp'
        N'<<<DÁN API_KEY Brevo ở đây (xkeysib-...)>>>',         -- ← paste từ appsettings.json Smtp:ApiKey
        N'<<<SMTP login nếu keyType=smtp (Username)>>>',         -- ← paste từ appsettings.json Smtp:Username
        N'<<<no-reply@mcfh.io.vn>>>',                            -- ← paste từ appsettings.json Smtp:FromAddress
        N'MCFH System Hub',                                      -- ← paste từ appsettings.json Smtp:FromName
        N'active',
        1,                                                       -- is_default = 1 (EmailService ưu tiên row này)
        N'Seeded từ appsettings.json bằng SQL template. Cảnh báo: api_key_encrypted đang là PLAIN TEXT — cần rotate bằng UI sau.',
        GETDATE(),
        GETDATE()
    );
END
ELSE
    PRINT 'BREVO_KEYS đã có default — skip seed.';
GO


-- =====================================================================
-- 2) PAYOS_KEYS
-- =====================================================================

IF NOT EXISTS (SELECT 1 FROM PAYOS_KEYS WHERE is_default = 1)
BEGIN
    PRINT 'Inserting default PayOsKey from appsettings...';

    INSERT INTO PAYOS_KEYS (
        client_id, api_key_encrypted, checksum_key_encrypted,
        environment, status, is_default, note, created_at, updated_at
    )
    VALUES (
        N'<<<ClientId PayOS>>>',                                 -- ← paste từ appsettings.json PayOS:ClientId
        N'<<<ApiKey PayOS>>>',                                   -- ← paste từ appsettings.json PayOS:ApiKey
        N'<<<ChecksumKey PayOS>>>',                              -- ← paste từ appsettings.json PayOS:ChecksumKey
        N'live',                                                  -- 'live' hoặc 'sandbox' theo appsettings
        N'active',
        1,                                                        -- is_default = 1 (PayOsService ưu tiên row này)
        N'Seeded từ appsettings.json bằng SQL template. Cảnh báo: api_key_encrypted đang là PLAIN TEXT — cần rotate bằng UI sau.',
        GETDATE(),
        GETDATE()
    );
END
ELSE
    PRINT 'PAYOS_KEYS đã có default — skip seed.';
GO


-- =====================================================================
-- Verify seed
-- =====================================================================
SELECT 'BREVO_KEYS' AS tbl, brevo_key_id AS id, key_type, status, is_default, from_address
FROM BREVO_KEYS
UNION ALL
SELECT 'PAYOS_KEYS'  AS tbl, payos_key_id  AS id, environment AS key_type, status, is_default, client_id AS from_address
FROM PAYOS_KEYS
ORDER BY tbl, is_default DESC, id;
