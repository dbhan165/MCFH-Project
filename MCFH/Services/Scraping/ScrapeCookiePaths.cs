namespace MCFH.Services.Scraping;

/// <summary>
/// Một thư mục cookie duy nhất: {ContentRoot}/cookies/ (dev và publish).
/// </summary>
public static class ScrapeCookiePaths
{
    public static string CookiesDirectory { get; private set; } = "";

    public static string TikTokCookiePath =>
        Path.Combine(CookiesDirectory, "tiktok_cookie.json");

    /// <summary>Bản backup tự động trước mỗi lần ghi cookie mới (phiên scrape thành công).</summary>
    public static string TikTokCookieBackupPath =>
        Path.Combine(CookiesDirectory, "tiktok_cookie.backup.json");

    public static string FacebookCookiePath =>
        Path.Combine(CookiesDirectory, "fb_cookie.json");

    public static void Initialize(string contentRootPath)
    {
        CookiesDirectory = Path.Combine(contentRootPath, "cookies");
        Directory.CreateDirectory(CookiesDirectory);
    }
}
