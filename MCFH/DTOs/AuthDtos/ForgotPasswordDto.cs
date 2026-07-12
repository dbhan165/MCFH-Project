using System.ComponentModel.DataAnnotations;

namespace MCFHBackend.DTOs.AuthDtos;

public class ForgotPasswordDto
{
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = null!;
}
