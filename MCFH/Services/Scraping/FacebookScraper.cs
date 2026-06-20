using Microsoft.Playwright;
using MCFH.Models.Scraping;

namespace MCFH.Services.Scraping;

public class FacebookScraper
{
    private readonly ILogger<FacebookScraper> _logger;

    public FacebookScraper(ILogger<FacebookScraper> logger)
    {
        _logger = logger;
    }

    public async Task<ScrapeResult> ScrapeCommentsAsync(string postUrl, int maxComments = 100)
    {
        var result = new ScrapeResult();

        try
        {
            _logger.LogInformation("Bắt đầu cào Facebook: {Url}", postUrl);

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new()
            {
                Headless = false,
                SlowMo = 500,
                Args = new[] { "--no-sandbox" }
            });

            var context = await browser.NewContextAsync(new()
            {
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/120.0.0.0 Safari/537.36"
            });

            // Load cookie từ session đã login thủ công — không tự động login nữa
            await FacebookSessionHelper.LoadCookiesAsync(context);

            var page = await context.NewPageAsync();

            await page.RouteAsync("**/*.{png,jpg,jpeg,gif,mp4,webp,svg}",
                route => route.AbortAsync());

            // ---- Truy cập post ----
            await page.GotoAsync(postUrl, new()
            {
                WaitUntil = WaitUntilState.DOMContentLoaded
            });
            await page.WaitForTimeoutAsync(3000);

            // ---- Kiểm tra redirect sang video/reel ----
            if (page.Url.Contains("/videos/") || page.Url.Contains("/reel/"))
            {
                _logger.LogWarning("URL bị redirect sang video/reel, bỏ qua: {Url}", page.Url);
                result.Success = false;
                result.ErrorMessage = "URL là video/reel, không hỗ trợ cào comment dạng này.";
                return result;
            }

            // ---- Đổi sang "All comments" thay vì "Most relevant" ----
            try
            {
                var sortButton = page.Locator("div[role='button']:has-text('Most relevant')");
                if (await sortButton.IsVisibleAsync())
                {
                    await sortButton.ClickAsync();
                    await page.WaitForTimeoutAsync(1000);

                    var allCommentsOption = page.Locator("div[role='option']:has-text('All comments')");
                    if (await allCommentsOption.IsVisibleAsync())
                    {
                        await allCommentsOption.ClickAsync();
                        await page.WaitForTimeoutAsync(2000);
                        _logger.LogInformation("Đã đổi sang All comments");
                    }
                }
            }
            catch { /* Không tìm thấy dropdown thì bỏ qua */ }

            // ---- Scroll trong modal bằng mouse wheel ----
            await page.Mouse.MoveAsync(784, 400);
            await page.WaitForTimeoutAsync(500);

            int previousCount = 0;
            int noChangeStreak = 0;

            while (true)
            {
                var currentCount = await page
                    .Locator("div[role='article'][aria-label]")
                    .CountAsync();
                Console.WriteLine($"currentCount={currentCount}, noChangeStreak={noChangeStreak}");

                if (currentCount >= maxComments) break;

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
                await page.WaitForTimeoutAsync(2000);
            }

            // ---- Parse comment ----
            var finalElements = await page
                .Locator("div[role='article'][aria-label]")
                .AllAsync();

            foreach (var element in finalElements.Take(maxComments))
            {
                try
                {
                    string author = "unknown";
                    try
                    {
                        author = await element
                            .Locator("a[role='link'] span[dir='auto']")
                            .First.InnerTextAsync(new() { Timeout = 2000 });
                    }
                    catch { }

                    var textElements = await element
                        .Locator("div[dir='auto']")
                        .AllAsync();

                    string text = "";
                    foreach (var t in textElements)
                    {
                        var txt = await t.InnerTextAsync();
                        if (!string.IsNullOrWhiteSpace(txt) && txt.Length > 5 && txt != author)
                        {
                            text = txt.Trim();
                            break;
                        }
                    }

                    if (string.IsNullOrWhiteSpace(text)) continue;

                    result.Comments.Add(new ScrapedComment
                    {
                        Author = author.Trim(),
                        Text = text,
                        Source = "facebook"
                    });
                }
                catch { continue; }
            }

            result.Success = true;
            result.TotalScraped = result.Comments.Count;
            _logger.LogInformation("Cào Facebook xong: {Count} comments", result.TotalScraped);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Lỗi khi cào Facebook: {Url}", postUrl);
        }

        return result;
    }
}