namespace MCFHBackend.DTOs.AuthDtos
{
    public class RegisterDto
{
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
    }
}
