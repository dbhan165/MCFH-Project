namespace MCFH.Configuration;

public class AuthOptions
{
    public const string SectionName = "Auth";

    public string FrontendBaseUrl { get; set; } = "http://localhost:5173";

    public string GoogleClientId { get; set; } = "";

    public int OtpExpiryMinutes { get; set; } = 15;
}
