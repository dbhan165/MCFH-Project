import { getAvatarFallback } from './authStorage';

export const WORKSPACE_ROLES = ['Owner', 'Editor', 'Viewer'] as const;
export type WorkspaceRole = (typeof WORKSPACE_ROLES)[number];

export function formatWorkspaceDate(value: string | null | undefined) {
  if (!value) return '—';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '—';
  return date.toLocaleDateString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });
}

export function formatWorkspaceDateTime(value: string | null | undefined) {
  if (!value) return '—';
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '—';
  return date.toLocaleString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function getRoleBadgeClass(role: string | null | undefined) {
  switch (role) {
    case 'Owner':
      return 'bg-orange-500/20 text-orange-400 border-orange-500/20';
    case 'Editor':
      return 'bg-blue-500/20 text-blue-400 border-blue-500/20';
    case 'Viewer':
      return 'bg-gray-500/20 text-gray-300 border-gray-500/20';
    default:
      return 'bg-white/10 text-gray-400 border-white/10';
  }
}

export function getRoleLabel(role: string | null | undefined) {
  switch (role) {
    case 'Owner':
      return 'Chủ sở hữu';
    case 'Editor':
      return 'Biên tập viên';
    case 'Viewer':
      return 'Người xem';
    default:
      return role || 'Thành viên';
  }
}

export function getActivityLabel(actionType: string) {
  const labels: Record<string, string> = {
    CREATE_WORKSPACE: 'Tạo workspace',
    UPDATE_WORKSPACE: 'Cập nhật workspace',
    DELETE_WORKSPACE: 'Xóa workspace',
    INVITE_MEMBER: 'Mời thành viên',
    APPROVE_INVITE: 'Duyệt lời mời',
    REJECT_INVITE: 'Từ chối lời mời',
    UPDATE_ROLE: 'Đổi vai trò',
    KICK_MEMBER: 'Xóa thành viên',
    CREATE_PROJECT: 'Tạo dự án',
    UPDATE_PROJECT: 'Cập nhật dự án',
    DELETE_PROJECT: 'Xóa dự án',
  };
  return labels[actionType] || actionType;
}

export function getMemberAvatar(name: string, avatarUrl?: string | null) {
  return avatarUrl || getAvatarFallback(name);
}

export function isWorkspaceOwner(role: string | null | undefined) {
  return role === 'Owner';
}

export function isSystemAdmin(role: string | null | undefined) {
  return role?.toLowerCase() === 'admin';
}

export function isSystemReporter(role: string | null | undefined) {
  return role?.toLowerCase() === 'reporter';
}

export function isSystemClient(role: string | null | undefined) {
  const r = role?.toLowerCase();
  return !r || r === 'client' || r === 'user';
}
