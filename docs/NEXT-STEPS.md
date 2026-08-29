# MCFH — Next Steps (Roadmap fix/improvement)

Danh sách fix bug, hardening và improvement thu thập từ Q&A session, sắp xếp theo **priority** × **effort**. Mỗi item liên kết Q&A gốc để có context đầy đủ.

> **Priority scale**: 🔴 P0 (blocker / security) · 🟠 P1 (nên làm trước release) · 🟡 P2 (nice-to-have)
> **Effort scale**: S = < 1 ngày · M = 1-3 ngày · L = > 3 ngày

> **Last updated**: `381e625` (origin/develop, 2026-08-21)
> **Line refs verified**: line numbers trong docs này đã re-verify sau khi pull 69 commits. Một số item có line refs từ docs cũ — đã update.

---

## 0. Trước khi sửa — kiểm tra môi trường

```bash
# Backend
cd MCFH && dotnet build -c Release
# Frontend
cd MCFH-Frontend && npm install && npm run build
# Container
docker compose build
```

> Nếu `dotnet build` lỗi → fix trước. Nếu pass → bắt đầu fix theo priority.

---

## 1. Security hardening

### 🔴 P0 — Fix CORS `AllowAnyOrigin` → whitelist domain
- **Context**: [DISCREPANCY-REPORT §CORS] — `Program.cs:89` cho phép MỌI domain
- **File**: `MCFH/Program.cs:85-93`
- **Issue**: Browser ở bất kỳ website nào cũng gọi được API MCFH → CSRF, token theft risk
- **Fix**:
  ```csharp
  options.AddPolicy("AllowAll", policy =>
      policy.WithOrigins(
          "https://mcfh.io.vn",
          "http://localhost:5173"        // dev
      )
      .AllowAnyMethod()
      .AllowAnyHeader()
      .AllowCredentials());
  ```
- **Effort**: S
- **Verify**: `curl -H "Origin: https://evil.com" https://mcfh.io.vn/api/...` → bị block

### 🔴 P0 — Hangfire dashboard phải có admin auth
- **Context**: [DISCREPANCY-REPORT §Hangfire public]
- **File**: `MCFH/Program.cs:228`
- **Issue**: `/hangfire` ở production đang public → xem/retry/delete bất kỳ job nào
- **Fix**:
  ```csharp
  app.UseHangfireDashboard("/hangfire", new DashboardOptions
  {
      Authorization = new[] { new HangfireAdminAuthFilter() }
  });
  ```
  Trong đó `HangfireAdminAuthFilter : IDashboardAuthorizationFilter` check `context.GetHttpContext().User.IsInRole("Admin")`
- **Effort**: S

### 🔴 P0 — Rate-limit login endpoint (chống brute-force)
- **Context**: [QA Câu 3 §Điểm yếu] — `AuthController.Login` không có rate-limit / lockout
- **File**: `MCFH/Controllers/Auths/AuthController.cs:277-330` (Q&A §Điểm yếu ghi line này — cần re-verify khi bắt tay vào fix)
- **Fix**:
  - Thêm `Microsoft.AspNetCore.RateLimiting` middleware hoặc dùng `AspNetCoreRateLimit` package
  - Partition key = IP + email
  - Rule: 5 attempts / 5 min → block 15 min
  - Hoặc: lockout account sau 10 failed attempts trong 1 giờ (lưu `FailedLoginCount` + `LockoutUntil` trong `User`)
- **Effort**: S (nếu dùng package) · M (nếu tự build lockout)
- **Verify**: test với `curl` spam login → expect 429

### 🔴 P0 — Refresh token + token blacklist
- **Context**: [QA Câu 3 §JWT] — JWT 24h, không có refresh, không có revocation
- **File**: `MCFH/Controllers/Auths/AuthController.cs`, `MCFH/Program.cs:65-83` (JWT Bearer config)
- **Fix**:
  - Thêm bảng `RefreshTokens` (`Id, UserId, TokenHash, ExpiresAt, RevokedAt`)
  - Endpoint `POST /api/auth/refresh` — issue access token mới + rotate refresh
  - Endpoint `POST /api/auth/logout` — revoke refresh token
  - Short access token (15 min) + long refresh (7 days)
- **Effort**: M
- **Verify**: login → access 15 min, refresh OK; logout → refresh fail

### 🟠 P1 — JWT blacklist khi admin ban user
- **Context**: [QA Câu 3 §Edge cases] — admin ban user nhưng JWT cũ v�n valid đến 24h
- **Fix**:
  - Distributed cache `IDistributedCache` lưu `revoked:{jti}` với TTL = JWT remaining lifetime
  - Middleware check blacklist trước khi authorize
  - Hoặc: thêm `UserTokenVersion` claim, increment khi ban
- **Effort**: S (cache blacklist) · M (token version)

### 🟠 P1 — 2FA cho login
- **Context**: [QA Câu 3 §Điểm yếu] — chỉ có OTP email verify, không có 2FA login
- **File**: `MCFH/Controllers/Auths/AuthController.cs`
- **Fix**:
  - Thêm `IsTwoFactorEnabled` field trong `User`
  - Dùng TOTP (`Otp.NET` package, RFC 6238)
  - Sau password → bư�c 2 nhập OTP từ authenticator app
  - Backup codes (10 codes, hash lưu DB)
- **Effort**: M

### 🟠 P1 — GDPR hard-delete / anonymize
- **Context**: [QA Câu 3 §Điểm yếu] — `IsBanned` không đủ cho GDPR right-to-erasure
- **File**: `MCFH/Models/User.cs`, các bảng chứa PII
- **Fix**:
  - Endpoint `DELETE /api/users/me` → soft-delete + anonymize email thành `deleted-{hash}@anonymized.local`
  - Scrape jobs / payments giữ lại cho accounting nhưng thay `UserId` → `null`
  - Log activity: ghi lại audit "user deleted at YYYY-MM-DD" (không xóa log)
- **Effort**: M

### 🟡 P2 — Chuyển sessionStorage → httpOnly cookie
- **Context**: [QA Câu 3 §Frontend] — `sessionStorage` dễ bị XSS đánh cắp token
- **File**: `MCFH-Frontend/src/utils/authStorage.ts:16`, backend `AuthController.cs`
- **Fix**:
  - Backend set cookie `mc_token=<jwt>; HttpOnly; Secure; SameSite=Strict; Path=/`
  - Frontend không cần lưu token — axios withCredentials=true
  - CSRF token cho state-changing requests
- **Effort**: M

### 🟡 P2 — Admin escalate privilege cần approval workflow
- **Context**: [QA Câu 3 §Edge cases] — admin có thể tự promote user lên admin
- **File**: `MCFH/Services/AdminPortalService.cs:506-540`
- **Fix**:
  - Tạo `AdminEscalationRequest` table
  - 2 admins phải approve trước khi role được đổi
  - Audit log + email notification
- **Effort**: L (architectural)

---

## 2. PayOS webhook hardening

### 🟠 P1 — Lưu PayOS webhook raw payload để debug replay
- **Context**: [QA Câu 2] — không có audit trail webhook
- **File**: `MCFH/Controllers/Payments/PayOsWebhookController.cs`
- **Fix**:
  - Bảng `WebhookEvents` (`Id, Provider, EventType, RawPayload, Signature, ReceivedAt, ProcessedAt, Result`)
  - Insert trước khi verify signature (giữ payload gốc)
  - Endpoint admin `GET /api/admin/webhook-events?provider=PayOS` filter
- **Effort**: S

### 🟠 P1 — Idempotency ở payment-row level (DB constraint)
- **Context**: [QA Câu 2 §Idempotency] — hiện chỉ lock in-memory
- **File**: `MCFH/Models/Payment.cs`
- **Fix**:
  - Thêm unique index trên `Payment.OrderCode + Type`
  - Trong webhook handler: nếu `Payment` đã success → no-op + log "duplicate"
  - Catch `DbUpdateException` (race condition) → return 200 OK để PayOS không retry
- **Effort**: S

### 🟡 P2 — Webhook secret rotation không downtime
- **Context**: PayOS có thể rotate `ChecksumKey`
- **File**: `MCFH/Services/Payments/PayOsService.cs:171-197`
- **Fix**:
  - Lưu cả `ChecksumKey_Current` + `ChecksumKey_Previous`
  - `VerifyWebhookAsync` thử cả 2
  - Sau 24h → xóa previous
- **Effort**: S

---

## 3. Workspace / multi-tenant

### 🔴 P0 — Global query filter cho WorkspaceId
- **Context**: [QA Câu 3 §Cross-cutting] — dev phải nhớ filter manually → bug risk
- **File**: `MCFH/Data/McfhDbContext.cs` (hoặc tương đương)
- **Fix**:
  - Implement `IWorkspaceScoped` interface cho Project, Comment, Mention, etc.
  - Override `OnModelCreating` → `modelBuilder.Entity<X>().HasQueryFilter(...)`
  - Set `CurrentWorkspaceId` qua scoped service từ JWT claim (cần thêm claim workspace active)
  - Admin endpoint dùng `.IgnoreQueryFilters()` để xem all
- **Effort**: M
- **Risk**: high — phải audit toàn bộ controller xem có chỗ nào assume "all rows" sẽ break
- **Verify**: integration test cho mỗi controller

### 🟠 P1 — Custom `[WorkspaceAuthorize(Role=Editor)]` attribute
- **Context**: [QA Câu 3 §WorkspaceRole] — pattern thủ công lặp lại ở mỗi service
- **File**: `MCFH/Filters/WorkspaceAuthorizeAttribute.cs` (new)
- **Fix**:
  - Tạo attribute + filter riêng
  - Filter resolve `WorkspaceId` từ route, check membership + role
  - Return 403 nếu không đủ quyền
  - Replace inline checks trong 6+ services
- **Effort**: M

### 🟡 P2 — Workspace invitation expiry
- **Context**: [QA Câu 3] — `WorkspaceInvitation` không có expiry
- **File**: `MCFH/Models/WorkspaceInvitation.cs:6-23`
- **Fix**:
  - Thêm `ExpiresAt` field (default 7 days)
  - Cron cleanup `WHERE ExpiresAt < now AND Status = 'pending'`
- **Effort**: S

---

## 4. Deploy / DevOps

### 🔴 P0 — DI CHUYỂN: hardcoded DB password → `.env`
> ⚠️ **Đã verify lại sau khi pull 69 commits** — bug vẫn còn nguyên, line numbers đã update.
- **Context**: [DISCREPANCY-REPORT §Hardcoded password]
- **File**: `docker-compose.yml:46-52` (ConnectionString), `:80` (sqlserver), `:93` (healthcheck), `:114` (sqlserver-init)
- **Issue**: `MCFH@2026` xuất hiện 4 chỗ, lộ trong git
- **Fix**:
  1. Tạo `.env.example` (commit) + `.env` (gitignore, thật)
  2. Đổi `MSSQL_SA_PASSWORD: "MCFH@2026"` → `MSSQL_SA_PASSWORD: ${MSSQL_SA_PASSWORD}`
  3. Đổi `ConnectionStrings__MyCnn` → dùng `${DB_CONNECTION_STRING}` (escape `$` cho sqlcmd)
  4. Healthcheck inline shell cũng phải đổi sang `${MSSQL_SA_PASSWORD}`
- **Effort**: S
- **Verify**: `git grep "MCFH@2026"` → chỉ còn trong `.env.example`/`.gitignore`

### 🟠 P1 — Healthcheck endpoint cho Docker
- **Context**: [DISCREPANCY-REPORT §Healthcheck chỉ cho SQL Server]
- **File**: `MCFH/Controllers/` (new `HealthController.cs`), `docker-compose.yml:26-71` (backend service)
- **Fix**:
  - `GET /healthz` — ping DB + Hangfire + Azure Blob
  - Trả 503 nếu dependency fail
  - `docker-compose.yml` thêm `healthcheck:` block cho backend (curl `/healthz`) và frontend (curl `/`)
- **Effort**: S

### 🟠 P1 — Database backup cron
- **Context**: [QA Câu 5 §Follow-up]
- **File**: `docker-compose.yml`
- **Fix**:
  - Thêm service `db-backup` chạy hàng đêm
  - Mount volume `/backups`
  - Push lên S3/Azure Blob
- **Effort**: S

### 🟠 P1 — Structured logging + error tracking
- **Context**: hiện chỉ `ILogger` mặc định
- **Fix**:
  - Tích hợp Serilog + Sentry
  - Sentry DSN qua env var
  - Capture exception + user context
- **Effort**: M

### 🟡 P2 — Zero-downtime deploy
- **Context**: [QA Câu 5 §Follow-up]
- **File**: `.github/workflows/deploy.yml:115-168`
- **Fix**:
  - Pull image mới → start container mới với port khác → Nginx switch upstream → stop container c�
  - Dùng `docker compose up --scale backend=2` + healthcheck
- **Effort**: M

### 🟡 P2 — Staging environment riêng
- **Context**: hiện chỉ 1 env production
- **Fix**:
  - Tạo `.github/workflows/deploy-staging.yml`
  - VPS thứ 2 (hoặc subdomain `staging.mcfh.io.vn`)
  - Auto-deploy từ branch `develop`
- **Effort**: M

---

## 5. Performance / Scaling

### � P2 — EF Core query optimization audit
- **Context**: multi-tenant + audit log → N+1 risk cao
- **Fix**:
  - Bật `EnableSensitiveDataLogging` ở dev
  - Dùng `.AsNoTracking()` cho read-only queries
  - `.Include()` thay vì lazy loading
  - Add index cho FK + `IsDeleted` filter
- **Effort**: M

### 🟡 P2 — Hangfire dashboard auth
- **Context**: `/hangfire` có thể public
- **File**: `MCFH/Program.cs` (Hangfire config)
- **Fix**:
  - `app.UseHangfireDashboard("/hangfire", new DashboardOptions { Authorization = new[] { new AdminAuthFilter() } })`
- **Effort**: S

### 🟡 P2 — Cache Hangfire recurring job status
- **Context**: monitor nhiều project → query DB mỗi phút
- **Fix**:
  - In-memory cache `IMemoryCache` cho `LastScrapeAt` per project
  - Invalidate khi scrape job complete
- **Effort**: S

---

## 6. Code quality

### 🟡 P2 — Refactor IsAdminAsync sang centralized policy
- **Context**: [QA Câu 3] — `IsAdminAsync` gọi ở 30+ chỗ
- **Fix**:
  - `services.AddAuthorization(o => o.AddPolicy("AdminOnly", p => p.RequireRole("Admin")))`
  - Replace bằng `[Authorize(Policy = "AdminOnly")]`
  - Loại bỏ manual `if (!await IsAdminAsync(...))` checks
- **Effort**: M

### 🟡 P2 — Replace sessionStorage bằng Zustand/Jotai
- **Context**: [QA Câu 3] — auth state rải rác
- **Fix**:
  - Zustand store với persist middleware (vẫn localStorage nhưng typed + reactive)
  - Hooks: `useAuth()`, `useUser()`
  - Loại bỏ prop drilling
- **Effort**: M

### 🟡 P2 — Add integration tests cho webhook
- **Context**: PayOS webhook là critical path
- **Fix**:
  - Test project mới `MCFH.Tests/`
  - Mock `PayOSClient`, assert state DB trước/sau
  - Test cases: success, invalid signature, duplicate, amount mismatch
- **Effort**: M

---

## Quick wins (làm trước)

> Sau khi pull 69 commits (đến `381e625`), item #1 (deploy fail) đã được fix. Quick wins cập nhật:

Nếu chỉ có 1-2 ngày, làm theo thứ tự:

1. **🔴 Fix CORS AllowAnyOrigin** (S) — whitelist domain
2. **🔴 Hangfire dashboard auth** (S) — chặn public access
3. **🔴 Move DB password → .env** (S) — không lộ password trong git
4. **🟠 Healthcheck endpoint** (S) — cần cho Docker
5. **🟠 DB backup cron** (S) — cần cho production
6. **🟡 PayOS webhook log table** (S) — debug production issue
7. **🟡 PayOS unique constraint** (S) — idempotency thật sự
8. **🟠 Rate-limit login** (M) — chống brute-force

Tổng effort: ~1 tuần cho quick wins.

---

## Sau khi xong quick wins

�ánh giá lại:
- [ ] Đọc lại `docs/QA-LOG.md` xem có câu nào cần hỏi tiếp
- [ ] Chạy `dotnet build` + `npm run build` đảm bảo không vỡ
- [ ] Cập nhật README.md với link đến `QA-LOG.md` + `NEXT-STEPS.md`
- [ ] Cân nhắc viết unit tests cho các fix mới
- [ ] Tạo PR riêng cho mỗi P0/P1 để dễ review
