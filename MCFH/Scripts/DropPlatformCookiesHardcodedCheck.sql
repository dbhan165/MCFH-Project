-- Xoá CHECK constraint cứng để cho phép thêm platform tuỳ ý
IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PlatformCookies_Platform' AND parent_object_id = OBJECT_ID('PLATFORM_COOKIES'))
BEGIN
    ALTER TABLE PLATFORM_COOKIES DROP CONSTRAINT CK_PlatformCookies_Platform;
    PRINT 'Da xoa CK_PlatformCookies_Platform';
END
ELSE
BEGIN
    PRINT 'CK_PlatformCookies_Platform khong ton tai hoac da bi xoa';
END
GO

-- Kiem tra lai
SELECT name AS ConstraintName, definition AS Definition
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID('PLATFORM_COOKIES');
GO
