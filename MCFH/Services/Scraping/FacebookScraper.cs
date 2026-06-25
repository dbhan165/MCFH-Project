using Microsoft.Playwright;
using MCFH.Configuration;
using MCFH.Models.Scraping;

namespace MCFH.Services.Scraping;

public class FacebookScraper
{
    private readonly ILogger<FacebookScraper> _logger;

    public FacebookScraper(ILogger<FacebookScraper> logger)
    {
        _logger = logger;
    }

    public async Task<ScrapeResult> ScrapeCommentsAsync(
        string postUrl,
        int maxComments = 100,
        ScrapeOptions? options = null)
    {
        var result = new ScrapeResult();
        options ??= new ScrapeOptions();
        maxComments = Math.Max(maxComments, options.FacebookMaxComments);

        try
        {
            _logger.LogInformation("[FB] Cào comment: {Url}", postUrl);

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(
                PlaywrightScrapeHelper.SocialLaunch(options));

            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                UserAgent =
                    "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36",
                ViewportSize = new ViewportSize { Width = 1366, Height = 900 },
                Locale = "vi-VN"
            });

            await FacebookSessionHelper.LoadCookiesAsync(context);
            var page = await context.NewPageAsync();

            var texts = await FacebookCommentExtractor.ScrapeFromPostUrlAsync(page, postUrl, maxComments);

            foreach (var text in texts)
            {
                result.Comments.Add(new ScrapedComment
                {
                    Author = "",
                    Text = text,
                    Source = "facebook"
                });
            }

            result.Success = result.Comments.Count > 0;
            result.TotalScraped = result.Comments.Count;
            _logger.LogInformation("[FB] Xong: {Count} comment text", result.TotalScraped);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "[FB] Lỗi cào {Url}", postUrl);
        }

        return result;
    }
}
