using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class SystemSetting
{
    public int SettingId { get; set; }

    public string SettingKey { get; set; } = null!;

    public string? SettingValue { get; set; }

    public bool? IsEncrypted { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int? UpdatedBy { get; set; }

    public virtual User? UpdatedByNavigation { get; set; }
}
