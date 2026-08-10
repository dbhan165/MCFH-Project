using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class Project
{
    public int ProjectId { get; set; }

    public int WorkspaceId { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string? SearchQuery { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<DataSource> DataSources { get; set; } = new List<DataSource>();

    public virtual ICollection<ImportFile> ImportFiles { get; set; } = new List<ImportFile>();

    public virtual ICollection<Influencer> Influencers { get; set; } = new List<Influencer>();

    public virtual ICollection<MutedEntity> MutedEntities { get; set; } = new List<MutedEntity>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<NsrSnapshot> NsrSnapshots { get; set; } = new List<NsrSnapshot>();

    public virtual ICollection<SavedFilter> SavedFilters { get; set; } = new List<SavedFilter>();

    public virtual ICollection<ScrapingJob> ScrapingJobs { get; set; } = new List<ScrapingJob>();

    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();

    public virtual Workspace Workspace { get; set; } = null!;

    public bool? EnableFacebook { get; set; }

    public bool? EnableTiktok { get; set; }

    public bool? EnableYoutube { get; set; }

    public bool? EnableMaps { get; set; }

    /// <summary>Tổng mentions quota đã mua cho Project (snapshot các package active).</summary>
    public int MentionsQuotaTotal { get; set; }

    /// <summary>Mentions đã dùng (do scrape order consume). Còn lại = Total - Used (trừ khi FullUnlimited).</summary>
    public int MentionsQuotaUsed { get; set; }

    /// <summary>Thời điểm quota hiện tại hết hạn. NULL = không hết hạn.</summary>
    public DateTime? MentionsExpiresAt { get; set; }

    /// <summary>True nếu user đã mua Full Unlimited — không giới hạn mentions.</summary>
    public bool MentionsFullUnlimited { get; set; }

    public virtual ICollection<ProjectMentionPackage> MentionPackages { get; set; } = new List<ProjectMentionPackage>();
}
