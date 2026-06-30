using Microsoft.EntityFrameworkCore;

namespace MCFH.Models;

public partial class McfhDbContext
{
    public virtual DbSet<ScrapeOrder> ScrapeOrders { get; set; }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ScrapeOrder>(entity =>
        {
            entity.HasKey(e => e.OrderId);
            entity.ToTable("SCRAPE_ORDERS");

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.WorkspaceId).HasColumnName("workspace_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Keyword).HasMaxLength(500).HasColumnName("keyword");
            entity.Property(e => e.PostedSinceDays).HasColumnName("posted_since_days");
            entity.Property(e => e.QuotedPrice).HasColumnType("decimal(18,2)").HasColumnName("quoted_price");
            entity.Property(e => e.Status).HasMaxLength(50).HasColumnName("status");
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.ScrapeJobId).HasMaxLength(100).HasColumnName("scrape_job_id");
            entity.Property(e => e.ProgressPercent).HasColumnName("progress_percent");
            entity.Property(e => e.StatusMessage).HasMaxLength(500).HasColumnName("status_message");
            entity.Property(e => e.EstimatedReportAt).HasColumnType("datetime").HasColumnName("estimated_report_at");
            entity.Property(e => e.ReportReadyAt).HasColumnType("datetime").HasColumnName("report_ready_at");
            entity.Property(e => e.CreatedAt).HasColumnType("datetime").HasColumnName("created_at");
            entity.Property(e => e.PaidAt).HasColumnType("datetime").HasColumnName("paid_at");
            entity.Property(e => e.CompletedAt).HasColumnType("datetime").HasColumnName("completed_at");

            entity.HasOne(d => d.Workspace).WithMany()
                .HasForeignKey(d => d.WorkspaceId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Project).WithMany()
                .HasForeignKey(d => d.ProjectId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.User).WithMany()
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Payment).WithMany()
                .HasForeignKey(d => d.PaymentId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
    }
}
