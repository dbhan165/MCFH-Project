using MCFH.Configuration;
using Microsoft.Playwright;

namespace MCFH.Services.Scraping;

/// <summary>
/// TikTok CAPTCHA — chế độ nền tảng: headless, không chờ user, fail nhanh + gợi ý admin refresh cookie.
/// Chế độ dev (TikTokAllowManualCaptcha + browser hiện): chờ admin giải tay.
/// </summary>
public static class TikTokCaptchaHelper
{
    public const string PlatformBlockedMessage =
        "TikTok chặn bot (CAPTCHA). Admin cần cập nhật cookies/tiktok_cookie.json trong thư mục project.";

    public static async Task<bool> IsVisibleAsync(IPage page)
    {
        try
        {
            return await page.EvaluateAsync<bool>(@"
                () => {
                    const body = (document.body?.innerText || '').toLowerCase();
                    const captchaPhrases = [
                        'captcha', 'xác minh', 'verify to continue', 'drag the slider',
                        'kéo thanh trượt', 'xoay hình', 'rotate', 'security check',
                        'secsdk', 'puzzle'
                    ];
                    if (captchaPhrases.some(p => body.includes(p))) return true;

                    const selectors = [
                        '#captcha-verify-container',
                        '[class*=""captcha""]',
                        '[id*=""captcha""]',
                        '[class*=""secsdk""]',
                        'iframe[src*=""captcha""]',
                        '[data-e2e=""captcha""]'
                    ];
                    for (const sel of selectors) {
                        try {
                            const el = document.querySelector(sel);
                            if (el) {
                                const r = el.getBoundingClientRect();
                                if (r.width > 0 && r.height > 0) return true;
                            }
                        } catch {}
                    }
                    return false;
                }
            ");
        }
        catch
        {
            return false;
        }
    }

    /// <summary>true = tiếp tục cào; false = bị CAPTCHA chặn (chế độ nền tảng).</summary>
    public static async Task<bool> TryContinueAsync(
        IPage page,
        ScrapeOptions options,
        string stage = "",
        bool? sessionHeadless = null,
        TikTokCaptchaTracker? tracker = null)
    {
        await page.WaitForTimeoutAsync(800);

        if (!await IsVisibleAsync(page))
            return true;

        tracker?.MarkEncountered();

        var headless = sessionHeadless ?? options.TikTokHeadless;
        var manualMode = options.TikTokAllowManualCaptcha
            && !headless
            && options.TikTokCaptchaWaitSeconds > 0;

        if (manualMode)
        {
            await WaitForManualSolveAsync(page, options.TikTokCaptchaWaitSeconds, stage);
            return !await IsVisibleAsync(page);
        }

        Console.WriteLine($"[TikTok CAPTCHA] Bị chặn tại bước '{stage}' — headless, không chờ user.");
        return false;
    }

    private static async Task WaitForManualSolveAsync(IPage page, int maxWaitSeconds, string stage)
    {
        var label = string.IsNullOrWhiteSpace(stage) ? "" : $" ({stage})";
        Console.WriteLine();
        Console.WriteLine($"[TikTok CAPTCHA{label}] Chế độ DEV — giải CAPTCHA trong cửa sổ Chromium (tối đa {maxWaitSeconds}s).");
        Console.WriteLine();

        try { await page.BringToFrontAsync(); } catch { }

        var deadline = DateTime.UtcNow.AddSeconds(maxWaitSeconds);
        while (DateTime.UtcNow < deadline)
        {
            await page.WaitForTimeoutAsync(2000);
            if (!await IsVisibleAsync(page))
            {
                await page.WaitForTimeoutAsync(1000);
                if (!await IsVisibleAsync(page))
                {
                    Console.WriteLine("[TikTok CAPTCHA] Đã qua — tiếp tục.");
                    return;
                }
            }
        }

        Console.WriteLine("[TikTok CAPTCHA] Hết thời gian chờ.");
    }
}
