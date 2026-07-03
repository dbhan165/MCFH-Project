# Hướng dẫn Setup Scraper — MCFH Backend

Tài liệu này hướng dẫn setup các thành phần **bắt buộc** trước khi chạy được tính năng Social Listening Scraper (Facebook, YouTube, TikTok) trên máy mới sau khi clone project.

---

## 1. Setup Database

Ngoài file database gốc, chạy thêm 1 file Query để bổ sung cả phần DB của Dũng và của Duy:
(File lấy trong zalo đã ghim)

---

## 2. Setup Facebook Cookie (BẮT BUỘC — mỗi máy phải tự làm riêng)

Facebook Scraper (`FacebookGroupScraper`, `FacebookScraper`) cần file cookie để giữ session đăng nhập. File này **không được commit lên Git** (đã có trong `.gitignore`), nên mỗi máy clone project về đều phải tự tạo lại.

### Bước 1 — Chuẩn bị tài khoản Facebook test

- Dùng 1 tài khoản Facebook riêng cho mục đích scraping (không dùng tài khoản cá nhân).
- **Tắt 2FA (Xác thực 2 yếu tố)** trên tài khoản này — Settings → Security and Login → Two-Factor Authentication.
- Join các group cần track (group tuyển dụng IT, group review doanh nghiệp...).

> ⚠️ Tài khoản Facebook rất dễ bị Facebook flag/khóa nếu dùng sai cách. **Tuyệt đối không** để code tự động điền email/password để login (`SaveSessionAsync` cũ đã bị xóa vì lý do này) — chỉ login thủ công bằng tay trên browser thật.

### Bước 2 — Cài extension Cookie Editor

- Cài extension **Cookie Editor** (tác giả: cgagnier) trên Chrome hoặc Edge.

### Bước 3 — Export cookie

1. Login Facebook bằng tài khoản test trên browser thật (không phải qua code).
2. Vào trang `facebook.com` (đảm bảo đã đăng nhập).
3. Click icon Cookie Editor trên thanh extension.
4. Chọn **Export** → **Export as JSON**.
5. Lưu nội dung ra file tên đúng là `fb_cookie.json`.

### Bước 4 — Copy file cookie vào đúng vị trí

Copy file `fb_cookie.json` vào thư mục output của project:

```
MCFHBackend/bin/Debug/net8.0/fb_cookie.json
```

> Đường dẫn này tương ứng với `AppContext.BaseDirectory` khi chạy Debug từ Visual Studio. Nếu chạy theo cách khác (publish, Release...), cần xác định lại đúng thư mục output tương ứng.

### Bước 5 — Verify cookie hoạt động

Chạy project, gọi thử endpoint test (nếu còn tồn tại trong code — `GET /api/scraper/test-fb-session`), kiểm tra response trả về `loggedIn: true`.

Nếu không có endpoint test, có thể gọi trực tiếp:
```
POST /api/scrape/by-keyword?projectId={id}
```
và quan sát console log — nếu thấy `[FB Session] Loaded N cookies.` và không bị redirect về trang login Facebook, là cookie hoạt động đúng.

---

## 3. SerpApi Key (News discovery)

Luồng **Tin tức** dùng SerpApi để tìm URL bài báo (Google `site:` query), sau đó Playwright đọc nội dung từng bài. Nếu SerpApi lỗi / hết quota / không có kết quả → tự fallback sang Playwright (Google → Bing).

1. Đăng ký tại [serpapi.com](https://serpapi.com) (free tier ~100 searches/tháng).
2. Thêm key vào User Secrets hoặc `appsettings.Development.json` (file này đã gitignore):

```json
"SerpApi": {
  "Enabled": true,
  "ApiKey": "YOUR_KEY_HERE"
}
```

Hoặc biến môi trường: `SerpApi__ApiKey=...`

3. `Scraping:NewsDiscoveryProvider`:
   - `auto` (mặc định) — SerpApi nếu có key, else Playwright
   - `serpapi` — ưu tiên SerpApi, fallback Playwright khi lỗi
   - `playwright` — bỏ qua SerpApi (dev / zero cost)

> Không commit ApiKey vào git. `appsettings.json` để `ApiKey` rỗng.

---

## 4. Test nhanh sau khi setup

### Gọi API test

```
POST /api/scrape/by-keyword?projectId=1
```

Kết quả mong đợi: browser Playwright tự mở lên (đang chạy `Headless = false` để dễ debug), chạy lần lượt Facebook → YouTube → TikTok, trả về JSON tổng hợp kết quả 3 platform.

---

## 5. Các file/folder KHÔNG được commit lên Git

Đã có trong `.gitignore`, nhắc lại để lưu ý khi thêm máy mới hoặc thêm scraper mới:

- `**/fb_cookie.json` — chứa session đăng nhập Facebook, nhạy cảm
- Mọi file cookie/session khác nếu phát sinh thêm sau này (TikTok, v.v.) nên áp dụng cùng nguyên tắc

---

## 6. Vấn đề thường gặp

| Lỗi | Nguyên nhân | Cách xử lý |
|---|---|---|
| `FileNotFoundException: fb_cookie.json` | Chưa copy file cookie vào đúng thư mục output | Làm lại Bước 4 ở mục 2 |
| Facebook trả về trang login khi chạy scraper | Cookie hết hạn hoặc tài khoản bị logout | Export cookie mới (Bước 3, mục 2) |
| Facebook yêu cầu reCAPTCHA/2-step verification | Tài khoản bị Facebook flag — **không dùng lại được** | Tạo tài khoản Facebook mới, lặp lại từ Bước 1 |
| TikTok trả về "Something went wrong" liên tục | CAPTCHA do test dồn dập trong thời gian ngắn | Chờ vài phút rồi thử lại; trong production (Hangfire chạy theo lịch) hiện tượng này hiếm gặp |
