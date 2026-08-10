namespace MCFH.Models;

/// <summary>
/// Một Brevo API key cấu hình cho EmailService. Multi-key để admin có thể rotate/đổi key
/// khi key cũ bị revoke mà không cần deploy lại. EmailService đọc <see cref="IsDefault"/> = true.
///
/// Brevo cung cấp 2 loại:
///   - xkeysib-... : API key cho Brevo REST API (/v3/smtp/email) — dùng SMTP HTTP.
///   - xsmtpsib-... : SMTP key cho smtp-relay.brevo.com (SMTP login/password).
/// Phân biệt qua <see cref="KeyType"/>.
/// </summary>
public class BrevoKey
{
    public int BrevoKeyId { get; set; }

    /// <summary>"api" | "smtp" — loại key để EmailService quyết định dùng API hay SMTP login.</summary>
    public string KeyType { get; set; } = "api";

    /// <summary>Plain text API key (đã được encrypt ở tầng service trước khi lưu DB). Read-once.</summary>
    public string ApiKeyEncrypted { get; set; } = null!;

    /// <summary>SMTP login (chỉ dùng khi KeyType="smtp"). Optional khi KeyType="api".</summary>
    public string? SmtpLogin { get; set; }

    /// <summary>Địa chỉ email sender đã verify trên Brevo. VD: no-reply@mcfh.io.vn.</summary>
    public string? FromAddress { get; set; }

    /// <summary>Tên hiển thị người gửi. VD: "MCFH System Hub".</summary>
    public string? FromName { get; set; }

    /// <summary>"active" | "disabled" — EmailService chỉ dùng row có status=active và is_default=1.</summary>
    public string Status { get; set; } = "active";

    public bool IsDefault { get; set; }

    public string? Note { get; set; }

    public DateTime? LastUsedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    public virtual User? UpdatedByNavigation { get; set; }
}
