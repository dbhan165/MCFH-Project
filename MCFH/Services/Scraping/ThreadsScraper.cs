using MCFH.Configuration;
using MCFH.Models.Scraping;
using Microsoft.Playwright;

namespace MCFH.Services.Scraping;

/// <summary>
/// Scrapes Threads (threads.net) via Playwright DOM extraction.
///
/// Flow:
///   1. Navigate to search URL: https://www.threads.com/search?q={keyword}&amp;serp_type=default
///   2. Scroll to load results
///   3. Extract post URLs from search results
///   4. For each post: navigate to post URL → scrape post data + comments → goBack to search
///   5. Repeat for all posts
/// </summary>
public class ThreadsScraper
{
    private readonly Random _rng = new();

    public async Task<ThreadsScrapeResult> ScrapeSearchAsync(
        string keyword,
        int maxPosts,
        ScrapeOptions options,
        Proxy? proxy = null,
        Action<string>? onStatus = null,
        Action? debugPause = null)
    {
        var result = new ThreadsScrapeResult
        {
            ProfileUrl = $"https://www.threads.com/search?q={Uri.EscapeDataString(keyword)}&serp_type=default"
        };

        IPlaywright? playwright = null;
        IBrowser? browser = null;
        IBrowserContext? context = null;

        try
        {
            ThreadsLog.Debug("Launching browser...");
            playwright = await Playwright.CreateAsync();
            browser = await playwright.Chromium.LaunchAsync(
                ThreadsStealthHelper.CreateLaunchOptions(options.ThreadsHeadless, proxy));
            context = await ThreadsStealthHelper.CreateContextAsync(browser, options.ThreadsHeadless, onStatus);
            await ThreadsSessionHelper.LoadCookiesAsync(context);

            var page = await context.NewPageAsync();
            await PlaywrightScrapeHelper.BlockHeavyAssetsAsync(page);

            var networkCapture = new ThreadsNetworkCapture();
            networkCapture.Attach(page);

            try
            {
                await ThreadsSearchScraper.ScrapeSearchOnPageAsync(
                    _rng, page, keyword, result, maxPosts, options, onStatus, debugPause, networkCapture);
            }
            finally
            {
                await page.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            ThreadsLog.Debug($"Error: {ex.Message}");
        }
        finally
        {
            if (context != null) await context.CloseAsync();
            if (browser != null) await browser.DisposeAsync();
            playwright?.Dispose();
        }

        return result;
    }

    public async Task<ScrapeResult> ScrapeCommentsAsync(
        string postUrl,
        int maxComments = 50,
        IBrowser? sharedBrowser = null,
        ScrapeOptions? options = null,
        Action<string>? onStatus = null,
        Action? debugPause = null,
        IBrowserContext? sharedContext = null)
    {
        var result = new ScrapeResult();
        var ownsBrowser = sharedBrowser == null;
        var ownsContext = sharedContext == null;
        IPlaywright? playwright = null;
        IBrowser? browser = sharedBrowser;
        IBrowserContext? context = sharedContext;

        try
        {
            if (browser == null)
            {
                playwright = await Playwright.CreateAsync();
                browser = await playwright.Chromium.LaunchAsync(
                    ThreadsStealthHelper.CreateLaunchOptions(options?.ThreadsHeadless ?? true, null));
            }

            if (context == null)
            {
                context = await ThreadsStealthHelper.CreateContextAsync(browser, options?.ThreadsHeadless ?? true, onStatus);
                await ThreadsSessionHelper.LoadCookiesAsync(context);
            }

            var page = await context.NewPageAsync();
            await PlaywrightScrapeHelper.BlockHeavyAssetsAsync(page);

            try
            {
                ThreadsLog.Debug($"Navigating to: {postUrl}");
                debugPause?.Invoke();

                await page.GotoAsync(postUrl, new PageGotoOptions
                {
                    Timeout = options?.ThreadsNavigationTimeoutMs ?? 30000
                });

                await ThreadsStealthHelper.DismissBlockingDialogsAsync(page, onStatus);
                await ThreadsStealthHelper.DelayAsync(_rng,
                    options?.ThreadsHumanizeDelayMinMs ?? 500,
                    options?.ThreadsHumanizeDelayMaxMs ?? 1000);

                debugPause?.Invoke();

                await ThreadsScrollingHelper.ScrollCommentsAsync(page, onStatus, options);
                var comments = await ThreadsCommentExtractor.ExtractCommentsAsync(page, maxComments, onStatus);

                result.Comments = comments
                    .Select(c => new ScrapedComment
                    {
                        Author = "",
                        Text = c,
                        Source = "threads"
                    })
                    .ToList();

                result.Success = true;
                ThreadsLog.Debug($"Scraped {result.Comments.Count} comments from {postUrl}");
            }
            finally
            {
                await page.CloseAsync();
                if (ownsContext)
                    await context.CloseAsync();
            }
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            ThreadsLog.Debug($"Error scraping comments: {ex.Message}");
        }
        finally
        {
            if (ownsBrowser && browser != null)
                await browser.DisposeAsync();
            playwright?.Dispose();
        }

        return result;
    }
}
