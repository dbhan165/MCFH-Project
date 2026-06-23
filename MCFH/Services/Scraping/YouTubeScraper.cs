using Microsoft.Playwright;
using MCFH.Models.Scraping;
using System.Web;

namespace MCFH.Services.Scraping;

public class YouTubeScraper
{
    private readonly ILogger<YouTubeScraper> _logger;

    public YouTubeScraper(ILogger<YouTubeScraper> logger)
    {
        _logger = logger;
    }

    private static string GetVideoId(string url)
    {
        var uri = new Uri(url);
        var query = HttpUtility.ParseQueryString(uri.Query);
        return query["v"] ?? "";
    }

    public async Task<ScrapeResult> ScrapeCommentsAsync(string videoUrl, int maxComments = 50)
    {
        var result = new ScrapeResult();

        try
        {
            _logger.LogInformation("Bắt đầu cào: {Url}", videoUrl);

            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new()
            {
                Headless = false,
                SlowMo = 500,
                Args = new[] { "--no-sandbox" }
            });

            var page = await browser.NewPageAsync();
            var originalUrl = videoUrl;
            var originalVideoId = GetVideoId(originalUrl);

            page.FrameNavigated += (_, e) =>
            {
                if (e.Url.Contains("youtube.com/watch") && GetVideoId(e.Url) != originalVideoId)
                {
                    _logger.LogWarning("YouTube cố chuyển sang video khác, chặn lại...");
                    page.GotoAsync(originalUrl);
                }
            };

            await page.RouteAsync("**/*.{png,jpg,jpeg,gif,mp4,webp}", route => route.AbortAsync());

            await page.GotoAsync(videoUrl, new() { WaitUntil = WaitUntilState.NetworkIdle });
            await page.WaitForTimeoutAsync(2000);

            // Chờ qua hết quảng cáo (có skip thì skip ngay, không có thì chờ tự hết)
            for (int i = 0; i < 30; i++) // tối đa ~30s đợi ad, tránh treo vô hạn nếu có lỗi khác
            {
                var isAdShowing = await page.EvalOnSelectorAsync<bool>(
                    ".html5-video-player",
                    "el => el.classList.contains('ad-showing')"
                );

                if (!isAdShowing) break;

                var skipButton = page.Locator(".ytp-ad-skip-button, .ytp-skip-ad-button");
                if (await skipButton.IsVisibleAsync())
                {
                    await skipButton.ClickAsync();
                }

                await page.WaitForTimeoutAsync(1000);
            }

            var rawTitle = await page.TitleAsync();
            result.Title = rawTitle.Replace(" - YouTube", "").Trim();

            try
            {
                var channelName = await page.Locator("ytd-channel-name yt-formatted-string a").First.InnerTextAsync();
                result.Author = channelName.Trim();
            }
            catch
            {
                _logger.LogWarning("Không lấy được tên channel cho {Url}", videoUrl);
            }

            await page.EvaluateAsync(@"
                const v = document.querySelector('video');
                if (v) {
                    v.currentTime = 0;
                    v.pause();
                    v.autoplay = false;
                    v.onended = null;
                }
            ");
            await page.EvaluateAsync("localStorage.setItem('yt-player-autonav-val', 'false')");

            await page.EvaluateAsync("window.scrollTo(0, 600)");
            await page.WaitForTimeoutAsync(3000);

            await page.Keyboard.PressAsync("Tab");
            await page.WaitForTimeoutAsync(500);

            int previousCount = 0;
            int noChangeStreak = 0;

            while (true)
            {
                var currentCount = await page.Locator("ytd-comment-thread-renderer").CountAsync();
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

                await page.Keyboard.PressAsync("End");
                await page.WaitForTimeoutAsync(2500);
            }

            var commentElements = await page.Locator("ytd-comment-thread-renderer").AllAsync();

            foreach (var element in commentElements.Take(maxComments))
            {
                try
                {
                    var author = await element.Locator("#author-text span").First.InnerTextAsync();
                    var text = await element.Locator("#content-text").First.InnerTextAsync();

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        result.Comments.Add(new ScrapedComment
                        {
                            Author = author.Trim(),
                            Text = text.Trim()
                        });
                    }
                }
                catch
                {
                    continue;
                }
            }

            result.Success = true;
            result.TotalScraped = result.Comments.Count;
            _logger.LogInformation("Cào xong: {Count} comments", result.TotalScraped);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.LogError(ex, "Lỗi khi cào {Url}", videoUrl);
        }

        return result;
    }
}