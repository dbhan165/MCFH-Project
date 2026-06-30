using MCFH.DTOs;
using MCFHBackend.DTOs.AuthDtos;
using MCFH.Models;
using MCFH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MCFH.Controllers.Auths;

/// <summary>
/// Profile API — tách riêng để không sửa AuthController gốc.
/// </summary>
[ApiController]
[Route("api/auth")]
[Authorize]
public class AuthProfileController : ControllerBase
{
    private readonly UserProfileService _profileService;

    public AuthProfileController(McfhDbContext db) => _profileService = new UserProfileService(db);

    private int? GetUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return claim != null ? int.Parse(claim) : null;
    }

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();
        var profile = await _profileService.GetProfileAsync(userId.Value);
        if (profile == null) return NotFound();
        return Ok(profile);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto model)
    {
        if (string.IsNullOrWhiteSpace(model.FullName))
            return BadRequest(new { message = "Họ và tên không được để trống." });

        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var profile = await _profileService.UpdateProfileAsync(userId.Value, model);
        if (profile == null) return NotFound();
        return Ok(profile);
    }
}
