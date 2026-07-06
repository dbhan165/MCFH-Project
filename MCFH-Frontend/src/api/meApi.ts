import axiosClient from './axiosClient';
import { pickField, pickNullableString, pickNumber, pickString } from '../utils/normalizeApi';
import type { ReceivedInvitation } from '../types/workspace';

export interface AppNotification {
  notificationId: number;
  title: string;
  message: string | null;
  type: string | null;
  relatedType: string | null;
  relatedId: number | null;
  projectId: number | null;
  workspaceId: number | null;
  isRead: boolean;
  createdAt: string | null;
}

function normalizeNotification(data: Record<string, unknown>): AppNotification {
  return {
    notificationId: pickNumber(data, 'notificationId', 'NotificationId'),
    title: pickString(data, 'title', 'Title'),
    message: pickNullableString(data, 'message', 'Message'),
    type: pickNullableString(data, 'type', 'Type'),
    relatedType: pickNullableString(data, 'relatedType', 'RelatedType'),
    relatedId: pickField<number>(data, 'relatedId', 'RelatedId') ?? null,
    projectId: pickField<number>(data, 'projectId', 'ProjectId') ?? null,
    workspaceId: pickField<number>(data, 'workspaceId', 'WorkspaceId') ?? null,
    isRead: pickField(data, 'isRead', 'IsRead') === true,
    createdAt: pickNullableString(data, 'createdAt', 'CreatedAt'),
  };
}

function normalizeReceivedInvitation(data: Record<string, unknown>): ReceivedInvitation {
  return {
    invitationId: pickNumber(data, 'invitationId', 'InvitationId'),
    workspaceId: pickNumber(data, 'workspaceId', 'WorkspaceId'),
    workspaceName: pickString(data, 'workspaceName', 'WorkspaceName'),
    invitedByName: pickString(data, 'invitedByName', 'InvitedByName'),
    status: pickString(data, 'status', 'Status'),
    createdAt: pickNullableString(data, 'createdAt', 'CreatedAt'),
  };
}

export const meApi = {
  getInvitations: async (): Promise<ReceivedInvitation[]> => {
    const response = await axiosClient.get<unknown[]>('/api/me/invitations');
    return (response.data ?? []).map((item) =>
      normalizeReceivedInvitation(item as Record<string, unknown>)
    );
  },

  acceptInvitation: async (invitationId: number): Promise<string> => {
    const response = await axiosClient.post<{ message?: string }>(
      `/api/me/invitations/${invitationId}/accept`
    );
    return response.data?.message ?? '─É├ú tham gia workspace.';
  },

  declineInvitation: async (invitationId: number): Promise<string> => {
    const response = await axiosClient.post<{ message?: string }>(
      `/api/me/invitations/${invitationId}/decline`
    );
    return response.data?.message ?? '─É├ú tß╗½ chß╗æi lß╗¥i mß╗¥i.';
  },

  getNotifications: async (limit = 30): Promise<AppNotification[]> => {
    const response = await axiosClient.get<unknown[]>('/api/me/notifications', {
      params: { limit },
    });
    return (response.data ?? []).map((item) =>
      normalizeNotification(item as Record<string, unknown>)
    );
  },

  getUnreadCount: async (): Promise<number> => {
    const response = await axiosClient.get<Record<string, unknown>>('/api/me/notifications/unread-count');
    return pickNumber(response.data, 'count', 'Count');
  },

  markNotificationRead: async (notificationId: number): Promise<void> => {
    await axiosClient.put(`/api/me/notifications/${notificationId}/read`);
  },

  markAllNotificationsRead: async (): Promise<void> => {
    await axiosClient.put('/api/me/notifications/read-all');
  },
};
