using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCFH.Configuration;
using MCFH.DTOs.ProjectDtos;
using MCFH.Services.Scraping;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.DependencyInjection;
using MCFH.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace MCFH.Services;

public interface IAiSentimentService
{
    bool IsConfigured { get; }
    Task<SentimentAnalysisResult?> AnalyzeAsync(
        string platform,
        string? author,
        string content,
        IReadOnlyList<string> comments,
        string? combinedText = null,
        CancellationToken cancellationToken = default);
    Task<AiModelTestResultDto> TestConnectionAsync(CancellationToken cancellationToken = default);
}

public class AiSentimentService : IAiSentimentService
{
    private readonly HttpClient _httpClient;
    private readonly AiModelOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AiSentimentService> _logger;
    private readonly ICommentBundleStorage _bundleStorage;

    /// <summary>
    /// Sau khi mọi model đều 429, tạm ngưng gọi AI trong một khoảng cooldown
    /// (thay vì khóa vĩnh viễn đến khi restart) rồi tự thử lại.
    /// </summary>
    private static long _quotaCooldownUntilTicks;
    private static readonly TimeSpan QuotaCooldown = TimeSpan.FromMinutes(15);

    private static bool IsQuotaCoolingDown =>
        DateTime.UtcNow.Ticks < Interlocked.Read(ref _quotaCooldownUntilTicks);

    private static void StartQuotaCooldown() =>
        Interlocked.Exchange(ref _quotaCooldownUntilTicks, DateTime.UtcNow.Add(QuotaCooldown).Ticks);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AiSentimentService(
        HttpClient httpClient,
        IOptions<AiModelOptions> options,
        IServiceScopeFactory scopeFactory,
        IMemoryCache cache,
        ILogger<AiSentimentService> logger,
        ICommentBundleStorage bundleStorage)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _cache = cache;
        _logger = logger;
        _bundleStorage = bundleStorage;
    }



    private async Task<(string ApiKey, string Model)> ResolveSettingsAsync(CancellationToken ct)
    {
        var cacheKey = "GeminiSettings";
        if (_cache.TryGetValue(cacheKey, out (string ApiKey, string Model) cachedSettings))
        {
            return cachedSettings;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<McfhDbContext>();
        
        // Giữ key cũ GEMINI_* trong DB để không phá dữ liệu SystemSettings đã có.
        var settings = await db.SystemSettings
            .Where(s => s.SettingKey == "AI_MODEL_API_KEY" || s.SettingKey == "AI_MODEL_NAME"
                     || s.SettingKey == "GEMINI_API_KEY" || s.SettingKey == "GEMINI_MODEL")
            .ToDictionaryAsync(s => s.SettingKey, s => s.SettingValue, ct);

        settings.TryGetValue("AI_MODEL_API_KEY", out var dbKey);
        if (string.IsNullOrWhiteSpace(dbKey))
            settings.TryGetValue("GEMINI_API_KEY", out dbKey);

        settings.TryGetValue("AI_MODEL_NAME", out var dbModel);
        if (string.IsNullOrWhiteSpace(dbModel))
            settings.TryGetValue("GEMINI_MODEL", out dbModel);

        var result = (
            !string.IsNullOrWhiteSpace(dbKey) ? dbKey : _options.ApiKey,
            !string.IsNullOrWhiteSpace(dbModel) ? dbModel : _options.Model
        );

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

        return result;
    }

    public bool IsConfigured => true; // Tránh dùng biến tĩnh để check config vì config có thể đổi trong DB

    public async Task<AiModelTestResultDto> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var (apiKey, model) = await ResolveSettingsAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return new AiModelTestResultDto
            {
                Configured = false,
                Success = false,
                Message = "Chưa cấu hình API Key trong System Settings."
            };
        }

        if (IsQuotaCoolingDown)
        {
            return new AiModelTestResultDto
            {
                Configured = true,
                Success = false,
                Message = "AI Model đang tạm ngưng do hết quota — hệ thống sẽ tự thử lại sau ít phút."
            };
        }

        var sampleComments = new[]
        {
            "Sản phẩm này rất tốt, mình rất hài lòng!",
            "Chất lượng kém, thất vọng quá.",
            "Giá hợp lý, giao hàng nhanh."
        };

        var combined = _bundleStorage.BuildCombinedAnalysisText(
            "Video review sản phẩm mới từ thương hiệu.",
            sampleComments);

        var result = await AnalyzeAsync(
            "test",
            "MCFH",
            "Video review sản phẩm mới từ thương hiệu.",
            sampleComments,
            combined,
            cancellationToken);

        if (result?.UsedAiModel == true)
        {
            return new AiModelTestResultDto
            {
                Configured = true,
                Success = true,
                ModelUsed = _lastSuccessfulModel,
                Message = $"AI Model hoạt động bình thường (model: {_lastSuccessfulModel}).",
                SampleSummary = result.Summary,
                SampleSentiment = result.Sentiment
            };
        }

        return new AiModelTestResultDto
        {
            Configured = true,
            Success = false,
            ModelUsed = _lastAttemptedModel,
            Message = _lastErrorMessage ?? "Gọi AI Model thất bại — kiểm tra API key, quota hoặc log server."
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
        var (apiKey, dynamicModel) = await ResolveSettingsAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey) || IsQuotaCoolingDown)
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

        var models = GetModelCandidates(dynamicModel).ToList();
        var quotaHits = 0;

        foreach (var model in models)
        {
            _lastAttemptedModel = model;
            var baseUrl = string.IsNullOrWhiteSpace(_options.BaseUrl) ? "https://api.tokenrouter.com/v1" : _options.BaseUrl;
            var url = $"{baseUrl.TrimEnd('/')}/chat/completions";

            var requestBody = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                response_format = new { type = "json_object" },
                temperature = 0.2
            };

            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                request.Content = JsonContent.Create(requestBody);

                using var response = await _httpClient.SendAsync(request, cancellationToken);
                var raw = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _lastErrorMessage = ExtractApiError(raw) ?? $"HTTP {(int)response.StatusCode}";

                    if ((int)response.StatusCode == 429)
                    {
                        quotaHits++;
                        _logger.LogWarning("AI model {Model} hết quota — thử model khác.", model);
                        continue;
                    }

                    _logger.LogWarning("AI API lỗi {StatusCode} ({Model}): {Body}",
                        response.StatusCode, model, raw);
                    continue;
                }

                var openAiResponse = JsonSerializer.Deserialize<OpenAiChatResponse>(raw, JsonOptions);
                var text = openAiResponse?.Choices?.FirstOrDefault()?.Message?.Content;

                if (string.IsNullOrWhiteSpace(text))
                {
                    _lastErrorMessage = "AI trả về rỗng.";
                    _logger.LogWarning("AI trả về rỗng (model {Model}).", model);
                    continue;
                }

                var parsed = JsonSerializer.Deserialize<AiSentimentPayload>(text, JsonOptions);
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
                    UsedAiModel = true
                };
            }
            catch (Exception ex)
            {
                _lastErrorMessage = ex.Message;
                _logger.LogError(ex, "Gọi AI sentiment thất bại (model {Model}).", model);
            }
        }

        if (quotaHits > 0 && quotaHits >= models.Count)
        {
            StartQuotaCooldown();
            _logger.LogWarning(
                "Mọi model AI đều hết quota — chuyển rule-based, tự thử lại sau {Minutes} phút.",
                QuotaCooldown.TotalMinutes);
        }

        return null;
    }

    private IEnumerable<string> GetModelCandidates(string dynamicModel)
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

        Add(dynamicModel);
        Add(_options.Model);
        foreach (var fb in _options.FallbackModels ?? Array.Empty<string>())
            Add(fb);

        return list;
    }

    private static string? ExtractApiError(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("error", out var error))
            {
                if (error.ValueKind == JsonValueKind.Object && error.TryGetProperty("message", out var message))
                {
                    return message.GetString();
                }
                else if (error.ValueKind == JsonValueKind.String)
                {
                    return error.GetString();
                }
            }
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

    private sealed class OpenAiChatResponse
    {
        public List<OpenAiChoice>? Choices { get; set; }
    }

    private sealed class OpenAiChoice
    {
        public OpenAiMessage? Message { get; set; }
    }

    private sealed class OpenAiMessage
    {
        public string? Content { get; set; }
    }

    private sealed class AiSentimentPayload
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
