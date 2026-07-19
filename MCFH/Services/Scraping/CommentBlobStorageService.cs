using System.Text.Json;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using MCFH.Configuration;
using MCFH.Models.Scraping;
using Microsoft.Extensions.Options;

namespace MCFH.Services.Scraping;

public class CommentBlobStorageService : ICommentBundleStorage
{
    private readonly BlobContainerClient _container = null!;
    private readonly ILogger<CommentBlobStorageService> _logger;
    private readonly bool _azureAvailable;

    public CommentBlobStorageService(
        IOptions<AzureBlobOptions> options,
        ILogger<CommentBlobStorageService> logger)
    {
        _logger = logger;
        var cfg = options.Value;

        if (string.IsNullOrWhiteSpace(cfg.ConnectionString))
        {
            _azureAvailable = false;
            _logger.LogWarning(
                "[BlobStorage] ConnectionString chưa được cấu hình — fallback sang file local.");
        }
        else if (string.IsNullOrWhiteSpace(cfg.ContainerName))
        {
            _azureAvailable = false;
            _logger.LogWarning(
                "[BlobStorage] ContainerName chưa được cấu hình — fallback sang file local.");
        }
        else
        {
            try
            {
                var blobServiceClient = new BlobServiceClient(cfg.ConnectionString);
                _container = blobServiceClient.GetBlobContainerClient(cfg.ContainerName);
                _azureAvailable = true;
                _logger.LogInformation(
                    "[BlobStorage] Sử dụng Azure Blob Storage — container: {Container}",
                    cfg.ContainerName);
            }
            catch (Exception ex)
            {
                _azureAvailable = false;
                _logger.LogWarning(ex,
                    "[BlobStorage] Không thể kết nối Azure Blob — fallback sang file local.");
            }
        }
    }

    private string GetBlobName(int feedbackId) =>
        $"comments/{feedbackId}.json";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<int> SaveAsync(int feedbackId, IEnumerable<string> comments)
    {
        var existing = await LoadAsync(feedbackId);
        var normalized = CommentTextHelper.Normalize(comments);

        var bundle = new StoredCommentBundle
        {
            Comments = normalized,
            AiSummary = existing.AiSummary,
            AnalyzedAt = existing.AnalyzedAt
        };

        var json = JsonSerializer.Serialize(bundle, JsonOptions);

        if (_azureAvailable)
        {
            try
            {
                var blobClient = _container.GetBlobClient(GetBlobName(feedbackId));
                using var stream = new MemoryStream(
                    System.Text.Encoding.UTF8.GetBytes(json));
                await blobClient.UploadAsync(stream, overwrite: true);
                _logger.LogDebug(
                    "[BlobStorage] Đã lưu feedback {FeedbackId} lên Azure ({Count} comments)",
                    feedbackId, normalized.Count);
                return normalized.Count;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[BlobStorage] Lưu Azure thất bại, fallback sang file local");
            }
        }

        Directory.CreateDirectory(CommentBundleStorage.GetCommentsFolder());
        await File.WriteAllTextAsync(
            CommentBundleStorage.GetBundlePath(feedbackId), json);
        _logger.LogDebug(
            "[BlobStorage] Đã lưu feedback {FeedbackId} vào file local ({Count} comments)",
            feedbackId, normalized.Count);
        return normalized.Count;
    }

    public async Task<StoredCommentBundle> LoadAsync(int feedbackId, string? filePath = null)
    {
        if (_azureAvailable)
        {
            try
            {
                var blobClient = _container.GetBlobClient(GetBlobName(feedbackId));
                if (await blobClient.ExistsAsync())
                {
                    var response = await blobClient.DownloadContentAsync();
                    var json = response.Value.Content.ToString();
                    if (!string.IsNullOrWhiteSpace(json))
                    {
                        var bundle = JsonSerializer.Deserialize<StoredCommentBundle>(json, JsonOptions);
                        if (bundle != null)
                        {
                            _logger.LogDebug(
                                "[BlobStorage] Đã đọc feedback {FeedbackId} từ Azure", feedbackId);
                            return bundle;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[BlobStorage] Đọc Azure thất bại, fallback sang file local");
            }
        }

        return await CommentBundleStorage.LoadAsync(feedbackId, filePath);
    }

    public async Task SaveAiSummaryAsync(int feedbackId, string? summary, string? filePath = null)
    {
        var bundle = await LoadAsync(feedbackId, filePath);
        if (bundle.Comments.Count == 0)
        {
            var canonical = await CommentBundleStorage.LoadAsync(feedbackId);
            if (canonical.Comments.Count > 0)
                bundle.Comments = canonical.Comments;
        }

        bundle.AiSummary = summary;
        bundle.AnalyzedAt = DateTime.Now;

        var json = JsonSerializer.Serialize(bundle, JsonOptions);

        if (_azureAvailable)
        {
            try
            {
                var blobClient = _container.GetBlobClient(GetBlobName(feedbackId));
                using var stream = new MemoryStream(
                    System.Text.Encoding.UTF8.GetBytes(json));
                await blobClient.UploadAsync(stream, overwrite: true);
                _logger.LogDebug(
                    "[BlobStorage] Đã cập nhật AI summary feedback {FeedbackId} lên Azure", feedbackId);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[BlobStorage] Cập nhật AI summary lên Azure thất bại, fallback sang file local");
            }
        }

        Directory.CreateDirectory(CommentBundleStorage.GetCommentsFolder());
        await File.WriteAllTextAsync(
            CommentBundleStorage.GetBundlePath(feedbackId), json);
    }

    public bool BundleFileExists(int feedbackId)
    {
        if (_azureAvailable)
        {
            try
            {
                var blobClient = _container.GetBlobClient(GetBlobName(feedbackId));
                return blobClient.ExistsAsync().GetAwaiter().GetResult().Value;
            }
            catch
            {
                return false;
            }
        }

        return CommentBundleStorage.BundleFileExists(feedbackId);
    }

    public async Task DeleteAsync(int feedbackId)
    {
        if (_azureAvailable)
        {
            try
            {
                var blobClient = _container.GetBlobClient(GetBlobName(feedbackId));
                await blobClient.DeleteIfExistsAsync();
                _logger.LogDebug(
                    "[BlobStorage] Đã xóa feedback {FeedbackId} khỏi Azure", feedbackId);
                return;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "[BlobStorage] Xóa Azure thất bại, thử xóa file local");
            }
        }

        CommentBundleStorage.TryDeleteCommentBundle(feedbackId);
    }

    public string BuildCombinedAnalysisText(string? caption, IReadOnlyList<string> comments) =>
        CommentBundleStorage.BuildCombinedAnalysisText(caption, comments);
}
