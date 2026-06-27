using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCFH.Configuration;
using MCFH.DTOs.ProjectDtos;
using MCFH.Services.Scraping;
using Microsoft.Extensions.Options;

namespace MCFH.Services;

public interface IGeminiSentimentService
{
    bool IsConfigured { get; }
    Task<SentimentAnalysisResult?> AnalyzeAsync(
        string platform,
        string? author,
        string content,
        IReadOnlyList<string> comments,
        string? combinedText = null,
        CancellationToken cancellationToken = default);
    Task<GeminiTestResultDto> TestConnectionAsync(CancellationToken cancellationToken = default);
}

public class GeminiSentimentService : IGeminiSentimentService
{
    private readonly HttpClient _httpClient;
    private readonly GeminiOptions _options;
    private readonly ILogger<GeminiSentimentService> _logger;

    /// <summary>Sau khi mọi model đều 429, bỏ qua Gemini cho đến khi restart server.</summary>
    private static volatile bool _quotaExhausted;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public GeminiSentimentService(
        HttpClient httpClient,
        IOptions<GeminiOptions> options,
        ILogger<GeminiSentimentService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<GeminiTestResultDto> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new GeminiTestResultDto
            {
                Configured = false,
                Success = false,
                Message = "Chưa cấu hình Gemini:ApiKey trong appsettings."
            };
        }

        if (_quotaExhausted)
        {
            return new GeminiTestResultDto
            {
                Configured = true,
                Success = false,
                Message = "Gemini đang bị tạm khóa do hết quota — restart backend rồi thử lại."
            };
        }

        var sampleComments = new[]
        {
            "Sản phẩm này rất tốt, mình rất hài lòng!",
            "Chất lượng kém, thất vọng quá.",
            "Giá hợp lý, giao hàng nhanh."
        };

        var combined = CommentBundleStorage.BuildCombinedAnalysisText(
            "Video review sản phẩm mới từ thương hiệu.",
            sampleComments);

        var result = await AnalyzeAsync(
            "test",
            "MCFH",
            "Video review sản phẩm mới từ thương hiệu.",
            sampleComments,
            combined,
            cancellationToken);

        if (result?.UsedGemini == true)
        {
            return new GeminiTestResultDto
            {
                Configured = true,
                Success = true,
                ModelUsed = _lastSuccessfulModel,
                Message = $"Gemini hoạt động bình thường (model: {_lastSuccessfulModel}).",
                SampleSummary = result.Summary,
                SampleSentiment = result.Sentiment
            };
        }

        return new GeminiTestResultDto
        {
            Configured = true,
            Success = false,
            ModelUsed = _lastAttemptedModel,
            Message = _lastErrorMessage ?? "Gọi Gemini thất bại — kiểm tra API key, quota hoặc log server."
        };
    }

    private static string? _lastSuccessfulModel;
    private static string? _lastAttemptedModel;
    private static string? _lastErrorMessage;

    public async Task<SentimentAnalysisResult?> AnalyzeAsync(
        string platform,
        string? author,
        string content,
        IReadOnlyList<string> comments,
        string? combinedText = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || _quotaExhausted)
            return null;

        var commentsBlock = comments.Count > 0
            ? string.Join("\n", comments.Take(_options.MaxCommentsInPrompt).Select((c, i) => $"{i + 1}. {c}"))
            : "(chưa có bình luận)";

        var fullBlock = !string.IsNullOrWhiteSpace(combinedText)
            ? combinedText
            : $"{content}\n\n{commentsBlock}";

        var prompt =
            "Bạn là chuyên gia social listening tiếng Việt. Phân tích TỔNG THỂ một bài đăng dựa trên caption và TOÀN BỘ bình luận (đã gom thành một khối).\n\n" +
            $"Nền tảng: {platform}\n" +
            $"Tác giả: {author ?? "không rõ"}\n" +
            $"Số bình luận đã thu thập: {comments.Count}\n\n" +
            "Dữ liệu:\n" +
            $"{fullBlock}\n\n" +
            "Trả về JSON duy nhất (không markdown):\n" +
            "{\"sentiment\":\"positive|negative|neutral\",\"confidence\":0.85,\"isCrisisAlert\":false,\"summary\":\"...\"}\n\n" +
            "Quy tắc:\n" +
            "- sentiment: đánh giá CHỦ ĐẠO từ cả bài + bình luận (ưu tiên bình luận nếu nhiều và rõ ràng)\n" +
            "- summary: 2-4 câu tiếng Việt mô tả TÌNH HÌNH/Ý KIẾN CỘNG ĐỒNG (khen/chê/tranh luận/chủ đề nóng), không chỉ liệt kê\n" +
            "- isCrisisAlert: true nếu có nguy cơ khủng hoảng truyền thông\n" +
            "- confidence: 0 đến 1";

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } }
            },
            generationConfig = new
            {
                responseMimeType = "application/json",
                temperature = 0.2
            }
        };

        var models = GetModelCandidates().ToList();
        var quotaHits = 0;

        foreach (var model in models)
        {
            _lastAttemptedModel = model;
            var url =
                $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={Uri.EscapeDataString(_options.ApiKey)}";

            try
            {
                using var response = await _httpClient.PostAsJsonAsync(url, requestBody, cancellationToken);
                var raw = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _lastErrorMessage = ExtractApiError(raw) ?? $"HTTP {(int)response.StatusCode}";

                    if ((int)response.StatusCode == 429)
                    {
                        quotaHits++;
                        _logger.LogWarning("Gemini model {Model} hết quota — thử model khác.", model);
                        continue;
                    }

                    _logger.LogWarning("Gemini API lỗi {StatusCode} ({Model}): {Body}",
                        response.StatusCode, model, raw);
                    continue;
                }

                var geminiResponse = JsonSerializer.Deserialize<GeminiGenerateResponse>(raw, JsonOptions);
                var text = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text;

                if (string.IsNullOrWhiteSpace(text))
                {
                    _lastErrorMessage = "Gemini trả về rỗng.";
                    _logger.LogWarning("Gemini trả về rỗng (model {Model}).", model);
                    continue;
                }

                var parsed = JsonSerializer.Deserialize<GeminiSentimentPayload>(text, JsonOptions);
                if (parsed == null)
                    continue;

                _lastSuccessfulModel = model;
                _lastErrorMessage = null;

                return new SentimentAnalysisResult
                {
                    Sentiment = NormalizeSentiment(parsed.Sentiment),
                    Confidence = ClampConfidence(parsed.Confidence),
                    IsCrisisAlert = parsed.IsCrisisAlert,
                    Summary = parsed.Summary,
                    UsedGemini = true
                };
            }
            catch (Exception ex)
            {
                _lastErrorMessage = ex.Message;
                _logger.LogError(ex, "Gọi Gemini sentiment thất bại (model {Model}).", model);
            }
        }

        if (quotaHits > 0 && quotaHits >= models.Count)
        {
            _quotaExhausted = true;
            _logger.LogWarning("Mọi model Gemini đều hết quota — chuyển rule-based cho các bài còn lại.");
        }

        return null;
    }

    private IEnumerable<string> GetModelCandidates()
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var list = new List<string>();

        void Add(string? model)
        {
            if (string.IsNullOrWhiteSpace(model)) return;
            var trimmed = model.Trim();
            if (seen.Add(trimmed))
                list.Add(trimmed);
        }

        Add(_options.Model);
        foreach (var fallback in _options.FallbackModels ?? [])
            Add(fallback);

        return list;
    }

    private static string? ExtractApiError(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message))
                return message.GetString();
        }
        catch
        {
            // ignore parse errors
        }

        return null;
    }

    private static string NormalizeSentiment(string? sentiment)
    {
        return sentiment?.Trim().ToLowerInvariant() switch
        {
            "positive" or "pos" or "tích cực" or "tich cuc" => "positive",
            "negative" or "neg" or "tiêu cực" or "tieu cuc" => "negative",
            _ => "neutral"
        };
    }

    private static double ClampConfidence(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
            return 0.5;
        return Math.Clamp(value, 0, 1);
    }

    private sealed class GeminiGenerateResponse
    {
        public List<GeminiCandidate>? Candidates { get; set; }
    }

    private sealed class GeminiCandidate
    {
        public GeminiContent? Content { get; set; }
    }

    private sealed class GeminiContent
    {
        public List<GeminiPart>? Parts { get; set; }
    }

    private sealed class GeminiPart
    {
        public string? Text { get; set; }
    }

    private sealed class GeminiSentimentPayload
    {
        [JsonPropertyName("sentiment")]
        public string? Sentiment { get; set; }

        [JsonPropertyName("confidence")]
        public double Confidence { get; set; }

        [JsonPropertyName("isCrisisAlert")]
        public bool IsCrisisAlert { get; set; }

        [JsonPropertyName("summary")]
        public string? Summary { get; set; }
    }
}
