namespace MCFH.Configuration;

public class PayOsOptions
{
    public const string SectionName = "PayOS";

    public string ClientId { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string ChecksumKey { get; set; } = "";

    /// <summary>URL PayOS redirect về sau khi thanh toán thành công. Để trống → dùng {Auth:FrontendBaseUrl}/payment/return.</summary>
    public string ReturnUrl { get; set; } = "";

    /// <summary>URL PayOS redirect về khi người dùng hủy thanh toán. Để trống → dùng {Auth:FrontendBaseUrl}/payment/return.</summary>
    public string CancelUrl { get; set; } = "";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ApiKey) &&
        !string.IsNullOrWhiteSpace(ChecksumKey);
}
