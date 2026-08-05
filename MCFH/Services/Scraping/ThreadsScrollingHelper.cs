using Microsoft.Playwright;
using MCFH.Configuration;

namespace MCFH.Services.Scraping;

public static class ThreadsScrollingHelper
{
    public static async Task ScrollCommentsAsync(IPage page, Action<string>? onStatus, ScrapeOptions? options = null)
    {
        try
        {
            onStatus?.Invoke("[Threads] Scrolling comments...");

            await page.EvaluateAsync<bool>(@"() => {
                const candidates = document.body.querySelectorAll('div, section');
                let bestContainer = null;
                let bestHeight = 0;

                const pagelets = document.querySelectorAll('[data-pagelet^=""threads_post_page_""]');
                let commentPagelet = null;
                for (const p of pagelets) {
                    const name = p.getAttribute('data-pagelet') || '';
                    const m = name.match(/threads_post_page_(\d+)/);
                    if (m && parseInt(m[1], 10) >= 1) { commentPagelet = p; break; }
                }

                if (commentPagelet) {
                    let p = commentPagelet.parentElement;
                    for (let step = 0; step < 10 && p; step++) {
                        const style = window.getComputedStyle(p);
                        const oy = (style.overflowY || style.overflow || '').toLowerCase();
                        if ((oy === 'auto' || oy === 'scroll') && p.scrollHeight > p.clientHeight) {
                            bestContainer = p;
                            break;
                        }
                        if (p.scrollHeight > p.clientHeight + 100 && !bestContainer) {
                            bestContainer = p;
                        }
                        p = p.parentElement;
                    }
                }

                for (const el of candidates) {
                    if (el.scrollHeight <= el.clientHeight + 100) continue;
                    const text = (el.innerText || '').trim();
                    if (!text) continue;

                    if (text.includes('Bảng feed') || text.includes('Đang theo dõi')) continue;
                    if (text.includes('Trang cá nhân') && text.length < 500) continue;

                    const hasReply = text.includes('Trả lời') || text.includes('Reply');
                    const hasTime = /\d+\s*(h|d|giờ|ngày|phút)/i.test(text);
                    if (!hasReply && !hasTime) continue;

                    const scrollableHeight = el.scrollHeight - el.clientHeight;
                    if (scrollableHeight > bestHeight) {
                        bestHeight = scrollableHeight;
                        bestContainer = el;
                    }
                }

                if (bestContainer) {
                    bestContainer.scrollTop = bestContainer.scrollHeight;
                    return true;
                }
                return false;
            }");

            var iterations = options?.EffectiveThreadsCommentScrollMaxIterations ?? 40;
            var stableRoundsNeeded = options?.EffectiveThreadsCommentScrollStableRounds ?? 6;
            var minGrowthRounds = options?.EffectiveThreadsCommentScrollMinGrowthRounds ?? 2;
            var previousCommentCount = 0;
            var noChangeCount = 0;
            var stagnantRounds = 0;
            var grewCount = 0;
            var consecutiveDeltaBelowOne = 0;

            var countJs = @"() => {
                let inMain = 0;
                const pagelets = document.querySelectorAll('[data-pagelet^=""threads_post_page_""]');
                for (const p of pagelets) {
                    const name = p.getAttribute('data-pagelet') || '';
                    if (name === 'threads_post_page_0') continue;
                    inMain += p.querySelectorAll('a[href*=""threads.com/@""],a[href*=""/@\""]').length;
                }
                return inMain;
            }";

            var pageletCountJs = @"() => {
                const pagelets = document.querySelectorAll('[data-pagelet^=""threads_post_page_""]');
                let commentPagelets = 0;
                for (const p of pagelets) {
                    const name = p.getAttribute('data-pagelet') || '';
                    if (name !== 'threads_post_page_0') commentPagelets++;
                }
                return commentPagelets;
            }";

            previousCommentCount = await page.EvaluateAsync<int>(countJs);

            for (int i = 0; i < iterations; i++)
            {
                await page.EvaluateAsync(@"() => {
                    window.scrollTo(0, document.body.scrollHeight);
                    window.scrollBy(0, window.innerHeight);

                    const pagelets = document.querySelectorAll('[data-pagelet^=""threads_post_page_""]');
                    if (pagelets.length > 0) {
                        const last = pagelets[pagelets.length - 1];
                        const name = (last.getAttribute('data-pagelet') || '');
                        if (/threads_post_page_\d+/.test(name)) {
                            try { last.scrollIntoView({ behavior: 'instant', block: 'end' }); } catch (e) {}
                            let w = last.parentElement;
                            for (let j = 0; j < 8 && w; j++) {
                                const s = window.getComputedStyle(w);
                                if (s.overflowY === 'auto' || s.overflowY === 'scroll' || s.overflow === 'auto' || s.overflow === 'scroll') {
                                    try { w.scrollTop = w.scrollHeight; } catch (e) {}
                                }
                                w = w.parentElement;
                            }
                        }
                    }
                }");

                try { await page.Keyboard.PressAsync("End"); } catch { }
                try { await page.Mouse.WheelAsync(0, 2000); } catch { }
                await page.WaitForTimeoutAsync(2000);

                var currentCommentCount = await page.EvaluateAsync<int>(countJs);
                var currentPagelets = await page.EvaluateAsync<int>(pageletCountJs);

                var delta = currentCommentCount - previousCommentCount;
                if (delta > 0)
                {
                    grewCount++;
                    noChangeCount = 0;
                    consecutiveDeltaBelowOne = 0;
                }
                else if (delta == 0)
                {
                    noChangeCount++;
                    consecutiveDeltaBelowOne++;
                }
                else
                {
                    noChangeCount = 0;
                    consecutiveDeltaBelowOne = 0;
                }

                if (delta < 1) stagnantRounds++;
                else stagnantRounds = 0;

                previousCommentCount = currentCommentCount;

                onStatus?.Invoke($"[Threads] Scroll step {i + 1}: {currentCommentCount} comment authors, pagelet #{currentPagelets}");

                var stableEnough = grewCount >= minGrowthRounds && noChangeCount >= stableRoundsNeeded;
                var flatEnough = consecutiveDeltaBelowOne >= stableRoundsNeeded + 2;
                if (stableEnough || flatEnough) break;

                if (stagnantRounds >= 3 && i > 5)
                {
                    await page.EvaluateAsync(@"() => {
                        window.scrollTo(0, document.body.scrollHeight);
                    }");
                    try { await page.Keyboard.PressAsync("PageDown"); } catch { }
                    await page.WaitForTimeoutAsync(2000);
                    stagnantRounds = 0;
                }
            }

            await page.EvaluateAsync(@"() => { window.scrollTo(0, 0); }");
            await page.WaitForTimeoutAsync(1000);

            onStatus?.Invoke($"[Threads] Comment scroll complete. Max iterations: {iterations}, stopped after {noChangeCount} stable reads (grew {grewCount} times).");
        }
        catch (Exception ex)
        {
            onStatus?.Invoke($"[Threads] Comment scroll: {ex.Message}");
        }
    }

    public static async Task ScrollToLoadAsync(
        Random rng,
        IPage page,
        int maxScrollSteps,
        int delayMinMs,
        int delayMaxMs,
        Action<string>? onStatus)
    {
        var lastHeight = 0L;
        var noProgressCount = 0;

        for (int i = 0; i < maxScrollSteps; i++)
        {
            await ThreadsStealthHelper.DelayAsync(rng, delayMinMs, delayMaxMs);

            var scrollStep = rng.Next(300, 700);
            await page.EvaluateAsync($@"() => window.scrollBy(0, {scrollStep})");
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await ThreadsStealthHelper.DelayAsync(rng, delayMinMs, delayMaxMs);

            var newHeight = await page.EvaluateAsync<long>("document.body.scrollHeight");
            if (newHeight == lastHeight)
            {
                noProgressCount++;
                if (noProgressCount >= 2)
                {
                    onStatus?.Invoke($"[Threads] Reached end at step {i + 1}");
                    break;
                }
            }
            else
            {
                noProgressCount = 0;
            }
            lastHeight = newHeight;
        }
    }
}
