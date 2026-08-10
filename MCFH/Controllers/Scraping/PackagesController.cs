using MCFH.DTOs;
using MCFH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MCFH.Controllers.Scraping;

/// <summary>Public catalog — danh sách gói scrape đang active. Không cần đăng nhập.</summary>
[ApiController]
[Route("api/packages")]
[AllowAnonymous]
public class PackagesController : ControllerBase
{
    private readonly ScrapePackagePublicService _service;

    public PackagesController(ScrapePackagePublicService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<IActionResult> List()
    {
        var packages = await _service.ListActiveAsync();
        return Ok(packages);
    }
}
