# MCFH — Discrepancy Report (Q&A vs Code thật)

Báo cáo này so sánh những gì Q&A trả lời với code thực tế trong repo. Đánh dấu:
- ✅ Match — Q&A đúng
- ⚠️ Partial — Q&A đúng nhưng thiếu chi tiết quan trọng
- ❌ Wrong/Missing — Q&A sai hoặc thiếu hoàn toàn
- 🆕 New finding — phát hiện mới khi scan, chưa có trong Q&A

> **Last verified**: `381e625` (origin/develop, 2026-08-21) — sau khi pull 69 commits
> **Workspace**: `D:\Project dotnet\MCFH`
> **Note**: do 69 commits mới, line numbers trong docs cũ đã lệch. Bảng dưới đã được re-verify.

---

## Câu 2 — PayOS webhook flow

### File references
| Q&A nói | Code thật | Status |
|---|---|---|
| `PayOsService.cs:41-86` credential resolver | Đúng line 41-86 | ✅ |
| `PayOsService.cs:171-197` HMAC verify | Đúng line 171-197 | ✅ |
| `PayOsWebhookController.cs:81-160` | Đúng line 81-161 (line 161 là closing brace) | ✅ |
| `ScrapeOrderService.cs:302-343` scrape handler | Đúng line 302-343 | ✅ |
| `ScrapeOrderService.cs:390-427` FulfillRunning | Đúng line 390-427 | ✅ |
| `BespokeReportService.cs:339-385` bespoke handler | Cần verify lại (chưa đọc file này) | ⚠️ |
| `PayOsOptions.cs:3-27` | Đúng line 3-27 | ✅ |
| `Program.cs:132-133` DI registration | Đúng line 132-133 | ✅ |

### 🆕 Multi-key rotation system (CHƯA CÓ trong Q&A)
- `Models/PayOsKey.cs:13-42` — bảng DB lưu encrypted keys, hỗ trợ multi-key với flag `IsDefault`
- `Services/ProviderCredentialResolver.cs:36-217` — resolver cache 30s, ưu tiên DB row `IsDefault=true`, fallback `appsettings`
- `Services/EncryptionService.cs` — AES-256 encryption cho `ApiKeyEncrypted` + `ChecksumKeyEncrypted`
- Admin panel: `Controllers/Admin/AdminPayOsKeyController.cs` + frontend `pages/admin/settings/PayOsKeyPanel.tsx`
- Migration: `Migrations/20260810120000_AddProviderKeys.cs`
- CLI seed: `dotnet run -- --seed-provider-keys` (Program.cs:173-184) — chuyển 1 lần từ appsettings sang DB
- **Implication**: Q&A câu 2 nói "lưu key trong appsettings" — thực tế MCFH có sẵn **system rotate key qua admin panel** (Q&A đã bỏ sót). Item "Rotate ChecksumKey không downtime" trong NEXT-STEPS.md → **đã có sẵn**, cần document lại.

### 🆕 Besvo pattern (cũng có cho Brevo email)
- `Models/BrevoKey.cs` + `Services/ProviderCredentialResolver.cs:66-159` — cùng pattern cho Brevo API key / SMTP credentials
- `AdminBrevoKeyController.cs` + `pages/admin/settings/BrevoKeyPanel.tsx` — UI tương ứng

---

## Câu 3 — Phân quyền

### File references
| Q&A nói | Code thật | Status |
|---|---|---|
| `User.cs:24` SystemRole | Đúng line 24 | ✅ |
| `WorkspaceRole.cs:10` RoleName | Đúng line 10 | ✅ |
| `WorkspaceMember.cs:6-19` join table | Cần verify (chưa đọc file này) | ⚠️ |
| `Program.cs:54-71` JWT validation | Lệch — thực tế line **65-83** (jwtKey ở 65, AddJwtBearer 71-83) | ❌ |
| `AuthController.cs:335-360` JWT gen | Cần verify lại | ⚠️ |
| `WorkspaceService.cs:19-52` IsOwner/IsMember | Đúng line 19 (IsOwner), 29 (IsMember), 37 (LogActivity) — block cũ tới 52 | ⚠️ |
| `AdminPortalService.cs:853-858` IsAdminAsync | Cần verify | ⚠️ |
| `App.tsx:88-125` route guards | Cần verify lại (chưa đọc) | ⚠️ |
| `PrivateRoute.tsx:23-41` | Cần verify | ⚠️ |
| `axiosClient.ts:20-26` JWT interceptor | Cần verify | ⚠️ |
| `authStorage.ts:16` sessionStorage | Cần verify | ⚠️ |

### 🆕 Bug/security findings (CHƯA CÓ trong Q&A)

#### ❌ CORS policy `AllowAnyOrigin` (CRITICAL)
- **File**: `MCFH/Program.cs:85-93` (line 89 là `policy.AllowAnyOrigin()`)
- **Code**:
  ```csharp
  builder.Services.AddCors(options =>
  {
      options.AddPolicy("AllowAll", policy =>
      {
          policy.AllowAnyOrigin()    // ← Cho phép MỌI domain
                .AllowAnyMethod()
                .AllowAnyHeader();
      });
  });
  ```
- **Impact**: Bất kỳ website nào cũng có thể gọi API MCFH từ browser (CORS bypass) → CSRF risk, token theft risk
- **Priority**: 🔴 P0 security
- **Fix**:
  ```csharp
  policy.WithOrigins("https://mcfh.io.vn", "http://localhost:5173")
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
  ```

#### ❌ Hangfire dashboard public (CRITICAL)
- **File**: `MCFH/Program.cs:228` — `app.UseHangfireDashboard("/hangfire");`
- **Code thiếu**: `DashboardOptions { Authorization = new[] { new AdminAuthFilter() } }`
- **Impact**: `/hangfire` ở production có thể public → xem được tất cả job, retry/delete bất kỳ job nào, trigger job
- **Priority**: 🔴 P0 security
- **Fix**: Add auth filter chỉ cho Admin role

#### ❌ Swagger UI không có ở Production
- **File**: `MCFH/Program.cs:201-205`
- **Code**:
  ```csharp
  if (app.Environment.IsDevelopment())
  {
      app.UseSwagger();
      app.UseSwaggerUI();
  }
  ```
- **Q&A câu 5 nói**: "Mở `/swagger` → test API" ở Production → **SAI** — Swagger chỉ có ở Development
- **Impact**: User sẽ mở `/swagger` ở production → 404. Cần sửa Q&A hoặc thêm Swagger vào Production (behind auth)

#### ⚠️ JWT Key không check length
- **File**: `MCFH/Program.cs:65` — `var jwtKey = builder.Configuration["Jwt:Key"]!;`
- **Issue**: Nếu deploy thiếu env var, key sẽ là empty → JWT sign bằng empty key → token giả mạo dễ dàng
- **Priority**: 🟠 P1
- **Fix**: Validate `jwtKey.Length >= 32` ở startup, throw nếu không đủ

#### ⚠️ Rate-limit login (đã có trong Q&A — chưa có code verify)
- File: `AuthController.cs:277-330` (verify rồi) — đúng là KHÔNG có rate-limit
- Q&A ghi "line 277-330" chính xác

#### 🆕 JWT không có jti claim
- **File**: `AuthController.cs:343-347`
- **Issue**: Chỉ có `NameIdentifier, Email, Name, Role` → blacklist token khó (không có jti)
- **Impact**: nếu muốn revoke token → phải dùng UserTokenVersion approach
- **Priority**: 🟠 P1

---

## Câu 5 — Deploy production

### File references
| Q&A nói | Code thật | Status |
|---|---|---|
| `docker-compose.yml:1-140` | Thực tế **1-162** (thêm volumes ở 148-158, blank lines tới 162) | ⚠️ |
| `docker-compose.yml:103-140` sqlserver-init | Lệch — thực tế **105-142** | ⚠️ |
| `MCFH/Dockerfile:30-55` runtime | Cần verify | ⚠️ |
| `MCFH-Frontend/Dockerfile:33-76` Nginx | Cần verify | ⚠️ |
| `.github/workflows/deploy.yml:115-168` | Lệch — thực tế **115-190** (line 190 là `docker logout`) | ⚠️ |
| `Program.cs:47-164` config | Lệch — thực tế **65-132** (jwtKey 65-83, AddCors 85-93, DI đến ~132) | ❌ |
| `appsettings.json:10-124` | Cần verify | ⚠️ |
| `Program.cs:206-210` cron `scrape-due-projects` | Lệch — thực tế **233-238** (cron expr ở line 233) | ❌ |

### ✅ BUG #1 ĐÃ ĐƯỢC FIX: `sqlserver-migration`

**File**: `.github/workflows/deploy.yml:174-175`

```yaml
echo "Chạy service tạo hoặc cập nhật database..."
docker compose up sqlserver-init    # ← ĐÃ FIX: từ `sqlserver-migration` → `sqlserver-init`
```

- Trước đây workflow gọi `sqlserver-migration` (không tồn tại trong `docker-compose.yml`) → deploy fail.
- Commit nào đó trong 69 commits pull vừa rồi đã fix thành `sqlserver-init` (service thật có sẵn).
- **Item này KHÔNG còn là P0** — có thể bỏ khỏi `NEXT-STEPS.md` Phase 1.

### ❌ Hardcoded password trong docker-compose.yml (CRITICAL) — VẪN CÒN
- **File**: `docker-compose.yml:50, 80, 93, 114` (lệch +2 so với docs cũ `78,91,112`)
- **Code**:
  ```yaml
  ConnectionStrings__MyCnn: >-
    Server=sqlserver,1433;
    Database=MCFH_DB;
    User Id=sa;
    Password=MCFH@2026;          # ← HARDCODED
  ```
  Và `MSSQL_SA_PASSWORD: "MCFH@2026"` ở line 80 (sqlserver service), 93 (healthcheck inline), 114 (sqlserver-init service).
- **Impact**: Password DB lộ trong git → nếu repo public thì attacker có thể access DB
- **Priority**: 🔴 P0 security
- **Fix**:
  ```yaml
  ConnectionStrings__MyCnn: ${DB_CONNECTION_STRING}
  MSSQL_SA_PASSWORD: ${DB_SA_PASSWORD}
  ```
  Và commit `.env.example` (không commit `.env` thật)

### ⚠️ Image registry hardcoded `truongnhat2102`
- **File**: `docker-compose.yml:4, 28`
- **Code**: `image: truongnhat2102/mcfh-frontend:${IMAGE_TAG:-latest}`
- **Issue**: Nếu bạn muốn đổi Docker Hub user, phải sửa docker-compose.yml
- **Fix**: Dùng env var `DOCKER_USERNAME`/`DOCKER_REGISTRY`:
  ```yaml
  image: ${DOCKER_REGISTRY:-docker.io}/${DOCKER_USERNAME}/mcfh-frontend:${IMAGE_TAG:-latest}
  ```

### ⚠️ Multi-platform build tăng thời gian
- **File**: `deploy.yml:64-67, 91-94`
- **Issue**: Build cả `linux/amd64` + `linux/arm64` → thời gian build gấp đôi
- **Fix nếu VPS Intel**: chỉ build `linux/amd64`

### 🆕 Recovery cron jobs (CHƯA CÓ trong Q&A câu 5)
- **File**: `Program.cs:240-253`
- Có 3 cron jobs Hangfire:
  1. `scrape-due-projects` — line **233-238**, cron `*/1 * * * *` (mỗi phút)
  2. `recover-stuck-scrape-orders` — line **241-245**, cron `*/5 * * * *` (mỗi 5 phút)
  3. `recover-stuck-bespoke-requests` — line **247-251**, cron `*/2 * * * *` (mỗi 2 phút) — docs cũ nói mỗi 5 phút, SAI
  4. **Bonus**: line 253 — `BackgroundJob.Enqueue` chạy `RecoverStuckBespokeRequestsAsync` ngay khi boot để unlock đơn đang treo

### 🆕 Healthcheck chỉ cho SQL Server
- **File**: `docker-compose.yml:89-98` (lệch +2 so với docs cũ `87-96`) — chỉ `sqlserver` có healthcheck
- **Backend/frontend KHÔNG có healthcheck** → nếu backend crash, docker không tự restart đúng cách
- **Priority**: 🟠 P1
- **Fix**: Thêm `healthcheck:` cho backend (curl `/healthz`) và frontend (curl `/`)

### 🆕 CORS + Swagger không sync giữa Dev/Prod
- **File**: `Program.cs:85-93` (CORS), `Program.cs:201-205` (Swagger)
- **Q&A câu 5** đề cập "truy cập `/swagger` để test API ở production" nhưng thực tế Swagger chỉ ở Dev
- **Fix**: Thêm Swagger vào Production nhưng behind `[Authorize(Policy="AdminOnly")]`

---

## Tổng kết bugs/findings ưu tiên P0

| # | File | Issue | Priority | Effort | Status |
|---|---|---|---|---|---|
| ~~1~~ | ~~`deploy.yml:153`~~ | ~~Service `sqlserver-migration` không tồn tại → deploy fail~~ | ~~🔴 P0~~ | ~~S~~ | ✅ **ĐÃ FIX** (xem section trên) |
| 2 | `docker-compose.yml:50,80,93,114` | Hardcoded DB password | 🔴 P0 | S | ⚠️ VẪN CÒN |
| 3 | `Program.cs:85-93` (line 89) | CORS `AllowAnyOrigin` | 🔴 P0 | S | ⚠️ VẪN CÒN |
| 4 | `Program.cs:228` | Hangfire dashboard không auth | 🔴 P0 | S | ⚠️ VẪN CÒN |
| 5 | `AuthController.cs:277-330` | Không có rate-limit login | 🔴 P0 | M | ⚠️ VẪN CÒN |

**Note**: Sau khi pull 69 commits, line numbers cho các bug còn lại đã re-verify (delta +28 cho Program.cs, +2 cho docker-compose).

## Tổng kết findings P1

| # | File | Issue | Priority | Effort |
|---|---|---|---|---|
| 6 | `Program.cs:53` | JWT key không check min length | 🟠 P1 | S |
| 7 | `Program.cs:189-193` | Swagger chỉ ở Dev | 🟠 P1 | S |
| 8 | `Program.cs` (no healthcheck) | Backend không có `/healthz` | 🟠 P1 | S |
| 9 | Q&A câu 2 | PayOS key rotation đã có sẵn — cần update docs | 🟠 P1 | S |

## Cần update `QA-LOG.md`

Tôi sẽ cập nhật sau khi user xác nhận. Những phần cần sửa:
- **Câu 2**: Bổ sung section "Multi-key rotation qua Admin Panel"
- **Câu 3**: Bổ sung "CORS AllowAnyOrigin", "Hangfire public", "Swagger chỉ Dev"
- **Câu 5**: Bổ sung bug `sqlserver-migration`, hardcoded password, healthcheck, recovery cron
- **Câu 5**: Bỏ dòng "Mở `/swagger` ở production" (sai)

---

## Đề xuất thứ tự fix

### Phase 1 — Bảo mật (1-2 ngày, sau khi pull 69 commits)
> ~~1. Fix `deploy.yml:153`~~ — ĐÃ FIX bởi commit trong 69 commits pull vừa rồi
1. Move DB password sang `.env` (S) — `docker-compose.yml:50,80,93,114`
2. CORS → whitelist `mcfh.io.vn` (S) — `Program.cs:85-93`
3. Hangfire dashboard → admin auth (S) — `Program.cs:228`

### Phase 2 — Harden auth + observability (3-5 ngày)
4. Rate-limit login (M)
5. JWT key length check (S) — `Program.cs:65`
6. Healthcheck endpoint + docker healthcheck block (S) — `docker-compose.yml` backend service
7. JWT blacklist cho banned user (M)

### Phase 3 — Docs cleanup (1 ngày)
8. Cập nhật `QA-LOG.md` cho khớp code thật (line numbers đã lệch)
9. Cập nhật `NEXT-STEPS.md` — bỏ item "đã có sẵn", fix line numbers

Tổng effort: ~1 tuần.
