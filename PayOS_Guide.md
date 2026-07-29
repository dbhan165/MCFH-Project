# Hướng dẫn chạy thanh toán PayOS (local)

Tài liệu dành cho thành viên team muốn chạy và test luồng thanh toán đơn cào dữ liệu trên máy local.

Nhánh code: `develop` (đã có PayOS + fix DI Azure Blob).

---

## 1. Lấy code mới

```powershell
git checkout develop
git pull origin develop
```

---

## 2. Chạy migration PayOS trên SQL Server

Mở SQL Server Management Studio (hoặc `sqlcmd`), chọn database `MCFH_DB`, chạy script:

```text
MCFH/Scripts/AlterPaymentsAddPayOsColumns.sql
```

Chỉ cần chạy **một lần** trên mỗi máy / mỗi database.

Script thêm các cột: `order_code`, `payment_link_id`, `checkout_url`, `paid_at` và index unique trên `order_code`.

---

## 3. Điền secret local (không commit)

Sửa file:

```text
MCFH/appsettings.Development.json
```

File này đang được track trong git — **chỉ giữ key trên máy local**, không push key thật lên repo.

Ví dụ cấu hình tối thiểu cho payment:

```json
{
  "ConnectionStrings": {
    "MyCnn": "Server=localhost;Database=MCFH_DB;Trusted_Connection=True;Encrypt=True;TrustServerCertificate=True"
  },
  "PayOS": {
    "ClientId": "<lấy từ my.payos.vn>",
    "ApiKey": "<lấy từ my.payos.vn>",
    "ChecksumKey": "<lấy từ my.payos.vn>",
    "ReturnUrl": "http://localhost:5173/payment/return",
    "CancelUrl": "http://localhost:5173/payment/return"
  },
  "AzureBlob": {
    "ConnectionString": "<connection string Dev của team, hoặc để trống để fallback file local>",
    "ContainerName": "comments"
  }
}
```

### Lấy key PayOS

1. Đăng nhập [my.payos.vn](https://my.payos.vn)
2. Chọn kênh thanh toán **MCFH**
3. Tab **Thông tin tích hợp** → copy `Client ID`, `Api Key`, `Checksum Key`

### Return / Cancel URL

Phải trỏ đúng trang frontend:

```text
http://localhost:5173/payment/return
```

Không dùng `/payment/success` hay `/payment/cancel` (route đó không tồn tại).

---

## 4. Cài Playwright Chromium (để cào sau khi trả tiền)

Thanh toán xong hệ thống mới bắt đầu scrape. Thiếu Chromium sẽ báo lỗi Playwright.

```powershell
cd MCFH
dotnet build
$env:PLAYWRIGHT_BROWSERS_PATH = "$PWD\.playwright"
powershell -ExecutionPolicy Bypass -File .\bin\Debug\net8.0\playwright.ps1 install chromium
```

Browser sẽ nằm trong thư mục `MCFH/.playwright` (không commit thư mục này).

---

## 5. Chạy backend + frontend

**Terminal 1 — Backend**

```powershell
cd MCFH
dotnet run --launch-profile http
```

→ http://localhost:5254

**Terminal 2 — Frontend**

```powershell
cd MCFH-Frontend
npm install
npm run dev
```

→ http://localhost:5173

---

## 6. Đăng ký Webhook PayOS (bắt buộc)

Webhook là nguồn tin cậy để kích hoạt đơn sau khi người dùng thanh toán.

### 6.1. Cài ngrok

1. Tải ngrok bản mới (≥ 3.20): https://ngrok.com/download  
2. Đăng ký tài khoản miễn phí  
3. Lấy authtoken tại dashboard ngrok  
4. Cấu hình:

```powershell
ngrok config add-authtoken <token-của-bạn>
```

### 6.2. Mở tunnel tới backend

Backend phải đang chạy trước:

```powershell
ngrok http 5254
```

Copy URL dạng:

```text
https://xxxx.ngrok-free.app
```

### 6.3. Dán Webhook URL trên PayOS

1. Vào [my.payos.vn](https://my.payos.vn) → kênh **MCFH**
2. **Chỉnh sửa thông tin** → ô **Webhook Url**
3. Dán:

```text
https://xxxx.ngrok-free.app/api/payments/payos/webhook
```

4. Bấm **Lưu**

Lúc lưu, PayOS sẽ gửi webhook test (`orderCode` 123). Backend trả 200 và bỏ qua nếu không khớp payment — đó là hành vi đúng.

> **Lưu ý:** Mỗi lần tắt/mở lại ngrok free, domain có thể đổi → phải cập nhật lại Webhook Url trên PayOS.

---

## 7. Test nhanh luồng thanh toán

1. Mở http://localhost:5173 → đăng nhập  
2. Tạo dự án mới → nhập keyword → chọn gói thời gian  
3. Bấm thanh toán → redirect sang trang PayOS (QR VietQR)  
4. Quét QR và chuyển **đúng số tiền** trên đơn  
5. PayOS đưa về `/payment/return` → trang xác nhận → đơn chuyển trạng thái `scraping`

### Giá test hiện tại (tạm thời)

| Gói | Giá test |
|-----|----------|
| 1 tuần / 1 tháng | 10.000 ₫ |
| 3 tháng | 20.000 ₫ |
| 6 tháng / 1 năm / Mọi thời gian | 50.000 ₫ |

Đây là giá **giảm để dễ test** — nhớ khôi phục giá thật trước khi lên production.

---

## Luồng kỹ thuật (tóm tắt)

```text
Tạo đơn (quoted)
  → POST /api/scrape-orders/{id}/pay  → tạo link PayOS (pending_payment)
  → User thanh toán trên pay.payos.vn
  → PayOS webhook (hoặc trang /payment/return poll confirm)
  → Payment success + đơn paid → start scrape → scraping
```

- Webhook: `POST /api/payments/payos/webhook` (verify chữ ký HMAC)
- Confirm: `GET /api/scrape-orders/{id}/payment-status` (không tin query param)

---

## Checklist trước khi báo “chạy được”

- [ ] `git pull` `develop` mới nhất  
- [ ] Đã chạy `AlterPaymentsAddPayOsColumns.sql`  
- [ ] Đã điền PayOS keys + ReturnUrl/CancelUrl trong `appsettings.Development.json`  
- [ ] Đã cài Playwright Chromium  
- [ ] Backend `:5254` và frontend `:5173` đang chạy  
- [ ] ngrok đang chạy và Webhook Url trên PayOS khớp  
- [ ] Test tạo đơn → thanh toán → đơn vào `scraping`

---

## Lưu ý quan trọng cho team

| Việc | Ghi chú |
|------|---------|
| Không commit `appsettings.Development.json` / `appsettings.Production.json` khi còn key | Secret |
| Không commit thư mục `.playwright` | Cài local từng máy |
| Giá 10k / 20k / 50k là giá test tạm | Đổi lại trước production |
| Production webhook | Dùng domain backend thật, không dùng ngrok |
| Production secrets | Dùng biến môi trường (`PayOS__ClientId`, `PayOS__ApiKey`, `PayOS__ChecksumKey`, …) |

### Biến môi trường gợi ý (production / Docker)

```env
PayOS__ClientId=...
PayOS__ApiKey=...
PayOS__ChecksumKey=...
PayOS__ReturnUrl=https://<frontend-domain>/payment/return
PayOS__CancelUrl=https://<frontend-domain>/payment/return
```

Webhook production:

```text
https://<backend-domain>/api/payments/payos/webhook
```

---

## Troubleshooting nhanh

| Triệu chứng | Cách xử lý |
|-------------|------------|
| Build lỗi liên quan `ICommentBundleStorage` | `git pull develop` — đã có fix DI |
| PayOS tạo link lỗi / 500 | Kiểm tra 3 key PayOS trong `appsettings.Development.json`, restart backend |
| Thanh toán xong đơn không chuyển `scraping` | Kiểm tra ngrok còn chạy + Webhook Url đúng; xem log backend |
| Lỗi `Executable doesn't exist` / Playwright | Chạy lại bước cài Chromium (mục 4), restart backend |
| Sau thanh toán về 404 | Sai ReturnUrl — phải là `/payment/return` |
| `appsettings.Development.json` JSON invalid | Không bọc thêm `{ }` quanh `AzureBlob`; validate JSON trước khi chạy |
