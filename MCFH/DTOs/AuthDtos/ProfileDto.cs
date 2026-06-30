namespace MCFHBackend.DTOs.AuthDtos
{
    public class ProfileResponseDto
    {
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
        public string AuthProvider { get; set; } = null!;
        public string Role { get; set; } = null!;
    }

    public class UpdateProfileDto
    {
        public string? FullName { get; set; }
        public string? Phone { get; set; }
        public string? AvatarUrl { get; set; }
    }
}
