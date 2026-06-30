namespace MCFH.DTOs;

public class SystemSettingDto
{
    public int SettingId { get; set; }
    public string SettingKey { get; set; } = null!;
    public string? SettingValue { get; set; }
    public bool IsEncrypted { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UpdateSystemSettingsDto
{
    public Dictionary<string, string?> Settings { get; set; } = new();
}
