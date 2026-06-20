using Microsoft.Playwright;
using MCFH.Models.Scraping;

namespace MCFH.Services.Scraping;

public class FacebookGroupScraper
{
    public async Task<List<GroupPost>> ScrapeAsync(string groupUrl, int maxPosts = 5)
    {
        var results = new List<GroupPost>();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = false,
            SlowMo = 500
        });

        var context = await browser.NewContextAsync(new BrowserNewContextOptions
        {
            UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36",
            ViewportSize = new ViewportSize { Width = 1280, Height = 800 }
        });

        await FacebookSessionHelper.LoadCookiesAsync(context);

        var page = await context.NewPageAsync();

        Console.WriteLine($"[FB Group] Navigating to: {groupUrl}");
        await page.GotoAsync(groupUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 30000
        });
        await page.WaitForTimeoutAsync(3000);

        await page.Mouse.MoveAsync(640, 400);

        for (int i = 0; i < 15; i++)
        {
            var count = await page.QuerySelectorAllAsync("div[aria-posinset]");
            Console.WriteLine($"[FB Group] Scroll {i + 1}: {count.Count} posts trong DOM");

            if (count.Count >= maxPosts)
                break;

            await page.Mouse.WheelAsync(0, 1500);
            await page.WaitForTimeoutAsync(2000);
        }

        var postElements = await page.QuerySelectorAllAsync("div[aria-posinset]");
        var posinsetValues = new List<string>();
        foreach (var el in postElements.Take(maxPosts))
        {
            var posinset = await el.GetAttributeAsync("aria-posinset");
            if (posinset != null) posinsetValues.Add(posinset);
        }

        Console.WriteLine($"[FB Group] Sẽ xử lý posinset: {string.Join(", ", posinsetValues)}");

        foreach (var posinset in posinsetValues)
        {
            try
            {
                var post = await OpenPostAndScrapeAsync(page, posinset);
                if (post != null)
                    results.Add(post);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FB Group] Lỗi posinset={posinset}: {ex.Message}");
            }
        }

        Console.WriteLine($"[FB Group] Final: {results.Count} posts");
        return results;
    }

    private async Task<GroupPost?> OpenPostAndScrapeAsync(IPage page, string posinset)
    {
        var postEl = await page.QuerySelectorAsync($"div[aria-posinset='{posinset}']");
        if (postEl == null)
        {
            Console.WriteLine($"[FB Group] Không tìm thấy posinset={posinset}");
            return null;
        }

        var textEl = await postEl.QuerySelectorAsync("div[data-ad-rendering-role='story_message']");
        var text = textEl != null ? await textEl.InnerTextAsync() : "";

        var authorEl = await postEl.QuerySelectorAsync("div[data-ad-rendering-role='profile_name'] a b span");
        var author = authorEl != null ? await authorEl.InnerTextAsync() : "";

        if (string.IsNullOrWhiteSpace(text))
        {
            Console.WriteLine($"[FB Group] posinset={posinset} không có story_message, skip");
            return null;
        }

        var post = new GroupPost { Author = author, Text = text };

        var timestampLink = await postEl.QuerySelectorAsync("a[href*='/posts/']");
        if (timestampLink == null)
        {
            Console.WriteLine($"[FB Group] posinset={posinset} không có timestamp link, giữ post không URL/comment");
            return post;
        }

        var urlBeforeClick = page.Url;
        await timestampLink.ClickAsync();
        await page.WaitForTimeoutAsync(2000);

        var urlAfterClick = page.Url;

        // Kiểm tra click có thành công không — nếu vẫn ở trang search (bị redirect lỗi) thì fallback
        if (urlAfterClick.Contains("/search/") || urlAfterClick == urlBeforeClick)
        {
            Console.WriteLine($"[FB Group] posinset={posinset} click bị lỗi/redirect về search, giữ post không URL/comment");
            return post;
        }

        post.PostUrl = urlAfterClick;
        Console.WriteLine($"[FB Group] Mở modal posinset={posinset}, URL = {post.PostUrl}");

        // Scroll trong modal để load comment
        await page.Mouse.MoveAsync(950, 500);
        int previousCount = 0;
        int noChangeStreak = 0;

        while (true)
        {
            var currentCount = await page.Locator("div[role='article'][aria-label]").CountAsync();
            if (currentCount == previousCount)
            {
                noChangeStreak++;
                if (noChangeStreak >= 3) break;
            }
            else
            {
                noChangeStreak = 0;
            }
            previousCount = currentCount;

            await page.Mouse.WheelAsync(0, 800);
            await page.WaitForTimeoutAsync(1500);
        }

        var commentEls = await page.Locator("div[role='article'][aria-label]").AllAsync();
        foreach (var c in commentEls)
        {
            try
            {
                var textEls = await c.Locator("div[dir='auto']").AllAsync();
                foreach (var t in textEls)
                {
                    var txt = await t.InnerTextAsync();
                    if (!string.IsNullOrWhiteSpace(txt) && txt.Length > 3)
                    {
                        post.Comments.Add(txt.Trim());
                        break;
                    }
                }
            }
            catch { }
        }

        Console.WriteLine($"[FB Group] posinset={posinset} có {post.Comments.Count} comments");

        // Đóng modal — hoặc quay lại trang search nếu bị redirect tới trang lỗi khác
        var closeBtn = await page.QuerySelectorAsync("div[aria-label='Đóng'], div[aria-label='Close']");
        if (closeBtn != null)
        {
            await closeBtn.ClickAsync();
            await page.WaitForTimeoutAsync(1500);
        }
        else
        {
            // Không có nút đóng — có thể đã rời khỏi trang search, navigate quay lại
            Console.WriteLine($"[FB Group] posinset={posinset} không tìm thấy nút đóng, quay lại trang search trực tiếp");
            await page.GoBackAsync();
            await page.WaitForTimeoutAsync(2000);
        }

        return post;
    }
}