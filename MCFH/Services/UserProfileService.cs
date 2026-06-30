using MCFHBackend.DTOs.AuthDtos;
using MCFH.Models;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Services;

public class UserProfileService
{
    private readonly McfhDbContext _context;

    public UserProfileService(McfhDbContext context) => _context = context;

    public async Task<ProfileResponseDto?> GetProfileAsync(int userId)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId);
        return user == null ? null : MapProfile(user);
    }

    public async Task<ProfileResponseDto?> UpdateProfileAsync(int userId, UpdateProfileDto model)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
        if (user == null) return null;

        user.FullName = model.FullName.Trim();
        if (model.Phone != null) user.Phone = model.Phone;
        if (model.AvatarUrl != null) user.AvatarUrl = model.AvatarUrl;

        await _context.SaveChangesAsync();
        return MapProfile(user);
    }

    private static ProfileResponseDto MapProfile(User user) => new()
    {
        UserId = user.UserId,
        Email = user.Email,
        FullName = user.FullName,
        Phone = user.Phone,
        AvatarUrl = user.AvatarUrl,
        AuthProvider = user.AuthProvider,
        Role = user.SystemRole
    };
}
