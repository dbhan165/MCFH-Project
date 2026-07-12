using MCFH.Configuration;
using Microsoft.Extensions.Options;

namespace MCFH.Services;

public record AuthEmailMessage(string Subject, string HtmlBody);

public interface IAuthEmailTemplateService
{
    AuthEmailMessage BuildRegistrationVerificationEmail(string displayName, string otpCode);

    AuthEmailMessage BuildResendOtpEmail(string displayName, string otpCode);

    AuthEmailMessage BuildPasswordResetEmail(string resetLink);
}

public class AuthEmailTemplateService : IAuthEmailTemplateService
{
    private readonly AuthOptions _authOptions;

    public AuthEmailTemplateService(IOptions<AuthOptions> authOptions)
    {
        _authOptions = authOptions.Value;
    }

    public AuthEmailMessage BuildRegistrationVerificationEmail(string displayName, string otpCode)
    {
        var body = $@"
            <p style='margin-top: 0; font-size: 16px;'>Xin chào <b>{displayName}</b>,</p>
            <p>Chào mừng bạn đến với hệ thống lắng nghe mạng xã hội MCFH! Hệ thống đã ghi nhận yêu cầu khởi tạo tài khoản của bạn.</p>
            <p>Vui lòng sử dụng mã OTP gồm 6 chữ số dưới đây để hoàn tất bước xác thực tài khoản:</p>
            {BuildOtpBlock(otpCode)}
            {BuildWarningBanner($"⚠️ <b>Thời hạn mã:</b> Mã OTP này chỉ có hiệu lực trong vòng <b>{_authOptions.OtpExpiryMinutes} phút</b>. Tuyệt đối không chia sẻ mã này với bất kỳ ai.")}
            <p style='font-size: 13px; color: #64748b; text-align: center; margin-top: 25px;'>
                Quay lại trang đăng nhập MCFH và nhập mã OTP để kích hoạt tài khoản.
            </p>";

        return new AuthEmailMessage(
            "MCFH Hub - Xác thực tài khoản đăng ký mới",
            WrapLayout(body));
    }

    public AuthEmailMessage BuildResendOtpEmail(string displayName, string otpCode)
    {
        var body = $@"
            <p style='margin-top: 0; font-size: 16px;'>Xin chào <b>{displayName}</b>,</p>
            <p>Hệ thống nhận được yêu cầu <b>gửi lại mã xác thực OTP</b> cho tài khoản đăng ký trên hệ thống MCFH.</p>
            <p>Dưới đây là mã OTP mới của bạn:</p>
            {BuildOtpBlock(otpCode)}
            {BuildWarningBanner($"⚠️ <b>Lưu ý:</b> Mã OTP cũ đã bị vô hiệu hóa. Mã mới có hiệu lực trong <b>{_authOptions.OtpExpiryMinutes} phút</b>.")}
            <p style='font-size: 13px; color: #64748b; text-align: center; margin-top: 25px;'>
                Quay lại trang đăng nhập MCFH và nhập mã OTP để kích hoạt tài khoản.
            </p>";

        return new AuthEmailMessage(
            "MCFH Hub - Gửi lại mã xác thực tài khoản",
            WrapLayout(body));
    }

    public AuthEmailMessage BuildPasswordResetEmail(string resetLink)
    {
        var body = $@"
            <p style='margin-top: 0; font-size: 16px;'>Xin chào,</p>
            <p>Hệ thống ghi nhận một yêu cầu thay đổi và khôi phục mật khẩu bảo mật phát ra từ tài khoản của bạn.</p>
            <p>Vui lòng bấm vào nút xác nhận bên dưới để truy cập vào trang thiết lập mật khẩu mới cho tài khoản:</p>
            <div style='text-align: center; margin: 32px 0;'>
                <a href='{resetLink}' style='display: inline-block; padding: 14px 32px; background-color: #2563eb; color: #ffffff; text-decoration: none; font-weight: 600; border-radius: 8px; box-shadow: 0 4px 6px -1px rgba(37, 99, 235, 0.2); font-size: 15px;'>
                    Đặt Lại Mật Khẩu
                </a>
            </div>
            {BuildWarningBanner($"⚠️ <b>Lưu ý thời hạn:</b> Liên kết khôi phục này có giá trị sử dụng duy nhất trong vòng <b>{_authOptions.OtpExpiryMinutes} phút</b>. Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua thư này để bảo vệ tài khoản an toàn.")}
            <p style='font-size: 12.5px; color: #64748b; margin-bottom: 0; background: #f8fafc; padding: 12px; border-radius: 6px; border: 1px dashed #cbd5e1;'>
                Nếu không tương tác được với nút bấm, bạn có thể sao chép liên kết dưới đây dán trực tiếp vào thanh địa chỉ trình duyệt:<br/>
                <a href='{resetLink}' style='color: #2563eb; word-break: break-all; text-decoration: underline;'>{resetLink}</a>
            </p>";

        return new AuthEmailMessage(
            "MCFH Hub - Yêu cầu đặt lại mật khẩu tài khoản",
            WrapLayout(body));
    }

    private static string BuildOtpBlock(string otpCode) => $@"
            <div style='text-align: center; margin: 30px 0;'>
                <div style='display: inline-block; background-color: #f8fafc; border: 2px dashed #cbd5e1; padding: 14px 40px; font-size: 32px; font-weight: 700; letter-spacing: 6px; color: #1e3a8a; border-radius: 8px;'>
                    {otpCode}
                </div>
            </div>";

    private static string BuildWarningBanner(string text) => $@"
            <div style='background-color: #fffbeb; border-left: 4px solid #f59e0b; padding: 16px; margin: 25px 0; border-radius: 6px;'>
                <p style='margin: 0; font-size: 13.5px; color: #b45309; line-height: 1.5;'>
                    {text}
                </p>
            </div>";

    private static string WrapLayout(string bodyContent) => $@"
<div style='font-family: -apple-system, BlinkMacSystemFont, ""Segoe UI"", Roboto, Helvetica, Arial, sans-serif; background-color: #f1f5f9; padding: 40px 20px; color: #334155;'>
    <div style='max-width: 580px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(15, 23, 42, 0.05); border: 1px solid #e2e8f0;'>
        <div style='background-color: #1e3a8a; padding: 30px 20px; text-align: center;'>
            <h2 style='color: #ffffff; margin: 0; font-size: 24px; font-weight: 700; letter-spacing: 0.5px;'>MCFH SYSTEM HUB</h2>
            <p style='color: #93c5fd; margin: 6px 0 0 0; font-size: 11px; text-transform: uppercase; font-weight: 600; letter-spacing: 1px;'>Multichannel Customer Feedback & Sentiment Analysis</p>
        </div>
        <div style='padding: 35px 30px; line-height: 1.6; font-size: 15px;'>
            {bodyContent}
        </div>
        <div style='background-color: #f8fafc; padding: 24px; text-align: center; border-top: 1px solid #e2e8f0; font-size: 12px; color: #94a3b8;'>
            <p style='margin: 0;'>Đây là email tự động thông báo từ nền tảng hỗ trợ Social Listening MCFH.</p>
            <p style='margin: 6px 0 0 0;'>© {DateTime.UtcNow.Year} MCFH Project Team. Mọi quyền được bảo lưu.</p>
        </div>
    </div>
</div>";
}
