export interface UserProfile {
  userId: number;
  email: string;
  fullName: string;
  phone?: string | null;
  avatarUrl?: string | null;
  authProvider: string;
  role: string;
}

export interface AuthResponse extends UserProfile {
  token: string;
}

/** Hỗ trợ cả camelCase và PascalCase từ ASP.NET */
export function normalizeProfile(data: Record<string, unknown>): UserProfile {
  return {
    userId: Number(data.userId ?? data.UserId ?? 0),
    email: String(data.email ?? data.Email ?? ''),
    fullName: String(data.fullName ?? data.FullName ?? ''),
    phone: (data.phone ?? data.Phone ?? null) as string | null,
    avatarUrl: (data.avatarUrl ?? data.AvatarUrl ?? null) as string | null,
    authProvider: String(data.authProvider ?? data.AuthProvider ?? 'local'),
    role: String(data.role ?? data.Role ?? 'Client'),
  };
}

export function normalizeAuthResponse(data: Record<string, unknown>): AuthResponse {
  const profile = normalizeProfile(data);
  return {
    ...profile,
    token: String(data.token ?? data.Token ?? ''),
  };
}

export function loadProfileFromStorage(): UserProfile | null {
  const email = localStorage.getItem('userEmail');
  const userId = localStorage.getItem('userId');
  if (!email || !userId) return null;

  return {
    userId: Number(userId),
    email,
    fullName: localStorage.getItem('fullName') || '',
    phone: localStorage.getItem('phone') || null,
    avatarUrl: localStorage.getItem('avatarUrl') || null,
    authProvider: localStorage.getItem('authProvider') || 'local',
    role: localStorage.getItem('userRole') || 'Client',
  };
}

export function saveAuthSession(data: AuthResponse | Record<string, unknown>) {
  const normalized = normalizeAuthResponse(data as Record<string, unknown>);
  clearEmailPendingVerification();

  localStorage.setItem('accessToken', normalized.token);
  localStorage.setItem('userId', String(normalized.userId));
  localStorage.setItem('userEmail', normalized.email);
  localStorage.setItem('fullName', normalized.fullName);
  localStorage.setItem('userRole', normalized.role);
  localStorage.setItem('authProvider', normalized.authProvider);

  if (normalized.phone) localStorage.setItem('phone', normalized.phone);
  else localStorage.removeItem('phone');

  if (normalized.avatarUrl) localStorage.setItem('avatarUrl', normalized.avatarUrl);
  else localStorage.removeItem('avatarUrl');
}

export function saveUserProfile(data: UserProfile) {
  const normalized = normalizeProfile(data as unknown as Record<string, unknown>);

  localStorage.setItem('userId', String(normalized.userId));
  localStorage.setItem('userEmail', normalized.email);
  localStorage.setItem('fullName', normalized.fullName);
  localStorage.setItem('userRole', normalized.role);
  localStorage.setItem('authProvider', normalized.authProvider);

  if (normalized.phone) localStorage.setItem('phone', normalized.phone);
  else localStorage.removeItem('phone');

  if (normalized.avatarUrl) localStorage.setItem('avatarUrl', normalized.avatarUrl);
  else localStorage.removeItem('avatarUrl');
}

export function getAvatarFallback(name: string) {
  const encoded = encodeURIComponent(name || 'U');
  return `https://ui-avatars.com/api/?name=${encoded}&background=FF7575&color=fff&size=256`;
}

export function extractApiError(error: unknown, fallback: string): string {
  if (error instanceof Error && error.message.trim()) {
    return error.message;
  }

  const err = error as { response?: { status?: number; data?: Record<string, unknown> } };
  const data = err.response?.data;
  const status = err.response?.status;

  if (status === 404) {
    return 'API chưa sẵn sàng. Hãy restart backend (dotnet run) rồi đăng nhập lại.';
  }
  if (status === 401) {
    return 'Phiên đăng nhập hết hạn. Vui lòng đăng xuất và đăng nhập lại.';
  }

  if (data?.message && typeof data.message === 'string') return data.message;
  if (data?.title && typeof data.title === 'string') return data.title;

  if (data?.errors && typeof data.errors === 'object') {
    const messages = Object.values(data.errors as Record<string, string[]>)
      .flat()
      .filter(Boolean);
    if (messages.length > 0) return messages.join(' ');
  }

  return fallback;
}

const PENDING_EMAIL_VERIFY_KEY = 'pendingEmailVerification';

export function clearAuthSession() {
  localStorage.removeItem('accessToken');
  localStorage.removeItem('userId');
  localStorage.removeItem('userEmail');
  localStorage.removeItem('fullName');
  localStorage.removeItem('userRole');
  localStorage.removeItem('authProvider');
  localStorage.removeItem('phone');
  localStorage.removeItem('avatarUrl');
  clearEmailPendingVerification();
}


export function markEmailPendingVerification(email: string) {
  sessionStorage.setItem(PENDING_EMAIL_VERIFY_KEY, email.trim().toLowerCase());
}

export function clearEmailPendingVerification() {
  sessionStorage.removeItem(PENDING_EMAIL_VERIFY_KEY);
}

export function isEmailPendingVerification(email: string) {
  return sessionStorage.getItem(PENDING_EMAIL_VERIFY_KEY) === email.trim().toLowerCase();
}
