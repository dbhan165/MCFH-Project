import axiosClient from './axiosClient';
import type { ActivityLog, Workspace, WorkspaceInvitation, WorkspaceMember } from '../types/workspace';
import { pickField, pickNullableString, pickNumber, pickString } from '../utils/normalizeApi';

function normalizeWorkspace(data: Record<string, unknown>): Workspace {
  return {
    workspaceId: pickNumber(data, 'workspaceId', 'WorkspaceId'),
    name: pickString(data, 'name', 'Name'),
    ownerName: pickString(data, 'ownerName', 'OwnerName'),
    myRole: pickNullableString(data, 'myRole', 'MyRole'),
    memberCount: pickNumber(data, 'memberCount', 'MemberCount'),
    projectCount: pickNumber(data, 'projectCount', 'ProjectCount'),
    createdAt: pickNullableString(data, 'createdAt', 'CreatedAt'),
  };
}

function normalizeMember(data: Record<string, unknown>): WorkspaceMember {
  return {
    userId: pickNumber(data, 'userId', 'UserId'),
    fullName: pickString(data, 'fullName', 'FullName'),
    email: pickString(data, 'email', 'Email'),
    avatarUrl: pickNullableString(data, 'avatarUrl', 'AvatarUrl'),
    roleName: pickString(data, 'roleName', 'RoleName'),
  };
}

function normalizeInvitation(data: Record<string, unknown>): WorkspaceInvitation {
  return {
    invitationId: pickNumber(data, 'invitationId', 'InvitationId'),
    workspaceId: pickField<number>(data, 'workspaceId', 'WorkspaceId'),
    workspaceName: pickNullableString(data, 'workspaceName', 'WorkspaceName') ?? undefined,
    invitedEmail: pickString(data, 'invitedEmail', 'InvitedEmail'),
    invitedByName: pickString(data, 'invitedByName', 'InvitedByName'),
    status: pickString(data, 'status', 'Status'),
    createdAt: pickNullableString(data, 'createdAt', 'CreatedAt'),
  };
}

function normalizeActivityLog(data: Record<string, unknown>): ActivityLog {
  return {
    logId: pickNumber(data, 'logId', 'LogId'),
    actionType: pickString(data, 'actionType', 'ActionType'),
    targetType: pickNullableString(data, 'targetType', 'TargetType'),
    targetId: pickField<number>(data, 'targetId', 'TargetId') ?? null,
    targetName: pickNullableString(data, 'targetName', 'TargetName'),
    description: pickNullableString(data, 'description', 'Description'),
    userFullName: pickString(data, 'userFullName', 'UserFullName'),
    userAvatarUrl: pickNullableString(data, 'userAvatarUrl', 'UserAvatarUrl'),
    createdAt: pickNullableString(data, 'createdAt', 'CreatedAt'),
  };
}

function mapList<T>(items: unknown[], mapper: (item: Record<string, unknown>) => T): T[] {
  return items.map((item) => mapper(item as Record<string, unknown>));
}

export const workspaceApi = {
  getMyWorkspaces: async (): Promise<Workspace[]> => {
    const response = await axiosClient.get<unknown[]>('/api/workspaces');
    return mapList(response.data ?? [], normalizeWorkspace);
  },

  getById: async (workspaceId: number): Promise<Workspace> => {
    const response = await axiosClient.get<Record<string, unknown>>(`/api/workspaces/${workspaceId}`);
    return normalizeWorkspace(response.data);
  },

  create: async (name: string): Promise<Workspace> => {
    const response = await axiosClient.post<Record<string, unknown>>('/api/workspaces', { name });
    const workspace = normalizeWorkspace(response.data);
    try {
      await axiosClient.post(`/api/workspaces/${workspace.workspaceId}/bootstrap`);
    } catch {
      // bootstrap optional — billing API will also ensure credits
    }
    return workspace;
  },

  update: async (workspaceId: number, name: string): Promise<Workspace> => {
    const response = await axiosClient.put<Record<string, unknown>>(`/api/workspaces/${workspaceId}`, { name });
    return normalizeWorkspace(response.data);
  },

  delete: async (workspaceId: number): Promise<void> => {
    await axiosClient.delete(`/api/workspaces/${workspaceId}`);
  },

  getMembers: async (workspaceId: number): Promise<WorkspaceMember[]> => {
    const response = await axiosClient.get<unknown[]>(`/api/workspaces/${workspaceId}/members`);
    return mapList(response.data ?? [], normalizeMember);
  },

  inviteMember: async (workspaceId: number, email: string): Promise<string> => {
    const response = await axiosClient.post<{ message?: string }>(
      `/api/workspaces/${workspaceId}/members/invite`,
      { email }
    );
    return response.data?.message ?? 'Đã gửi lời mời thành công.';
  },

  getPendingInvitations: async (workspaceId: number): Promise<WorkspaceInvitation[]> => {
    const response = await axiosClient.get<unknown[]>(
      `/api/workspaces/${workspaceId}/members/invitations`
    );
    return mapList(response.data ?? [], normalizeInvitation);
  },

  approveInvitation: async (workspaceId: number, invitationId: number): Promise<string> => {
    const response = await axiosClient.put<{ message?: string }>(
      `/api/workspaces/${workspaceId}/members/invitations/${invitationId}/approve`
    );
    return response.data?.message ?? 'Đã duyệt lời mời.';
  },

  rejectInvitation: async (workspaceId: number, invitationId: number): Promise<string> => {
    const response = await axiosClient.put<{ message?: string }>(
      `/api/workspaces/${workspaceId}/members/invitations/${invitationId}/reject`
    );
    return response.data?.message ?? 'Đã từ chối lời mời.';
  },

  updateMemberRole: async (workspaceId: number, targetUserId: number, roleName: string): Promise<string> => {
    const response = await axiosClient.put<{ message?: string }>(
      `/api/workspaces/${workspaceId}/members/${targetUserId}/role`,
      { roleName }
    );
    return response.data?.message ?? 'Đã cập nhật vai trò.';
  },

  kickMember: async (workspaceId: number, targetUserId: number): Promise<string> => {
    const response = await axiosClient.delete<{ message?: string }>(
      `/api/workspaces/${workspaceId}/members/${targetUserId}`
    );
    return response.data?.message ?? 'Đã xóa thành viên.';
  },

  getActivityLogs: async (workspaceId: number, page = 1, pageSize = 20): Promise<ActivityLog[]> => {
    const response = await axiosClient.get<unknown[]>(
      `/api/workspaces/${workspaceId}/activity-logs`,
      { params: { page, pageSize } }
    );
    return mapList(response.data ?? [], normalizeActivityLog);
  },
};
