namespace MCFHBackend.DTOs.AuthDtos
{
    public class AuthResponseDto
    {
        public string Token { get; set; } = null!;
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public string AuthProvider { get; set; } = null!;
        public string Role { get; set; } = null!;
    }
}
