using System.ComponentModel.DataAnnotations;

namespace MCFHBackend.DTOs.AuthDtos;

public class RegisterDto
{
    [Required(ErrorMessage = "Email không được để trống.")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Mật khẩu không được để trống.")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "Họ và tên không được để trống.")]
    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }
}
