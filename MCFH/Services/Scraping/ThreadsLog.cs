namespace MCFH.Services.Scraping;

/// <summary>
/// Helper gom 2 cách gọi log cho Threads scraper:
///   - <c>Status(...)</c>: gửi lên UI thông qua <c>onStatus</c> callback (chỉ dùng cho message tiếng Việt thân thiện).
///   - <c>Debug(...)</c>: chỉ ghi console, không hiển thị lên UI (dùng cho debug log dạng [Threads] ...).
/// </summary>
public static class ThreadsLog
{
    public static void Status(Action<string>? onStatus, string message)
    {
        onStatus?.Invoke(message);
    }

    public static void Debug(string message)
    {
        Console.WriteLine($"[Threads] {message}");
    }

    public static void Debug(Action<string>? onStatus, string message)
    {
        // Khi cần vừa log console vừa không lộ lên UI, dùng overload này.
        Console.WriteLine($"[Threads] {message}");
    }
}