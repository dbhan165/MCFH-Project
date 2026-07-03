namespace MCFH.Configuration;

public class ScrapeOptions
{
    public const string SectionName = "Scraping";

    /// <summary>YouTube chạy headless nhanh hơn, ít bị chặn hơn FB/TikTok.</summary>
    public bool YouTubeHeadless { get; set; } = true;

    /// <summary>Timeout Goto YouTube (ms) — proxy/datacenter thường cần &gt;30s.</summary>
    public int YouTubeNavigationTimeoutMs { get; set; } = 90_000;

    /// <summary>Facebook — headless ẩn cửa sổ khi cào nền tảng.</summary>
    public bool SocialHeadless { get; set; } = true;

    /// <summary>TikTok headless — ẩn Chromium, phù hợp multi-user platform.</summary>
    public bool TikTokHeadless { get; set; } = true;

    /// <summary>Luôn cào headless trước; chỉ mở cửa sổ Chromium khi gặp CAPTCHA (cần TikTokAllowManualCaptcha).</summary>
    public bool TikTokHeadedOnCaptchaOnly { get; set; } = true;

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

    /// <summary>Số bài báo tối đa mỗi lần cào (Google/Bing + keyword).</summary>
    public int MaxNewsArticles { get; set; } = 5;

    /// <summary>Tin tức — headless qua Playwright.</summary>
    public bool NewsHeadless { get; set; } = true;

    /// <summary>Domain báo VN dùng trong truy vấn site: (Google/Bing).</summary>
    public List<string> NewsSearchSites { get; set; } =
    [
        "vnexpress.net",
        "tuoitre.vn",
        "thanhnien.vn",
        "vietnamnet.vn"
    ];

    /// <summary>
    /// News discovery: auto (SerpApi nếu có key, else Playwright), serpapi, playwright.
    /// </summary>
    public string NewsDiscoveryProvider { get; set; } = "auto";

    /// <summary>Chỉ lấy nội dung từ feed — không click mở modal (ổn định hơn headless).</summary>
    public bool FacebookFeedOnly { get; set; } = true;

    /// <summary>Cào FB + YouTube + TikTok song song.</summary>
    public bool ParallelPlatforms { get; set; } = true;

    /// <summary>Cron Hangfire kiểm tra project đến hạn cào (mặc định mỗi phút).</summary>
    public string HangfireSchedulerCron { get; set; } = "*/1 * * * *";

    /// <summary>Khoảng cách tối thiểu giữa hai lần bắt đầu cào của cùng project (phút), căn cứ SCRAPING_JOBS.started_at.</summary>
    public int PerProjectScrapeIntervalMinutes { get; set; } = 15;

    /// <summary>Job status=running quá lâu (phút) được coi là stale — không chặn lần cào mới.</summary>
    public int StaleRunningJobMinutes { get; set; } = 120;

    /// <summary>Giới hạn thời gian tìm video TikTok (giây) — tránh treo quá lâu.</summary>
    public int TikTokDiscoveryTimeoutSeconds { get; set; } = 90;

    /// <summary>Chờ TikTok load API search sau khi mở trang (ms).</summary>
    public int TikTokSearchWaitMs { get; set; } = 12000;

    /// <summary>Delay/cuộn ngẫu nhiên giống người dùng — giảm CAPTCHA TikTok.</summary>
    public bool TikTokHumanizeBehavior { get; set; } = true;

    /// <summary>Giây chờ ngẫu nhiên giữa các thao tác (ms, min).</summary>
    public int TikTokHumanizeDelayMinMs { get; set; } = 1000;

    /// <summary>Giây chờ ngẫu nhiên giữa các thao tác (ms, max).</summary>
    public int TikTokHumanizeDelayMaxMs { get; set; } = 3000;

    /// <summary>Mỗi bước cuộn chuột tối thiểu (px).</summary>
    public int TikTokHumanizeScrollStepMinPx { get; set; } = 280;

    /// <summary>Mỗi bước cuộn chuột tối đa (px).</summary>
    public int TikTokHumanizeScrollStepMaxPx { get; set; } = 620;

    /// <summary>Pause giữa các bước cuộn nhỏ (ms, min).</summary>
    public int TikTokHumanizeScrollPauseMinMs { get; set; } = 450;

    /// <summary>Pause giữa các bước cuộn nhỏ (ms, max).</summary>
    public int TikTokHumanizeScrollPauseMaxMs { get; set; } = 1100;

    /// <summary>Nếu headless không tìm được video, thử lại 1 lần với cửa sổ Chromium thật.</summary>
    public bool TikTokRetryHeadedOnFailure { get; set; } = true;

    /// <summary>Nếu headless lưu được video nhưng 0 comment (CAPTCHA), thử lại headed.</summary>
    public bool TikTokRetryHeadedWhenNoComments { get; set; } = true;

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

    public int EffectiveMaxNewsArticles =>
        FastDemoMode ? FastDemoMaxPostsPerPlatform : MaxNewsArticles;

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
