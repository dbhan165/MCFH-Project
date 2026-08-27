using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace MCFH.Services.Scraping;

public static class ThreadsCommentExtractor
{
    public static async Task<List<string>> ExtractFromDomAsync(
        IPage page,
        int maxComments,
        Action<string>? onStatus)
    {
        try
        {
            const string js = @"() => {
                const max = __MAX__;
                const results = [];
                const seen = new Set();

                // === Only look at comment pagelets, never the post body (page 0) ===
                const rootSel = '[data-pagelet^=""threads_post_page_""]:not([data-pagelet=""threads_post_page_0""])';

                // === Span-level timestamp/UI filter ===
                // A span is treated as a timestamp container (not a comment body) when:
                //  - it (or any single direct ancestor we care about) holds a <time> element
                //  - it is itself a clickable link/button (tabindex + role=link)
                //  - it carries the timestamp-specific class xo1y3bh (and not xo1l8bm)
                const isTimestampSpan = (span) => {
                    if (!span) return false;
                    if (span.querySelector('time, abbr[title]')) return true;
                    if (span.getAttribute('role') === 'link' && span.hasAttribute('tabindex')) return true;
                    const cls = (span.className || '');
                    if (cls.includes('xo1y3bh') && !cls.includes('xo1l8bm')) return true;
                    return false;
                };

                // === Collect candidate comment-body spans ===
                // 1. x1jp7ctv > div > span inside comment pagelets (original happy path)
                // 2. spans carrying the xo1l8bm class anywhere inside comment pagelets
                // 3. any non-timestamp span inside a [data-pressable-container] inside comment pagelets
                const collected = new Set();
                const addAll = (nodes) => {
                    if (!nodes) return;
                    for (const s of nodes) {
                        if (isTimestampSpan(s)) continue;
                        collected.add(s);
                    }
                };

                const root = document.querySelectorAll(rootSel);
                if (!root.length) return results;

                root.forEach(pagelet => {
                    addAll(pagelet.querySelectorAll('span.x1jp7ctv > div > span'));
                    addAll(pagelet.querySelectorAll('span[class*=""xo1l8bm""]'));
                    // Broad fallback: every leaf-ish span inside a pressable container,
                    // excluding anything that looks like a timestamp.
                    const containers = pagelet.querySelectorAll('[data-pressable-container=""true""]');
                    containers.forEach(c => {
                        addAll(c.querySelectorAll('span'));
                    });
                });

                // === Filter helpers ===
                const uiTexts = new Set([
                    'Reply', 'Tra loi', 'Trả lời', 'Thich', 'Thích',
                    'Share', 'Chia se', 'Chia sẻ', 'Theo doi', 'Theo dõi',
                    'Hien thi phan hoi', 'Hiển thị phản hồi',
                    'Xem them', 'Xem thêm', 'Translate', 'Tac gia', 'Tác giả',
                    'Dang tai...', 'Loading...', 'More', 'Like', 'Repost',
                    'Xem hoạt động', 'Xem hoat dong', 'Hide', 'Xem bản dịch',
                    'Đã ghim', 'Pinned', 'Lưu', 'Saved', 'Save',
                    'Điều khoản của Threads', 'Điều khoản', 'Điều kiện',
                    'Chính sách quyền riêng tư', 'Chính sách', 'Terms of Service',
                    'Privacy Policy', 'Cookie Policy', 'Trợ giúp', 'Help',
                    'Báo cáo', 'Report', 'Tùy chọn', 'Options', 'Quảng cáo'
                ]);
                const isNumericUsername = (s) => /^@\d+$/.test(s);
                const footerTextStarts = [
                    'Điều khoản', 'Điều kiện', 'Chính sách', 'Quyền riêng tư',
                    'Terms of', 'Privacy', 'Cookie', 'About', 'Về',
                    'Trợ giúp', 'Help center', 'Báo cáo'
                ];
                const startsWithFooterText = (s) => {
                    const t = (s || '').trim();
                    return footerTextStarts.some(p => t.toLowerCase().startsWith(p.toLowerCase()));
                };

                const isTimestamp = (s) => /^\d{1,2}\/\d{1,2}\/\d{2,4}$/.test(s.trim());
                const isRelativeTime = (s) => {
                    const t = s.trim();
                    if (/^\d{1,3}\s*(ngày|giờ|phút|giây|tuần|tháng|năm|ngay|gio|phut|giay|tuan|thang|nam)$/i.test(t)) return true;
                    if (/^\d{1,3}[dhmwys]$/i.test(t)) return true;
                    return false;
                };
                const isYearOnly = (s) => /^[©]\s*\d{4}$/.test(s.trim());
                const isNumberOnly = (s) => /^\d+$/.test(s.trim());
                const isLikesCount = (s) => {
                    const t = s.trim();
                    if (/^[\d.,]+\s*[KkMm]$/.test(t)) return true;
                    return false;
                };
                const isCopyright = (s) => /^[©]\s*\d{4}$/.test(s.trim());
                const isViewCount = (s) => /\d+[.,]?\d*[KkMm]?\s*lượt\s*xem/i.test(s) || /\d+[.,]?\d*[KkMm]?\s*(lượt|xem|views?)/i.test(s);
                const isActionLabel = (s) => {
                    const t = s.trim();
                    return /^Trả lời\s+@/i.test(t) || /^Reply\s+@/i.test(t)
                        || /^Xem hoạt động/i.test(t) || /^Xem hoat dong/i.test(t)
                        || /^Trả lời\s*\w/i.test(t) || /^Reply\s*\w/i.test(t);
                };

                for (const bodySpan of collected) {
                    const text = (bodySpan.textContent || '').trim();
                    if (!text) continue;
                    if (text.length < 1) continue;

                    // === Find parent comment container ===
                    const container = bodySpan.closest('[data-pressable-container=""true""]');
                    if (!container) continue;

                    // Get username from the comment link inside the same container
                    const usernameLink = container.querySelector('a[href^=""/@""][role=""link""]');
                    let username = '';
                    if (usernameLink) {
                        const m = (usernameLink.getAttribute('href') || '').match(/^\/@([^\/]+)/);
                        if (m) username = m[1];
                    }
                    if (!username) continue;
                    if (isNumericUsername('@' + username)) continue;

                    // === Clean text: remove trailing action labels ===
                    let cleanText = text;
                    cleanText = cleanText.replace(/\s*Xem hoạt động(\s*Xem hoạt động)?\s*(Trả lời\s+\S+)?\s*\.{0,3}\s*$/i, '');
                    cleanText = cleanText.replace(/\s*Trả lời\s+\S+\.{0,3}\s*$/i, '');
                    cleanText = cleanText.replace(/\s*Reply\s+@\S+\.{0,3}\s*$/i, '');
                    cleanText = cleanText.replace(/\s*Xem bản dịch\s*$/i, '');
                    cleanText = cleanText.replace(/\s*Hide\s*$/i, '');
                    cleanText = cleanText.trim();

                    // Allow an empty body so the parser still emits `@username:` rows
                    if (cleanText.length > 0 && cleanText.length < 2) continue;

                    if (cleanText) {
                        if (uiTexts.has(cleanText)) continue;
                        if (isTimestamp(cleanText)) continue;
                        if (isRelativeTime(cleanText)) continue;
                        if (isYearOnly(cleanText)) continue;
                        if (isCopyright(cleanText)) continue;
                        if (isNumberOnly(cleanText)) continue;
                        if (isLikesCount(cleanText)) continue;
                        if (isViewCount(cleanText)) continue;
                        if (isActionLabel(cleanText)) continue;
                        if (startsWithFooterText(cleanText)) continue;

                        if (cleanText === username) continue;
                        if (cleanText === '@' + username) continue;

                        const colonMatch = cleanText.match(/^@\S+:\s*(.+)$/);
                        if (colonMatch) {
                            const remainder = colonMatch[1].trim();
                            if (isRelativeTime(remainder) || isTimestamp(remainder) || isNumberOnly(remainder)
                                || isLikesCount(remainder) || isActionLabel(remainder)) continue;
                            if (!remainder || /^\.+$/.test(remainder)) continue;
                        }

                        if (/\d+[.,]?\d*[KkMm]?\s*lượt\s*xem/i.test(cleanText)) continue;
                    }

                    const resultText = '@' + username + ': ' + cleanText;

                    const key = resultText.toLowerCase().substring(0, 120);
                    if (seen.has(key)) continue;
                    seen.add(key);

                    results.push(resultText);
                    if (results.length >= max) break;
                }

                return results;
            }";

            var finalJs = js.Replace("__MAX__", maxComments.ToString());

            var comments = await page.EvaluateAsync<string[]>(finalJs) ?? Array.Empty<string>();
            ThreadsLog.Debug($"DOM extracted {comments.Length} comments");
            return comments.ToList();
        }
        catch (Exception ex)
        {
            ThreadsLog.Debug($"DOM extraction error: {ex.Message}");
            return new List<string>();
        }
    }

    public static async Task<List<string>> ExtractCommentsAsync(
        IPage page,
        int maxComments,
        Action<string>? onStatus)
    {
        var comments = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var domComments = await ExtractFromDomAsync(page, maxComments, onStatus);
            foreach (var c in domComments)
                comments.Add(c);
        }
        catch (Exception ex)
        {
            ThreadsLog.Debug($"Comment extraction: {ex.Message}");
        }

        var filtered = CommentTextHelper.FilterThreads(comments, maxComments);

        ThreadsLog.Debug($"Extracted {filtered.Count} comments.");
        return filtered;
    }
}
