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

            // Không cần filter theo keyword nữa — Facebook đã search đúng rồi
            foreach(var post in posts)
{
                var feedbackId = await SaveFeedbackAsync(
                    sourceId: source.SourceId,
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

                var feedbackId = await SaveFeedbackAsync(
                    sourceId: source.SourceId,
                    content: $"YouTube video: {url}", // Chưa có tiêu đề video, dùng URL tạm
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

            // Chưa có comment scraper — chỉ trả URL, không lưu DB
            result.TikTok.AddRange(urls);
        }
        catch (Exception ex)
        {
            result.Errors.Add($"TikTok: {ex.Message}");
        }
    }

    private async Task<int> SaveFeedbackAsync(
        int sourceId, string content, string? authorName, string originalUrl, List<string> comments)
    {
        var feedback = new ScrapedFeedback
        {
            SourceId = sourceId,
            Content = content,
            AuthorName = authorName,
            OriginalUrl = originalUrl,
            CommentsCount = comments.Count
        };

        _db.ScrapedFeedbacks.Add(feedback);
        await _db.SaveChangesAsync(); // Có feedback_id sau dòng này

        // Ghi file JSON comment
        var folder = Path.Combine(AppContext.BaseDirectory, "StorageData", "comments");
        Directory.CreateDirectory(folder);
        var filePath = Path.Combine(folder, $"{feedback.FeedbackId}.json");
        await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(comments));

        // Update lại comments_file_url
        feedback.CommentsFileUrl = filePath;
        await _db.SaveChangesAsync();

        return feedback.FeedbackId;
    }
}