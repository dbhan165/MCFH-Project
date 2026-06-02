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
}
