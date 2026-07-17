namespace MCFH.Configuration;

public class AiModelOptions
{
    public const string SectionName = "AiModel";

    public string ApiKey { get; set; } = "";
    public string BaseUrl { get; set; } = "https://api.tokenrouter.com/v1";
    public string Model { get; set; } = "nvidia/nemotron-3-nano-omni-30b-a3b-reasoning:free";
    public string[] FallbackModels { get; set; } = ["z-ai/glm-5.2"];
    public int MaxCommentsInPrompt { get; set; } = 50;
}
