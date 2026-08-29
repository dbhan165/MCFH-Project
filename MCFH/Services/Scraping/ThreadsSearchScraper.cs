using MCFH.Configuration;
using MCFH.Models.Scraping;
using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace MCFH.Services.Scraping;

public static class ThreadsSearchScraper
{
    public static async Task ScrapeSearchOnPageAsync(
        Random rng,
        IPage page,
        string keyword,
        ThreadsScrapeResult result,
        int maxPosts,
        ScrapeOptions options,
        Action<string>? onStatus,
        Action? debugPause,
        ThreadsNetworkCapture networkCapture,
        Func<string, bool>? isAlreadyScraped = null)
    {
        var searchUrl = $"https://www.threads.com/search?q={Uri.EscapeDataString(keyword)}&serp_type=default";
        ThreadsLog.Debug($"Navigating to search: {searchUrl}");

        try
        {
            await page.GotoAsync(searchUrl, new PageGotoOptions
            {
                Timeout = options.ThreadsNavigationTimeoutMs
            });
        }
        catch (Exception ex)
        {
            ThreadsLog.Debug($"Navigation failed: {ex.Message}");
            throw;
        }

        await ThreadsStealthHelper.DismissBlockingDialogsAsync(page, onStatus);
        await ThreadsStealthHelper.DelayAsync(rng,
            options.ThreadsHumanizeDelayMinMs, options.ThreadsHumanizeDelayMaxMs);

        ThreadsLog.Debug("Scrolling to load search results...");
        debugPause?.Invoke();
        await ThreadsScrollingHelper.ScrollToLoadAsync(rng, page, options.ThreadsMaxScrollSteps,
            options.ThreadsHumanizeDelayMinMs, options.ThreadsHumanizeDelayMaxMs, onStatus);

        ThreadsLog.Debug("Extracting post URLs from search page...");
        var postUrls = await ExtractPostUrlsFromSearchPageAsync(page, maxPosts, onStatus);
        ThreadsLog.Debug($"Found {postUrls.Count} post URLs.");

        for (var i = 0; i < postUrls.Count; i++)
        {
            result.Posts.Add(new ThreadsPost
            {
                PostId = ThreadsTextHelper.ExtractPostIdFromUrl(postUrls[i]),
                PostUrl = postUrls[i]
            });
        }

        result.Success = true;
        result.Username = keyword;

        if (postUrls.Count > 0)
        {
            for (var i = 0; i < postUrls.Count; i++)
            {
                var postUrl = postUrls[i];
                var post = result.Posts[i];
                
                if (isAlreadyScraped != null && isAlreadyScraped(postUrl))
                {
                    post.IsSkipped = true;
                    continue;
                }

                onStatus?.Invoke($"Threads: đang xem bài {i + 1}/{postUrls.Count}...");

                try
                {
                    await page.GotoAsync(postUrl, new PageGotoOptions
                    {
                        Timeout = options.ThreadsNavigationTimeoutMs
                    });
                    await ThreadsStealthHelper.DismissBlockingDialogsAsync(page, onStatus);
                    await ThreadsStealthHelper.DelayAsync(rng,
                        options.ThreadsHumanizeDelayMinMs, options.ThreadsHumanizeDelayMaxMs);

                    var postIdFromUrl = ThreadsTextHelper.ExtractPostIdFromUrl(postUrl);
                    if (!string.IsNullOrEmpty(postIdFromUrl))
                    {
                        var waitDeadline = DateTime.UtcNow.AddSeconds(8);
                        while (DateTime.UtcNow < waitDeadline && !networkCapture.TryGetPost(postIdFromUrl, out _))
                        {
                            await page.WaitForTimeoutAsync(300);
                        }
                    }

                    debugPause?.Invoke();
                    await ThreadsPostParser.ScrapePostPageAsync(page, post, options, onStatus, debugPause, networkCapture, postIdFromUrl);

                    await page.GoBackAsync(new PageGoBackOptions
                    {
                        Timeout = options.ThreadsNavigationTimeoutMs
                    });
                    await ThreadsStealthHelper.DismissBlockingDialogsAsync(page, onStatus);
                    await ThreadsStealthHelper.DelayAsync(rng,
                        options.ThreadsHumanizeDelayMinMs, options.ThreadsHumanizeDelayMaxMs);

                    ThreadsLog.Debug($"Back on search page. Comments: {post.Comments.Count}");
                }
                catch (Exception ex)
                {
                    ThreadsLog.Debug($"Error scraping {postUrl}: {ex.Message}");
                    try
                    {
                        await page.GoBackAsync(new PageGoBackOptions { Timeout = 10000 });
                    }
                    catch { }
                }
            }
        }
    }

    public static async Task<List<string>> ExtractPostUrlsFromSearchPageAsync(
        IPage page,
        int maxPosts,
        Action<string>? onStatus)
    {
        var urls = new List<string>();
        var seenUrls = new HashSet<string>();

        urls = await TryExtractUrlsFromJsonAsync(page, maxPosts, seenUrls, onStatus);
        if (urls.Count > 0) return urls;

        ThreadsLog.Debug("Trying DOM extraction for post URLs...");
        urls = await TryExtractUrlsFromDomAsync(page, maxPosts, seenUrls, onStatus);

        return urls;
    }

    private static async Task<List<string>> TryExtractUrlsFromJsonAsync(
        IPage page,
        int maxPosts,
        HashSet<string> seenUrls,
        Action<string>? onStatus)
    {
        var urls = new List<string>();

        try
        {
            var scripts = await page.Locator("script:not([src])").AllAsync();

            foreach (var script in scripts)
            {
                try
                {
                    var content = await script.TextContentAsync();
                    if (string.IsNullOrWhiteSpace(content)) continue;
                    if (!content.TrimStart().StartsWith("{")) continue;

                    var matches = Regex.Matches(content,
                        @"(?:https://www\.threads\.com/|threads\.com/)(@[\w.]+)/(?:post|reel)/([A-Za-z0-9_-]+)",
                        RegexOptions.IgnoreCase);
                    foreach (Match match in matches)
                    {
                        var url = $"https://www.threads.com/@{match.Groups[1].Value}/post/{match.Groups[2].Value}";
                        if (!seenUrls.Contains(url))
                        {
                            seenUrls.Add(url);
                            urls.Add(url);
                        }
                        if (urls.Count >= maxPosts) break;
                    }

                    var shortMatches = Regex.Matches(content,
                        @"""code""\s*:\s*""([A-Za-z0-9_-]+)""[^}]*?""user""[^}]*?""username""\s*:\s*""([^""]+)""",
                        RegexOptions.IgnoreCase);
                    foreach (Match match in shortMatches)
                    {
                        var code = match.Groups[1].Value;
                        var username = match.Groups[2].Value;
                        var url = $"https://www.threads.com/@{username}/post/{code}";
                        if (!seenUrls.Contains(url))
                        {
                            seenUrls.Add(url);
                            urls.Add(url);
                        }
                        if (urls.Count >= maxPosts) break;
                    }

                    var relMatches = Regex.Matches(content,
                        @"/(@[\w.]+)/(?:post|reel)/([A-Za-z0-9_-]+)",
                        RegexOptions.IgnoreCase);
                    foreach (Match match in relMatches)
                    {
                        var url = $"https://www.threads.com/{match.Groups[1].Value}/post/{match.Groups[2].Value}";
                        if (!seenUrls.Contains(url))
                        {
                            seenUrls.Add(url);
                            urls.Add(url);
                        }
                        if (urls.Count >= maxPosts) break;
                    }

                    var directMatches = Regex.Matches(content,
                        @"""username""\s*:\s*""([^""]+)""[^}]*?""code""\s*:\s*""([A-Za-z0-9_-]+)""",
                        RegexOptions.IgnoreCase);
                    foreach (Match match in directMatches)
                    {
                        var username = match.Groups[1].Value;
                        var code = match.Groups[2].Value;
                        var url = $"https://www.threads.com/@{username}/post/{code}";
                        if (!seenUrls.Contains(url))
                        {
                            seenUrls.Add(url);
                            urls.Add(url);
                        }
                        if (urls.Count >= maxPosts) break;
                    }
                }
                catch { }

                if (urls.Count >= maxPosts) break;
            }
        }
        catch (Exception ex)
        {
            ThreadsLog.Debug($"JSON URL extraction: {ex.Message}");
        }

        return urls;
    }

    private static async Task<List<string>> TryExtractUrlsFromDomAsync(
        IPage page,
        int maxPosts,
        HashSet<string> seenUrls,
        Action<string>? onStatus)
    {
        var urls = new List<string>();

        try
        {
            var allLinks = await page.Locator("a").AllAsync();

            foreach (var link in allLinks)
            {
                if (urls.Count >= maxPosts) break;

                try
                {
                    var href = await link.GetAttributeAsync("href");
                    if (string.IsNullOrWhiteSpace(href)) continue;

                    var fullUrl = ThreadsTextHelper.NormalizePostUrl(href);
                    if (seenUrls.Contains(fullUrl)) continue;
                    if (!fullUrl.Contains("/post/") && !fullUrl.Contains("/reel/")) continue;

                    seenUrls.Add(fullUrl);
                    urls.Add(fullUrl);
                }
                catch { }
            }

            if (urls.Count == 0)
            {
                var lis = await page.Locator("li").AllAsync();
                foreach (var li in lis)
                {
                    if (urls.Count >= maxPosts) break;

                    try
                    {
                        var postLinks = await li.Locator("a[href*='/post/'], a[href*='/reel/']").AllAsync();
                        foreach (var link in postLinks)
                        {
                            var href = await link.GetAttributeAsync("href") ?? "";
                            var fullUrl = ThreadsTextHelper.NormalizePostUrl(href);
                            if (!seenUrls.Contains(fullUrl) &&
                                (fullUrl.Contains("/post/") || fullUrl.Contains("/reel/")))
                            {
                                seenUrls.Add(fullUrl);
                                urls.Add(fullUrl);
                            }
                            if (urls.Count >= maxPosts) break;
                        }
                    }
                    catch { }
                }
            }
        }
        catch (Exception ex)
        {
            ThreadsLog.Debug($"DOM URL extraction: {ex.Message}");
        }

        return urls;
    }
}
