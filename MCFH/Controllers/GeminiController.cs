using MCFH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MCFH.Controllers;

[ApiController]
[Route("api/gemini")]
[Authorize]
public class GeminiController : ControllerBase
{
    private readonly IGeminiSentimentService _gemini;

    public GeminiController(IGeminiSentimentService gemini)
    {
        _gemini = gemini;
    }

    /// <summary>Kiểm tra kết nối Gemini với dữ liệu mẫu (caption + 3 comment).</summary>
    [HttpPost("test")]
    public async Task<IActionResult> Test(CancellationToken cancellationToken)
    {
        var result = await _gemini.TestConnectionAsync(cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("status")]
    public IActionResult Status()
    {
        return Ok(new
        {
            configured = _gemini.IsConfigured,
            message = _gemini.IsConfigured
                ? "Gemini API key đã cấu hình. Gọi POST /api/gemini/test để thử phân tích mẫu."
                : "Chưa cấu hình Gemini:ApiKey."
        });
    }
}
