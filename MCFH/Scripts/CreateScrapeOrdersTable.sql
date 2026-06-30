-- Chạy script này trên MCFH_DB trước khi dùng luồng báo giá / thanh toán cào dữ liệu.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'SCRAPE_ORDERS')
BEGIN
    CREATE TABLE SCRAPE_ORDERS (
        order_id            INT IDENTITY(1,1) PRIMARY KEY,
        workspace_id        INT NOT NULL,
        project_id          INT NOT NULL,
        user_id             INT NOT NULL,
        keyword             NVARCHAR(500) NOT NULL,
        posted_since_days   INT NOT NULL DEFAULT 30,
        quoted_price        DECIMAL(18,2) NOT NULL,
        status              VARCHAR(50) NOT NULL DEFAULT 'quoted',
        payment_id          INT NULL,
        scrape_job_id       VARCHAR(100) NULL,
        progress_percent    INT NOT NULL DEFAULT 0,
        status_message      NVARCHAR(500) NULL,
        estimated_report_at DATETIME NULL,
        report_ready_at     DATETIME NULL,
        created_at          DATETIME NOT NULL DEFAULT GETDATE(),
        paid_at             DATETIME NULL,
        completed_at        DATETIME NULL,
        CONSTRAINT FK_ScrapeOrder_Workspace FOREIGN KEY (workspace_id) REFERENCES WORKSPACES(workspace_id),
        CONSTRAINT FK_ScrapeOrder_Project FOREIGN KEY (project_id) REFERENCES PROJECTS(project_id),
        CONSTRAINT FK_ScrapeOrder_User FOREIGN KEY (user_id) REFERENCES USERS(user_id),
        CONSTRAINT FK_ScrapeOrder_Payment FOREIGN KEY (payment_id) REFERENCES PAYMENTS(payment_id)
    );
    CREATE INDEX IX_ScrapeOrders_User ON SCRAPE_ORDERS(user_id, created_at DESC);
    CREATE INDEX IX_ScrapeOrders_Project ON SCRAPE_ORDERS(project_id);
END
GO
