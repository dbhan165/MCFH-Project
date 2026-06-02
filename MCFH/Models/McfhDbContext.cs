using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace MCFH.Models;

public partial class McfhDbContext : DbContext
{
    public McfhDbContext()
    {
    }

    public McfhDbContext(DbContextOptions<McfhDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AiAnalysis> AiAnalyses { get; set; }

    public virtual DbSet<BespokeReport> BespokeReports { get; set; }

    public virtual DbSet<BespokeRequest> BespokeRequests { get; set; }

    public virtual DbSet<DataSource> DataSources { get; set; }

    public virtual DbSet<EmailVerification> EmailVerifications { get; set; }

    public virtual DbSet<FeedbackAspect> FeedbackAspects { get; set; }

    public virtual DbSet<ImportFile> ImportFiles { get; set; }

    public virtual DbSet<Influencer> Influencers { get; set; }

    public virtual DbSet<MutedEntity> MutedEntities { get; set; }

    public virtual DbSet<Notification> Notifications { get; set; }

    public virtual DbSet<NsrSnapshot> NsrSnapshots { get; set; }

    public virtual DbSet<PasswordResetToken> PasswordResetTokens { get; set; }

    public virtual DbSet<Payment> Payments { get; set; }

    public virtual DbSet<Project> Projects { get; set; }

    public virtual DbSet<SavedFilter> SavedFilters { get; set; }

    public virtual DbSet<ScrapedFeedback> ScrapedFeedbacks { get; set; }

    public virtual DbSet<ScrapingJob> ScrapingJobs { get; set; }

    public virtual DbSet<Subscription> Subscriptions { get; set; }

    public virtual DbSet<SubscriptionPlan> SubscriptionPlans { get; set; }

    public virtual DbSet<SystemProxy> SystemProxies { get; set; }

    public virtual DbSet<SystemSetting> SystemSettings { get; set; }

    public virtual DbSet<Tag> Tags { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<Workspace> Workspaces { get; set; }

    public virtual DbSet<WorkspaceCredit> WorkspaceCredits { get; set; }

    public virtual DbSet<WorkspaceInvitation> WorkspaceInvitations { get; set; }

    public virtual DbSet<WorkspaceMember> WorkspaceMembers { get; set; }

    public virtual DbSet<WorkspaceRole> WorkspaceRoles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.json").Build();
            optionsBuilder.UseSqlServer(config.GetConnectionString("MyCnn"));
        }
    }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AiAnalysis>(entity =>
        {
            entity.HasKey(e => e.AnalysisId).HasName("PK__AI_ANALY__5B14DE5AFC9853C5");

            entity.ToTable("AI_ANALYSIS");

            entity.HasIndex(e => e.FeedbackId, "UQ_Analysis_Feedback").IsUnique();

            entity.Property(e => e.AnalysisId).HasColumnName("analysis_id");
            entity.Property(e => e.ConfidenceScore).HasColumnName("confidence_score");
            entity.Property(e => e.FeedbackId).HasColumnName("feedback_id");
            entity.Property(e => e.IsCrisisAlert)
                .HasDefaultValue(false)
                .HasColumnName("is_crisis_alert");
            entity.Property(e => e.MainSentiment)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("main_sentiment");
            entity.Property(e => e.ProcessedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("processed_at");
            entity.Property(e => e.SentimentOverrideBy).HasColumnName("sentiment_override_by");

            entity.HasOne(d => d.Feedback).WithOne(p => p.AiAnalysis)
                .HasForeignKey<AiAnalysis>(d => d.FeedbackId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Analysis_Feedback");

            entity.HasOne(d => d.SentimentOverrideByNavigation).WithMany(p => p.AiAnalyses)
                .HasForeignKey(d => d.SentimentOverrideBy)
                .HasConstraintName("FK_Analysis_OverrideUser");
        });

        modelBuilder.Entity<BespokeReport>(entity =>
        {
            entity.HasKey(e => e.ReportId).HasName("PK__BESPOKE___779B7C581A85FC78");

            entity.ToTable("BESPOKE_REPORTS");

            entity.Property(e => e.ReportId).HasColumnName("report_id");
            entity.Property(e => e.FileUrl).HasColumnName("file_url");
            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.UploadedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("uploaded_at");
            entity.Property(e => e.Version)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("version");

            entity.HasOne(d => d.Request).WithMany(p => p.BespokeReports)
                .HasForeignKey(d => d.RequestId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Report_Request");
        });

        modelBuilder.Entity<BespokeRequest>(entity =>
        {
            entity.HasKey(e => e.RequestId).HasName("PK__BESPOKE___18D3B90FFEEC8ABE");

            entity.ToTable("BESPOKE_REQUESTS");

            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.AgreedPrice)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("agreed_price");
            entity.Property(e => e.AssignedAt)
                .HasColumnType("datetime")
                .HasColumnName("assigned_at");
            entity.Property(e => e.AssignedBy).HasColumnName("assigned_by");
            entity.Property(e => e.ClientId).HasColumnName("client_id");
            entity.Property(e => e.CustomMetrics).HasColumnName("custom_metrics");
            entity.Property(e => e.Deadline)
                .HasColumnType("datetime")
                .HasColumnName("deadline");
            entity.Property(e => e.ReporterId).HasColumnName("reporter_id");
            entity.Property(e => e.Requirements).HasColumnName("requirements");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.SubmittedAt)
                .HasColumnType("datetime")
                .HasColumnName("submitted_at");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");

            entity.HasOne(d => d.AssignedByNavigation).WithMany(p => p.BespokeRequestAssignedByNavigations)
                .HasForeignKey(d => d.AssignedBy)
                .HasConstraintName("FK_Bespoke_Admin");

            entity.HasOne(d => d.Client).WithMany(p => p.BespokeRequestClients)
                .HasForeignKey(d => d.ClientId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Bespoke_Client");

            entity.HasOne(d => d.Reporter).WithMany(p => p.BespokeRequestReporters)
                .HasForeignKey(d => d.ReporterId)
                .HasConstraintName("FK_Bespoke_Reporter");
        });

        modelBuilder.Entity<DataSource>(entity =>
        {
            entity.HasKey(e => e.SourceId).HasName("PK__DATA_SOU__3035A9B6D208F19F");

            entity.ToTable("DATA_SOURCES");

            entity.Property(e => e.SourceId).HasColumnName("source_id");
            entity.Property(e => e.Platform)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("platform");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.SearchQuery).HasColumnName("search_query");
            entity.Property(e => e.SourceType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("source_type");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("active")
                .HasColumnName("status");
            entity.Property(e => e.TargetUrl).HasColumnName("target_url");

            entity.HasOne(d => d.Project).WithMany(p => p.DataSources)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Source_Project");
        });

        modelBuilder.Entity<EmailVerification>(entity =>
        {
            entity.HasKey(e => e.VerificationId).HasName("PK__EMAIL_VE__24F17969176A7C24");

            entity.ToTable("EMAIL_VERIFICATIONS");

            entity.Property(e => e.VerificationId).HasColumnName("verification_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiredAt)
                .HasColumnType("datetime")
                .HasColumnName("expired_at");
            entity.Property(e => e.IsUsed)
                .HasDefaultValue(false)
                .HasColumnName("is_used");
            entity.Property(e => e.OtpCode)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("otp_code");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.VerificationToken)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("verification_token");

            entity.HasOne(d => d.User).WithMany(p => p.EmailVerifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_EmailVerif_User");
        });

        modelBuilder.Entity<FeedbackAspect>(entity =>
        {
            entity.HasKey(e => e.AspectId).HasName("PK__FEEDBACK__F50CDEFE89E30CC0");

            entity.ToTable("FEEDBACK_ASPECTS");

            entity.Property(e => e.AspectId).HasColumnName("aspect_id");
            entity.Property(e => e.AnalysisId).HasColumnName("analysis_id");
            entity.Property(e => e.Category)
                .HasMaxLength(100)
                .HasColumnName("category");
            entity.Property(e => e.ConfidenceScore).HasColumnName("confidence_score");
            entity.Property(e => e.Sentiment)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("sentiment");

            entity.HasOne(d => d.Analysis).WithMany(p => p.FeedbackAspects)
                .HasForeignKey(d => d.AnalysisId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Aspect_Analysis");
        });

        modelBuilder.Entity<ImportFile>(entity =>
        {
            entity.HasKey(e => e.FileId).HasName("PK__IMPORT_F__07D884C6A37E7BEA");

            entity.ToTable("IMPORT_FILES");

            entity.Property(e => e.FileId).HasColumnName("file_id");
            entity.Property(e => e.FileName)
                .HasMaxLength(255)
                .HasColumnName("file_name");
            entity.Property(e => e.FileUrl).HasColumnName("file_url");
            entity.Property(e => e.ImportedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("imported_at");
            entity.Property(e => e.ImportedRows).HasColumnName("imported_rows");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.SourceId).HasColumnName("source_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("processing")
                .HasColumnName("status");
            entity.Property(e => e.TotalRows).HasColumnName("total_rows");
            entity.Property(e => e.UploadedBy).HasColumnName("uploaded_by");

            entity.HasOne(d => d.Project).WithMany(p => p.ImportFiles)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Import_Project");

            entity.HasOne(d => d.Source).WithMany(p => p.ImportFiles)
                .HasForeignKey(d => d.SourceId)
                .HasConstraintName("FK_Import_Source");

            entity.HasOne(d => d.UploadedByNavigation).WithMany(p => p.ImportFiles)
                .HasForeignKey(d => d.UploadedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Import_Uploader");
        });

        modelBuilder.Entity<Influencer>(entity =>
        {
            entity.HasKey(e => e.InfluencerId).HasName("PK__INFLUENC__D0669ABDE8CA1A89");

            entity.ToTable("INFLUENCERS");

            entity.Property(e => e.InfluencerId).HasColumnName("influencer_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Followers)
                .HasDefaultValue(0)
                .HasColumnName("followers");
            entity.Property(e => e.HandleUrl).HasColumnName("handle_url");
            entity.Property(e => e.InfluenceScore).HasColumnName("influence_score");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Platform)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("platform");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.Reach)
                .HasDefaultValue(0)
                .HasColumnName("reach");

            entity.HasOne(d => d.Project).WithMany(p => p.Influencers)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Influencer_Project");
        });

        modelBuilder.Entity<MutedEntity>(entity =>
        {
            entity.HasKey(e => e.MuteId).HasName("PK__MUTED_EN__84EE96EBD9B518BA");

            entity.ToTable("MUTED_ENTITIES");

            entity.Property(e => e.MuteId).HasColumnName("mute_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.EntityType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("entity_type");
            entity.Property(e => e.EntityValue)
                .HasMaxLength(255)
                .HasColumnName("entity_value");
            entity.Property(e => e.MutedBy).HasColumnName("muted_by");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");

            entity.HasOne(d => d.MutedByNavigation).WithMany(p => p.MutedEntities)
                .HasForeignKey(d => d.MutedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Muted_User");

            entity.HasOne(d => d.Project).WithMany(p => p.MutedEntities)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Muted_Project");
        });

        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.NotificationId).HasName("PK__NOTIFICA__E059842F9AC3F414");

            entity.ToTable("NOTIFICATIONS");

            entity.Property(e => e.NotificationId).HasColumnName("notification_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.IsRead)
                .HasDefaultValue(false)
                .HasColumnName("is_read");
            entity.Property(e => e.Message).HasColumnName("message");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.RelatedId).HasColumnName("related_id");
            entity.Property(e => e.RelatedType)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("related_type");
            entity.Property(e => e.Title)
                .HasMaxLength(255)
                .HasColumnName("title");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("type");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Project).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_Notification_Project");

            entity.HasOne(d => d.User).WithMany(p => p.Notifications)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Notification_User");
        });

        modelBuilder.Entity<NsrSnapshot>(entity =>
        {
            entity.HasKey(e => e.SnapshotId).HasName("PK__NSR_SNAP__C27CFBF7D0092DAD");

            entity.ToTable("NSR_SNAPSHOTS");

            entity.Property(e => e.SnapshotId).HasColumnName("snapshot_id");
            entity.Property(e => e.CalculatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("calculated_at");
            entity.Property(e => e.NsrScore).HasColumnName("nsr_score");
            entity.Property(e => e.Platform)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("platform");
            entity.Property(e => e.PresenceScore).HasColumnName("presence_score");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.SnapshotDate).HasColumnName("snapshot_date");
            entity.Property(e => e.TotalNegative)
                .HasDefaultValue(0)
                .HasColumnName("total_negative");
            entity.Property(e => e.TotalNeutral)
                .HasDefaultValue(0)
                .HasColumnName("total_neutral");
            entity.Property(e => e.TotalPositive)
                .HasDefaultValue(0)
                .HasColumnName("total_positive");
            entity.Property(e => e.TotalReach)
                .HasDefaultValue(0)
                .HasColumnName("total_reach");

            entity.HasOne(d => d.Project).WithMany(p => p.NsrSnapshots)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Snapshot_Project");
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.TokenId).HasName("PK__PASSWORD__CB3C9E177983BC6B");

            entity.ToTable("PASSWORD_RESET_TOKENS");

            entity.HasIndex(e => e.ResetToken, "UQ_ResetToken").IsUnique();

            entity.Property(e => e.TokenId).HasColumnName("token_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.ExpiredAt)
                .HasColumnType("datetime")
                .HasColumnName("expired_at");
            entity.Property(e => e.IsUsed)
                .HasDefaultValue(false)
                .HasColumnName("is_used");
            entity.Property(e => e.ResetToken)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("reset_token");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.PasswordResetTokens)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_PwdReset_User");
        });

        modelBuilder.Entity<Payment>(entity =>
        {
            entity.HasKey(e => e.PaymentId).HasName("PK__PAYMENTS__ED1FC9EA8603306C");

            entity.ToTable("PAYMENTS");

            entity.HasIndex(e => e.TransactionRef, "UQ_TxRef").IsUnique();

            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("amount");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.RequestId).HasColumnName("request_id");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TransactionRef)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("transaction_ref");
            entity.Property(e => e.Type)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("type");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Payments)
                .HasForeignKey(d => d.CreatedBy)
                .HasConstraintName("FK_Payment_Creator");

            entity.HasOne(d => d.Plan).WithMany(p => p.Payments)
                .HasForeignKey(d => d.PlanId)
                .HasConstraintName("FK_Payment_Plan");

            entity.HasOne(d => d.Request).WithMany(p => p.Payments)
                .HasForeignKey(d => d.RequestId)
                .HasConstraintName("FK_Payment_Request");
        });

        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.ProjectId).HasName("PK__PROJECTS__BC799E1F6713875E");

            entity.ToTable("PROJECTS");

            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt)
                .HasColumnType("datetime")
                .HasColumnName("deleted_at");
            entity.Property(e => e.Description).HasColumnName("description");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.SearchQuery).HasColumnName("search_query");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.Workspace).WithMany(p => p.Projects)
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Project_Workspace");
        });

        modelBuilder.Entity<SavedFilter>(entity =>
        {
            entity.HasKey(e => e.FilterId).HasName("PK__SAVED_FI__833C443F9E93686D");

            entity.ToTable("SAVED_FILTERS");

            entity.Property(e => e.FilterId).HasColumnName("filter_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.FilterConfig).HasColumnName("filter_config");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.SavedFilters)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Filter_Creator");

            entity.HasOne(d => d.Project).WithMany(p => p.SavedFilters)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Filter_Project");
        });

        modelBuilder.Entity<ScrapedFeedback>(entity =>
        {
            entity.HasKey(e => e.FeedbackId).HasName("PK__SCRAPED___7A6B2B8C16F9AF60");

            entity.ToTable("SCRAPED_FEEDBACKS");

            entity.Property(e => e.FeedbackId).HasColumnName("feedback_id");
            entity.Property(e => e.AuthorName)
                .HasMaxLength(255)
                .HasColumnName("author_name");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.DeletedAt)
                .HasColumnType("datetime")
                .HasColumnName("deleted_at");
            entity.Property(e => e.EngagementCount)
                .HasDefaultValue(0)
                .HasColumnName("engagement_count");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.OriginalUrl).HasColumnName("original_url");
            entity.Property(e => e.PinnedForReport)
                .HasDefaultValue(false)
                .HasColumnName("pinned_for_report");
            entity.Property(e => e.PostedAt)
                .HasColumnType("datetime")
                .HasColumnName("posted_at");
            entity.Property(e => e.Reach)
                .HasDefaultValue(0)
                .HasColumnName("reach");
            entity.Property(e => e.ScrapedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("scraped_at");
            entity.Property(e => e.SourceId).HasColumnName("source_id");

            entity.HasOne(d => d.Source).WithMany(p => p.ScrapedFeedbacks)
                .HasForeignKey(d => d.SourceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Feedback_Source");

            entity.HasMany(d => d.Tags).WithMany(p => p.Feedbacks)
                .UsingEntity<Dictionary<string, object>>(
                    "MentionTag",
                    r => r.HasOne<Tag>().WithMany()
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_MentionTag_Tag"),
                    l => l.HasOne<ScrapedFeedback>().WithMany()
                        .HasForeignKey("FeedbackId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("FK_MentionTag_Feedback"),
                    j =>
                    {
                        j.HasKey("FeedbackId", "TagId").HasName("PK__MENTION___0E4241A77F852E4C");
                        j.ToTable("MENTION_TAGS");
                        j.IndexerProperty<int>("FeedbackId").HasColumnName("feedback_id");
                        j.IndexerProperty<int>("TagId").HasColumnName("tag_id");
                    });
        });

        modelBuilder.Entity<ScrapingJob>(entity =>
        {
            entity.HasKey(e => e.JobId).HasName("PK__SCRAPING__6E32B6A5112EB2DB");

            entity.ToTable("SCRAPING_JOBS");

            entity.Property(e => e.JobId)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("job_id");
            entity.Property(e => e.ErrorLog).HasColumnName("error_log");
            entity.Property(e => e.FinishedAt)
                .HasColumnType("datetime")
                .HasColumnName("finished_at");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.ProxyId).HasColumnName("proxy_id");
            entity.Property(e => e.SourceId).HasColumnName("source_id");
            entity.Property(e => e.StartedAt)
                .HasColumnType("datetime")
                .HasColumnName("started_at");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.TotalScraped)
                .HasDefaultValue(0)
                .HasColumnName("total_scraped");

            entity.HasOne(d => d.Project).WithMany(p => p.ScrapingJobs)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Job_Project");

            entity.HasOne(d => d.Proxy).WithMany(p => p.ScrapingJobs)
                .HasForeignKey(d => d.ProxyId)
                .HasConstraintName("FK_Job_Proxy");

            entity.HasOne(d => d.Source).WithMany(p => p.ScrapingJobs)
                .HasForeignKey(d => d.SourceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Job_Source");
        });

        modelBuilder.Entity<Subscription>(entity =>
        {
            entity.HasKey(e => e.SubscriptionId).HasName("PK__SUBSCRIP__863A7EC1BCAA53D3");

            entity.ToTable("SUBSCRIPTIONS");

            entity.Property(e => e.SubscriptionId).HasColumnName("subscription_id");
            entity.Property(e => e.ExpiryDate)
                .HasColumnType("datetime")
                .HasColumnName("expiry_date");
            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.StartDate)
                .HasColumnType("datetime")
                .HasColumnName("start_date");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("active")
                .HasColumnName("status");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.Plan).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.PlanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Subscription_Plan");

            entity.HasOne(d => d.Workspace).WithMany(p => p.Subscriptions)
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Subscription_Workspace");
        });

        modelBuilder.Entity<SubscriptionPlan>(entity =>
        {
            entity.HasKey(e => e.PlanId).HasName("PK__SUBSCRIP__BE9F8F1DB8FCAA99");

            entity.ToTable("SUBSCRIPTION_PLANS");

            entity.Property(e => e.PlanId).HasColumnName("plan_id");
            entity.Property(e => e.AiCreditLimit).HasColumnName("ai_credit_limit");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.Price)
                .HasColumnType("decimal(18, 2)")
                .HasColumnName("price");
        });

        modelBuilder.Entity<SystemProxy>(entity =>
        {
            entity.HasKey(e => e.ProxyId).HasName("PK__SYSTEM_P__9FE1B4A8A86BA5B9");

            entity.ToTable("SYSTEM_PROXIES");

            entity.Property(e => e.ProxyId).HasColumnName("proxy_id");
            entity.Property(e => e.AuthPass)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("auth_pass");
            entity.Property(e => e.AuthUser)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("auth_user");
            entity.Property(e => e.FailCount)
                .HasDefaultValue(0)
                .HasColumnName("fail_count");
            entity.Property(e => e.IpAddress)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("ip_address");
            entity.Property(e => e.LastUsedAt)
                .HasColumnType("datetime")
                .HasColumnName("last_used_at");
            entity.Property(e => e.Port).HasColumnName("port");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("active")
                .HasColumnName("status");
        });

        modelBuilder.Entity<SystemSetting>(entity =>
        {
            entity.HasKey(e => e.SettingId).HasName("PK__SYSTEM_S__256E1E32D347203C");

            entity.ToTable("SYSTEM_SETTINGS");

            entity.HasIndex(e => e.SettingKey, "UQ_SettingKey").IsUnique();

            entity.Property(e => e.SettingId).HasColumnName("setting_id");
            entity.Property(e => e.IsEncrypted)
                .HasDefaultValue(false)
                .HasColumnName("is_encrypted");
            entity.Property(e => e.SettingKey)
                .HasMaxLength(100)
                .IsUnicode(false)
                .HasColumnName("setting_key");
            entity.Property(e => e.SettingValue).HasColumnName("setting_value");
            entity.Property(e => e.UpdatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany(p => p.SystemSettings)
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK_Setting_UpdatedBy");
        });

        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(e => e.TagId).HasName("PK__TAGS__4296A2B674FA44A3");

            entity.ToTable("TAGS");

            entity.Property(e => e.TagId).HasColumnName("tag_id");
            entity.Property(e => e.Color)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("color");
            entity.Property(e => e.CreatedBy).HasColumnName("created_by");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");

            entity.HasOne(d => d.CreatedByNavigation).WithMany(p => p.Tags)
                .HasForeignKey(d => d.CreatedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tag_Creator");

            entity.HasOne(d => d.Project).WithMany(p => p.Tags)
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Tag_Project");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__USERS__B9BE370FA534CBB4");

            entity.ToTable("USERS");

            entity.HasIndex(e => e.Email, "UQ_Users_Email").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.AuthProvider)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("local")
                .HasColumnName("auth_provider");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.BannedAt)
                .HasColumnType("datetime")
                .HasColumnName("banned_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("email");
            entity.Property(e => e.FullName)
                .HasMaxLength(100)
                .HasColumnName("full_name");
            entity.Property(e => e.GoogleId)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("google_id");
            entity.Property(e => e.IsBanned)
                .HasDefaultValue(false)
                .HasColumnName("is_banned");
            entity.Property(e => e.IsVerified)
                .HasDefaultValue(false)
                .HasColumnName("is_verified");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("phone");
            entity.Property(e => e.SystemRole)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("system_role");
            entity.Property(e => e.VerifiedAt)
                .HasColumnType("datetime")
                .HasColumnName("verified_at");
        });

        modelBuilder.Entity<Workspace>(entity =>
        {
            entity.HasKey(e => e.WorkspaceId).HasName("PK__WORKSPAC__7C58AC0B19C3C21F");

            entity.ToTable("WORKSPACES");

            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.DeletedAt)
                .HasColumnType("datetime")
                .HasColumnName("deleted_at");
            entity.Property(e => e.IsDeleted)
                .HasDefaultValue(false)
                .HasColumnName("is_deleted");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.OwnerId).HasColumnName("owner_id");

            entity.HasOne(d => d.Owner).WithMany(p => p.Workspaces)
                .HasForeignKey(d => d.OwnerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Workspace_Owner");
        });

        modelBuilder.Entity<WorkspaceCredit>(entity =>
        {
            entity.HasKey(e => e.WorkspaceId).HasName("PK_WorkspaceCredits");

            entity.ToTable("WORKSPACE_CREDITS");

            entity.Property(e => e.WorkspaceId)
                .ValueGeneratedNever()
                .HasColumnName("workspace_id");
            entity.Property(e => e.LastUpdated)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("last_updated");
            entity.Property(e => e.TotalCredits).HasColumnName("total_credits");
            entity.Property(e => e.UsedCredits)
                .HasDefaultValue(0)
                .HasColumnName("used_credits");

            entity.HasOne(d => d.Workspace).WithOne(p => p.WorkspaceCredit)
                .HasForeignKey<WorkspaceCredit>(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Credits_Workspace");
        });

        modelBuilder.Entity<WorkspaceInvitation>(entity =>
        {
            entity.HasKey(e => e.InvitationId).HasName("PK__WORKSPAC__94B74D7CFE0C2630");

            entity.ToTable("WORKSPACE_INVITATIONS");

            entity.Property(e => e.InvitationId).HasColumnName("invitation_id");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
            entity.Property(e => e.InvitedBy).HasColumnName("invited_by");
            entity.Property(e => e.InvitedEmail)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("invited_email");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("pending")
                .HasColumnName("status");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");

            entity.HasOne(d => d.InvitedByNavigation).WithMany(p => p.WorkspaceInvitations)
                .HasForeignKey(d => d.InvitedBy)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invitation_InvitedBy");

            entity.HasOne(d => d.Workspace).WithMany(p => p.WorkspaceInvitations)
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Invitation_Workspace");
        });

        modelBuilder.Entity<WorkspaceMember>(entity =>
        {
            entity.HasKey(e => new { e.WorkspaceId, e.UserId }).HasName("PK__WORKSPAC__97C34F7B6C37E4F5");

            entity.ToTable("WORKSPACE_MEMBERS");

            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");

            entity.HasOne(d => d.Role).WithMany(p => p.WorkspaceMembers)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Member_Role");

            entity.HasOne(d => d.User).WithMany(p => p.WorkspaceMembers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Member_User");

            entity.HasOne(d => d.Workspace).WithMany(p => p.WorkspaceMembers)
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Member_Workspace");
        });

        modelBuilder.Entity<WorkspaceRole>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__WORKSPAC__760965CC1A537511");

            entity.ToTable("WORKSPACE_ROLES");

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("role_name");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
