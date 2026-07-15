namespace MCFH.Configuration;

public class GeminiOptions
{
    public const string SectionName = "Gemini";

    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "https://api.tokenrouter.com/v1";
    public string Model { get; set; } = "gemini-2.5-flash";
    public string[] FallbackModels { get; set; } = ["gemini-2.0-flash-lite", "gemini-flash-latest"];
    public int MaxCommentsInPrompt { get; set; } = 50;
}
