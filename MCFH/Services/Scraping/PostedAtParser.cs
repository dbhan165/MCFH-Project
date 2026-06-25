using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MCFH.Services.Scraping;

public static class PostedAtParser
{
    private static readonly Regex RelativeVi = new(
        @"(\d+)\s*(giây|giờ|phút|ngày|tuần|tuan|tháng|thang|năm|nam)\s*trước",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RelativeEn = new(
        @"(\d+)\s*(second|minute|hour|day|week|month|year)s?\s*ago",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static DateTime? FromUnixSeconds(long seconds)
    {
        if (seconds <= 0) return null;
        try
        {
            return DateTimeOffset.FromUnixTimeSeconds(seconds).LocalDateTime;
        }
        catch
        {
            return null;
        }
    }

    public static bool TryParseUnixSeconds(string? raw, out DateTime? postedAt)
    {
        postedAt = null;
        if (string.IsNullOrWhiteSpace(raw)) return false;
        if (!long.TryParse(raw.Trim(), out var seconds)) return false;
        postedAt = FromUnixSeconds(seconds);
        return postedAt.HasValue;
    }

    public static bool TryParseIso8601(string? raw, out DateTime? postedAt)
    {
        postedAt = null;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        if (DateTimeOffset.TryParse(raw.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var dto))
        {
            postedAt = dto.LocalDateTime;
            return true;
        }

        if (DateTime.TryParse(raw.Trim(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var dt))
        {
            postedAt = dt;
            return true;
        }

        return false;
    }

    /// <summary>Chuỗi kiểu "2024-1-15" hoặc "15-1-2024" từ TikTok UI.</summary>
    public static bool TryParseLooseDate(string? raw, out DateTime? postedAt)
    {
        postedAt = null;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var text = raw.Trim();
        var m = Regex.Match(text, @"(\d{4})[-/.](\d{1,2})[-/.](\d{1,2})");
        if (m.Success &&
            int.TryParse(m.Groups[1].Value, out var y) &&
            int.TryParse(m.Groups[2].Value, out var mo) &&
            int.TryParse(m.Groups[3].Value, out var d))
        {
            try
            {
                postedAt = new DateTime(y, mo, d);
                return true;
            }
            catch
            {
                return false;
            }
        }

        m = Regex.Match(text, @"(\d{1,2})[-/.](\d{1,2})[-/.](\d{4})");
        if (m.Success &&
            int.TryParse(m.Groups[1].Value, out var d2) &&
            int.TryParse(m.Groups[2].Value, out var mo2) &&
            int.TryParse(m.Groups[3].Value, out var y2))
        {
            try
            {
                postedAt = new DateTime(y2, mo2, d2);
                return true;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>"2 ngày trước", "1 week ago", "3h ago" (một số UI social).</summary>
    public static bool TryParseRelativeDate(string? raw, out DateTime? postedAt)
    {
        postedAt = null;
        if (string.IsNullOrWhiteSpace(raw)) return false;

        var text = raw.Trim().ToLowerInvariant();
        var m = RelativeVi.Match(text);
        if (!m.Success)
            m = RelativeEn.Match(text);
        if (!m.Success) return false;

        if (!int.TryParse(m.Groups[1].Value, out var amount) || amount < 0)
            return false;

        var unit = m.Groups[2].Value.ToLowerInvariant();
        var now = DateTime.Now;
        postedAt = unit switch
        {
            "giây" or "second" => now.AddSeconds(-amount),
            "phút" or "minute" => now.AddMinutes(-amount),
            "giờ" or "hour" => now.AddHours(-amount),
            "ngày" or "day" => now.AddDays(-amount),
            "tuần" or "tuan" or "week" => now.AddDays(-amount * 7),
            "tháng" or "thang" or "month" => now.AddMonths(-amount),
            "năm" or "nam" or "year" => now.AddYears(-amount),
            _ => null
        };

        return postedAt.HasValue;
    }

    public static bool TryParseAny(string? raw, out DateTime? postedAt)
    {
        if (TryParseUnixSeconds(raw, out postedAt)) return true;
        if (TryParseIso8601(raw, out postedAt)) return true;
        if (TryParseLooseDate(raw, out postedAt)) return true;
        if (TryParseRelativeDate(raw, out postedAt)) return true;
        postedAt = null;
        return false;
    }

    public static bool TryParseUnixToken(JsonElement token, out DateTime? postedAt)
    {
        postedAt = null;
        if (token.ValueKind == JsonValueKind.Number && token.TryGetInt64(out var n))
        {
            postedAt = FromUnixSeconds(n);
            return postedAt.HasValue;
        }

        if (token.ValueKind == JsonValueKind.String && TryParseUnixSeconds(token.GetString(), out postedAt))
            return true;

        return false;
    }
}
