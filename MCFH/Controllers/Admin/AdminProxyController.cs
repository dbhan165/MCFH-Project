using MCFH.DTOs;
using MCFH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace MCFH.Controllers.Admin;

/// <summary>UC-70–73 (Proxy CRUD), UC-74 (giám sát scraping jobs).</summary>
[ApiController]
[Route("api/admin")]
[Authorize]
public class AdminProxyController : ControllerBase
{
    private readonly ProxyAdminService _proxyAdmin;

    public AdminProxyController(ProxyAdminService proxyAdmin)
    {
        _proxyAdmin = proxyAdmin;
    }

    private int GetUserId() =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    [HttpGet("proxies")]
    public async Task<IActionResult> ListProxies()
    {
        if (GetUserId() <= 0)
            return Unauthorized();

        var list = await _proxyAdmin.ListProxiesAsync(GetUserId());
        return Ok(list);
    }

    [HttpPost("proxies")]
    public async Task<IActionResult> CreateProxy([FromBody] UpsertSystemProxyDto dto)
    {
        if (GetUserId() <= 0)
            return Unauthorized();

        var result = await _proxyAdmin.CreateProxyAsync(GetUserId(), dto);
        if (result == null)
            return Forbid();

        return Ok(result);
    }

    [HttpPut("proxies/{proxyId}")]
    public async Task<IActionResult> UpdateProxy(int proxyId, [FromBody] UpsertSystemProxyDto dto)
    {
        if (GetUserId() <= 0)
            return Unauthorized();

        var result = await _proxyAdmin.UpdateProxyAsync(GetUserId(), proxyId, dto);
        if (result == null)
            return NotFound(new { message = "Không tìm thấy proxy hoặc không có quyền Admin." });

        return Ok(result);
    }

    [HttpDelete("proxies/{proxyId}")]
    public async Task<IActionResult> DeleteProxy(int proxyId)
    {
        if (GetUserId() <= 0)
            return Unauthorized();

        var ok = await _proxyAdmin.DeleteProxyAsync(GetUserId(), proxyId);
        if (!ok)
            return NotFound(new { message = "Không tìm thấy proxy hoặc không có quyền Admin." });

        return Ok(new { message = "Đã xóa proxy." });
    }

    [HttpGet("scraping-jobs")]
    public async Task<IActionResult> ListScrapingJobs()
    {
        if (GetUserId() <= 0)
            return Unauthorized();

        return Ok(await _proxyAdmin.ListScrapingJobsAsync(GetUserId()));
    }
}
