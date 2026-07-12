using System.ComponentModel.DataAnnotations;

namespace MCFHBackend.DTOs.AuthDtos;

public class VerifyEmailDto
{
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = null!;

    public string? OtpCode { get; set; }

    public string? VerificationToken { get; set; }
}
