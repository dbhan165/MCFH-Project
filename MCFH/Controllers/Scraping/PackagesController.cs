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

    [HttpPost("seed")]
    public IActionResult Seed([FromServices] MCFH.Models.McfhDbContext db)
    {
        if (!db.ScrapePackages.Any())
        {
            db.ScrapePackages.AddRange(
                new MCFH.Models.ScrapePackage { Code = "PACK_100", Name = "Gói Cơ bản", Description = "Phù hợp thử nghiệm", Price = 5000, Currency = "VND", DurationDays = 3, MaxItems = 100, MaxSources = 1, SortOrder = 1, IsActive = true, CreatedAt = System.DateTime.Now },
                new MCFH.Models.ScrapePackage { Code = "PACK_300", Name = "Gói Tiêu chuẩn", Description = "Cho dự án vừa và nhỏ", Price = 10000, Currency = "VND", DurationDays = 7, MaxItems = 300, MaxSources = 3, SortOrder = 2, IsActive = true, CreatedAt = System.DateTime.Now },
                new MCFH.Models.ScrapePackage { Code = "PACK_600", Name = "Gói Nâng cao", Description = "Cho dự án cần nhiều dữ liệu", Price = 20000, Currency = "VND", DurationDays = 14, MaxItems = 600, MaxSources = 5, SortOrder = 3, IsActive = true, CreatedAt = System.DateTime.Now },
                new MCFH.Models.ScrapePackage { Code = "FULL_UNLIMITED", Name = "Gói Toàn diện", Description = "Không giới hạn mentions", Price = 30000, Currency = "VND", DurationDays = 30, MaxItems = 9999, MaxSources = 99, SortOrder = 4, IsActive = true, CreatedAt = System.DateTime.Now }
            );
            db.SaveChanges();
            
            // Invalidate cache
            var catalog = HttpContext.RequestServices.GetService<ScrapePackageCatalog>();
            catalog?.Invalidate();
        }
        return Ok("Seeded");
    }
}
