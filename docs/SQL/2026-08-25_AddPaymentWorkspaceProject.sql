-- Migration: Thêm workspace_id + project_id vào PAYMENTS
-- Chạy trên database production nếu chưa áp dụng migration tự động.
-- Idempotent: an toàn chạy lại nhiều lần.

IF COL_LENGTH('PAYMENTS', 'workspace_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[PAYMENTS]
        ADD [workspace_id] INT NULL;
END
GO

IF COL_LENGTH('PAYMENTS', 'project_id') IS NULL
BEGIN
    ALTER TABLE [dbo].[PAYMENTS]
        ADD [project_id] INT NULL;
END
GO

-- Indexes
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_PAYMENTS_workspace_id' AND object_id = OBJECT_ID('dbo.PAYMENTS')
)
BEGIN
    CREATE INDEX [IX_PAYMENTS_workspace_id] ON [dbo].[PAYMENTS] ([workspace_id]);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = 'IX_PAYMENTS_project_id' AND object_id = OBJECT_ID('dbo.PAYMENTS')
)
BEGIN
    CREATE INDEX [IX_PAYMENTS_project_id] ON [dbo].[PAYMENTS] ([project_id]);
END
GO

-- Foreign keys (Restrict vì 2 phía đều nullable, muốn tránh cascade)
IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_Payment_Workspace' AND parent_object_id = OBJECT_ID('dbo.PAYMENTS')
)
BEGIN
    ALTER TABLE [dbo].[PAYMENTS]
        ADD CONSTRAINT [FK_Payment_Workspace]
        FOREIGN KEY ([workspace_id]) REFERENCES [dbo].[WORKSPACES] ([workspace_id])
        ON DELETE NO ACTION;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys
    WHERE name = 'FK_Payment_Project' AND parent_object_id = OBJECT_ID('dbo.PAYMENTS')
)
BEGIN
    ALTER TABLE [dbo].[PAYMENTS]
        ADD CONSTRAINT [FK_Payment_Project]
        FOREIGN KEY ([project_id]) REFERENCES [dbo].[PROJECTS] ([project_id])
        ON DELETE NO ACTION;
END
GO