using MCFH.Models;
using MCFH.Models.Scraping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using System.Text.Json;

namespace MCFH.Services.Scraping;

public class ScrapeByKeywordService
{
    private readonly McfhDbContext _db;

    public ScrapeByKeywordService(McfhDbContext db)
    {
        _db = db;
    }

    public async Task<ScrapeByKeywordResult> ScrapeAsync(int projectId)
    {
        var result = new ScrapeByKeywordResult();

        var project = await _db.Projects.FindAsync(projectId);
        if (project == null)
        {
            result.ErrorMessage = "Project không tồn tại.";
            return result;
        }

        if (string.IsNullOrWhiteSpace(project.SearchQuery))
        {
            result.ErrorMessage = "Project chưa setup keyword (search_query rỗng).";
            return result;
        }

        var keyword = project.SearchQuery;
        result.Keyword = keyword;

        var sources = await _db.DataSources
            .Where(s => s.ProjectId == projectId && s.Status == "active")
            .ToListAsync();

        foreach (var source in sources)
        {
            switch (source.Platform)
            {
                case "facebook":
                    await ScrapeFacebookGroupAsync(source, keyword, result);
                    break;
                case "youtube":
                    await ScrapeYouTubeAsync(source, keyword, result);
                    break;
                case "tiktok":
                    await ScrapeTikTokAsync(keyword, result);
                    break;
            }
        }

        return result;
    }

    private async Task ScrapeFacebookGroupAsync(DataSource source, string keyword, ScrapeByKeywordResult result)
    {
        try
        {
            var encodedKeyword = Uri.EscapeDataString(keyword);
            var searchUrl = $"{source.TargetUrl!.TrimEnd('/')}/search/?q={encodedKeyword}";

            var scraper = new FacebookGroupScraper();
            var posts = await scraper.ScrapeAsync(searchUrl, maxPosts: 5);

            foreach (var post in posts)
            {
                // Đã bỏ tham số sourceId tại đây
                var feedbackId = await SaveFeedbackAsync(
                    content: post.Text,
                    authorName: post.Author,
                    originalUrl: post.PostUrl,
                    comments: post.Comments
                );

                result.Facebook.Add(new PlatformPostResult
                {
                    FeedbackId = feedbackId,
                    Author = post.Author,
                    Text = post.Text,
                    Url = post.PostUrl,
                    CommentsCount = post.Comments.Count
                });
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"Facebook ({source.TargetUrl}): {ex.Message}");
        }
    }

    private async Task ScrapeYouTubeAsync(DataSource source, string keyword, ScrapeByKeywordResult result)
    {
        try
        {
            var searchScraper = new YouTubeSearchScraper();
            var urls = await searchScraper.SearchAsync(keyword, maxVideos: 5);

            var commentScraper = new YouTubeScraper(NullLoggerFactory.Instance.CreateLogger<YouTubeScraper>());

            foreach (var url in urls)
            {
                var scrapeResult = await commentScraper.ScrapeCommentsAsync(url, maxComments: 50);
                if (!scrapeResult.Success) continue;

                var commentTexts = scrapeResult.Comments.Select(c => c.Text).ToList();

                // Đã bỏ tham số sourceId tại đây
                var feedbackId = await SaveFeedbackAsync(
                    content: $"YouTube video: {url}",
                    authorName: null,
                    originalUrl: url,
                    comments: commentTexts
                );

                result.YouTube.Add(new PlatformPostResult
                {
                    FeedbackId = feedbackId,
                    Url = url,
                    CommentsCount = commentTexts.Count
                });
            }
        }
        catch (Exception ex)
        {
            result.Errors.Add($"YouTube: {ex.Message}");
        }
    }

    private async Task ScrapeTikTokAsync(string keyword, ScrapeByKeywordResult result)
    {
        try
        {
            var searchScraper = new TikTokSearchScraper();
            var urls = await searchScraper.SearchAsync(keyword, maxVideos: 5);

            result.TikTok.AddRange(urls);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"TikTok: {ex.Message}");
        }
    }

    // Đã cập nhật signature loại bỏ hoàn toàn int sourceId và gán SourceId = null trực tiếp
    private async Task<int> SaveFeedbackAsync(
        string content, string? authorName, string originalUrl, List<string> comments)
    {
        var feedback = new ScrapedFeedback
        {
            SourceId = null, // Không gắn DATA_SOURCES
            Content = content,
            AuthorName = authorName,
            OriginalUrl = originalUrl,
            CommentsCount = comments.Count
        };

        _db.ScrapedFeedbacks.Add(feedback);
        await _db.SaveChangesAsync();

        var folder = Path.Combine(AppContext.BaseDirectory, "StorageData", "comments");
        Directory.CreateDirectory(folder);
        var filePath = Path.Combine(folder, $"{feedback.FeedbackId}.json");
        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(comments));

        feedback.CommentsFileUrl = filePath;
        await _db.SaveChangesAsync();

        return feedback.FeedbackId;
    }
}
