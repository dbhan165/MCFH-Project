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
    Task<ReportInsightsResultDto?> GenerateReportInsightsAsync(
        string projectName,
        int totalMentions,
        double nsrScore,
        string topChannelInfo,
        string topNegativeAspects,
        List<string>? pinnedMentions,
        List<string>? topReputableMentions = null,
        List<string>? topInfluencers = null,
        CancellationToken cancellationToken = default);
}

public class AiSentimentService : IAiSentimentService
{
    private readonly HttpClient _httpClient;
    private readonly AiModelOptions _options;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AiSentimentService> _logger;
    private readonly ICommentBundleStorage _bundleStorage;
    private readonly EncryptionService _encryption;

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
        ICommentBundleStorage bundleStorage,
        EncryptionService encryption)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _scopeFactory = scopeFactory;
        _cache = cache;
        _logger = logger;
        _bundleStorage = bundleStorage;
        _encryption = encryption;
    }



    private async Task<(string ApiKey, string Model, string BaseUrl)> ResolveSettingsAsync(CancellationToken ct)
    {
        var cacheKey = "GeminiSettings";
        if (_cache.TryGetValue(cacheKey, out (string ApiKey, string Model, string BaseUrl) cachedSettings))
        {
            return cachedSettings;
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<McfhDbContext>();
        
        // Giữ key cũ GEMINI_* trong DB để không phá dữ liệu SystemSettings đã có.
        var settingsList = await db.SystemSettings
            .Where(s => s.SettingKey == "AI_MODEL_API_KEY" || s.SettingKey == "AI_MODEL_NAME"
                     || s.SettingKey == "GEMINI_API_KEY" || s.SettingKey == "GEMINI_MODEL"
                     || s.SettingKey == "AI_MODEL_BASE_URL")
            .ToListAsync(ct);

        var settings = settingsList.ToDictionary(
            s => s.SettingKey, 
            s => s.IsEncrypted == true ? _encryption.Decrypt(s.SettingValue) : s.SettingValue);

        settings.TryGetValue("AI_MODEL_API_KEY", out var dbKey);
        if (string.IsNullOrWhiteSpace(dbKey))
            settings.TryGetValue("GEMINI_API_KEY", out dbKey);

        settings.TryGetValue("AI_MODEL_NAME", out var dbModel);
        if (string.IsNullOrWhiteSpace(dbModel))
            settings.TryGetValue("GEMINI_MODEL", out dbModel);

        settings.TryGetValue("AI_MODEL_BASE_URL", out var dbBaseUrl);

        var result = (
            !string.IsNullOrWhiteSpace(dbKey) ? dbKey : _options.ApiKey,
            !string.IsNullOrWhiteSpace(dbModel) ? dbModel : _options.Model,
            !string.IsNullOrWhiteSpace(dbBaseUrl) ? dbBaseUrl : _options.BaseUrl
        );

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

        return result;
    }

    public bool IsConfigured => true; // Tránh dùng biến tĩnh để check config vì config có thể đổi trong DB

    public async Task<AiModelTestResultDto> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var (apiKey, model, baseUrl) = await ResolveSettingsAsync(cancellationToken);
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
        var (apiKey, dynamicModel, dynamicBaseUrl) = await ResolveSettingsAsync(cancellationToken);
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
            var baseUrl = string.IsNullOrWhiteSpace(dynamicBaseUrl) ? "https://api.tokenrouter.com/v1" : dynamicBaseUrl;
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

            int maxRetries = 2;
            for (int retry = 0; retry <= maxRetries; retry++)
            {
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
                            if (retry < maxRetries)
                            {
                                _logger.LogWarning("AI model {Model} bị 429 (Rate Limit) — chờ 2s rồi thử lại (Lần {Retry}).", model, retry + 1);
                                await Task.Delay(2000, cancellationToken);
                                continue;
                            }
                            
                            quotaHits++;
                            _logger.LogWarning("AI model {Model} hết quota/rate limit sau {Max} lần — thử model khác.", model, maxRetries);
                            break; // Hết số lần thử, bỏ model này
                        }

                        _logger.LogWarning("AI API lỗi {StatusCode} ({Model}): {Body}",
                            response.StatusCode, model, raw);
                        break; // Lỗi khác, bỏ qua model này luôn
                    }

                    var openAiResponse = JsonSerializer.Deserialize<OpenAiChatResponse>(raw, JsonOptions);
                    var text = openAiResponse?.Choices?.FirstOrDefault()?.Message?.Content;

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        _lastErrorMessage = "AI trả về rỗng.";
                        _logger.LogWarning("AI trả về rỗng (model {Model}). Raw response: {Raw}", model, raw);
                        break;
                    }

                    var parsed = JsonSerializer.Deserialize<AiSentimentPayload>(text, JsonOptions);
                    if (parsed == null)
                        break;

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
                    break;
                }
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

    public async Task<ReportInsightsResultDto?> GenerateReportInsightsAsync(
        string projectName,
        int totalMentions,
        double nsrScore,
        string topChannelInfo,
        string topNegativeAspects,
        List<string>? pinnedMentions,
        List<string>? topReputableMentions = null,
        List<string>? topInfluencers = null,
        CancellationToken cancellationToken = default)
    {
        var (apiKey, dynamicModel, dynamicBaseUrl) = await ResolveSettingsAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(apiKey) || IsQuotaCoolingDown)
            return null;

        var quotesStr = pinnedMentions != null && pinnedMentions.Count > 0 
            ? "\n- Các trích dẫn đáng chú ý (Pinned Quotes):\n" + string.Join("\n", pinnedMentions.Select(p => $"  + \"{p}\""))
            : "";

        var topMentionsStr = topReputableMentions != null && topReputableMentions.Count > 0
            ? "\n- Top bình luận/bài viết uy tín (đã lọc nhiễu, tương tác cao):\n" + string.Join("\n", topReputableMentions.Select(p => $"  + {p}"))
            : "";

        var topInfluencersStr = topInfluencers != null && topInfluencers.Count > 0
            ? "\n- Các tài khoản/người ảnh hưởng dẫn dắt thảo luận (KOLs/KOCs):\n" + string.Join("\n", topInfluencers.Select(p => $"  + {p}"))
            : "";

        var prompt = 
            $"Bạn là chuyên gia tư vấn chiến lược truyền thông (PR Consultant) xuất sắc. " +
            $"Hãy đọc các dữ liệu thực tế sau đây của dự án '{projectName}' và viết báo cáo phân tích chiến lược.\n\n" +
            $"Dữ liệu:\n" +
            $"- Tổng mentions: {totalMentions}\n" +
            $"- NSR Score (Net Sentiment Rate): {nsrScore}%\n" +
            $"- Kênh dẫn đầu/nổi bật: {topChannelInfo}\n" +
            $"- Các khía cạnh bị phàn nàn nhiều (Negative Aspects): {topNegativeAspects}{quotesStr}{topMentionsStr}{topInfluencersStr}\n\n" +
            "YÊU CẦU (BẮT BUỘC ĐỌC KỸ):\n" +
            "- PHẢI TRẢ LỜI 100% BẰNG TIẾNG VIỆT.\n" +
            "- KHÔNG dùng từ ngữ sáo rỗng, phải bám sát dữ liệu uy tín ở trên.\n" +
            "- JSON KHÔNG được thiếu bất kỳ trường nào.\n" +
            "1. Viết 3-4 câu 'executiveInsights' (Tóm tắt điều hành) bằng Tiếng Việt, chỉ ra điểm sáng và rủi ro lớn nhất.\n" +
            "2. Đề xuất 3 'contentDirections' (Định hướng nội dung) cụ thể: Các chủ đề/từ khóa nên khai thác trong tuần tới dựa trên trend.\n" +
            "3. Đề xuất 3 'riskMitigation' (Chiến lược xử lý rủi ro): Các bước cụ thể để dập tắt hoặc phản hồi các điểm chê bai.\n" +
            "4. Đề xuất 3 'productOptimization' (Tối ưu sản phẩm/dịch vụ): Góp ý thẳng thắn vào sản phẩm dựa trên feedback của khách hàng.\n" +
            "5. Viết một 'nsrComment' (1-2 câu) bình luận khách quan về sức khỏe thương hiệu dựa trên chỉ số NSR.\n" +
            "6. Viết 1 đoạn văn 'sentimentAnalysis' phân tích nguyên nhân hình thành làn sóng cảm xúc hiện tại và tầm ảnh hưởng.\n" +
            "7. Viết 1 đoạn văn 'channelAnalysis' giải thích lý do kênh hàng đầu lại chiếm volume lớn.\n" +
            "8. Viết 1 đoạn văn 'influencerAnalysis' đánh giá cách các KOLs đang định hướng dư luận.\n" +
            "9. Phân tích SWOT ngắn gọn (điền vào 'swotAnalysis').\n" +
            "Trả về JSON ĐÚNG cấu trúc sau (không bọc trong markdown):\n" +
            "{\n  \"executiveInsights\": [\"câu 1\", \"câu 2\"],\n  \"contentDirections\": [\"định hướng 1\", \"định hướng 2\"],\n  \"riskMitigation\": [\"chiến lược 1\", \"chiến lược 2\"],\n  \"productOptimization\": [\"đề xuất 1\", \"đề xuất 2\"],\n  \"nsrComment\": \"bình luận\",\n  \"sentimentAnalysis\": \"phân tích cảm xúc...\",\n  \"channelAnalysis\": \"phân tích kênh...\",\n  \"influencerAnalysis\": \"phân tích KOLs...\",\n  \"swotAnalysis\": {\n    \"strengths\": [\"điểm 1\"],\n    \"weaknesses\": [\"điểm 1\"],\n    \"opportunities\": [\"điểm 1\"],\n    \"threats\": [\"điểm 1\"]\n  }\n}";

        var models = GetModelCandidates(dynamicModel).ToList();
        
        foreach (var model in models)
        {
            var baseUrl = string.IsNullOrWhiteSpace(dynamicBaseUrl) ? "https://api.tokenrouter.com/v1" : dynamicBaseUrl;
            var url = $"{baseUrl.TrimEnd('/')}/chat/completions";

            var requestBody = new
            {
                model = model,
                messages = new[] { new { role = "user", content = prompt } },
                response_format = new { type = "json_object" },
                temperature = 0.3
            };

            for (int retry = 0; retry <= 2; retry++)
            {
                try
                {
                    var request = new HttpRequestMessage(HttpMethod.Post, url);
                    request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                    request.Content = JsonContent.Create(requestBody);

                    using var response = await _httpClient.SendAsync(request, cancellationToken);
                    
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                    {
                        await Task.Delay(2000, cancellationToken);
                        continue;
                    }

                    response.EnsureSuccessStatusCode();

                    var contentString = await response.Content.ReadAsStringAsync(cancellationToken);
                    var openAiResponse = JsonSerializer.Deserialize<OpenAiChatResponse>(contentString, JsonOptions);
                    var jsonContent = openAiResponse?.Choices?.FirstOrDefault()?.Message?.Content;

                    if (!string.IsNullOrWhiteSpace(jsonContent))
                    {
                        var result = JsonSerializer.Deserialize<ReportInsightsResultDto>(jsonContent, JsonOptions);
                        if (result != null && (result.ExecutiveInsights.Any() || result.ContentDirections.Any()))
                        {
                            return result;
                        }
                    }
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[GenerateReportInsightsAsync] Lỗi khi gọi model {Model} (retry {Retry})", model, retry);
                    if (retry == 2) break;
                    await Task.Delay(1000, cancellationToken);
                }
            }
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
