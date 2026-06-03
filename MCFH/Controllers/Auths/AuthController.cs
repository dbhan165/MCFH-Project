using MCFHBackend.DTOs;
using MCFH.Models; // Đổi lại đúng namespace của folder Models của bạn
using MCFHBackend.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MCFH.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly McfhDbContext _context; // Đổi tên Class Context cho đúng với dự án của bạn
        private readonly IConfiguration _config;

        public AuthController(McfhDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // ==========================================
        // UC-03: REGISTER ACCOUNT
        // ==========================================
        // Thay thế đoạn code bên trong API [HttpPost("register")] cũ của bạn:

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (await _context.Users.AnyAsync(u => u.Email == model.Email))
            {
                return BadRequest(new { message = "Email này đã được sử dụng trên hệ thống." });
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);

            var newUser = new User
            {
                Email = model.Email,
                PasswordHash = passwordHash,
                FullName = model.FullName,
                Phone = model.Phone,
                AuthProvider = "local",
                SystemRole = "Client",
                IsVerified = false,     // Mặc định chưa xác thực
                IsBanned = false,
                CreatedAt = DateTime.Now
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync(); // Lưu user trước để lấy được newUser.UserId tự sinh

            // --- ĐOẠN THÊM MỚI: SINH MÃ OTP/TOKEN XÁC THỰC ---
            // 1. Tạo OTP ngẫu nhiên gồm 6 chữ số
            string randomOtp = new Random().Next(100000, 999999).ToString();
            // 2. Tạo một chuỗi Guid ngẫu nhiên làm Token kích hoạt qua link url
            string randomToken = Guid.NewGuid().ToString();

            var emailVerification = new EmailVerification
            {
                UserId = newUser.UserId,
                OtpCode = randomOtp,
                VerificationToken = randomToken,
                ExpiredAt = DateTime.Now.AddMinutes(15), // Mã có hiệu lực trong 15 phút
                IsUsed = false,
                CreatedAt = DateTime.Now
            };

            _context.EmailVerifications.Add(emailVerification);
            await _context.SaveChangesAsync();

            // 3. Gọi hàm gửi Email (Tạm thời in ra màn hình Console để test, tránh nghẽn do cấu hình SMTP)
            SendMockEmail(newUser.Email, randomOtp, randomToken);

            return Ok(new
            {
                message = "Đăng ký thành công! Hệ thống đã gửi mã OTP xác thực tới email của bạn (Hiệu lực 15 phút)."
            });
        }

        // Hàm bổ trợ giả lập gửi Email - Hãy viết nó nằm bên dưới hàm Register
        private void SendMockEmail(string email, string otp, string token)
        {
            Console.WriteLine("==================================================");
            Console.WriteLine($"[EMAIL SIMULATOR] Gửi tới: {email}");
            Console.WriteLine($"Mã OTP của bạn là: {otp}");
            Console.WriteLine($"Hoặc click vào link để xác thực: https://localhost:7000/api/auth/verify-email?token={token}");
            Console.WriteLine("==================================================");
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
    }
}