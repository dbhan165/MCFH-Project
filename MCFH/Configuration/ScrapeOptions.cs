namespace MCFH.Configuration;

public class ScrapeOptions
{
    public const string SectionName = "Scraping";

    /// <summary>YouTube chạy headless nhanh hơn, ít bị chặn hơn FB/TikTok.</summary>
    public bool YouTubeHeadless { get; set; } = true;

    /// <summary>Facebook — headless ẩn cửa sổ khi cào nền tảng.</summary>
    public bool SocialHeadless { get; set; } = true;

    /// <summary>TikTok headless — ẩn Chromium, phù hợp multi-user platform.</summary>
    public bool TikTokHeadless { get; set; } = true;

    /// <summary>Chỉ bật trên máy dev: hiện browser + chờ giải CAPTCHA tay.</summary>
    public bool TikTokAllowManualCaptcha { get; set; } = false;

    /// <summary>Giây chờ giải CAPTCHA tay (chỉ khi TikTokAllowManualCaptcha=true).</summary>
    public int TikTokCaptchaWaitSeconds { get; set; } = 0;

    public int MaxVideosPerPlatform { get; set; } = 5;
    public int MaxCommentsPerItem { get; set; } = 15;
    public int YouTubeMaxComments { get; set; } = 40;
    public int TikTokMaxComments { get; set; } = 40;
    public int FacebookMaxComments { get; set; } = 40;
    public int MaxFacebookPosts { get; set; } = 5;

    /// <summary>Cào FB + YouTube + TikTok song song.</summary>
    public bool ParallelPlatforms { get; set; } = true;

    /// <summary>Giới hạn thời gian tìm video TikTok (giây) — tránh treo quá lâu.</summary>
    public int TikTokDiscoveryTimeoutSeconds { get; set; } = 90;

    /// <summary>Chờ TikTok load API search sau khi mở trang (ms).</summary>
    public int TikTokSearchWaitMs { get; set; } = 12000;

    /// <summary>Nếu headless không tìm được video, thử lại 1 lần với cửa sổ Chromium thật.</summary>
    public bool TikTokRetryHeadedOnFailure { get; set; } = true;

    /// <summary>Chế độ demo: ít bài/comment, bỏ cào sâu FB, TikTok song song, không chờ AI.</summary>
    public bool FastDemoMode { get; set; } = false;

    public int FastDemoMaxPostsPerPlatform { get; set; } = 2;
    public int FastDemoMaxCommentsPerItem { get; set; } = 8;
    public bool FastDemoSkipAiAnalysis { get; set; } = true;
    public bool FastDemoFacebookFeedOnly { get; set; } = true;
    public bool FastDemoRunTikTokParallel { get; set; } = true;

    public int EffectiveMaxFacebookPosts =>
        FastDemoMode ? FastDemoMaxPostsPerPlatform : MaxFacebookPosts;

    public int EffectiveMaxVideosPerPlatform =>
        FastDemoMode ? FastDemoMaxPostsPerPlatform : MaxVideosPerPlatform;

    public int EffectiveMaxCommentsPerItem =>
        FastDemoMode ? FastDemoMaxCommentsPerItem : MaxCommentsPerItem;

    public int EffectiveYouTubeMaxComments =>
        FastDemoMode ? FastDemoMaxCommentsPerItem : YouTubeMaxComments;

    public int EffectiveTikTokMaxComments =>
        FastDemoMode ? FastDemoMaxCommentsPerItem : TikTokMaxComments;

    public int EffectiveFacebookMaxComments =>
        FastDemoMode ? FastDemoMaxCommentsPerItem : FacebookMaxComments;

    public int EffectiveTikTokDiscoveryTimeoutSeconds =>
        FastDemoMode ? Math.Min(TikTokDiscoveryTimeoutSeconds, 45) : TikTokDiscoveryTimeoutSeconds;

    public int EffectiveTikTokSearchWaitMs =>
        FastDemoMode ? Math.Min(TikTokSearchWaitMs, 5000) : TikTokSearchWaitMs;

    public bool EffectiveTikTokRetryHeaded =>
        FastDemoMode ? false : TikTokRetryHeadedOnFailure;
}
