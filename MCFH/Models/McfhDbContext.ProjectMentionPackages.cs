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
            entity.HasKey(e => e.PlatformCookieId);
            entity.ToTable("PLATFORM_COOKIES");
            entity.HasIndex(e => e.Platform, "UQ_PlatformCookies_Platform").IsUnique();

            entity.Property(e => e.PlatformCookieId).HasColumnName("platform_cookie_id");
            entity.Property(e => e.Platform)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("platform");
            entity.Property(e => e.FilePath)
                .HasMaxLength(500)
                .HasColumnName("file_path");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasColumnName("status")
                .HasDefaultValue("active");
            entity.Property(e => e.Note)
                .HasMaxLength(1000)
                .HasColumnName("note");
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
                .HasColumnType("datetime")
                .HasColumnName("created_at")
                .HasDefaultValueSql("(getdate())");
        });

        modelBuilder.Entity<BrevoKey>(entity =>
        {
            entity.HasKey(e => e.BrevoKeyId);
            entity.ToTable("BREVO_KEYS");

            entity.HasIndex(e => new { e.IsDefault, e.Status }, "IX_BrevoKeys_Default_Status");

            entity.Property(e => e.BrevoKeyId).HasColumnName("brevo_key_id");
            entity.Property(e => e.KeyType)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("api")
                .HasColumnName("key_type");
            entity.Property(e => e.ApiKeyEncrypted)
                .HasColumnName("api_key_encrypted");
            entity.Property(e => e.SmtpLogin)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("smtp_login");
            entity.Property(e => e.FromAddress)
                .HasMaxLength(255)
                .IsUnicode(false)
                .HasColumnName("from_address");
            entity.Property(e => e.FromName)
                .HasMaxLength(100)
                .HasColumnName("from_name");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("active")
                .HasColumnName("status");
            entity.Property(e => e.IsDefault)
                .HasDefaultValue(false)
                .HasColumnName("is_default");
            entity.Property(e => e.Note)
                .HasMaxLength(1000)
                .HasColumnName("note");
            entity.Property(e => e.LastUsedAt)
                .HasColumnType("datetime")
                .HasColumnName("last_used_at");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at")
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany()
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK_BrevoKeys_UpdatedBy")
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<PayOsKey>(entity =>
        {
            entity.HasKey(e => e.PayOsKeyId);
            entity.ToTable("PAYOS_KEYS");

            entity.HasIndex(e => new { e.IsDefault, e.Status }, "IX_PayOsKeys_Default_Status");

            entity.Property(e => e.PayOsKeyId).HasColumnName("payos_key_id");
            entity.Property(e => e.ClientId)
                .HasMaxLength(64)
                .IsUnicode(false)
                .HasColumnName("client_id");
            entity.Property(e => e.ApiKeyEncrypted)
                .HasColumnName("api_key_encrypted");
            entity.Property(e => e.ChecksumKeyEncrypted)
                .HasColumnName("checksum_key_encrypted");
            entity.Property(e => e.Environment)
                .HasMaxLength(20)
                .IsUnicode(false)
                .HasDefaultValue("live")
                .HasColumnName("environment");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .IsUnicode(false)
                .HasDefaultValue("active")
                .HasColumnName("status");
            entity.Property(e => e.IsDefault)
                .HasDefaultValue(false)
                .HasColumnName("is_default");
            entity.Property(e => e.Note)
                .HasMaxLength(1000)
                .HasColumnName("note");
            entity.Property(e => e.LastUsedAt)
                .HasColumnType("datetime")
                .HasColumnName("last_used_at");
            entity.Property(e => e.CreatedAt)
                .HasColumnType("datetime")
                .HasColumnName("created_at")
                .HasDefaultValueSql("(getdate())");
            entity.Property(e => e.UpdatedAt)
                .HasColumnType("datetime")
                .HasColumnName("updated_at");
            entity.Property(e => e.UpdatedBy).HasColumnName("updated_by");

            entity.HasOne(d => d.UpdatedByNavigation).WithMany()
                .HasForeignKey(d => d.UpdatedBy)
                .HasConstraintName("FK_PayOsKeys_UpdatedBy")
                .OnDelete(DeleteBehavior.ClientSetNull);
        });
    }
}
