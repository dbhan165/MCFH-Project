using MCFH.Models; // Đổi lại đúng namespace của folder Models của bạn
using MCFH.Services;
using MCFHBackend.DTOs.AuthDtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authorization;

namespace MCFH.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly McfhDbContext _context; // Đổi tên Class Context cho đúng với dự án của bạn
        private readonly IConfiguration _config;
        private readonly IEmailService _emailService;

        public AuthController(McfhDbContext context, IConfiguration config, IEmailService emailService)
        {
            _context = context;
            _config = config;
            _emailService = emailService; // Nạp Service thật vào
        }

        // ==========================================
        // UC-03: REGISTER ACCOUNT
        // ==========================================
        // Thay thế đoạn code bên trong API [HttpPost("register")] cũ của bạn:

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            // 1. Tìm xem email này đã từng xuất hiện trong DB chưa
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            int targetUserId;
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            if (existingUser != null)
            {
                // TRƯỜNG HỢP 1: Email đã tồn tại VÀ ĐÃ XÁC THỰC -> Chặn không cho đăng ký
                if (existingUser.IsVerified == true)
                {
                    return BadRequest(new { message = "Email này đã được đăng ký và xác thực trên hệ thống." });
                }

                // TRƯỜNG HỢP 2: Email đã tồn tại nhưng CHƯA XÁC THỰC -> Cho phép đè thông tin mới để đăng ký lại
                existingUser.PasswordHash = passwordHash;
                existingUser.FullName = model.FullName;
                existingUser.Phone = model.Phone;
                existingUser.CreatedAt = DateTime.Now; // Reset lại thời gian tạo

                _context.Users.Update(existingUser);
                await _context.SaveChangesAsync();

                targetUserId = existingUser.UserId;
            }
            else
            {
                // TRƯỜNG HỢP 3: Email hoàn toàn mới -> Tiến hành tạo mới dữ liệu bình thường
                var newUser = new User
                {
                    Email = model.Email,
                    PasswordHash = passwordHash,
                    FullName = model.FullName,
                    Phone = model.Phone,
                    AuthProvider = "local",
                    SystemRole = "Client",
                    IsVerified = false,
                    IsBanned = false,
                    CreatedAt = DateTime.Now
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                targetUserId = newUser.UserId;
            }

            // 2. Hủy kích hoạt tất cả các mã OTP cũ của User này (nếu có) để tránh xung đột
            var oldVerifications = await _context.EmailVerifications
                .Where(v => v.UserId == targetUserId && v.IsUsed == false)
                .ToListAsync();
            foreach (var old in oldVerifications)
            {
                old.IsUsed = true; // Đánh dấu là đã bỏ qua
            }

            // 3. Sinh mã OTP và Token mới
            string randomOtp = new Random().Next(100000, 999999).ToString();
            string randomToken = Guid.NewGuid().ToString();

            var emailVerification = new EmailVerification
            {
                UserId = targetUserId,
                OtpCode = randomOtp,
                VerificationToken = randomToken,
                ExpiredAt = DateTime.Now.AddMinutes(15),
                IsUsed = false,
                CreatedAt = DateTime.Now
            };

            _context.EmailVerifications.Add(emailVerification);
            await _context.SaveChangesAsync();

            // 4. Tiến hành gửi Email thật
            // --- ĐOẠN FIX LỖI TÊN & ĐỒNG BỘ GIAO DIỆN BRAND CHO EMAIL ĐĂNG KÝ ---
            string displayName = string.IsNullOrWhiteSpace(model.FullName) ? "Thành viên MCFH" : model.FullName;
            string emailSubject = "MCFH Hub - Xác thực tài khoản đăng ký mới";
            string activationLink = $"https://localhost:7000/api/auth/verify-email?token={randomToken}";

            string emailBody = $@"
<div style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; background-color: #f1f5f9; padding: 40px 20px; color: #334155;'>
    <div style='max-width: 580px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(15, 23, 42, 0.05); border: 1px solid #e2e8f0;'>
        
        <!-- Header Brand -->
        <div style='background-color: #1e3a8a; padding: 30px 20px; text-align: center;'>
            <h2 style='color: #ffffff; margin: 0; font-size: 24px; font-weight: 700; letter-spacing: 0.5px;'>MCFH SYSTEM HUB</h2>
            <p style='color: #93c5fd; margin: 6px 0 0 0; font-size: 11px; text-transform: uppercase; font-weight: 600; letter-spacing: 1px;'>Multichannel Customer Feedback & Sentiment Analysis</p>
        </div>

        <!-- Body Content -->
        <div style='padding: 35px 30px; line-height: 1.6; font-size: 15px;'>
            <p style='margin-top: 0; font-size: 16px;'>Xin chào <b>{displayName}</b>,</p>
            <p>Chào mừng bạn đến với hệ thống lắng nghe mạng xã hội MCFH! Hệ thống đã ghi nhận yêu cầu khởi tạo tài khoản của bạn.</p>
            <p>Vui lòng sử dụng mã OTP gồm 6 chữ số dưới đây để hoàn tất bước xác thực tài khoản:</p>
            
            <!-- Khung hiển thị OTP nổi bật -->
            <div style='text-align: center; margin: 30px 0;'>
                <div style='display: inline-block; background-color: #f8fafc; border: 2px dashed #cbd5e1; padding: 14px 40px; font-size: 32px; font-weight: 700; letter-spacing: 6px; color: #1e3a8a; border-radius: 8px;'>
                    {randomOtp}
                </div>
            </div>

            <!-- Banner Cảnh báo bảo mật -->
            <div style='background-color: #fffbeb; border-left: 4px solid #f59e0b; padding: 16px; margin: 25px 0; border-radius: 6px;'>
                <p style='margin: 0; font-size: 13.5px; color: #b45309; line-height: 1.5;'>
                    ⚠️ <b>Thời hạn mã:</b> Mã OTP này chỉ có hiệu lực sử dụng trong vòng <b>15 minutes</b>. Tuyệt đối không chia sẻ mã này với bất kỳ ai để tránh mất an toàn thông tin tài khoản.
                </p>
            </div>

            <!-- Link kích hoạt nhanh thay thế -->
            <p style='font-size: 13px; color: #64748b; text-align: center; margin-top: 25px;'>
                Hoặc bạn có thể click vào đường link sau để kích hoạt nhanh tài khoản:<br/>
                <a href='{activationLink}' style='color: #2563eb; text-decoration: underline; font-weight: 500;'>Kích hoạt tài khoản ngay</a>
            </p>
        </div>

        <!-- Footer -->
        <div style='background-color: #f8fafc; padding: 24px; text-align: center; border-top: 1px solid #e2e8f0; font-size: 12px; color: #94a3b8;'>
            <p style='margin: 0;'>Đây là email tự động thông báo từ nền tảng hỗ trợ Social Listening MCFH.</p>
            <p style='margin: 6px 0 0 0;'>© {DateTime.Now.Year} MCFH Project Team. Mọi quyền được bảo lưu.</p>
        </div>
    </div>
</div>";

            // Thực hiện gửi qua SMTP Gmail thật
            await _emailService.SendEmailAsync(model.Email, emailSubject, emailBody);

            await _emailService.SendEmailAsync(model.Email, emailSubject, emailBody);

            return Ok(new
            {
                message = "Đăng ký thành công! Hệ thống đã gửi mã OTP mới tới email của bạn."
            });
        }

        // ==========================================
        // API: RESEND OTP (GỬI LẠI MÃ XÁC THỰC)
        // ==========================================
        [HttpPost("resend-otp")]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpDto model)
        {
            // 1. Kiểm tra tài khoản
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy thông tin tài khoản với email này." });
            }

            if (user.IsVerified == true)
            {
                return BadRequest(new { message = "Tài khoản này đã được xác thực từ trước. Bạn có thể đăng nhập ngay." });
            }

            // 2. Vô hiệu hóa các mã OTP cũ chưa dùng
            var oldVerifications = await _context.EmailVerifications
                .Where(v => v.UserId == user.UserId && v.IsUsed == false)
                .ToListAsync();
            foreach (var old in oldVerifications)
            {
                old.IsUsed = true;
            }

            // 3. Tạo mã OTP / Token mới hoàn toàn
            string randomOtp = new Random().Next(100000, 999999).ToString();
            string randomToken = Guid.NewGuid().ToString();

            var emailVerification = new EmailVerification
            {
                UserId = user.UserId,
                OtpCode = randomOtp,
                VerificationToken = randomToken,
                ExpiredAt = DateTime.Now.AddMinutes(15),
                IsUsed = false,
                CreatedAt = DateTime.Now
            };

            _context.EmailVerifications.Add(emailVerification);
            await _context.SaveChangesAsync();

            // 4. Gửi email thông báo mã mới
            // --- ĐOẠN FIX LỖI TÊN & ĐỒNG BỘ GIAO DIỆN BRAND CHO EMAIL GỬI LẠI MÃ ---
            string displayName = string.IsNullOrWhiteSpace(user.FullName) ? "Thành viên MCFH" : user.FullName;
            string emailSubject = "MCFH Hub - Gửi lại mã xác thực tài khoản";
            string activationLink = $"https://localhost:7000/api/auth/verify-email?token={randomToken}";

            string emailBody = $@"
<div style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; background-color: #f1f5f9; padding: 40px 20px; color: #334155;'>
    <div style='max-width: 580px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(15, 23, 42, 0.05); border: 1px solid #e2e8f0;'>
        
        <!-- Header Brand -->
        <div style='background-color: #1e3a8a; padding: 30px 20px; text-align: center;'>
            <h2 style='color: #ffffff; margin: 0; font-size: 24px; font-weight: 700; letter-spacing: 0.5px;'>MCFH SYSTEM HUB</h2>
            <p style='color: #93c5fd; margin: 6px 0 0 0; font-size: 11px; text-transform: uppercase; font-weight: 600; letter-spacing: 1px;'>Multichannel Customer Feedback & Sentiment Analysis</p>
        </div>

        <!-- Body Content -->
        <div style='padding: 35px 30px; line-height: 1.6; font-size: 15px;'>
            <p style='margin-top: 0; font-size: 16px;'>Xin chào <b>{displayName}</b>,</p>
            <p>Hệ thống nhận được yêu cầu <b>Gửi lại mã xác thực OTP</b> từ bạn cho tài khoản đăng ký trên hệ thống MCFH.</p>
            <p>Dưới đây là mã OTP mới được tạo lập lại của bạn:</p>
            
            <!-- Khung hiển thị OTP nổi bật -->
            <div style='text-align: center; margin: 30px 0;'>
                <div style='display: inline-block; background-color: #f8fafc; border: 2px dashed #cbd5e1; padding: 14px 40px; font-size: 32px; font-weight: 700; letter-spacing: 6px; color: #1e3a8a; border-radius: 8px;'>
                    {randomOtp}
                </div>
            </div>

            <!-- Banner Cảnh báo bảo mật -->
            <div style='background-color: #fffbeb; border-left: 4px solid #f59e0b; padding: 16px; margin: 25px 0; border-radius: 6px;'>
                <p style='margin: 0; font-size: 13.5px; color: #b45309; line-height: 1.5;'>
                    ⚠️ <b>Lưu ý:</b> Mã OTP cũ của bạn đã bị vô hiệu hóa trên hệ thống. Mã OTP mới này sẽ tiếp tục có hiệu lực trong vòng <b>15 minutes</b>.
                </p>
            </div>

            <!-- Link kích hoạt nhanh thay thế -->
            <p style='font-size: 13px; color: #64748b; text-align: center; margin-top: 25px;'>
                Hoặc click vào đường link sau để thực hiện kích hoạt nhanh:<br/>
                <a href='{activationLink}' style='color: #2563eb; text-decoration: underline; font-weight: 500;'>Kích hoạt tài khoản ngay</a>
            </p>
        </div>

        <!-- Footer -->
        <div style='background-color: #f8fafc; padding: 24px; text-align: center; border-top: 1px solid #e2e8f0; font-size: 12px; color: #94a3b8;'>
            <p style='margin: 0;'>Đây là email tự động thông báo từ nền tảng hỗ trợ Social Listening MCFH.</p>
            <p style='margin: 6px 0 0 0;'>© {DateTime.Now.Year} MCFH Project Team. Mọi quyền được bảo lưu.</p>
        </div>
    </div>
</div>";

            // Thực hiện gửi qua SMTP Gmail thật
            await _emailService.SendEmailAsync(user.Email, emailSubject, emailBody);

            return Ok(new { message = "Hệ thống đã gửi lại mã OTP mới. Vui lòng kiểm tra hộp thư." });
        }

        // ==========================================
        // UC-04: VERIFY EMAIL (XÁC THỰC TÀI KHOẢN)
        // ==========================================
        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto model)
        {
            // 1. Kiểm tra xem tài khoản có tồn tại không
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy thông tin tài khoản." });
            }

            if (user.IsVerified == true)
            {
                return BadRequest(new { message = "Tài khoản này đã được xác thực từ trước." });
            }

            // 2. Tìm bản ghi xác thực mới nhất, chưa sử dụng của User này
            var verification = await _context.EmailVerifications
                .Where(v => v.UserId == user.UserId && v.IsUsed == false)
                .OrderByDescending(v => v.CreatedAt)
                .FirstOrDefaultAsync();

            if (verification == null)
            {
                return BadRequest(new { message = "Yêu cầu xác thực không hợp lệ hoặc đã hết hạn." });
            }

            // 3. Kiểm tra thời hạn mã
            if (verification.ExpiredAt < DateTime.Now)
            {
                return BadRequest(new { message = "Mã xác thực đã hết hạn. Vui lòng bấm gửi lại mã mới." });
            }

            // 4. Kiểm tra tính hợp lệ của dữ liệu gửi lên (Hỗ trợ cả OTP và Token Link)
            bool isValid = false;

            if (!string.IsNullOrEmpty(model.OtpCode))
            {
                isValid = (verification.OtpCode == model.OtpCode);
            }
            else if (!string.IsNullOrEmpty(model.VerificationToken))
            {
                isValid = (verification.VerificationToken == model.VerificationToken);
            }

            if (!isValid)
            {
                return BadRequest(new { message = "Mã xác thực hoặc token không chính xác." });
            }

            // 5. Cập nhật trạng thái xác thực thành công
            verification.IsUsed = true; // Đánh dấu mã này đã xài xong

            user.IsVerified = true;
            user.VerifiedAt = DateTime.Now;

            // Lưu thay đổi đồng thời vào cả 2 bảng
            await _context.SaveChangesAsync();

            return Ok(new { message = "Xác thực email thành công! Tài khoản của bạn đã được kích hoạt." });
        }

        // ==========================================
        // UC-05: LOG IN WITH EMAIL / PASSWORD
        // ==========================================
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            // 1. Tìm user theo Email
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                return Unauthorized(new { message = "Tài khoản hoặc mật khẩu không chính xác." });
            }

            // 2. Kiểm tra nếu tài khoản đăng ký qua Google SSO mà cố login thủ công
            if (user.AuthProvider == "google" && string.IsNullOrEmpty(user.PasswordHash))
            {
                return BadRequest(new { message = "Tài khoản này được đăng ký thông qua Google. Vui lòng đăng nhập bằng Google." });
            }

            // 3. Kiểm tra tính chính xác của mật khẩu
            bool isPasswordValid = BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash);
            if (!isPasswordValid)
            {
                return Unauthorized(new { message = "Tài khoản hoặc mật khẩu không chính xác." });
            }

            // 4. Kiểm tra tài khoản có bị khóa (Banned) hay không
            if (user.IsBanned == true)
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Tài khoản của bạn đã bị khóa bởi hệ thống quản trị." });
            }

            // 5. Tạo JWT Token nếu mọi thứ hợp lệ
            string token = GenerateJwtToken(user);

            // 6. Trả dữ liệu sạch về cho Frontend lưu trữ tại LocalStorage/Redux
            var response = new AuthResponseDto
            {
                Token = token,
                UserId = user.UserId,
                Email = user.Email,
                FullName = user.FullName,
                Role = user.SystemRole
            };

            return Ok(response);
        }

        // ==========================================
        // HÀM HỖ TRỢ TẠO JWT TOKEN
        // ==========================================
        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_config["Jwt:Key"]!);

            // Đóng gói các thông tin cơ bản của User vào Token để Frontend giải nén dùng luôn
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.FullName),
                new Claim(ClaimTypes.Role, user.SystemRole) // Cực kỳ quan trọng để Middleware kiểm tra quyền truy cập
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(double.Parse(_config["Jwt:DurationInMinutes"]!)),
                Issuer = _config["Jwt:Issuer"],
                Audience = _config["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginDto model)
        {
            try
            {
                // TRƯỜNG HỢP 1: Xác thực tính chính thống của mã Token gửi từ Google
                // Thư viện sẽ tự động liên kết server Google để kiểm tra tính toàn vẹn và thời hạn
                var payload = await GoogleJsonWebSignature.ValidateAsync(model.IdToken, new GoogleJsonWebSignature.ValidationSettings
                {
                    // Nếu sau này bạn có Google Client ID ở Frontend, hãy điền vào đây để tăng bảo mật:
                    // Audience = new[] { "YOUR_GOOGLE_CLIENT_ID.apps.googleusercontent.com" }
                });

                if (payload == null)
                {
                    return BadRequest(new { message = "Xác thực tài khoản Google thất bại." });
                }

                string email = payload.Email;
                string googleId = payload.Subject; // Đây là ID định danh duy nhất của User trên hệ thống Google
                string fullName = payload.Name;
                string? avatarUrl = payload.Picture;

                // Tìm kiếm xem Email này đã có trong Database chưa
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == email);

                if (user != null)
                {
                    // TRƯỜNG HỢP 5: Tài khoản đang bị Admin khóa (Banned)
                    if (user.IsBanned == true)
                    {
                        return StatusCode(StatusCodes.Status403Forbidden, new { message = "Tài khoản của bạn đã bị khóa bởi hệ thống quản trị." });
                    }

                    // TRƯỜNG HỢP 4: Email đã đăng ký bằng Form thủ công trước đó (local), giờ chọn Login Google lần đầu
                    if (user.AuthProvider == "local" && string.IsNullOrEmpty(user.GoogleId))
                    {
                        // Thực hiện liên kết tài khoản (Link Account) tự động để tăng trải nghiệm người dùng
                        user.GoogleId = googleId;
                        if (string.IsNullOrEmpty(user.AvatarUrl)) user.AvatarUrl = avatarUrl; // Cập nhật avatar nếu trống

                        _context.Users.Update(user);
                        await _context.SaveChangesAsync();
                    }
                    // TRƯỜNG HỢP 3: Tài khoản Google cũ quay lại đăng nhập -> Không cần làm gì thêm, lấy dữ liệu ra dùng
                }
                else
                {
                    // TRƯỜNG HỢP 2: Tài khoản hoàn toàn mới -> Tự động đăng ký tài khoản ngầm (Auto-provisioning)
                    user = new User
                    {
                        Email = email,
                        PasswordHash = null,    // Đăng nhập qua Google thì không có mật khẩu cục bộ
                        FullName = fullName,
                        AvatarUrl = avatarUrl,
                        AuthProvider = "google",// Đánh dấu nhà cung cấp là Google
                        GoogleId = googleId,    // Lưu lại ID Google
                        SystemRole = "Client",  // Vai trò mặc định cho người dùng mới
                        IsVerified = true,      // Vì đã qua Google xác thực hòm thư nên mặc định kích hoạt luôn
                        VerifiedAt = DateTime.Now,
                        IsBanned = false,
                        CreatedAt = DateTime.Now
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                // BƯỚC CUỐI: Cấp JWT Token nội bộ của hệ thống MCFH để Frontend sử dụng cho các API sau
                string systemToken = GenerateJwtToken(user);

                var response = new AuthResponseDto
                {
                    Token = systemToken,
                    UserId = user.UserId,
                    Email = user.Email,
                    FullName = user.FullName,
                    Role = user.SystemRole
                };

                return Ok(response);
            }
            catch (InvalidJwtException)
            {
                // Bắt chính xác lỗi nếu Frontend truyền lên Token fake, chế, hoặc Token đã hết hạn từ hôm qua
                return BadRequest(new { message = "Mã xác thực Google không hợp lệ hoặc đã hết hạn sử dụng." });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Lỗi hệ thống khi xử lý xác thực Google.", error = ex.Message });
            }
        }

        // ==========================================
        // UC-07: FORGET PASSWORD (QUÊN MẬT KHẨU) - FIXED UI/BUG
        // ==========================================
        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            // 1. Kiểm tra email có tồn tại không
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
            if (user == null)
            {
                return NotFound(new { message = "Email này không tồn tại trên hệ thống." });
            }

            // 2. Kiểm tra nếu tài khoản đăng nhập thuần bằng Google (không có mật khẩu)
            if (user.AuthProvider == "google" && string.IsNullOrEmpty(user.PasswordHash))
            {
                return BadRequest(new { message = "Tài khoản của bạn đăng nhập thông qua Google. Vui lòng chọn chức năng 'Đăng nhập bằng Google'." });
            }

            // 3. Vô hiệu hóa toàn bộ các token khôi phục cũ chưa xài của user này
            var oldTokens = await _context.PasswordResetTokens
                .Where(t => t.UserId == user.UserId && t.IsUsed == false)
                .ToListAsync();
            foreach (var token in oldTokens)
            {
                token.IsUsed = true;
            }

            // 4. Sinh một Token khôi phục ngẫu nhiên duy nhất (Unique Guid)
            string resetToken = Guid.NewGuid().ToString();

            var passwordResetToken = new PasswordResetToken
            {
                UserId = user.UserId,
                ResetToken = resetToken,
                ExpiredAt = DateTime.Now.AddMinutes(15), // Token có hiệu lực trong 15 phút
                IsUsed = false,
                CreatedAt = DateTime.Now
            };

            _context.PasswordResetTokens.Add(passwordResetToken);
            await _context.SaveChangesAsync();

            // ========================================================
            // ĐOẠN FIX LỖI "CHÀO USER" & ĐỒNG BỘ GIAO DIỆN BRAND CHUẨN
            // ========================================================

            // Fix lỗi: Nếu FullName bị null hoặc trống, hệ thống tự fallback thành "Thành viên MCFH" hoặc "bạn"
            string displayName = string.IsNullOrWhiteSpace(user.FullName) ? "Thành viên MCFH" : user.FullName;

            string emailSubject = "MCFH Hub - Yêu cầu đặt lại mật khẩu tài khoản";
            string resetLink = $"http://localhost:3000/reset-password?token={resetToken}"; // Đường dẫn FrontEnd React sau này

            // Sử dụng giao diện mẫu Card-Layout chuyên nghiệp đồng bộ với hệ thống Email thông báo
            string emailBody = $@"
    <div style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; background-color: #f1f5f9; padding: 40px 20px; color: #334155;'>
        <div style='max-width: 580px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(15, 23, 42, 0.05); border: 1px solid #e2e8f0;'>
            
            <div style='background-color: #1e3a8a; padding: 30px 20px; text-align: center;'>
                <h2 style='color: #ffffff; margin: 0; font-size: 24px; font-weight: 700; letter-spacing: 0.5px;'>MCFH SYSTEM HUB</h2>
                <p style='color: #93c5fd; margin: 6px 0 0 0; font-size: 11px; text-transform: uppercase; font-weight: 600; letter-spacing: 1px;'>Multichannel Customer Feedback & Sentiment Analysis</p>
            </div>

            <div style='padding: 35px 30px; line-height: 1.6; font-size: 15px;'>
                <p style='margin-top: 0; font-size: 16px;'>Xin chào,</p>
                <p>Hệ thống ghi nhận một yêu cầu thay đổi và khôi phục mật khẩu bảo mật phát ra từ tài khoản của bạn.</p>
                <p>Vui lòng bấm vào nút xác nhận bên dưới để truy cập vào trang thiết lập mật khẩu mới cho tài khoản:</p>
                
                <div style='text-align: center; margin: 32px 0;'>
                    <a href='{resetLink}' style='display: inline-block; padding: 14px 32px; background-color: #2563eb; color: #ffffff; text-decoration: none; font-weight: 600; border-radius: 8px; box-shadow: 0 4px 6px -1px rgba(37, 99, 235, 0.2); font-size: 15px; transition: background-color 0.2s;'>
                        Đặt Lại Mật Khẩu
                    </a>
                </div>

                <div style='background-color: #fffbeb; border-left: 4px solid #f59e0b; padding: 16px; margin: 25px 0; border-radius: 6px;'>
                    <p style='margin: 0; font-size: 13.5px; color: #b45309; line-height: 1.5;'>
                        ⚠️ <b>Lưu ý thời hạn:</b> Liên kết khôi phục này có giá trị sử dụng duy nhất trong vòng <b>15 phút</b>. Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua thư này để bảo vệ tài khoản an toàn.
                    </p>
                </div>

                <p style='font-size: 12.5px; color: #64748b; margin-bottom: 0; background: #f8fafc; padding: 12px; border-radius: 6px; border: 1px dashed #cbd5e1;'>
                    Nếu không tương tác được với nút bấm, bạn có thể sao chép liên kết dưới đây dán trực tiếp vào thanh địa chỉ trình duyệt:<br/>
                    <a href='{resetLink}' style='color: #2563eb; word-break: break-all; text-decoration: underline;'>{resetLink}</a>
                </p>
            </div>

            <div style='background-color: #f8fafc; padding: 24px; text-align: center; border-top: 1px solid #e2e8f0; font-size: 12px; color: #94a3b8;'>
                <p style='margin: 0;'>Đây là email tự động thông báo từ nền tảng hỗ trợ Social Listening MCFH.</p>
                <p style='margin: 6px 0 0 0;'>© {DateTime.Now.Year} MCFH Project Team. Mọi quyền được bảo lưu.</p>
            </div>
        </div>
    </div>";

            // Thực hiện gửi qua SMTP Gmail thật
            await _emailService.SendEmailAsync(model.Email, emailSubject, emailBody);

            return Ok(new { message = "Hệ thống đã gửi link khôi phục mật khẩu vào hòm thư của bạn." });
        }

        // ==========================================
        // UC-08: RESET PASSWORD (ĐẶT LẠI MẬT KHẨU MỚI)
        // ==========================================
        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            // 1. Kiểm tra mật khẩu khớp nhau không
            if (model.NewPassword != model.ConfirmPassword)
            {
                return BadRequest(new { message = "Mật khẩu mới và mật khẩu xác nhận không trùng khớp." });
            }

            // 2. Tìm kiếm và kiểm tra tính hợp lệ của Token
            var tokenRecord = await _context.PasswordResetTokens
                .FirstOrDefaultAsync(t => t.ResetToken == model.Token);

            if (tokenRecord == null || tokenRecord.IsUsed == true || tokenRecord.ExpiredAt < DateTime.Now)
            {
                return BadRequest(new { message = "Liên kết khôi phục không hợp lệ, đã được sử dụng hoặc đã hết hạn." });
            }

            // 3. Tìm User sở hữu token này
            var user = await _context.Users.FindAsync(tokenRecord.UserId);
            if (user == null)
            {
                return NotFound(new { message = "Không tìm thấy tài khoản liên quan." });
            }

            // 4. Tiến hành mã hóa mật khẩu mới và cập nhật trạng thái
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

            // Nếu tài khoản từng dùng Google nhưng giờ thiết lập mật khẩu, có thể cho phép hybrid đăng nhập cả 2 cách
            if (user.AuthProvider == "google")
            {
                user.AuthProvider = "local"; // Hoặc giữ nguyên tùy logic nghiệp vụ của team bạn
            }

            tokenRecord.IsUsed = true; // Đánh dấu token này đã xài xong, không cho xài lại lần 2

            _context.Users.Update(user);
            _context.PasswordResetTokens.Update(tokenRecord);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đặt lại mật khẩu thành công! Bạn có thể đăng nhập bằng mật khẩu mới." });
        }

        // ==========================================
        // UC-11: CHANGE PASSWORD (ĐỔI MẬT KHẨU KHI ĐANG ĐĂNG NHẬP)
        // ==========================================
        [Authorize] // Bắt buộc phải truyền JWT Token hợp lệ lên Header
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
        {
            if (model.NewPassword != model.ConfirmPassword)
            {
                return BadRequest(new { message = "Mật khẩu mới và mật khẩu xác nhận không trùng khớp." });
            }

            // 1. Lấy thông tin UserId ra từ các Claims nằm trong JWT Token của người dùng đang gọi API
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                              ?? User.FindFirst("userId")?.Value; // Tùy thuộc vào tên claim bạn đặt ở hàm GenerateJwtToken

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { message = "Vui lòng đăng nhập để thực hiện tính năng này." });
            }

            int userId = int.Parse(userIdClaim);

            // 2. Lấy thông tin user trong database ra đối chiếu
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { message = "Tài khoản không tồn tại." });
            }

            // 3. Kiểm tra mật khẩu hiện tại gõ vào có đúng với mật khẩu đã mã hóa trong DB không
            if (string.IsNullOrEmpty(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
            {
                return BadRequest(new { message = "Mật khẩu hiện tại không chính xác." });
            }

            // 4. Kiểm tra xem mật khẩu mới có bị trùng mật khẩu cũ không (Tối ưu nghiệp vụ)
            if (model.CurrentPassword == model.NewPassword)
            {
                return BadRequest(new { message = "Mật khẩu mới không được trùng với mật khẩu hiện tại." });
            }

            // 5. Cập nhật mật khẩu mới hóa băm vào DB
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
            _context.Users.Update(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Thay đổi mật khẩu thành công." });
        }
    }
}