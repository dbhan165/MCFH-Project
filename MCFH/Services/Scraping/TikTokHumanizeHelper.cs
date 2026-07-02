using MCFH.Configuration;
using Microsoft.Playwright;

namespace MCFH.Services.Scraping;

/// <summary>
/// Delay và cuộn ngẫu nhiên — giảm phát hiện bot (§4 humanize).
/// </summary>
public static class TikTokHumanizeHelper
{
    public static bool IsEnabled(ScrapeOptions? options) =>
        options?.TikTokHumanizeBehavior != false;

    public static async Task DelayAsync(
        IPage page,
        ScrapeOptions? options,
        int legacyMs,
        int? minMs = null,
        int? maxMs = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int waitMs;
        if (!IsEnabled(options))
        {
            waitMs = legacyMs;
        }
        else
        {
            var min = minMs ?? options?.TikTokHumanizeDelayMinMs ?? 1000;
            var max = maxMs ?? options?.TikTokHumanizeDelayMaxMs ?? 3000;
            if (min > max) (min, max) = (max, min);
            waitMs = min >= max ? min : Random.Shared.Next(min, max + 1);
        }

        if (waitMs > 0)
            await page.WaitForTimeoutAsync(waitMs);
    }

    /// <summary>Cuộn xuống từng bước nhỏ thay vì một lần nhảy lớn.</summary>
    public static async Task ScrollDownAsync(
        IPage page,
        ScrapeOptions? options,
        int legacyWheelPx,
        int legacyWaitMs,
        int? totalPx = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsEnabled(options))
        {
            await page.Mouse.WheelAsync(0, legacyWheelPx);
            await page.WaitForTimeoutAsync(legacyWaitMs);
            return;
        }

        var minStep = options?.TikTokHumanizeScrollStepMinPx ?? 280;
        var maxStep = options?.TikTokHumanizeScrollStepMaxPx ?? 620;
        if (minStep > maxStep) (minStep, maxStep) = (maxStep, minStep);

        var target = totalPx ?? Random.Shared.Next(
            Math.Min(minStep * 2, legacyWheelPx),
            legacyWheelPx + 1);

        var remaining = Math.Max(target, minStep);
        while (remaining > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var step = Math.Min(remaining, Random.Shared.Next(minStep, maxStep + 1));
            await page.Mouse.WheelAsync(0, step);
            remaining -= step;

            if (remaining > 0)
            {
                await DelayAsync(
                    page, options, legacyWaitMs / 3,
                    options?.TikTokHumanizeScrollPauseMinMs ?? 450,
                    options?.TikTokHumanizeScrollPauseMaxMs ?? 1100,
                    cancellationToken);
            }
        }

        await DelayAsync(
            page, options, legacyWaitMs,
            options?.TikTokHumanizeScrollPauseMinMs ?? 450,
            options?.TikTokHumanizeScrollPauseMaxMs ?? 1100,
            cancellationToken);
    }

    /// <summary>Cuộn panel bình luận — bước nhỏ hơn trang search.</summary>
    public static async Task ScrollCommentPanelAsync(
        IPage page,
        ScrapeOptions options,
        bool aggressive,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsEnabled(options))
        {
            await page.EvaluateAsync(@"() => {
                const panels = [
                    document.querySelector('[class*=""CommentListContainer""]'),
                    document.querySelector('[class*=""comment-list""]'),
                    document.querySelector('[data-e2e=""comment-list""]'),
                    document.querySelector('div[class*=""DivCommentList""]')
                ].filter(Boolean);
                for (const panel of panels) panel.scrollTop += 900;
                window.scrollBy(0, 600);
            }");
            await page.Mouse.WheelAsync(0, aggressive ? 1600 : 1200);
            await page.WaitForTimeoutAsync(aggressive ? 1100 : 900);
            return;
        }

        var panelStep = Random.Shared.Next(aggressive ? 320 : 220, aggressive ? 520 : 380);
        var windowStep = Random.Shared.Next(aggressive ? 280 : 180, aggressive ? 420 : 300);

        await page.EvaluateAsync(
            @"([panelStep, windowStep]) => {
                const panels = [
                    document.querySelector('[class*=""CommentListContainer""]'),
                    document.querySelector('[class*=""comment-list""]'),
                    document.querySelector('[data-e2e=""comment-list""]'),
                    document.querySelector('div[class*=""DivCommentList""]')
                ].filter(Boolean);
                for (const panel of panels) panel.scrollTop += panelStep;
                window.scrollBy(0, windowStep);
            }",
            new object[] { panelStep, windowStep });

        await ScrollDownAsync(
            page, options,
            aggressive ? 1600 : 1200,
            aggressive ? 1100 : 900,
            Random.Shared.Next(aggressive ? 450 : 300, aggressive ? 750 : 520),
            cancellationToken);
    }

    public static async Task AfterClickAsync(
        IPage page,
        ScrapeOptions? options,
        CancellationToken cancellationToken = default) =>
        await DelayAsync(page, options, 1000, 600, 1400, cancellationToken);

    public static async Task AfterNavigationAsync(
        IPage page,
        ScrapeOptions? options,
        CancellationToken cancellationToken = default) =>
        await DelayAsync(page, options, 2000, 1500, 3000, cancellationToken);

    /// <summary>Pause giữa hai video — không cần IPage.</summary>
    public static async Task BetweenVideosAsync(
        ScrapeOptions? options,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled(options))
            return;

        var min = options?.TikTokHumanizeDelayMinMs ?? 1000;
        var max = options?.TikTokHumanizeDelayMaxMs ?? 3000;
        if (min > max) (min, max) = (max, min);
        var waitMs = min >= max ? min : Random.Shared.Next(min, max + 1);
        await Task.Delay(waitMs, cancellationToken);
    }
}
