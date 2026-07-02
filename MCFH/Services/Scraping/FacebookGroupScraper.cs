using MCFH.Configuration;
using Microsoft.Playwright;
using MCFH.Models.Scraping;

namespace MCFH.Services.Scraping;

public class FacebookGroupScraper
{
    public async Task<List<GroupPost>> ScrapeAsync(
        string groupUrl, int maxPosts, ScrapeOptions? options = null, bool feedOnly = false, Proxy? proxy = null)
    {
        options ??= new ScrapeOptions();
        var fast = options.FastDemoMode;
        feedOnly = feedOnly || options.FacebookFeedOnly || (fast && options.FastDemoFacebookFeedOnly);
        var results = new List<GroupPost>();

        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(
            PlaywrightScrapeHelper.SocialLaunch(options, proxy));

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
        await page.WaitForTimeoutAsync(fast ? 800 : 1500);

        await DismissCommonOverlaysAsync(page);
        await page.Mouse.MoveAsync(640, 400);

        var maxScrolls = fast ? 4 : 12;
        var scrollWait = fast ? 500 : 1000;

        for (int i = 0; i < maxScrolls; i++)
        {
            var count = await page.QuerySelectorAllAsync("div[aria-posinset]");
            Console.WriteLine($"[FB Group] Scroll {i + 1}: {count.Count} posts trong DOM");

            if (count.Count >= maxPosts)
                break;

            await page.Mouse.WheelAsync(0, fast ? 2000 : 1500);
            await page.WaitForTimeoutAsync(scrollWait);
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
                var post = feedOnly
                    ? await ExtractPostFromFeedAsync(page, posinset)
                    : await OpenPostAndScrapeAsync(page, posinset, options);
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

    /// <summary>Lấy nhanh từ feed — không mở modal/permalink (phù hợp demo).</summary>
    private async Task<GroupPost?> ExtractPostFromFeedAsync(IPage page, string posinset)
    {
        var postEl = await page.QuerySelectorAsync($"div[aria-posinset='{posinset}']");
        if (postEl == null) return null;

        var textEl = await postEl.QuerySelectorAsync("div[data-ad-rendering-role='story_message']");
        var text = textEl != null ? await textEl.InnerTextAsync() : "";

        var authorEl = await postEl.QuerySelectorAsync("div[data-ad-rendering-role='profile_name'] a b span");
        var author = authorEl != null ? await authorEl.InnerTextAsync() : "";

        if (string.IsNullOrWhiteSpace(text))
            return null;

        var post = new GroupPost { Author = author, Text = text };
        post.PostedAt = await TryExtractFacebookPostedAtAsync(postEl);

        foreach (var link in await postEl.QuerySelectorAllAsync("a[role='link']"))
        {
            var href = await link.GetAttributeAsync("href");
            if (string.IsNullOrWhiteSpace(href) || IsSkippedFacebookLink(href)) continue;
            if (IsFacebookPostPermalink(href))
            {
                post.PostUrl = NormalizeFacebookUrl(href);
                break;
            }
        }

        return post;
    }

    private async Task<GroupPost?> OpenPostAndScrapeAsync(IPage page, string posinset, ScrapeOptions options)
    {
        var postEl = await page.QuerySelectorAsync($"div[aria-posinset='{posinset}']");
        if (postEl == null) return null;

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

        post.PostedAt = await TryExtractFacebookPostedAtAsync(postEl);
        var candidateLinks = await postEl.QuerySelectorAllAsync("a[role='link']");
        IElementHandle? timestampLink = null;
        string? permalinkHref = null;

        foreach (var link in candidateLinks)
        {
            var href = await link.GetAttributeAsync("href");
            if (string.IsNullOrWhiteSpace(href)) continue;
            if (IsSkippedFacebookLink(href)) continue;

            if (IsFacebookPostPermalink(href))
            {
                timestampLink = link;
                permalinkHref = href;
                break;
            }

            timestampLink ??= link;
            permalinkHref ??= href;
        }

        if (timestampLink == null || string.IsNullOrWhiteSpace(permalinkHref))
        {
            Console.WriteLine($"[FB Group] posinset={posinset} không tìm được timestamp link, giữ post không URL/comment");
            return post;
        }

        post.PostUrl = NormalizeFacebookUrl(permalinkHref);

        var urlBeforeClick = page.Url;
        try
        {
            await timestampLink.ClickAsync(new ElementHandleClickOptions { Timeout = 15_000 });
            await page.WaitForTimeoutAsync(2500);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[FB Group] posinset={posinset} click thất bại — giữ bài từ feed: {ex.Message}");
            return post;
        }

        var urlAfterClick = page.Url;
        if (urlAfterClick != urlBeforeClick
            && !urlAfterClick.Contains("/search/", StringComparison.OrdinalIgnoreCase)
            && !urlAfterClick.Contains("/stories/", StringComparison.OrdinalIgnoreCase)
            && IsFacebookPostPermalink(urlAfterClick))
        {
            post.PostUrl = NormalizeFacebookUrl(urlAfterClick);
        }
        else if (urlAfterClick != urlBeforeClick
                 && (urlAfterClick.Contains("/search/", StringComparison.OrdinalIgnoreCase)
                     || urlAfterClick.Contains("/stories/", StringComparison.OrdinalIgnoreCase)))
        {
            Console.WriteLine($"[FB Group] posinset={posinset} click sai đích, back lại trang search");
            await page.GoBackAsync();
            await page.WaitForTimeoutAsync(2000);
            return post;
        }
        post.PostedAt ??= await TryExtractFacebookPostedAtAsync(page);

        Console.WriteLine($"[FB Group] posinset={posinset} mở thành công, URL = {post.PostUrl}");

        // Scroll + lấy comment text trong modal/post
        var fbMax = Math.Max(options.FacebookMaxComments, options.MaxCommentsPerItem);
        await FacebookCommentExtractor.TrySortAllCommentsAsync(page);
        await FacebookCommentExtractor.ScrollCommentsAsync(page, fbMax);
        post.Comments = await FacebookCommentExtractor.ExtractFromDomAsync(page, fbMax);

        Console.WriteLine($"[FB Group] posinset={posinset} có {post.Comments.Count} comments text");

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

    private static bool IsSkippedFacebookLink(string href) =>
        href.Contains("/user/", StringComparison.OrdinalIgnoreCase)
        || href.Contains("/stories/", StringComparison.OrdinalIgnoreCase)
        || href.Contains("/hashtag/", StringComparison.OrdinalIgnoreCase)
        || href.Contains("/friends/", StringComparison.OrdinalIgnoreCase)
        || href.Contains("l.facebook.com", StringComparison.OrdinalIgnoreCase);

    private static bool IsFacebookPostPermalink(string href) =>
        href.Contains("/posts/", StringComparison.OrdinalIgnoreCase)
        || href.Contains("pfbid", StringComparison.OrdinalIgnoreCase)
        || href.Contains("permalink.php", StringComparison.OrdinalIgnoreCase)
        || href.Contains("story_fbid", StringComparison.OrdinalIgnoreCase)
        || href.Contains("/videos/", StringComparison.OrdinalIgnoreCase)
        || href.Contains("/photo/?fbid=", StringComparison.OrdinalIgnoreCase)
        || href.Contains("/photo.php", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeFacebookUrl(string href)
    {
        var url = href.StartsWith('/') ? $"https://www.facebook.com{href}" : href;
        var q = url.IndexOf('?');
        if (q < 0) return url;

        var query = url[(q + 1)..];
        var keep = query.Split('&')
            .Where(p => p.StartsWith("id=", StringComparison.OrdinalIgnoreCase)
                        || p.StartsWith("story_fbid=", StringComparison.OrdinalIgnoreCase)
                        || p.StartsWith("fbid=", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var baseUrl = url[..q];
        return keep.Count > 0 ? $"{baseUrl}?{string.Join('&', keep)}" : baseUrl;
    }

    private static async Task<DateTime?> TryExtractFacebookPostedAtAsync(IPage page) =>
        await TryExtractFacebookPostedAtFromHandleAsync(page);

    private static async Task<DateTime?> TryExtractFacebookPostedAtAsync(IElementHandle element) =>
        await TryExtractFacebookPostedAtFromHandleAsync(element);

    private static async Task<DateTime?> TryExtractFacebookPostedAtFromHandleAsync(object root)
    {
        try
        {
            string? raw = null;

            if (root is IPage page)
            {
                raw = await page.EvaluateAsync<string?>(@"() => {
                    const abbr = document.querySelector('abbr[data-utime]');
                    if (abbr?.getAttribute('data-utime')) return abbr.getAttribute('data-utime');
                    const time = document.querySelector('time[datetime]');
                    if (time?.getAttribute('datetime')) return time.getAttribute('datetime');
                    const stamp = document.querySelector('a[href*=""/posts/""] abbr, a[href*=""pfbid""] abbr');
                    if (stamp?.getAttribute('data-utime')) return stamp.getAttribute('data-utime');
                    const aria = document.querySelector('a[href*=""/posts/""][aria-label], a[href*=""pfbid""][aria-label]');
                    if (aria?.getAttribute('aria-label')) return aria.getAttribute('aria-label');
                    const span = document.querySelector('span[id*=""jsc_c""]');
                    if (span?.textContent) return span.textContent.trim();
                    return null;
                }");
            }
            else if (root is IElementHandle el)
            {
                raw = await el.EvaluateAsync<string?>(@"el => {
                    const abbr = el.querySelector('abbr[data-utime]');
                    if (abbr?.getAttribute('data-utime')) return abbr.getAttribute('data-utime');
                    const time = el.querySelector('time[datetime]');
                    if (time?.getAttribute('datetime')) return time.getAttribute('datetime');
                    const linkAbbr = el.querySelector('a abbr[data-utime]');
                    if (linkAbbr?.getAttribute('data-utime')) return linkAbbr.getAttribute('data-utime');
                    const aria = el.querySelector('a[aria-label]');
                    if (aria?.getAttribute('aria-label')) return aria.getAttribute('aria-label');
                    const spans = el.querySelectorAll('span');
                    for (const s of spans) {
                        const t = (s.textContent || '').trim();
                        if (/trước|ago|\d{4}/i.test(t) && t.length < 40) return t;
                    }
                    return null;
                }");
            }

            if (PostedAtParser.TryParseAny(raw, out var parsed))
                return parsed;
        }
        catch { }

        return null;
    }

    private static async Task DismissCommonOverlaysAsync(IPage page)
    {
        var labels = new[] { "Close", "Đóng", "Allow all cookies", "Cho phép tất cả cookie", "Từ chối cookie không cần thiết" };
        foreach (var label in labels)
        {
            try
            {
                var btn = page.GetByRole(AriaRole.Button, new() { Name = label, Exact = false });
                if (await btn.CountAsync() > 0)
                {
                    await btn.First.ClickAsync(new LocatorClickOptions { Timeout = 2000 });
                    await page.WaitForTimeoutAsync(400);
                }
            }
            catch { }
        }
    }
}