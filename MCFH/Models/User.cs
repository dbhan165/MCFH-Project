using System;
using System.Collections.Generic;

namespace MCFH.Models;

public partial class User
{
    public int UserId { get; set; }

    public string Email { get; set; } = null!;

    public string? PasswordHash { get; set; }

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? AvatarUrl { get; set; }

    public string AuthProvider { get; set; } = null!;

    public string? GoogleId { get; set; }

    public string SystemRole { get; set; } = null!;

    public bool? IsVerified { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public bool? IsBanned { get; set; }

    public DateTime? BannedAt { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<AiAnalysis> AiAnalyses { get; set; } = new List<AiAnalysis>();

    public virtual ICollection<BespokeRequest> BespokeRequestAssignedByNavigations { get; set; } = new List<BespokeRequest>();

    public virtual ICollection<BespokeRequest> BespokeRequestClients { get; set; } = new List<BespokeRequest>();

    public virtual ICollection<BespokeRequest> BespokeRequestReporters { get; set; } = new List<BespokeRequest>();

    public virtual ICollection<EmailVerification> EmailVerifications { get; set; } = new List<EmailVerification>();

    public virtual ICollection<ImportFile> ImportFiles { get; set; } = new List<ImportFile>();

    public virtual ICollection<MutedEntity> MutedEntities { get; set; } = new List<MutedEntity>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<SavedFilter> SavedFilters { get; set; } = new List<SavedFilter>();

    public virtual ICollection<SystemSetting> SystemSettings { get; set; } = new List<SystemSetting>();

    public virtual ICollection<Tag> Tags { get; set; } = new List<Tag>();

    public virtual ICollection<WorkspaceInvitation> WorkspaceInvitations { get; set; } = new List<WorkspaceInvitation>();

    public virtual ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();

    public virtual ICollection<Workspace> Workspaces { get; set; } = new List<Workspace>();
}
