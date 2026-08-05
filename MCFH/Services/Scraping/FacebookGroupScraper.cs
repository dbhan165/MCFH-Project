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

        Console.WriteLine("[FB Group] Navigating to: " + groupUrl);
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
            var count = await page.Locator("div[role='article']").CountAsync();
            Console.WriteLine("[FB Group] Scroll " + (i + 1) + ": " + count + " posts trong DOM");

            if (count >= maxPosts)
                break;

            await page.Mouse.WheelAsync(0, fast ? 2000 : 1500);
            await page.WaitForTimeoutAsync(scrollWait);
        }

        var postCount = await page.Locator("div[role='article']").CountAsync();
        Console.WriteLine("[FB Group] Tổng articles: " + postCount);

        // Dump DOM snippet của 5 article đầu để debug
        var dumpCount = Math.Min(5, postCount);
        for (int d = 0; d < dumpCount; d++)
        {
            try
            {
                var dumpLoc = page.Locator("div[role='article']").Nth(d);
                var dumpHtml = await dumpLoc.InnerHTMLAsync();
                Console.WriteLine($"[FB Group] DOM_SNIPPET #{d + 1} length={dumpHtml.Length} preview={dumpHtml.Substring(0, Math.Min(400, dumpHtml.Length)).Replace("\n", " ")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FB Group] DOM dump #{d + 1} lỗi: {ex.Message}");
            }
        }

        // Bỏ qua profile cards (chỉ chứa <a href=".../profile..."> và avatar)
        // → thử nhiều selector nội dung: div[dir='auto'], div[data-ad-preview="message"], span[dir='auto'], div.story_body_container
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var noContentCount = 0;
        for (int idx = 0; idx < Math.Min(postCount, maxPosts + 30); idx++)
        {
            try
            {
                var postLocator = page.Locator("div[role='article']").Nth(idx);
                var groupPost = feedOnly
                    ? await ExtractPostFromFeedAsync(page, postLocator)
                    : await OpenPostAndScrapeAsync(page, postLocator, options);

                if (groupPost == null || string.IsNullOrWhiteSpace(groupPost.Text))
                {
                    noContentCount++;
                    if (noContentCount >= 4) break; // nhiều post skeleton liên tiếp → dừng
                    continue;
                }

                noContentCount = 0;

                // De-dupe theo PostUrl
                if (!string.IsNullOrWhiteSpace(groupPost.PostUrl) && !seenUrls.Add(groupPost.PostUrl))
                {
                    Console.WriteLine("[FB Group] Duplicate URL, skip: " + groupPost.PostUrl);
                    continue;
                }

                results.Add(groupPost);
                if (results.Count >= maxPosts) break;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[FB Group] Lỗi post #" + (idx + 1) + ": " + ex.Message);
            }
        }

        Console.WriteLine("[FB Group] Final: " + results.Count + " posts");
        return results;
    }

    private async Task<GroupPost?> ExtractPostFromFeedAsync(IPage page, ILocator postLocator)
    {
        var post = new GroupPost();

        try
        {
            var authorLocator = postLocator.Locator("h2 span a[role='link']").First;
            if (await authorLocator.CountAsync() > 0)
                post.Author = await authorLocator.InnerTextAsync();
        }
        catch { }

        try
        {
            var contentLocator = postLocator.Locator("div[dir='auto']").First;
            if (await contentLocator.CountAsync() > 0)
            {
                post.Text = await contentLocator.InnerTextAsync();
                await TryClickSeeMoreAsync(postLocator);
                post.Text = await contentLocator.InnerTextAsync();
            }
        }
        catch { }

        if (string.IsNullOrWhiteSpace(post.Text))
            return null;

        post.PostedAt = await TryExtractFacebookPostedAtAsync(postLocator);

        var linkCount = await postLocator.Locator("a[role='link']").CountAsync();
        for (int i = 0; i < linkCount; i++)
        {
            var link = postLocator.Locator("a[role='link']").Nth(i);
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

    private async Task<GroupPost?> OpenPostAndScrapeAsync(IPage page, ILocator postLocator, ScrapeOptions options)
    {
        var post = new GroupPost();

        // Chờ element có nội dung thật (không phải skeleton loading)
        try
        {
            for (var w = 0; w < 8; w++)
            {
                var hasContent = await postLocator.EvaluateAsync<bool>(@"el => {
                    if (!el) return false;
                    if (el.querySelector('[aria-label*=""Đang tải""], [aria-label*=""Loading""]')) return false;
                    var dirAuto = el.querySelectorAll('div[dir=""auto""]');
                    if (dirAuto.length === 0) return false;
                    var first = dirAuto[0];
                    var t = (first.textContent || '').trim();
                    return t.length >= 5;
                }");
                if (hasContent) break;
                await page.WaitForTimeoutAsync(500);
            }
        }
        catch { }

        try
        {
            var authorLocator = postLocator.Locator("h2 span a[role='link']").First;
            if (await authorLocator.CountAsync() > 0)
                post.Author = await authorLocator.InnerTextAsync();
        }
        catch { }

        try
        {
            var contentLocator = postLocator.Locator("div[dir='auto']").First;
            if (await contentLocator.CountAsync() > 0)
            {
                post.Text = await contentLocator.InnerTextAsync();
                await TryClickSeeMoreAsync(postLocator);
                post.Text = await contentLocator.InnerTextAsync();
            }
            else
            {
                var outerHtml = await postLocator.EvaluateAsync<string>("el => el.outerHTML.substring(0, 1200)");
                Console.WriteLine("[FB Group] DEBUG: post khong co div[dir='auto']. Snippet=" + outerHtml);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[FB Group] DEBUG: ExtractText error=" + ex.Message);
        }

        if (string.IsNullOrWhiteSpace(post.Text))
        {
            Console.WriteLine("[FB Group] Post khong co noi dung, skip");
            return null;
        }

        post.PostedAt = await TryExtractFacebookPostedAtAsync(postLocator);

        string? permalinkHref = null;
        ILocator? timestampLink = null;

        var linkCount = await postLocator.Locator("a[role='link']").CountAsync();
        for (int i = 0; i < linkCount; i++)
        {
            var link = postLocator.Locator("a[role='link']").Nth(i);
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
            Console.WriteLine("[FB Group] Khong tim duoc timestamp link");
            return post;
        }

        post.PostUrl = NormalizeFacebookUrl(permalinkHref);

        var urlBeforeClick = page.Url;
        bool navigatedToPermalink = false;

        try
        {
            // Thử navigate trực tiếp permalink để DOM chỉ chứa 1 post (không lẫn feed background).
            await page.GotoAsync(post.PostUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.DOMContentLoaded,
                Timeout = 45000
            });
            await page.WaitForTimeoutAsync(3000);
            navigatedToPermalink = IsFacebookPostPermalink(page.Url) && page.Url != urlBeforeClick;
        }
        catch (Exception ex)
        {
            Console.WriteLine("[FB Group] Goto permalink that bai, fallback click: " + ex.Message);
        }

        if (!navigatedToPermalink)
        {
            try
            {
                await timestampLink.ClickAsync(new LocatorClickOptions { Timeout = 15000 });
                await page.WaitForTimeoutAsync(2500);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[FB Group] Click that bai: " + ex.Message);
                return post;
            }
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
            Console.WriteLine("[FB Group] Click sai dich, back lai");
            await page.GoBackAsync();
            await page.WaitForTimeoutAsync(2000);
            return post;
        }

        post.PostedAt ??= await TryExtractFacebookPostedAtFromPageAsync(page);

        Console.WriteLine("[FB Group] Mo thanh cong, URL = " + post.PostUrl);

        var fbMax = Math.Max(options.FacebookMaxComments, options.MaxCommentsPerItem);
        await FacebookCommentExtractor.TrySortAllCommentsAsync(page);
        await FacebookCommentExtractor.ScrollCommentsAsync(page, fbMax);
        post.Comments = await FacebookCommentExtractor.ExtractFromDomAsync(page, fbMax);

        Console.WriteLine("[FB Group] Co " + post.Comments.Count + " comments");

        // Quay lại feed để scrape post tiếp theo
        try
        {
            if (navigatedToPermalink)
            {
                await page.GoBackAsync(new PageGoBackOptions { Timeout = 15000 });
                await page.WaitForTimeoutAsync(2500);

                // Sau khi back, feed cần re-render. Scroll nhẹ để trigger lazy-load.
                await page.Mouse.MoveAsync(640, 400);
                for (var i = 0; i < 3; i++)
                {
                    await page.Mouse.WheelAsync(0, 600);
                    await page.WaitForTimeoutAsync(800);
                }
            }
            else
            {
                await CloseModalIfExistsAsync(page);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[FB Group] GoBack that bai: " + ex.Message);
        }

        return post;
    }

    private static async Task TryClickSeeMoreAsync(ILocator container)
    {
        var seeMorePatterns = new[]
        {
            "Xem thêm",
            "See more",
            "See earlier"
        };

        foreach (var text in seeMorePatterns)
        {
            try
            {
                var seeMore = container.GetByText(text).First;
                if (await seeMore.CountAsync() > 0 && await seeMore.IsVisibleAsync())
                {
                    await seeMore.ClickAsync(new LocatorClickOptions { Timeout = 3000 });
                    await Task.Delay(500);
                    return;
                }
            }
            catch { }
        }
    }

    private static async Task CloseModalIfExistsAsync(IPage page)
    {
        var closePatterns = new[] { "Đóng", "Close", "Back" };

        foreach (var label in closePatterns)
        {
            try
            {
                var closeBtn = page.Locator("div[aria-label='" + label + "']").First;
                if (await closeBtn.CountAsync() > 0 && await closeBtn.IsVisibleAsync())
                {
                    await closeBtn.ClickAsync(new LocatorClickOptions { Timeout = 3000 });
                    await page.WaitForTimeoutAsync(1500);
                    return;
                }
            }
            catch { }
        }

        if (page.Url.Contains("/posts/") || page.Url.Contains("pfbid"))
        {
            Console.WriteLine("[FB Group] Quay lai trang truoc");
            await page.GoBackAsync();
            await page.WaitForTimeoutAsync(2000);
        }
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
        var url = href.StartsWith('/') ? "https://www.facebook.com" + href : href;
        var q = url.IndexOf('?');
        if (q < 0) return url;

        var query = url.Substring(q + 1);
        var keep = query.Split('&')
            .Where(p => p.StartsWith("id=", StringComparison.OrdinalIgnoreCase)
                        || p.StartsWith("story_fbid=", StringComparison.OrdinalIgnoreCase)
                        || p.StartsWith("fbid=", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var baseUrl = url.Substring(0, q);
        return keep.Count > 0 ? baseUrl + "?" + string.Join('&', keep) : baseUrl;
    }

    private static async Task<DateTime?> TryExtractFacebookPostedAtAsync(ILocator element)
    {
        try
        {
            string? raw = null;

            var linkCount = await element.Locator("a[role='link']").CountAsync();
            for (int i = 0; i < linkCount; i++)
            {
                var link = element.Locator("a[role='link']").Nth(i);
                var ariaLabel = await link.GetAttributeAsync("aria-label");
                if (!string.IsNullOrWhiteSpace(ariaLabel) && IsTimeRelated(ariaLabel))
                {
                    raw = ariaLabel;
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(raw))
            {
                var allText = await element.InnerTextAsync();
                raw = ExtractTimeText(allText);
            }

            if (!string.IsNullOrWhiteSpace(raw) && PostedAtParser.TryParseAny(raw, out var parsed))
            {
                Console.WriteLine($"[FB Group] PostedAt in-feed raw='{raw}' -> {parsed:yyyy-MM-dd HH:mm:ss}");
                return parsed;
            }
        }
        catch { }

        return null;
    }

    private static async Task<DateTime?> TryExtractFacebookPostedAtFromPageAsync(IPage page)
    {
        try
        {
            string? raw = await page.EvaluateAsync<string?>(@"function() {
                function vnToEn(s) {
                    return s.replace(/gio/g, 'hour')
                            .replace(/giờ/g, 'hour')
                            .replace(/phut/g, 'minute')
                            .replace(/phút/g, 'minute')
                            .replace(/ngay/g, 'day')
                            .replace(/ngày/g, 'day')
                            .replace(/tuan/g, 'week')
                            .replace(/tuần/g, 'week')
                            .replace(/thang/g, 'month')
                            .replace(/tháng/g, 'month')
                            .replace(/truoc/g, 'ago')
                            .replace(/trước/g, 'ago');
                }

                // 1. a[role=link][aria-label] chứa time text
                var links = document.querySelectorAll('a[role=""link""][aria-label]');
                for (var i = 0; i < links.length; i++) {
                    var label = links[i].getAttribute('aria-label');
                    if (label && /\d+\s*(gio|giờ|ngay|ngày|tuan|tuần|thang|tháng|phut|phút|hour|day|week|month|minute)/i.test(label)) {
                        return label;
                    }
                }

                // 2. <time datetime='...'>
                var timeEl = document.querySelector('time[datetime]');
                if (timeEl) return timeEl.getAttribute('datetime');

                // 3. <abbr title='...'>
                var abbrs = document.querySelectorAll('abbr[title]');
                for (var i2 = 0; i2 < abbrs.length; i2++) {
                    var t = abbrs[i2].getAttribute('title');
                    if (t && /\d{4}|\d+\s*(day|hour|week|month)/i.test(t)) return t;
                }

                // 4. span/[title] chứa ISO date
                var titled = document.querySelectorAll('[title]');
                for (var i3 = 0; i3 < titled.length; i3++) {
                    var t2 = titled[i3].getAttribute('title');
                    if (!t2) continue;
                    var dt = Date.parse(t2);
                    if (!isNaN(dt)) return new Date(dt).toISOString();
                }

                // 5. text chứa 'X ngày/giờ trước' hoặc 'X hours/days ago'
                var all = document.querySelectorAll('span, a');
                for (var i4 = 0; i4 < all.length; i4++) {
                    var txt = (all[i4].textContent || '').trim();
                    if (!txt || txt.length > 80) continue;
                    if (/\d+\s*(gio|giờ|ngay|ngày|tuan|tuần|thang|tháng|phut|phút)\s*(truoc|trước)?/i.test(txt) ||
                        /\d+\s*(hour|day|week|month|minute)s?\s*(ago)?/i.test(txt)) {
                        return vnToEn(txt);
                    }
                }

                return null;
            }");

            if (!string.IsNullOrWhiteSpace(raw))
            {
                Console.WriteLine("[FB Group] PostedAt raw: " + raw);
                if (PostedAtParser.TryParseAny(raw, out var parsed))
                    return parsed;
                Console.WriteLine("[FB Group] PostedAt parse FAIL: " + raw);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[FB Group] PostedAt extract error: " + ex.Message);
        }

        return null;
    }

    private static bool IsTimeRelated(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;
        var pattern1 = @"\b(giờ|ngày|tuần|tháng|phút|trước|hour|day|week|month|minute|ago|yesterday|today|just now)";
        var pattern2 = @"^\d{1,2}\s*[\/\-]\s*\d{1,2}(\/\d{2,4})?$";
        return System.Text.RegularExpressions.Regex.IsMatch(text, pattern1, System.Text.RegularExpressions.RegexOptions.IgnoreCase)
               || System.Text.RegularExpressions.Regex.IsMatch(text.Trim(), pattern2);
    }

    private static string? ExtractTimeText(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var match = System.Text.RegularExpressions.Regex.Match(text,
            @"((\d+)\s*(giờ|ngày|tuần|tháng|phút|hour|day|week|month|minute)s?\s*(trước|ago)?|\d{1,2}\s*[\/\-]\s*\d{1,2})",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        return match.Success ? match.Value : null;
    }

    private static async Task DismissCommonOverlaysAsync(IPage page)
    {
        var labels = new[] { "Close", "Đóng", "Allow all cookies", "Cho phép tất cả cookie", "Từ chối cookie không cần thiết" };
        foreach (var label in labels)
        {
            try
            {
                var btn = page.GetByRole(AriaRole.Button, new() { Name = label });
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
