using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public int UserId { get; set; }

    public int? ProjectId { get; set; }

    public string Title { get; set; } = null!;

    public string? Message { get; set; }

    public string? Type { get; set; }

    public int? RelatedId { get; set; }

    public string? RelatedType { get; set; }

    public bool? IsRead { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Project? Project { get; set; }

    public virtual User User { get; set; } = null!;
}
