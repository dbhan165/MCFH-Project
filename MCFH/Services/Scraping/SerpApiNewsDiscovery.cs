using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using MCFH.Configuration;
using Microsoft.Extensions.Options;

namespace MCFH.Services.Scraping;

public sealed class SerpApiDiscoveryResult
{
    public List<string> Urls { get; init; } = [];
    public bool ShouldFallback { get; init; }
    public string? Message { get; init; }
}

public class SerpApiNewsDiscovery
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _http;
    private readonly SerpApiOptions _options;

    public SerpApiNewsDiscovery(HttpClient http, IOptions<SerpApiOptions> options)
    {
        _http = http;
        _options = options.Value;
        _http.Timeout = TimeSpan.FromSeconds(Math.Clamp(_options.TimeoutSeconds, 5, 120));
    }

    public bool IsConfigured => _options.IsConfigured;

    public async Task<SerpApiDiscoveryResult> DiscoverArticleUrlsAsync(
        string query,
        IReadOnlyList<string> sites,
        int maxArticles,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return new SerpApiDiscoveryResult
            {
                ShouldFallback = true,
                Message = "SerpApi chưa cấu hình (thiếu ApiKey hoặc Enabled=false)."
            };
        }

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var num = Math.Clamp(maxArticles, 1, 20);
            var url =
                $"https://serpapi.com/search.json?engine={Uri.EscapeDataString(_options.Engine)}" +
                $"&q={Uri.EscapeDataString(query)}" +
                $"&api_key={Uri.EscapeDataString(_options.ApiKey!)}" +
                $"&gl={Uri.EscapeDataString(_options.Gl)}" +
                $"&hl={Uri.EscapeDataString(_options.Hl)}" +
                $"&num={num}";

            Console.WriteLine($"[News][SerpApi] Query: {query}");

            using var response = await _http.GetAsync(url, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var fallback = ShouldFallbackForHttp(response.StatusCode, body);
                Console.WriteLine($"[News][SerpApi] HTTP {(int)response.StatusCode}: {Truncate(body, 200)}");
                return new SerpApiDiscoveryResult
                {
                    ShouldFallback = fallback,
                    Message = $"SerpApi HTTP {(int)response.StatusCode}"
                };
            }

            var parsed = JsonSerializer.Deserialize<SerpApiSearchResponse>(body, JsonOptions);
            if (parsed == null)
            {
                Console.WriteLine("[News][SerpApi] Không parse được JSON.");
                return new SerpApiDiscoveryResult
                {
                    ShouldFallback = true,
                    Message = "SerpApi trả về JSON không hợp lệ."
                };
            }

            if (!string.IsNullOrWhiteSpace(parsed.Error))
            {
                var fallback = ShouldFallbackForError(parsed.Error);
                Console.WriteLine($"[News][SerpApi] Error: {parsed.Error}");
                return new SerpApiDiscoveryResult
                {
                    ShouldFallback = fallback,
                    Message = parsed.Error
                };
            }

            var links = parsed.OrganicResults?
                .Select(r => r.Link)
                .Where(l => !string.IsNullOrWhiteSpace(l))
                .Cast<string>()
                .ToList() ?? [];

            var urls = NewsUrlFilter.FilterUrls(links, sites, maxArticles);
            Console.WriteLine($"[News][SerpApi] Found {urls.Count} URL(s) (raw organic: {links.Count})");

            return new SerpApiDiscoveryResult
            {
                Urls = urls,
                ShouldFallback = urls.Count == 0,
                Message = urls.Count == 0 ? "SerpApi không có URL báo phù hợp." : null
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[News][SerpApi] Exception: {ex.Message}");
            return new SerpApiDiscoveryResult
            {
                ShouldFallback = true,
                Message = ex.Message
            };
        }
    }

    private static bool ShouldFallbackForHttp(HttpStatusCode status, string body) =>
        status is HttpStatusCode.TooManyRequests
            or HttpStatusCode.Unauthorized
            or HttpStatusCode.Forbidden
            or HttpStatusCode.PaymentRequired
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout
        || ShouldFallbackForError(body);

    private static bool ShouldFallbackForError(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        var m = message.ToLowerInvariant();
        return m.Contains("run out of searches", StringComparison.Ordinal)
               || m.Contains("quota", StringComparison.Ordinal)
               || m.Contains("limit", StringComparison.Ordinal)
               || m.Contains("invalid api key", StringComparison.Ordinal)
               || m.Contains("unauthorized", StringComparison.Ordinal)
               || m.Contains("account", StringComparison.Ordinal) && m.Contains("searches", StringComparison.Ordinal);
    }

    private static string Truncate(string? s, int max) =>
        string.IsNullOrEmpty(s) ? "" : s.Length <= max ? s : s[..max] + "...";

    private sealed class SerpApiSearchResponse
    {
        [JsonPropertyName("organic_results")]
        public List<SerpApiOrganicResult>? OrganicResults { get; set; }

        [JsonPropertyName("error")]
        public string? Error { get; set; }
    }

    private sealed class SerpApiOrganicResult
    {
        [JsonPropertyName("link")]
        public string? Link { get; set; }
    }
}
