using MCFH.Configuration;
using MCFH.Models.Scraping;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace MCFH.Services.Scraping;

public static class ThreadsPostParser
{
    /// <summary>
    /// Per-container fallback: walk every <c>[data-pressable-container]</c> inside comment pagelets,
    /// grab the closest user link, and concatenate the visible text inside the container into a single
    /// comment string. This catches multi-line / emoji / non-Latin bodies that the per-span extractor misses.
    /// </summary>
    public static async Task<List<string>> ExtractFromPageletsAsync(
        IPage page,
        int maxComments,
        Action<string>? onStatus)
    {
        try
        {
            const string js = @"() => {
                const max = __MAX__;
                const rootSel = '[data-pagelet^=""threads_post_page_""]:not([data-pagelet=""threads_post_page_0""])';
                const results = [];
                const seen = new Set();

                const isUiText = (s) => {
                    if (!s) return true;
                    const t = s.trim();
                    if (!t) return true;
                    if (t.length < 2) return true;
                    return /^(Reply|Trả lời|Tra loi|Share|Chia sẻ|Chia se|Follow|Theo dõi|Theo doi|Translate|Tác giả|Tac gia|Loading|Dang tai|More|Like|Repost|Hide|Xem bản dịch|Xem hoạt động|Xem hoat dong|Pinned|Đã ghim|Saved|Lưu|Save)$/i.test(t);
                };
                const isTimestamp = (s) => /^\d{1,2}\/\d{1,2}\/\d{2,4}$/.test((s||'').trim());
                const isRelativeTime = (s) => {
                    const t = (s||'').trim();
                    return /^\d{1,3}\s*(ngày|giờ|phút|giây|tuần|tháng|năm|ngay|gio|phut|giay|tuan|thang|nam|d|h|w|m|y)$/i.test(t) ||
                           /^\d{1,3}[dhmwys]$/i.test(t);
                };
                const isNumberOnly = (s) => /^\d+$/.test((s||'').trim());
                const isLikesCount = (s) => /^[\d.,]+\s*[KkMm]$/.test((s||'').trim());
                const isNumericUsername = (s) => /^@\d+$/.test(s);

                const pagelets = document.querySelectorAll(rootSel);
                for (const pagelet of pagelets) {
                    const containers = pagelet.querySelectorAll('[data-pressable-container=""true""]');
                    for (const c of containers) {
                        const link = c.querySelector('a[href^=""/@""][role=""link""]');
                        if (!link) continue;
                        const m = (link.getAttribute('href') || '').match(/^\/@([^\/]+)/);
                        if (!m) continue;
                        const username = m[1];
                        if (isNumericUsername('@' + username)) continue;

                        const seen2 = new Set();
                        const parts = [];
                        const allSpans = c.querySelectorAll('span');
                        for (const s of allSpans) {
                            const t = (s.textContent || '').trim();
                            if (!t) continue;
                            if (isUiText(t)) continue;
                            if (isTimestamp(t)) continue;
                            if (isRelativeTime(t)) continue;
                            if (isNumberOnly(t)) continue;
                            if (isLikesCount(t)) continue;
                            if (t === username || t === '@' + username) continue;
                            const key = t.toLowerCase().substring(0, 80);
                            if (seen2.has(key)) continue;
                            seen2.add(key);
                            parts.push(t);
                        }
                        if (parts.length === 0) continue;

                        const body = parts.join(' ').replace(/\s+/g, ' ').trim();
                        if (!body) continue;

                        const text = '@' + username + ': ' + body;
                        const key = text.toLowerCase().substring(0, 120);
                        if (seen.has(key)) continue;
                        seen.add(key);
                        results.push(text);
                        if (results.length >= max) break;
                    }
                    if (results.length >= max) break;
                }
                return results;
            }";

            var finalJs = js.Replace("__MAX__", maxComments.ToString());
            var comments = await page.EvaluateAsync<string[]>(finalJs) ?? Array.Empty<string>();
            ThreadsLog.Debug($"Pagelet fallback extracted {comments.Length} comments");
            return comments.ToList();
        }
        catch (Exception ex)
        {
            ThreadsLog.Debug($"Pagelet fallback error: {ex.Message}");
            return new List<string>();
        }
    }

    /// <summary>
    /// Merge two comment lists with case-insensitive dedup, keeping DOM-extracted entries first.
    /// </summary>
    public static List<string> MergeCommentResults(List<string> primary, List<string> secondary, int max)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var merged = new List<string>(primary.Count + secondary.Count);
        foreach (var c in primary)
        {
            var key = c.Length >= 120 ? c.ToLowerInvariant().Substring(0, 120) : c.ToLowerInvariant();
            if (seen.Add(key)) merged.Add(c);
            if (merged.Count >= max) break;
        }
        if (merged.Count < max)
        {
            foreach (var c in secondary)
            {
                var key = c.Length >= 120 ? c.ToLowerInvariant().Substring(0, 120) : c.ToLowerInvariant();
                if (seen.Add(key)) merged.Add(c);
                if (merged.Count >= max) break;
            }
        }
        return merged;
    }

    public static async Task ScrapePostPageAsync(
        IPage page,
        ThreadsPost post,
        ScrapeOptions options,
        Action<string>? onStatus,
        Action? debugPause,
        ThreadsNetworkCapture? networkCapture = null,
        string? postIdHint = null)
    {
        var currentUrl = page.Url;
        ThreadsLog.Debug($"Post page URL: {currentUrl}");

        var urlMatch = Regex.Match(currentUrl, @"threads\.(?:net|com)/(@[\w.]+)/post/");
        if (urlMatch.Success)
        {
            post.AuthorUsername = urlMatch.Groups[1].Value;
            ThreadsLog.Debug($"Author from URL: {post.AuthorUsername}");
        }

        if (networkCapture != null && !string.IsNullOrEmpty(postIdHint))
        {
            if (networkCapture.TryGetPost(postIdHint, out var captured) && captured != null)
            {
                ThreadsLog.Debug($"[NetworkCapture] Got post: @{captured.Username}, {captured.Replies.Count} replies");
                if (!string.IsNullOrEmpty(captured.Username) && (string.IsNullOrEmpty(post.AuthorUsername) || post.AuthorUsername == "@"))
                    post.AuthorUsername = "@" + captured.Username;
                if (!string.IsNullOrEmpty(captured.Text) && string.IsNullOrEmpty(post.Text))
                    post.Text = captured.Text;
                if (captured.LikeCount > 0)
                    post.LikeCount = (int)captured.LikeCount;
                if (captured.CommentCount > 0)
                    post.CommentCount = (int)captured.CommentCount;

                if (captured.Replies.Count > 0)
                {
                    var commentTexts = captured.Replies
                        .Where(r => !string.IsNullOrWhiteSpace(r.Text))
                        .Select(r => r.Text)
                        .ToList();
                    if (commentTexts.Count > 0)
                    {
                        post.Comments = commentTexts;
                        ThreadsLog.Debug($"[NetworkCapture] Set {commentTexts.Count} comments from captured replies");
                    }
                }
            }
            else
            {
                ThreadsLog.Debug("[NetworkCapture] No captured data for this post yet");
            }
        }

        // DOM fallback for PostedAt + ViewCount when network capture misses
        var domMeta = await ExtractPostMetaFromDomAsync(page, onStatus);
        if (!post.PostedAt.HasValue && domMeta.PostedAt.HasValue)
        {
            post.PostedAt = domMeta.PostedAt;
            ThreadsLog.Debug($"[DOM] PostedAt: {post.PostedAt:yyyy-MM-dd HH:mm}");
        }
        if (domMeta.ViewCount > 0)
        {
            post.ViewCount = domMeta.ViewCount;
            ThreadsLog.Debug($"[DOM] ViewCount: {post.ViewCount}");
        }

        // DOM fallback for post body text when network capture misses
        if (string.IsNullOrWhiteSpace(post.Text))
        {
            var domText = await ExtractPostTextFromDomAsync(page, post.AuthorUsername, onStatus);
            if (!string.IsNullOrWhiteSpace(domText))
            {
                post.Text = domText;
                ThreadsLog.Debug($"[DOM] Post text: {(domText.Length > 80 ? domText[..80] + "..." : domText)}");
            }
        }

        debugPause?.Invoke();

        await ThreadsScrollingHelper.ScrollCommentsAsync(page, onStatus, options);

        var extractedComments = await ThreadsCommentExtractor.ExtractCommentsAsync(page, options.EffectiveThreadsMaxComments, onStatus);
        var pageletFallback = await ExtractFromPageletsAsync(page, options.EffectiveThreadsMaxComments, onStatus);
        if (pageletFallback.Count > 0)
        {
            extractedComments = MergeCommentResults(extractedComments, pageletFallback, options.EffectiveThreadsMaxComments);
            ThreadsLog.Debug($"After pagelet fallback: {extractedComments.Count} comments");
        }
        if (post.Comments == null || post.Comments.Count == 0)
        {
            post.Comments = extractedComments;
        }
        else
        {
            foreach (var c in extractedComments.Where(c => !post.Comments.Contains(c)))
                post.Comments.Add(c);
        }

        ThreadsLog.Debug($"Post: {post.AuthorUsername} | Text: {(post.Text?.Length > 50 ? post.Text[..50] + "..." : post.Text)} | Comments: {post.Comments?.Count ?? 0}");
    }

    /// <summary>
    /// Parses the post's metadata (posted_at, view_count) directly from the DOM,
    /// targeting only the main post pagelet (<c>threads_post_page_0</c>) so comment
    /// timestamps and per-comment like counts are not picked up.
    /// </summary>
    public static async Task<(DateTime? PostedAt, int? ViewCount)> ExtractPostMetaFromDomAsync(
        IPage page,
        Action<string>? onStatus)
    {
        try
        {
            const string js = @"
                (() => {
                    const rootSel = '[data-pagelet=\u0022threads_post_page_0\u0022]';
                    const root = document.querySelector(rootSel);
                    if (!root) return { postedAt: null, viewCount: null };

                    let postedAt = null;
                    const timeEl = root.querySelector('time[datetime]');
                    if (timeEl) {
                        const dt = timeEl.getAttribute('datetime');
                        if (dt) postedAt = dt;
                    }
                    if (!postedAt) {
                        const abbr = root.querySelector('abbr[title]');
                        if (abbr) postedAt = abbr.getAttribute('title');
                    }

                    let viewCount = null;
                    const all = root.querySelectorAll('span, div');
                    for (const el of all) {
                        const t = (el.textContent || '').trim();
                        if (!t) continue;
                        const m = t.match(/(\d+(?:[.,]\d+)?)\s*([KkMm])?\s*(?:lượt\s*xem|views?)/i);
                        if (!m) continue;
                        let numStr = m[1].replace(',', '.');
                        let num = parseFloat(numStr);
                        if (isNaN(num)) continue;
                        const suffix = (m[2] || '').toUpperCase();
                        if (suffix === 'K') num *= 1000;
                        else if (suffix === 'M') num *= 1000000;
                        viewCount = Math.round(num);
                        break;
                    }

                    return { postedAt: postedAt, viewCount: viewCount };
                })()
            ";

            var raw = await page.EvaluateAsync<System.Text.Json.JsonElement?>(js);
            if (raw == null || raw.Value.ValueKind != System.Text.Json.JsonValueKind.Object)
                return (null, null);

            DateTime? postedAt = null;
            int? viewCount = null;

            if (raw.Value.TryGetProperty("postedAt", out var paEl) && paEl.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                var s = paEl.GetString();
                if (!string.IsNullOrWhiteSpace(s) &&
                    DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal,
                        out var parsed))
                {
                    postedAt = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
                }
            }

            if (raw.Value.TryGetProperty("viewCount", out var vcEl))
            {
                if (vcEl.ValueKind == System.Text.Json.JsonValueKind.Number)
                    viewCount = vcEl.GetInt32();
                else if (vcEl.ValueKind == System.Text.Json.JsonValueKind.String &&
                         int.TryParse(vcEl.GetString(), out var vi))
                    viewCount = vi;
            }

            return (postedAt, viewCount);
        }
        catch (Exception ex)
        {
            ThreadsLog.Debug($"DOM meta extraction error: {ex.Message}");
            return (null, null);
        }
    }

    /// <summary>
    /// Extracts the body text of the main post from the DOM when the network capture
    /// didn't provide it. Targets the first pagelet (<c>threads_post_page_0</c>) which
    /// contains the original post (not replies). Walks all <c>&lt;span&gt;</c> elements,
    /// filters out UI labels, timestamps, numbers, and the author username, then
    /// concatenates the remaining text fragments.
    /// </summary>
    public static async Task<string?> ExtractPostTextFromDomAsync(
        IPage page,
        string? authorUsername,
        Action<string>? onStatus)
    {
        try
        {
            // Pass the author username (stripped of leading @) so we can exclude it from the text
            var cleanAuthor = (authorUsername ?? "").TrimStart('@');

            const string jsTemplate = @"(authorUser) => {
                const rootSel = '[data-pagelet=""threads_post_page_0""]';
                const root = document.querySelector(rootSel);
                if (!root) return null;

                const isUiText = (s) => {
                    if (!s) return true;
                    const t = s.trim();
                    if (!t) return true;
                    if (t.length < 2) return true;
                    return /^(Reply|Trả lời|Tra loi|Share|Chia sẻ|Chia se|Follow|Theo dõi|Theo doi|Translate|Tác giả|Tac gia|Loading|Dang tai|More|Like|Repost|Hide|Xem bản dịch|Xem hoạt động|Xem hoat dong|Pinned|Đã ghim|Saved|Lưu|Save|Replies|replies|Send|Gửi|View activity|View replies|View all)$/i.test(t);
                };
                const isTimestamp = (s) => /^\d{1,2}\/\d{1,2}\/\d{2,4}$/.test((s||'').trim());
                const isRelativeTime = (s) => {
                    const t = (s||'').trim();
                    return /^\d{1,3}\s*(ngày|giờ|phút|giây|tuần|tháng|năm|ngay|gio|phut|giay|tuan|thang|nam|d|h|w|m|y)$/i.test(t) ||
                           /^\d{1,3}[dhmwys]$/i.test(t);
                };
                const isNumberOnly = (s) => /^\d+$/.test((s||'').trim());
                const isLikesCount = (s) => /^[\d.,]+\s*[KkMm]?\s*(lượt thích|likes?|lượt xem|views?)$/i.test((s||'').trim()) ||
                                            /^[\d.,]+\s*[KkMm]$/.test((s||'').trim());
                const isUrl = (s) => /^https?:\/\//i.test((s||'').trim());

                // Find the first pressable container (the main post body area)
                const containers = root.querySelectorAll('[data-pressable-container=""true""]');
                const firstContainer = containers.length > 0 ? containers[0] : root;

                const seen = new Set();
                const parts = [];
                const allSpans = firstContainer.querySelectorAll('span');
                for (const s of allSpans) {
                    const t = (s.textContent || '').trim();
                    if (!t) continue;
                    if (isUiText(t)) continue;
                    if (isTimestamp(t)) continue;
                    if (isRelativeTime(t)) continue;
                    if (isNumberOnly(t)) continue;
                    if (isLikesCount(t)) continue;
                    if (isUrl(t)) continue;
                    if (authorUser && (t === authorUser || t === '@' + authorUser)) continue;
                    const key = t.toLowerCase().substring(0, 80);
                    if (seen.has(key)) continue;
                    seen.add(key);
                    parts.push(t);
                }

                if (parts.length === 0) return null;
                return parts.join(' ').replace(/\s+/g, ' ').trim();
            }";

            var text = await page.EvaluateAsync<string?>(jsTemplate, cleanAuthor);
            return string.IsNullOrWhiteSpace(text) ? null : text;
        }
        catch (Exception ex)
        {
            ThreadsLog.Debug($"DOM post text extraction error: {ex.Message}");
            return null;
        }
    }
}
