namespace MCFH.Services;

/// <summary>
/// Che đi phần giữa secret key để hiển thị an toàn (vẫn nhận biết được key nào).
/// Pattern: "first8…last4" với chiều dài tùy input.
/// </summary>
public static class SecretKeyMasker
{
    public const string HiddenValue = "********";

    /// <summary>Input null/empty → trả "********". Quá ngắn → trả "********".</summary>
    public static string Mask(string? raw, int headLen = 4, int tailLen = 4)
    {
        if (string.IsNullOrWhiteSpace(raw)) return HiddenValue;
        var s = raw.Trim();
        // Bỏ "REPLACE_..." / placeholder → cũng trả HiddenValue
        if (s.StartsWith("REPLACE_", StringComparison.Ordinal)) return HiddenValue;

        if (s.Length <= headLen + tailLen + 3)
        {
            // Quá ngắn để lộ nhiều — chỉ hiện 1 ký tự đầu.
            return s.Length <= 1 ? HiddenValue : s[0] + "***";
        }
        return $"{s[..headLen]}…{s[^tailLen..]}";
    }

    /// <summary>
    /// Mask trên PLAINTEXT key (sau khi decrypt). Admin thấy 1 đoạn key thật để nhận biết.
    /// Mặc định: 8 đầu + "…" + 4 cuối. Truyền vào <paramref name="decryptFn"/> để tự decrypt.
    /// Trả "********" nếu decrypt fail / input rỗng.
    /// </summary>
    public static string MaskPlaintext(string? encrypted, Func<string, string?> decryptFn, int headLen = 8, int tailLen = 4)
    {
        if (string.IsNullOrWhiteSpace(encrypted)) return HiddenValue;
        try
        {
            var plain = decryptFn(encrypted);
            if (string.IsNullOrWhiteSpace(plain)) return HiddenValue;
            return Mask(plain, headLen, tailLen);
        }
        catch
        {
            return HiddenValue;
        }
    }

    /// <summary>Helper tạo placeholder cho input form khi edit (giữ masked).</summary>
    public static string PlaceholderForEdit(string? raw) => string.IsNullOrWhiteSpace(raw)
        ? HiddenValue
        : Mask(raw);
}
