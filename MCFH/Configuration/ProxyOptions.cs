namespace MCFH.Configuration;

/// <summary>UC-78 — Xoay vòng proxy khi cào dữ liệu.</summary>
public class ProxyOptions
{
    public const string SectionName = "ProxyRotation";

    /// <summary>Bật chọn proxy từ SYSTEM_PROXIES khi cào (tắt = direct, như dev local).</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Đổi proxy trước mỗi nền tảng (FB / YT / TT) trong cùng một job.</summary>
    public bool RotatePerPlatform { get; set; } = true;

    /// <summary>Số lần fail liên tiếp trước khi đánh dấu proxy là dead.</summary>
    public int MaxFailBeforeDead { get; set; } = 3;
}
