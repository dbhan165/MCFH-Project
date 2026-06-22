using System.Text.Json.Serialization;

namespace MCFH.Models.Scraping;

public class CookieEditorEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("value")]
    public string Value { get; set; } = "";

    [JsonPropertyName("domain")]
    public string Domain { get; set; } = "";

    [JsonPropertyName("path")]
    public string Path { get; set; } = "/";

    [JsonPropertyName("httpOnly")]
    public bool HttpOnly { get; set; }

    [JsonPropertyName("secure")]
    public bool Secure { get; set; }

    [JsonPropertyName("sameSite")]
    public string? SameSite { get; set; }

    [JsonPropertyName("expirationDate")]
    public double? ExpirationDate { get; set; }
}