namespace MCFH.Services.Scraping;

/// <summary>Ghi nhận CAPTCHA trong một phiên cào TikTok (discovery + video).</summary>
public sealed class TikTokCaptchaTracker
{
    public bool Encountered { get; private set; }

    public void MarkEncountered() => Encountered = true;
}
