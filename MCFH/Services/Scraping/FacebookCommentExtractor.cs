using Microsoft.Playwright;

namespace MCFH.Services.Scraping;

public static class FacebookCommentExtractor
{
    public static async Task TrySortAllCommentsAsync(IPage page)
    {
        foreach (var label in new[] { "Most relevant", "Liên quan nhất", "Phù hợp nhất" })
        {
            try
            {
                var sortBtn = page.Locator($"div[role='button']:has-text('{label}')").First;
                if (!await sortBtn.IsVisibleAsync()) continue;

                await sortBtn.ClickAsync();
                await page.WaitForTimeoutAsync(800);

                foreach (var option in new[] { "All comments", "Tất cả bình luận", "All" })
                {
                    var opt = page.Locator($"div[role='option']:has-text('{option}')").First;
                    if (await opt.IsVisibleAsync())
                    {
                        await opt.ClickAsync();
                        await page.WaitForTimeoutAsync(1500);
                        return;
                    }
                }
            }
            catch { }
        }
    }

    public static async Task ScrollCommentsAsync(IPage page, int maxComments)
    {
        await page.Mouse.MoveAsync(784, 500);
        var previous = 0;
        var stale = 0;

        for (var i = 0; i < 15; i++)
        {
            var count = await page.Locator("ul[role='list'] div[role='article'], div[role='dialog'] div[role='article']").CountAsync();
            if (count >= maxComments) break;

            if (count == previous)
            {
                stale++;
                if (stale >= 3) break;
            }
            else stale = 0;

            previous = count;
            await page.Mouse.WheelAsync(0, 900);
            await page.WaitForTimeoutAsync(1200);
        }
    }

    public static async Task<List<string>> ExtractFromDomAsync(IPage page, int maxComments)
    {
        var raw = await page.EvaluateAsync<string[]>(@"
            () => {
                const results = [];
                const seen = new Set();

                const add = (text) => {
                    const s = (text || '').replace(/\s+/g, ' ').trim();
                    if (!s || seen.has(s)) return;
                    seen.add(s);
                    results.push(s);
                };

                const isMetaLabel = (s) =>
                    /^(Bình luận|Comment)\s+(của|by|dưới tên|from)\b/i.test(s) ||
                    /\b\d+\s*(phút|giờ|ngày|tuần|tháng|năm|h|m|d|w|y)\s+trước$/i.test(s);

                const isAction = (s) =>
                    /^(Like|Thích|Reply|Phản hồi|Share|Chia sẻ|Follow|Theo dõi|Send|Gửi|Ẩn|Hide)$/i.test(s);

                const hasSentenceHint = (s) =>
                    /\b(là|có|không|sao|hả|ạ|nhé|được|nên|sẽ|rất|quá|và|nhưng|thì|mà|để|cho|với|trong|ngoài|người|thôi|nhỉ|ơi|gì|đâu|vậy|đi|đó|này|kia|mình|tôi|bạn|anh|chị|em)\b/i.test(s);

                const looksLikeNameTagOnly = (s) => {
                    if (!s || s.length > 45) return false;
                    if (s.includes('@')) return true;
                    if (/[.!?,:;]/.test(s)) return false;
                    if (hasSentenceHint(s)) return false;
                    const words = s.split(/\s+/).filter(Boolean);
                    if (words.length < 1 || words.length > 4) return false;
                    return words.every(w => /^[A-ZĐÀÁẢÃẠÂẤẦẨẪẬĂẮẰẲẴẶÉÈẺẼẸÊẾỀỂỄỆÍÌỈĨỊÓÒỎÕỌÔỐỒỔỖỘƠỚỜỞỠỢÚÙỦŨỤƯỨỪỬỮỰÝỲỶỸỴ0-9]/.test(w));
                };

                const extractFromArticle = (article) => {
                    const authorEl = article.querySelector('a[role=""link""] span[dir=""auto""], a[role=""link""] b, a[role=""link""] strong');
                    const authorName = (authorEl?.textContent || '').replace(/\s+/g, ' ').trim();

                    article.querySelectorAll('div[dir=""auto""], span[dir=""auto""]').forEach(el => {
                        const parentDir = el.parentElement?.closest('div[dir=""auto""], span[dir=""auto""]');
                        if (parentDir && parentDir !== el) return;

                        const text = (el.textContent || '').replace(/\s+/g, ' ').trim();
                        if (text.length < 3) return;
                        if (isMetaLabel(text) || isAction(text)) return;
                        if (authorName && text === authorName) return;

                        const inAuthorLink = el.closest('a[role=""link""]');
                        if (inAuthorLink) {
                            const linkText = (inAuthorLink.textContent || '').replace(/\s+/g, ' ').trim();
                            if (text === linkText) return;
                        }

                        const mentionLinks = el.querySelectorAll('a[role=""link""]');
                        if (mentionLinks.length === 1 && mentionLinks[0].textContent?.trim() === text && text.length < 40)
                            return;

                        if (looksLikeNameTagOnly(text)) return;

                        add(text);
                    });
                };

                document.querySelectorAll('ul[role=""list""]').forEach(list => {
                    list.querySelectorAll(':scope > div[role=""article""], :scope > li div[role=""article""]').forEach(extractFromArticle);
                });

                if (results.length === 0) {
                    document.querySelectorAll('div[role=""dialog""] ul[role=""list""] div[role=""article""]').forEach(extractFromArticle);
                }

                return results;
            }
        ");

        return CommentTextHelper.FilterFacebook(raw, maxComments);
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
