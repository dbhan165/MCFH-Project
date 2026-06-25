export interface Workspace {
  workspaceId: number;
  name: string;
  ownerName: string;
  myRole: string | null;
  memberCount: number;
  projectCount: number;
  createdAt: string | null;
}

export interface WorkspaceMember {
  userId: number;
  fullName: string;
  email: string;
  avatarUrl: string | null;
  roleName: string;
}

export interface WorkspaceInvitation {
  invitationId: number;
  invitedEmail: string;
  invitedByName: string;
  status: string;
  createdAt: string | null;
}

export interface ActivityLog {
  logId: number;
  actionType: string;
  targetType: string | null;
  targetId: number | null;
  targetName: string | null;
  description: string | null;
  userFullName: string;
  userAvatarUrl: string | null;
  createdAt: string | null;
}
