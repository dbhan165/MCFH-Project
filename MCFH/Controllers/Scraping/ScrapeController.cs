// Controllers/Scraping/ScrapeController.cs
using MCFH.Services.Scraping;
using Microsoft.AspNetCore.Mvc;

namespace MCFH.Controllers.Scraping;

[ApiController]
[Route("api/scrape")]
public class ScrapeController : ControllerBase
{
    private readonly ScrapeByKeywordService _scrapeService;

    public ScrapeController(ScrapeByKeywordService scrapeService)
    {
        _scrapeService = scrapeService;
    }

    [HttpPost("by-keyword")]
    public async Task<IActionResult> ScrapeByKeyword([FromQuery] int projectId)
    {
        var result = await _scrapeService.ScrapeAsync(projectId);

        if (!string.IsNullOrEmpty(result.ErrorMessage))
            return BadRequest(result.ErrorMessage);

        return Ok(result);
    }
}