# MCFH — Q&A Log

Log các câu hỏi đã trả lời xuyên suốt phiên làm việc. Mỗi entry có:
- Câu hỏi gốc
- Câu trả lời đầy đủ
- File references với line ranges
- Follow-up gợi ý (đã chuyển vào `NEXT-STEPS.md`)

> Tham chiếu nhanh:
> - Câu 2 — PayOS webhook flow
> - Câu 3 — Phân quyền (SystemRole + WorkspaceRole + JWT)
> - Câu 5 — Deploy production cho người mới

---

## Câu 2 — PayOS webhook flow như thế nào?

### Tổng quan
PayOS webhook là **nguồn tin cậy duy nhất** về trạng thái thanh toán: sau khi verify chữ ký HMAC, controller đối soát `orderCode` → `Payment` → `ScrapeOrder`/`BespokeRequest`, idempotently fulfill đơn và khởi động job cào dữ liệu.

### 1. Cấu hình `PayOsOptions`
`MCFH/Configuration/PayOsOptions.cs:3-27` — bind section `"PayOS"` của `appsettings.json`:

```csharp
public class PayOsOptions
{
    public const string SectionName = "PayOS";
    public string ClientId { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string ChecksumKey { get; set; } = "";
    public string ReturnUrl { get; set; } = "";
    public string CancelUrl { get; set; } = "";
    public bool Bypass { get; set; }   // dev only
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(ChecksumKey);
}
```

Credential ưu tiên lấy từ DB qua `IProviderCredentialResolver` (cache 30s); fallback `appsettings` nếu DB không có row default — `MCFH/Services/Payments/PayOsService.cs:41-86`. Khi credential thay đổi, `PayOSClient` được rebuild qua `SemaphoreSlim` + `fingerprint`.

### 2. Webhook endpoint
`MCFH/Controllers/Payments/PayOsWebhookController.cs:81-160` — `[AllowAnonymous]` POST `/api/payments/payos/webhook`:

```csharp
[AllowAnonymous]
[HttpPost("webhook")]
public async Task<IActionResult> Webhook([FromBody] PayOsWebhookRequest payload)
{
    if (payload == null) return BadRequest();
    var webhook = new Webhook { /* map fields... */ };
    var data = await _payOs.VerifyWebhookAsync(webhook);
    if (data == null) return BadRequest(new { message = "Invalid signature." });

    await _scrapeOrders.HandlePayOsWebhookAsync(data);
    await _bespoke.HandlePayOsWebhookAsync(data);
    return Ok(new { success = true });
}
```

### 3. Signature verification
`MCFH/Services/Payments/PayOsService.cs:171-197`:

```csharp
public async Task<WebhookData?> VerifyWebhookAsync(Webhook webhook)
{
    if (webhook?.Data == null || string.IsNullOrEmpty(webhook.Signature)) return null;
    try
    {
        var client = await GetClientAsync();
        var result = await client.Webhooks.VerifyAsync(webhook);   // HMAC-SHA256
        return result;
    }
    catch (WebhookException ex) { _logger.LogWarning(...); return null; }
    catch (Exception ex)        { _logger.LogError(...);   return null; }
}
```

Sai chữ ký → trả `BadRequest("Invalid signature.")`, không bao giờ tin payload.

### 4. Idempotency (2 lớp)
**Lớp 1 — process lock**: `ConcurrentDictionary<int, byte> FulfillRunning` ở `MCFH/Services/ScrapeOrderService.cs:390,400-426`:

```csharp
if (!FulfillRunning.TryAdd(order.OrderId, 0)) return;   // đang fulfill → no-op
try {
    if (payment.Status != "success") {
        payment.Status = "success"; payment.PaidAt = now;
        order.Status   = "paid";     order.PaidAt   = now;
        await _context.SaveChangesAsync();
    }
    if (!string.IsNullOrEmpty(order.MentionsPackage))
        await EnsureProjectPackageFromOrderAsync(order, payment);
    if (order.Status == "paid" && string.IsNullOrEmpty(order.ScrapeJobId))
        await StartScrapeForPaidOrderAsync(order);
}
finally { FulfillRunning.TryRemove(order.OrderId, out _); }
```

**Lớp 2 — DB state check**: chỉ update khi chưa success; `EnsureProjectPackageFromOrderAsync` check `ProjectMentionPackages.AnyAsync(p => p.PaymentId == ...)` chống double-create package.

### 5. Order types
`Payment.Type` discriminator — `MCFH/Models/Payment.cs:16`:
- **`scrape_order`** — `ScrapeOrderService.HandlePayOsWebhookAsync` (line 302-343)
- **`bespoke`** — `BespokeReportService.HandlePayOsWebhookAsync` (line 339-385)

Cả 2 handler chạy song song trong controller; mỗi handler tự filter theo `Type`.

### 6. Database update flow
Handler làm 5 bước trong EF Core transaction:
1. Lookup `Payment` by `OrderCode` + `Type`
2. Validate 3-way amount: `data.Amount == payment.Amount == order.QuotedPrice` (line 319-340)
3. `FulfillPaidOrderAsync` → set `payment.Status="success"`, `order.Status="paid"`
4. `EnsureProjectPackageFromOrderAsync` → INSERT `ProjectMentionPackages` + cộng quota
5. `StartScrapeForPaidOrderAsync` → `ScrapingJobService.StartAsync` (Hangfire job, `postedDays=30`)

### 7. End-to-end flow
1. Frontend gọi `POST /api/scrape-orders` → tạo order + quote giá
2. Frontend gọi `POST /api/scrape-orders/{id}/pay` → `PayOsService.CreatePaymentLinkAsync` (line 206-211) → lưu `Payment { OrderCode, CheckoutUrl, QrCode, Status="pending" }`
3. User mở `checkoutUrl`/QR, thanh toán trên PayOS
4. PayOS POST → `/api/payments/payos/webhook` → verify HMAC → dispatch 2 handler
5. Handler update DB + enqueue Hangfire scrape job (`postedDays=30`)
6. User redirect `PaymentReturn.tsx?orderId=X` → `GET /api/scrape-orders/{id}/payment-status` → server lại gọi `PayOS.GetPaymentLinkAsync` (không tin query param)

### 8. Security highlights
- `[AllowAnonymous]` nhưng **bắt buộc HMAC-SHA256** với `ChecksumKey`
- **3-way amount check** chống replay/false-injection
- Return page không trust query — luôn re-fetch từ PayOS
- `PayOS.Bypass` chỉ bật trong `appsettings.Development.json`
- Webhook **luôn trả 200** nếu signature OK để PayOS không retry

### 9. Edge cases
- `orderCode = 123` (test đăng ký URL) → no-op
- `data.Code != "00"` (failed/cancelled) → bỏ qua
- Webhook retry khi DB đã update → `FulfillRunning.TryAdd` fail + status check skip
- Amount mismatch → log error, KHÔNG activate
- Bespoke payment thiếu `RequestId` → log warning, no-op

### File references
- `MCFH/Controllers/Payments/PayOsWebhookController.cs:81-160`
- `MCFH/Services/Payments/PayOsService.cs:41-86` (credential resolver)
- `MCFH/Services/Payments/PayOsService.cs:171-197` (HMAC verify)
- `MCFH/Services/ScrapeOrderService.cs:302-343` (scrape order handler)
- `MCFH/Services/ScrapeOrderService.cs:390-427` (FulfillRunning lock)
- `MCFH/Services/BespokeReportService.cs:339-385` (bespoke handler)
- `MCFH/Configuration/PayOsOptions.cs:3-27`
- `MCFH/Program.cs:132-133` (DI registration)

---

## Câu 3 — Phân quyền như thế nào?

### Tổng quan
MCFH phân quyền theo **2 lớp độc lập**: `SystemRole` (Admin/Reporter/Client) kiểm soát truy cập toàn cục, `WorkspaceRole` (Owner/Editor/Viewer) kiểm soát multi-tenant isolation.

### SystemRole (Admin / Reporter / Client)
- **Không phải enum C#** mà là `string` field trong `USERS` (`Models/User.cs:24`)
- 3 giá trị: `"Admin" | "Reporter" | "Client"` — gieo khi Register hoặc Google login
- **Không dùng `[Authorize(Roles=...)]`** — chỉ dùng `[Authorize]` (cần JWT), check quyền Admin trong service qua `IsAdminAsync(userId)` (`AdminPortalService.cs:853-858`)
- JWT claim `ClaimTypes.Role` set trong `AuthController.cs:346`

### WorkspaceRole (Owner / Editor / Viewer)
- Bảng `WORKSPACE_ROLES` (`McfhDbContext.cs:1199-1210`)
- **Không có custom attribute** — check inline trong service qua:
  - `IsOwnerAsync` (`WorkspaceService.cs:19-25`)
  - `IsMemberAsync` (`WorkspaceService.cs:28-33`)
  - `CanEditAsync` (`ProjectService.cs:24-30`)
- Multi-tenant: mọi query filter `WorkspaceMembers.Any(m => m.UserId == userId)` — không có global query filter
- Join table `WorkspaceMember` (composite PK `WorkspaceId + UserId`)

### JWT
- Claims: `NameIdentifier` (UserId), `Email`, `Name`, `Role`
- **Không có claim workspace_id** — membership phải query DB
- Validate ở `Program.cs:65-83` (`jwtKey` config ở 65, `AddJwtBearer` 71-83), HMAC-SHA256
- Lifetime = **1440 phút (24h)** từ `Jwt:DurationInMinutes`
- **Không có refresh token** — user phải login lại sau 24h

### Backend authorization
- `[Authorize]` dùng ở: `AdminPortalController`, `WorkspaceController`, `MeController`, `AuthProfileController`, `change-password`
- Pattern: controller gọi service → service check `IsAdminAsync()` / `IsOwnerAsync()` thủ công

### Frontend route guards
- `<PrivateRoute allowedRoles>` (`components/auth/PrivateRoute.tsx:23-41`) — check token + role, redirect về `resolveRoleHomePath`
- `App.tsx:89-105`: `/admin/*` → `["Admin"]`
- `App.tsx:108-125`: `/reporter/*` → `["Reporter", "Admin"]`
- JWT interceptor ở `axiosClient.ts:20-26`
- Lưu ở `sessionStorage` (per-tab) — **không phải httpOnly cookie** (XSS risk)

### Cross-cutting
- **Audit log**: `WorkspaceActivityLog` + `LogActivityAsync` (`WorkspaceService.cs:36-52`); Admin global audit ở `AdminPortalService.GetAuditLogsAsync`
- **Soft delete**: `Workspace.IsDeleted` flag lọc mọi query; cascade soft-delete projects

### Edge cases
- **User bị kick khi đang login** → JWT cũ vẫn valid 24h (không có token blacklist), nhưng `IsMemberAsync` query DB sẽ 403 ngay
- **Admin escalate privilege** → `AdminPortalService.UpdateUserAsync` cho đổi `SystemRole` thành `"Admin"` không cần approval
- **Cross-workspace** → dev phải nhớ thêm `WHERE WorkspaceId IN (...)` — không có global filter

### File references
- `MCFH/Models/User.cs:24` — SystemRole
- `MCFH/Models/WorkspaceRole.cs:10` — RoleName
- `MCFH/Models/WorkspaceMember.cs:6-19` — join table
- `MCFH/Program.cs:54-71` — JWT validation
- `MCFH/Controllers/Auths/AuthController.cs:335-360` — JWT generation
- `MCFH/Services/WorkspaceService.cs:19-52` — IsOwner/IsMember/LogActivity
- `MCFH/Services/AdminPortalService.cs:853-858` — IsAdminAsync
- `MCFH-Frontend/src/App.tsx:88-125` — route guards
- `MCFH-Frontend/src/components/auth/PrivateRoute.tsx:23-41`
- `MCFH-Frontend/src/api/axiosClient.ts:20-26` — JWT interceptor

### Điểm yếu đáng drill-down
- **Brute-force**: không có rate-limit / lockout account (`AuthController.Login` line 277-330)
- **2FA**: chỉ OTP email ban đầu, không có 2FA login sau
- **GDPR**: có `IsBanned` nhưng chưa hard-delete / data anonymization
- **Token revocation**: không có blacklist

---

## Câu 5 — Deploy production cho người mới

### Tóm tắt 1 dòng
**MCFH deploy bằng Docker lên 1 con VPS Ubuntu** (đã có sẵn `docker-compose.yml` + GitHub Actions auto-deploy).

### Ví dụ đời thường
Deploy giống **chuyển phòng trọ**:
1. **Đóng gói đồ** vào thùng (Docker image)
2. **Khuân thùng lên xe tải** (push lên Docker Hub)
3. **Bốc thùng vào nhà mới** (VPS Ubuntu)
4. **Lắp đồ vào đúng chỗ** (chạy container, trỏ domain)
5. **Vào ở** (user truy cập `https://mcfh.io.vn`)

### Bước 0 — Code sẵn sàng
- Backend chạy được: `dotnet build` không lỗi
- Test scraper thử trên local (xem `README.md` mục 4)

### Bước 1 — Chuẩn bị server
- Mua **VPS Ubuntu 22.04+**, tối thiểu **2 CPU + 4 GB RAM + 40 GB SSD** (MCFH có Playwright chạy browser nặng)
- Cài **Docker + Docker Compose** lên VPS
- Tạo user `mcfh`, ssh vào bằng key (lưu key vào GitHub Secrets)

### Bước 2 — Database
- MCFH dùng **SQL Server 2022** — không phải Postgres
- Database + user tự tạo lần đầu qua container `sqlserver-init` (`docker-compose.yml:103-140`)
- Migration EF Core: chạy thủ công **lần đầu** bằng `dotnet ef database update`

### Bước 3 — Cấu hình secrets (env vars)
Backend cần (`Program.cs:65-132` và `appsettings.json:10-124`):

```bash
ConnectionStrings__MyCnn=Server=sqlserver,1433;Database=MCFH_DB;User Id=sa;Password=...;Encrypt=False;TrustServerCertificate=True
Jwt__Key=<random-32-ký-tự>
Jwt__Issuer=MCFH_Backend
Jwt__Audience=MCFH_Frontend
Auth__FrontendBaseUrl=https://mcfh.io.vn
Auth__GoogleClientId=<...>.apps.googleusercontent.com
PayOS__ClientId=<...>
PayOS__ApiKey=<...>
PayOS__ChecksumKey=<hex>
AiModel__ApiKey=<groq-api-key>
Smtp__Host=...
Smtp__Username=...
Smtp__Password=...
SerpApi__ApiKey=<...>
AzureBlob__ConnectionString=<...>
```

### Bước 4 — Deploy (đã có sẵn)
Project đã chuẩn bị sẵn:
1. **Dockerfile backend** (`.NET 8` + `Playwright 1.60`) ở `MCFH/Dockerfile:30-55`
2. **Dockerfile frontend** (React build + Nginx reverse proxy) ở `MCFH-Frontend/Dockerfile:33-76`
3. **`docker-compose.yml`** ở root — chỉ huy 4 service: `frontend`, `backend`, `sqlserver`, `sqlserver-init`

```bash
cd /home/mcfh/mcfh
docker compose pull
docker compose up -d
```

### Bước 5 — Auto-deploy bằng GitHub Actions
Workflow `.github/workflows/deploy.yml:115-168`: `git push main` → GitHub tự build image → push lên Docker Hub → SSH sang VPS kéo image mới → restart container.

### Bước 6 — Domain + HTTPS
- Trỏ DNS `mcfh.io.vn` → IP VPS
- **Caddy** hoặc **Nginx + Let's Encrypt** làm reverse proxy ngoài Docker

### Bước 7 — Verify + monitor
- Vào `https://mcfh.io.vn` → thử login Google
- Mở `/hangfire` → thấy cron `scrape-due-projects` chạy mỗi phút (`Program.cs:233-238`; còn có `recover-stuck-scrape-orders` mỗi 5 phút `:241-245` và `recover-stuck-bespoke-requests` mỗi 2 phút `:247-251` — docs cũ ghi 5 phút là SAI)
- **⚠️ `/swagger` chỉ có ở Development** (`Program.cs:201-205`). Production không có. Nếu muốn test API ở production, cần đăng nhập admin qua API trực tiếp.

### File references
- `docker-compose.yml:1-162` — toàn bộ kiến trúc 4 service (volumes 148-158, blank lines tới 162)
- `docker-compose.yml:105-142` — sqlserver-init service
- `MCFH/Dockerfile:30-55` — backend chạy Playwright (cần re-verify)
- `MCFH-Frontend/Dockerfile:33-76` — frontend + Nginx proxy (cần re-verify)
- `.github/workflows/deploy.yml:115-190` — auto-deploy (line 190 là `docker logout`)
- `MCFH/Program.cs:65-132` — chỗ app đọc config (jwtKey 65, AddJwtBearer 71-83, AddCors 85-93, DI đến ~132)
- `MCFH/appsettings.json:10-124` — danh sách config cần override
- `MCFH/Program.cs:228` — `app.UseHangfireDashboard("/hangfire")` — **CHƯA CÓ auth**, cần fix P0

### Follow-up
- Rollback: `docker compose pull backend:abc123`
- Zero-downtime: scale + Nginx load-balance
- Backup DB: cron `docker exec mcfh-sqlserver ... BACKUP DATABASE...`

---

## Câu hỏi dự kiến tiếp theo (khi user yêu cầu)
- **Câu 4** — Rate-limit Facebook/TikTok (ProxyRotation, postedDays scheduling)
- **Câu 6** — Monitor khi scrape job fail (Hangfire log, StaleRunningJobMinutes > 120)
- **Câu 7** — CommentAnalyzer pipeline (GPT/Groq batch processing)
- **Câu 8** — Mentions tracking & quota enforcement
- **Câu 9** — Soft delete vs hard delete (GDPR)
- **Câu 10** — Hangfire scheduler & cron jobs
