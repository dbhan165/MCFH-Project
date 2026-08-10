using Microsoft.Playwright;

namespace MCFH.Services.Scraping;

public static class FacebookCommentExtractor
{
    public static async Task TrySortAllCommentsAsync(IPage page)
    {
        var sortPatterns = new[]
        {
            "Phù hợp nhất",
            "Most relevant",
            "Liên quan nhất",
            "Most recent"
        };

        foreach (var text in sortPatterns)
        {
            try
            {
                var sortBtn = page.Locator("div[role='button']").Filter(new LocatorFilterOptions { HasText = text }).First;
                if (!await sortBtn.IsVisibleAsync()) continue;

                await sortBtn.ClickAsync(new LocatorClickOptions { Timeout = 5000 });
                await page.WaitForTimeoutAsync(800);

                var options = new[] { "Tất cả bình luận", "All comments", "All", "Mới nhất trước", "Newest first" };

                foreach (var optText in options)
                {
                    try
                    {
                        var opt = page.Locator("div[role='option']").Filter(new LocatorFilterOptions { HasText = optText }).First;
                        if (await opt.IsVisibleAsync())
                        {
                            await opt.ClickAsync(new LocatorClickOptions { Timeout = 3000 });
                            await page.WaitForTimeoutAsync(1500);
                            Console.WriteLine("[FB Comment] Da chuyen sang: " + optText);
                            return;
                        }
                    }
                    catch { }
                }
            }
            catch { }
        }
    }

    public static async Task ScrollCommentsAsync(IPage page, int maxComments)
    {
        await page.Mouse.MoveAsync(784, 500);
        var previous = -1;
        var stale = 0;

        for (var i = 0; i < 25; i++)
        {
            var count = await page.Locator("ul[role='list'] div[role='article']").CountAsync();
            if (count == 0)
            {
                count = await page.Locator("div[role='article']").CountAsync();
            }

            if (count >= maxComments) break;

            if (count == previous)
            {
                stale++;
                if (stale >= 4) break;
            }
            else stale = 0;

            previous = count;

            await TryClickViewMoreCommentsAsync(page);

            await page.Mouse.WheelAsync(0, 900);
            await page.WaitForTimeoutAsync(1500);
        }

        // Đợi thêm cho comments lazy-load xuất hiện
        await page.WaitForTimeoutAsync(1500);
    }

    private static async Task TryClickViewMoreCommentsAsync(IPage page)
    {
        var patterns = new[]
        {
            "Xem thêm bình luận",
            "View more comments",
            "Xem thêm",
            "View more"
        };

        foreach (var text in patterns)
        {
            try
            {
                var btn = page.Locator("div[role='button'], div[role='link'], span").Filter(new LocatorFilterOptions { HasText = text }).First;
                if (await btn.CountAsync() > 0 && await btn.IsVisibleAsync())
                {
                    await btn.ClickAsync(new LocatorClickOptions { Timeout = 3000 });
                    await page.WaitForTimeoutAsync(1000);
                    return;
                }
            }
            catch { }
        }
    }

    public static async Task<List<string>> ExtractFromDomAsync(IPage page, int maxComments)
    {
        string script = @"
function() {
    var results = [];
    var seen = {};

    function add(text) {
        var s = (text || '').replace(/\s+/g, ' ').trim();
        if (!s || seen[s]) return;
        if (s.length < 3 || s.length > 2000) return;
        seen[s] = true;
        results.push(s);
    }

    function isMetaLabel(s) {
        return /^(Binh luan|Comment|Phan hoi|Reply|Thich|Like|Chia se|Share|Theo doi|Follow|Gui|Send)\s*/i.test(s) ||
               /\b\d+\s*(phut|gio|ngay|tuan|thang|nam)\s+truoc$/i.test(s);
    }

    function isActionOnly(s) {
        return /^(Thich|Like|Phan hoi|Reply|Chia se|Share)$/i.test(s);
    }

    function looksLikeNameOnly(s) {
        if (!s || s.length > 30) return false;
        var words = s.split(/\s+/).filter(Boolean);
        if (words.length <= 2 && /^[A-Z]/.test(s)) {
            return !/[.!?,:;]/.test(s);
        }
        return false;
    }

    // Tìm comment container qua nhiều selector phổ biến trong FB
    var commentSelectors = [
        'div[role=""article""]',
        'ul[role=""list""] > li',
        'div[aria-label*=""comment""]',
        'div[aria-label*=""Comment""]'
    ];

    var articles = [];
    for (var s = 0; s < commentSelectors.length; s++) {
        var found = document.querySelectorAll(commentSelectors[s]);
        if (found.length >= articles.length) articles = Array.from(found);
    }
    if (!articles.length) return results;

    for (var i = 0; i < articles.length; i++) {
        var article = articles[i];

        // Thử nhiều cách lấy text: dir=auto, span text, generic blocks
        var candidates = [];

        // 1. div[dir='auto'] (cách cũ)
        var dirAuto = article.querySelectorAll('div[dir=""auto""]');
        for (var j = 0; j < dirAuto.length; j++) candidates.push(dirAuto[j]);

        // 2. span[dir='auto']
        var spanAuto = article.querySelectorAll('span[dir=""auto""]');
        for (var k = 0; k < spanAuto.length; k++) candidates.push(spanAuto[k]);

        // 3. div với role='text'
        var roleText = article.querySelectorAll('div[role=""text""]');
        for (var t = 0; t < roleText.length; t++) candidates.push(roleText[t]);

        // 4. nested span có nhiều text (đoạn dài)
        if (candidates.length === 0) {
            var allSpans = article.querySelectorAll('span');
            for (var u = 0; u < allSpans.length; u++) {
                var txt = (allSpans[u].textContent || '').trim();
                if (txt.length >= 5 && txt.length <= 1000 && txt.indexOf('\n') === -1) {
                    candidates.push(allSpans[u]);
                }
            }
        }

        for (var c = 0; c < candidates.length; c++) {
            var el = candidates[c];
            // Bỏ qua nếu có parent chứa dir='auto' (nested)
            var parent = el.parentElement;
            var skipNested = false;
            while (parent && parent !== article) {
                var pdir = parent.getAttribute && parent.getAttribute('dir');
                if (pdir === 'auto' && parent !== el) { skipNested = true; break; }
                parent = parent.parentElement;
            }
            if (skipNested) continue;

            var text = (el.textContent || '').replace(/\s+/g, ' ').trim();
            if (text.length < 5) continue;
            if (isMetaLabel(text)) continue;
            if (isActionOnly(text)) continue;
            if (looksLikeNameOnly(text)) continue;

            // Bỏ nếu text trùng author name
            var authorLinks = article.querySelectorAll('h2 a[role=""link""], strong a[role=""link""]');
            var isAuthor = false;
            for (var k2 = 0; k2 < authorLinks.length; k2++) {
                if ((authorLinks[k2].textContent || '').trim() === text) {
                    isAuthor = true;
                    break;
                }
            }
            if (isAuthor) continue;

            add(text);
        }
    }

    return results;
}";

        try
        {
            var raw = await page.EvaluateAsync<string[]>(script);
            return CommentTextHelper.FilterFacebook(raw, maxComments);
        }
        catch (Exception ex)
        {
            Console.WriteLine("[FB Comment] Loi extract: " + ex.Message);
            return new List<string>();
        }
    }

    public static async Task<List<string>> ScrapeFromPostUrlAsync(IPage page, string postUrl, int maxComments)
    {
        await page.GotoAsync(postUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded,
            Timeout = 45000
        });
        await page.WaitForTimeoutAsync(2500);

        if (page.Url.Contains("/videos/", StringComparison.OrdinalIgnoreCase)
            || page.Url.Contains("/reel/", StringComparison.OrdinalIgnoreCase))
            return new List<string>();

        await TrySortAllCommentsAsync(page);
        await ScrollCommentsAsync(page, maxComments);
        return await ExtractFromDomAsync(page, maxComments);
    }
}
