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

        if (project.EnableFacebook == true)
        {
            var fbSources = await _db.FbSources
                .Where(s => s.Status == "active")
                .ToListAsync();

            foreach (var source in fbSources)
            {
                await ScrapeFacebookGroupAsync(source, projectId, keyword, result);
            }
        }

        if (project.EnableYoutube == true)
        {
            await ScrapeYouTubeAsync(projectId, keyword, result);
        }

        if (project.EnableTiktok == true)
        {
            await ScrapeTikTokAsync(keyword, result);  // TikTok chưa lưu DB, không cần projectId ở đây
        }

        return result;
    }

    // Trong ScrapeFacebookGroupAsync — sửa signature nhận thêm projectId
    private async Task ScrapeFacebookGroupAsync(FbSource source, int projectId, string keyword, ScrapeByKeywordResult result)
    {
        try
        {
            var encodedKeyword = Uri.EscapeDataString(keyword);
            var searchUrl = $"{source.GroupUrl.TrimEnd('/')}/search/?q={encodedKeyword}";

            var scraper = new FacebookGroupScraper();
            var posts = await scraper.ScrapeAsync(searchUrl, maxPosts: 5);

            foreach (var post in posts)
            {
                var feedbackId = await SaveFeedbackAsync(
                    projectId: projectId,
                    platform: "facebook",
                    content: post.Text,
                    authorName: post.Author,
                    originalUrl: post.PostUrl,
                    comments: post.Comments,
                    postedAt: post.PostedAt
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
            result.Errors.Add($"Facebook ({source.GroupUrl}): {ex.Message}");
        }
    }

    // Trong ScrapeYouTubeAsync — sửa signature nhận thêm projectId
    private async Task ScrapeYouTubeAsync(int projectId, string keyword, ScrapeByKeywordResult result)
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

                var feedbackId = await SaveFeedbackAsync(
                    projectId: projectId,
                    platform: "youtube",
                    content: scrapeResult.Title ?? $"YouTube video: {url}",  // fallback nếu lấy title lỗi
                    authorName: scrapeResult.Author,
                    originalUrl: url,
                    comments: commentTexts,
                    postedAt: scrapeResult.PostedAt
);

                result.YouTube.Add(new PlatformPostResult
                {
                    FeedbackId = feedbackId,
                    Author = scrapeResult.Author,
                    Text = scrapeResult.Title,
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
    int projectId, string platform, string content, string? authorName,
    string originalUrl, List<string> comments, DateTime? postedAt = null)
    {
        var feedback = new ScrapedFeedback
        {
            SourceId = null,
            ProjectId = projectId,
            Platform = platform,
            Content = content,
            AuthorName = authorName,
            OriginalUrl = originalUrl,
            PostedAt = postedAt,
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
