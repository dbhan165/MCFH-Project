using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class ScrapedFeedback
{
    public int FeedbackId { get; set; }

    public int SourceId { get; set; }

    public string Content { get; set; } = null!;

    public string? AuthorName { get; set; }

    public string? OriginalUrl { get; set; }

    public DateTime? PostedAt { get; set; }

    public int? Reach { get; set; }

    public int? EngagementCount { get; set; }

    public bool? PinnedForReport { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime? ScrapedAt { get; set; }

    public string? CommentsFileUrl { get; set; }

    public int? CommentsCount { get; set; }

    public virtual AiAnalysis? AiAnalysis { get; set; }

    public virtual DataSource Source { get; set; } = null!;

    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();
}
