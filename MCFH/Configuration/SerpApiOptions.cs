namespace MCFH.Configuration;

public class SerpApiOptions
{
    public const string SectionName = "SerpApi";

    /// <summary>Bật SerpApi cho news discovery. Cần ApiKey hợp lệ.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Đặt qua User Secrets / biến môi trường SerpApi__ApiKey — không commit key thật.</summary>
    public string? ApiKey { get; set; }

    public string Engine { get; set; } = "google";

    public string Gl { get; set; } = "vn";

    public string Hl { get; set; } = "vi";

    public int TimeoutSeconds { get; set; } = 30;

    public bool IsConfigured =>
        Enabled && !string.IsNullOrWhiteSpace(ApiKey);
}
