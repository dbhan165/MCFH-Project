// Controllers/Scraping/ScrapeController.cs

using MCFH.Models.Scraping;

using MCFH.Services.Scraping;

using Microsoft.AspNetCore.Authorization;

using Microsoft.AspNetCore.Mvc;

using System.Security.Claims;



namespace MCFH.Controllers.Scraping;



[ApiController]

[Route("api/scrape")]

[Authorize]

public class ScrapeController : ControllerBase

{

    private readonly ScrapeJobRunner _jobRunner;



    public ScrapeController(ScrapeJobRunner jobRunner)

    {

        _jobRunner = jobRunner;

    }



    private int GetUserId()

    {

        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        return int.TryParse(claim, out var id) ? id : 0;

    }



    [HttpPost("by-keyword")]

    public async Task<IActionResult> StartScrapeByKeyword([FromQuery] int projectId)

    {

        var userId = GetUserId();

        if (userId <= 0)

            return Unauthorized();



        var jobId = await _jobRunner.StartAsync(projectId, userId);

        if (jobId == null)

            return NotFound(new { message = "Project không tồn tại hoặc bạn không có quyền truy cập." });



        return Accepted(new ScrapeJobStartResponse { JobId = jobId, ProjectId = projectId });

    }



    [HttpGet("jobs/{jobId}")]

    public IActionResult GetScrapeJob(string jobId)

    {

        var userId = GetUserId();

        if (userId <= 0)

            return Unauthorized();



        var job = _jobRunner.GetJob(jobId, userId);

        if (job == null)

            return NotFound(new { message = "Job không tồn tại hoặc đã hết hạn." });



        return Ok(job.ToDto());

    }

    [HttpPost("jobs/{jobId}/cancel")]
    public IActionResult CancelScrapeJob(string jobId)
    {
        var userId = GetUserId();
        if (userId <= 0)
            return Unauthorized();

        if (!_jobRunner.CancelJob(jobId, userId))
            return NotFound(new { message = "Job không tồn tại, đã kết thúc hoặc bạn không có quyền." });

        return Ok(new { message = "Đang dừng job — dữ liệu đã cào sẽ được giữ lại." });
    }

}

