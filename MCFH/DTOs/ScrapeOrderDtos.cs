using System.Text.Json.Serialization;
using MCFH.Models;

namespace MCFH.DTOs;

/// <summary>Loại gói mentions — enum string để PayOS/UI trao đổi dễ.</summary>
public static class MentionPackageTypes
{
    public const string Pack100 = "PACK_100";
    public const string Pack300 = "PACK_300";
    public const string Pack600 = "PACK_600";
    public const string FullUnlimited = "FULL_UNLIMITED";

    /// <summary>Trả về null nếu input không phải package hợp lệ.</summary>
    public static string? Normalize(string? raw) => raw switch
    {
        Pack100 or Pack300 or Pack600 or FullUnlimited => raw,
        _ => null
    };
}

public class ScrapeQuoteRequestDto
{
    [JsonPropertyName("mentionsPackage")]
    public string MentionsPackage { get; set; } = MentionPackageTypes.Pack100;
}

public class ScrapeQuoteDto
{
    [JsonPropertyName("mentionsPackage")]
    public string MentionsPackage { get; set; } = null!;

    [JsonPropertyName("packageLabel")]
    public string PackageLabel { get; set; } = null!;

    [JsonPropertyName("mentionsIncluded")]
    public int MentionsIncluded { get; set; }

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("priceLabel")]
    public string PriceLabel { get; set; } = null!;

    [JsonPropertyName("estimatedMinutes")]
    public int EstimatedMinutes { get; set; }

    [JsonPropertyName("estimatedDeliveryLabel")]
    public string EstimatedDeliveryLabel { get; set; } = null!;

    /// <summary>Số mentions còn lại cho Project sau khi mua gói này (bao gồm cả full unlimited).</summary>
    [JsonPropertyName("projectRemainingMentions")]
    public int? ProjectRemainingMentions { get; set; }

    [JsonPropertyName("projectHasFullUnlimited")]
    public bool ProjectHasFullUnlimited { get; set; }
}

public class CreateScrapeOrderDto
{
    [JsonPropertyName("workspaceId")]
    public int WorkspaceId { get; set; }

    [JsonPropertyName("projectId")]
    public int ProjectId { get; set; }

    [JsonPropertyName("keyword")]
    public string Keyword { get; set; } = null!;

    [JsonPropertyName("mentionsPackage")]
    public string MentionsPackage { get; set; } = MentionPackageTypes.Pack100;
}

/// <summary>Kết quả tạo checkout PayOS cho đơn cào dữ liệu — frontend redirect sang CheckoutUrl hoặc hiển thị QrCode.</summary>
public class ScrapeOrderCheckoutDto
{
    public ScrapeOrderDto Order { get; set; } = null!;
    public long OrderCode { get; set; }
    public string PaymentLinkId { get; set; } = null!;
    public string CheckoutUrl { get; set; } = null!;
    /// <summary>Chuỗi VietQR thô — có thể render thành mã QR phía client.</summary>
    public string QrCode { get; set; } = null!;
    public decimal Amount { get; set; }
}

public class ScrapeOrderDto
{
    public int OrderId { get; set; }
    public int WorkspaceId { get; set; }
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = null!;
    public string Keyword { get; set; } = null!;

    /// <summary>Khoảng thời gian cào (truyền cho runtime scrape). Order mới luôn = 30.</summary>
    public int PostedSinceDays { get; set; }

    [JsonPropertyName("mentionsPackage")]
    public string? MentionsPackage { get; set; }

    [JsonPropertyName("mentionsIncluded")]
    public int? MentionsIncluded { get; set; }

    [JsonPropertyName("packageLabel")]
    public string? PackageLabel { get; set; }

    public decimal QuotedPrice { get; set; }
    public string PriceLabel { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string StatusLabel { get; set; } = null!;
    public int ProgressPercent { get; set; }
    public string? StatusMessage { get; set; }
    public string? ScrapeJobId { get; set; }
    public DateTime? EstimatedReportAt { get; set; }
    public DateTime? ReportReadyAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Models.Scraping.ScrapeJobStatusDto? ScrapeJob { get; set; }
}

/// <summary>Tóm tắt quota mentions của Project — frontend tab "Gói mentions" dùng.</summary>
public class ProjectMentionsQuotaDto
{
    [JsonPropertyName("projectId")]
    public int ProjectId { get; set; }

    /// <summary>Tổng mentions đã mua (cộng các package active + unlimited).</summary>
    [JsonPropertyName("mentionsQuotaTotal")]
    public int MentionsQuotaTotal { get; set; }

    /// <summary>Đã dùng (consume qua scrape orders của Project này).</summary>
    [JsonPropertyName("mentionsQuotaUsed")]
    public int MentionsQuotaUsed { get; set; }

    /// <summary>Còn lại có thể cào (NULL nếu Full Unlimited).</summary>
    [JsonPropertyName("mentionsRemaining")]
    public int? MentionsRemaining { get; set; }

    /// <summary>True nếu đã mua Full Unlimited.</summary>
    [JsonPropertyName("fullUnlimited")]
    public bool FullUnlimited { get; set; }

    /// <summary>Thời điểm quota active sớm nhất hết hạn (NULL = vĩnh viễn).</summary>
    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    /// <summary>Các gói đã mua đang active.</summary>
    [JsonPropertyName("activePackages")]
    public List<MentionsPackageDto> ActivePackages { get; set; } = new();
}

public class MentionsPackageDto
{
    [JsonPropertyName("packageId")]
    public int PackageId { get; set; }

    [JsonPropertyName("packageType")]
    public string PackageType { get; set; } = null!;

    [JsonPropertyName("packageLabel")]
    public string PackageLabel { get; set; } = null!;

    [JsonPropertyName("mentionsIncluded")]
    public int MentionsIncluded { get; set; }

    [JsonPropertyName("mentionsUsed")]
    public int MentionsUsed { get; set; }

    [JsonPropertyName("mentionsRemaining")]
    public int? MentionsRemaining { get; set; }

    [JsonPropertyName("expiresAt")]
    public DateTime? ExpiresAt { get; set; }

    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }
}
