using MCFH.Models.Scraping;

namespace MCFH.Services.Scraping;

public interface ICommentBundleStorage
{
    Task<int> SaveAsync(int feedbackId, IEnumerable<string> comments);
    Task<StoredCommentBundle> LoadAsync(int feedbackId, string? filePath = null);
    Task SaveAiSummaryAsync(int feedbackId, string? summary, string? filePath = null);
    Task DeleteAsync(int feedbackId);
    bool BundleFileExists(int feedbackId);
    string BuildCombinedAnalysisText(string? caption, IReadOnlyList<string> comments);
}
