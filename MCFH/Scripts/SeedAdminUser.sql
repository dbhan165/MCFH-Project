-- Tạo tài khoản Admin hệ thống (dev/demo).
-- Email: admin@gmail.com
-- Mật khẩu: 123 (BCrypt hash bên dưới, giống AuthController)
--
-- Chạy trong SSMS trên database MCFH_DB.

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
