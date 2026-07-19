-- Chạy script này trên MCFH_DB trước khi dùng thanh toán PayOS cho đơn cào dữ liệu.
-- Thêm các cột gateway PayOS vào bảng PAYMENTS (order_code, payment_link_id, checkout_url, paid_at).
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('PAYMENTS') AND name = 'order_code')
BEGIN
    ALTER TABLE PAYMENTS ADD order_code BIGINT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('PAYMENTS') AND name = 'payment_link_id')
BEGIN
    ALTER TABLE PAYMENTS ADD payment_link_id VARCHAR(100) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('PAYMENTS') AND name = 'checkout_url')
BEGIN
    ALTER TABLE PAYMENTS ADD checkout_url VARCHAR(500) NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('PAYMENTS') AND name = 'paid_at')
BEGIN
    ALTER TABLE PAYMENTS ADD paid_at DATETIME NULL;
END
GO

-- Webhook tra cứu payment theo order_code — unique (filtered) để chống trùng.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('PAYMENTS') AND name = 'UQ_Payments_OrderCode')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_Payments_OrderCode
        ON PAYMENTS(order_code)
        WHERE order_code IS NOT NULL;
END
GO
