namespace MCFHBackend.DTOs.AuthDtos
{
    public class VerifyEmailDto
    {
        public string Email { get; set; } = null!;
        public string? OtpCode { get; set; }
        public string? VerificationToken { get; set; } // Dùng cho trường hợp click link
    }
}
