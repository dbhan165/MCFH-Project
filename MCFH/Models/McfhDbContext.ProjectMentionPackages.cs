using Microsoft.EntityFrameworkCore;

namespace MCFH.Models;

/// <summary>Mapping cho bảng PROJECT_MENTION_PACKAGES — mỗi lần mua gói = 1 row.</summary>
public partial class McfhDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProjectMentionPackage>(entity =>
        {
            entity.HasKey(e => e.PackageId);
            entity.ToTable("PROJECT_MENTION_PACKAGES");

            entity.Property(e => e.PackageId).HasColumnName("package_id");
            entity.Property(e => e.ProjectId).HasColumnName("project_id");
            entity.Property(e => e.PaymentId).HasColumnName("payment_id");
            entity.Property(e => e.PackageType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("package_type");
            entity.Property(e => e.MentionsIncluded).HasColumnName("mentions_included");
            entity.Property(e => e.MentionsUsed).HasColumnName("mentions_used");
            entity.Property(e => e.ExpiresAt)
                .HasColumnType("datetime")
                .HasColumnName("expires_at");
            entity.Property(e => e.Status)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasColumnName("status");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");

            entity.HasOne(d => d.Project).WithMany(p => p.MentionPackages)
                .HasForeignKey(d => d.ProjectId)
                .HasConstraintName("FK_PkgPkg_Project")
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Payment).WithMany()
                .HasForeignKey(d => d.PaymentId)
                .HasConstraintName("FK_PkgPkg_Payment")
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasIndex(e => new { e.ProjectId, e.Status }, "IX_PkgPackages_Project_Status");
        });

        modelBuilder.Entity<PlatformCookie>(entity =>
        {
            entity.HasKey(e => e.PlatformCookieId).HasName("PK_PlatformCookies");

            entity.ToTable("PLATFORM_COOKIES");

            entity.Property(e => e.PlatformCookieId).HasColumnName("platform_cookie_id");
            entity.Property(e => e.Platform)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("platform");
            entity.Property(e => e.FilePath).HasColumnName("file_path");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("active")
                .HasColumnName("status");
            entity.Property(e => e.Note).HasColumnName("note");
            entity.Property(e => e.CookieCount)
                .HasDefaultValue(0)
                .HasColumnName("cookie_count");
            entity.Property(e => e.ExpiresAt)
                .HasColumnType("datetime")
                .HasColumnName("expires_at");
            entity.Property(e => e.UploadedAt)
                .HasColumnType("datetime")
                .HasColumnName("uploaded_at");
            entity.Property(e => e.LastUsedAt)
                .HasColumnType("datetime")
                .HasColumnName("last_used_at");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(getdate())")
                .HasColumnType("datetime")
                .HasColumnName("created_at");
        });
    }
}
