using MCFH.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MCFH.Controllers;

[ApiController]
[Route("api/ai-model")]
[Authorize]
public class AiModelController : ControllerBase
{
    private readonly IAiSentimentService _aiSentiment;

    public AiModelController(IAiSentimentService aiSentiment)
    {
        _aiSentiment = aiSentiment;
    }

    [HttpPost("test")]
    public async Task<IActionResult> Test(CancellationToken cancellationToken)
    {
        var result = await _aiSentiment.TestConnectionAsync(cancellationToken);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    [HttpGet("status")]
    public IActionResult Status()
    {
        return Ok(new
        {
            configured = _aiSentiment.IsConfigured,
            message = _aiSentiment.IsConfigured
                ? "AI Model API key đã cấu hình. Gọi POST /api/ai-model/test để thử phân tích mẫu."
                : "Chưa cấu hình AiModel:ApiKey."
        });
    }
}
