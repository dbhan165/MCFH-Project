import { projectApi } from '../api/projectApi';
import { workspaceApi } from '../api/workspaceApi';
import { loadProfileFromStorage } from './authStorage';

function resolveRoleHomePath(role: string): string | null {
  const normalized = role.trim().toLowerCase();
  if (normalized === 'admin') return '/admin/dashboard';
  if (normalized === 'reporter') return '/reporter/tasks';
  return null;
}

/** Sau đăng nhập: Admin/Reporter → portal; Client → /workspaces hoặc project gần nhất */
export async function resolvePostLoginPath(): Promise<string> {
  const profile = loadProfileFromStorage();
  const rolePath = profile ? resolveRoleHomePath(profile.role) : null;
  if (rolePath) return rolePath;

  try {
    const workspaces = await workspaceApi.getMyWorkspaces();

    if (workspaces.length === 0) {
      return '/workspaces';
    }

    const workspace = workspaces[0];
    const projects = await projectApi.getProjects(workspace.workspaceId);

    if (projects.length === 0) {
      return `/create-project?wid=${workspace.workspaceId}&onboarding=1`;
    }

    return `/workspace/${workspace.workspaceId}/projects`;
  } catch {
    return '/workspaces';
  }
}

export function getPrimaryKeyword(keywords: string): string {
  return keywords
    .split(',')
    .map((item) => item.trim())
    .filter(Boolean)[0] ?? '';
}

export const SCRAPABLE_PLATFORMS = ['facebook', 'youtube', 'news'] as const;

export function buildDataSources(
  selectedSources: string[]
): { platform: string; targetUrl?: string }[] {
  return selectedSources
    .filter((platform) => SCRAPABLE_PLATFORMS.includes(platform as (typeof SCRAPABLE_PLATFORMS)[number]))
    .map((platform) => ({
      platform,
    }));
}
