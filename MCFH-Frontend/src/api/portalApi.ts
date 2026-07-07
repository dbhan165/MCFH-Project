import axiosClient from './axiosClient';
import { pickField, pickNullableString, pickNumber, pickString } from '../utils/normalizeApi';

export interface AdminDashboard {
  totalUsers: number;
  totalReporters: number;
  totalClients: number;
  totalWorkspaces: number;
  totalProjects: number;
  totalMentions: number;
  pendingBespoke: number;
  inProgressBespoke: number;
  completedBespoke: number;
  recentBespoke: {
    requestId: number;
    title: string;
    status: string;
    clientName: string | null;
    reporterName: string | null;
    deadline: string | null;
  }[];
}

export interface AdminUser {
  userId: number;
  fullName: string;
  email: string;
  avatarUrl: string | null;
  systemRole: string;
  isBanned: boolean;
  isVerified: boolean;
  createdAt: string | null;
}

export interface AdminUserDetail {
  userId: number;
  fullName: string;
  email: string;
  phone: string | null;
  avatarUrl: string | null;
  authProvider: string;
  systemRole: string;
  isBanned: boolean;
  isVerified: boolean;
  verifiedAt: string | null;
  bannedAt: string | null;
  createdAt: string | null;
  stats: {
    ownedWorkspaces: number;
    memberWorkspaces: number;
    totalProjects: number;
    bespokeAsClient: number;
    bespokeAsReporter: number;
    unreadNotifications: number;
  };
  workspaces: {
    workspaceId: number;
    name: string;
    membershipRole: string;
    isOwner: boolean;
    projectCount: number;
    subscriptionPlan: string | null;
    subscriptionStatus: string | null;
    createdAt: string | null;
  }[];
  bespokeRequests: {
    requestId: number;
    title: string;
    status: string;
    involvement: string;
    submittedAt: string | null;
  }[];
  recentPayments: {
    paymentId: number;
    amount: number;
    status: string | null;
    type: string | null;
    planName: string | null;
    createdAt: string | null;
  }[];
}

export interface FbSource {
  fbSourceId: number;
  groupUrl: string;
  groupName?: string | null;
  status?: string | null;
  addedBy: number;
  addedByName?: string | null;
  createdAt?: string | null;
}

export interface UpsertFbSource {
  groupUrl: string;
  groupName?: string;
  status?: string;
  enabled: boolean;
}

export interface PortalBespokeRequest {
  requestId: number;
  title: string;
  requirements: string | null;
  status: string;
  statusLabel: string;
  deadline: string | null;
  submittedAt: string | null;
  assignedAt: string | null;
  clientName: string | null;
  reporterName: string | null;
  reporterId: number | null;
  workspaceId: number;
  projectId: number;
  projectName: string | null;
  workspaceName: string | null;
  modules: string[];
  dateFrom: string | null;
  dateTo: string | null;
  agreedPrice: number | null;
  hasDeliverable: boolean;
  deliverableReportId: number | null;
}

function mapPortalRequest(r: Record<string, unknown>): PortalBespokeRequest {
  return {
    requestId: pickNumber(r, 'requestId', 'RequestId'),
    title: pickString(r, 'title', 'Title'),
    requirements: pickNullableString(r, 'requirements', 'Requirements'),
    status: pickString(r, 'status', 'Status'),
    statusLabel: pickString(r, 'statusLabel', 'StatusLabel'),
    deadline: pickNullableString(r, 'deadline', 'Deadline'),
    submittedAt: pickNullableString(r, 'submittedAt', 'SubmittedAt'),
    assignedAt: pickNullableString(r, 'assignedAt', 'AssignedAt'),
    clientName: pickNullableString(r, 'clientName', 'ClientName'),
    reporterName: pickNullableString(r, 'reporterName', 'ReporterName'),
    reporterId: pickField<number>(r, 'reporterId', 'ReporterId') ?? null,
    workspaceId: pickNumber(r, 'workspaceId', 'WorkspaceId'),
    projectId: pickNumber(r, 'projectId', 'ProjectId'),
    projectName: pickNullableString(r, 'projectName', 'ProjectName'),
    workspaceName: pickNullableString(r, 'workspaceName', 'WorkspaceName'),
    modules: (pickField<string[]>(r, 'modules', 'Modules') ?? []).filter(Boolean),
    dateFrom: pickNullableString(r, 'dateFrom', 'DateFrom'),
    dateTo: pickNullableString(r, 'dateTo', 'DateTo'),
    agreedPrice: pickField<number>(r, 'agreedPrice', 'AgreedPrice') ?? null,
    hasDeliverable: pickField(r, 'hasDeliverable', 'HasDeliverable') === true,
    deliverableReportId: pickField<number>(r, 'deliverableReportId', 'DeliverableReportId') ?? null,
  };
}

export const adminApi = {
  getDashboard: async (): Promise<AdminDashboard> => {
    const res = await axiosClient.get<Record<string, unknown>>('/api/admin/dashboard');
    const d = res.data;
    const recent = (pickField<unknown[]>(d, 'recentBespoke', 'RecentBespoke') ?? []) as Record<string, unknown>[];
    return {
      totalUsers: pickNumber(d, 'totalUsers', 'TotalUsers'),
      totalReporters: pickNumber(d, 'totalReporters', 'TotalReporters'),
      totalClients: pickNumber(d, 'totalClients', 'TotalClients'),
      totalWorkspaces: pickNumber(d, 'totalWorkspaces', 'TotalWorkspaces'),
      totalProjects: pickNumber(d, 'totalProjects', 'TotalProjects'),
      totalMentions: pickNumber(d, 'totalMentions', 'TotalMentions'),
      pendingBespoke: pickNumber(d, 'pendingBespoke', 'PendingBespoke'),
      inProgressBespoke: pickNumber(d, 'inProgressBespoke', 'InProgressBespoke'),
      completedBespoke: pickNumber(d, 'completedBespoke', 'CompletedBespoke'),
      recentBespoke: recent.map((r) => ({
        requestId: pickNumber(r, 'requestId', 'RequestId'),
        title: pickString(r, 'title', 'Title'),
        status: pickString(r, 'status', 'Status'),
        clientName: pickNullableString(r, 'clientName', 'ClientName'),
        reporterName: pickNullableString(r, 'reporterName', 'ReporterName'),
        deadline: pickNullableString(r, 'deadline', 'Deadline'),
      })),
    };
  },

  getUsers: async (params?: { search?: string; role?: string; page?: number; pageSize?: number }) => {
    const res = await axiosClient.get<Record<string, unknown>>('/api/admin/users', { params });
    const d = res.data;
    const items = (pickField<unknown[]>(d, 'items', 'Items') ?? []) as Record<string, unknown>[];
    return {
      items: items.map(
        (u): AdminUser => ({
          userId: pickNumber(u, 'userId', 'UserId'),
          fullName: pickString(u, 'fullName', 'FullName'),
          email: pickString(u, 'email', 'Email'),
          avatarUrl: pickNullableString(u, 'avatarUrl', 'AvatarUrl'),
          systemRole: pickString(u, 'systemRole', 'SystemRole'),
          isBanned: pickField(u, 'isBanned', 'IsBanned') === true,
          isVerified: pickField(u, 'isVerified', 'IsVerified') === true,
          createdAt: pickNullableString(u, 'createdAt', 'CreatedAt'),
        })
      ),
      total: pickNumber(d, 'total', 'Total'),
      page: pickNumber(d, 'page', 'Page'),
      pageSize: pickNumber(d, 'pageSize', 'PageSize'),
    };
  },

  updateUser: async (userId: number, payload: { systemRole?: string; isBanned?: boolean }) => {
    const res = await axiosClient.patch<Record<string, unknown>>(`/api/admin/users/${userId}`, payload);
    const u = res.data;
    return {
      userId: pickNumber(u, 'userId', 'UserId'),
      fullName: pickString(u, 'fullName', 'FullName'),
      email: pickString(u, 'email', 'Email'),
      avatarUrl: pickNullableString(u, 'avatarUrl', 'AvatarUrl'),
      systemRole: pickString(u, 'systemRole', 'SystemRole'),
      isBanned: pickField(u, 'isBanned', 'IsBanned') === true,
      isVerified: pickField(u, 'isVerified', 'IsVerified') === true,
      createdAt: pickNullableString(u, 'createdAt', 'CreatedAt'),
    } as AdminUser;
  },

  getUserDetail: async (userId: number): Promise<AdminUserDetail> => {
    const res = await axiosClient.get<Record<string, unknown>>(`/api/admin/users/${userId}`);
    const d = res.data;
    const stats = (pickField<Record<string, unknown>>(d, 'stats', 'Stats') ?? {}) as Record<string, unknown>;
    const workspaces = (pickField<unknown[]>(d, 'workspaces', 'Workspaces') ?? []) as Record<string, unknown>[];
    const bespokeRequests = (pickField<unknown[]>(d, 'bespokeRequests', 'BespokeRequests') ?? []) as Record<
      string,
      unknown
    >[];
    const recentPayments = (pickField<unknown[]>(d, 'recentPayments', 'RecentPayments') ?? []) as Record<
      string,
      unknown
    >[];

    return {
      userId: pickNumber(d, 'userId', 'UserId'),
      fullName: pickString(d, 'fullName', 'FullName'),
      email: pickString(d, 'email', 'Email'),
      phone: pickNullableString(d, 'phone', 'Phone'),
      avatarUrl: pickNullableString(d, 'avatarUrl', 'AvatarUrl'),
      authProvider: pickString(d, 'authProvider', 'AuthProvider'),
      systemRole: pickString(d, 'systemRole', 'SystemRole'),
      isBanned: pickField(d, 'isBanned', 'IsBanned') === true,
      isVerified: pickField(d, 'isVerified', 'IsVerified') === true,
      verifiedAt: pickNullableString(d, 'verifiedAt', 'VerifiedAt'),
      bannedAt: pickNullableString(d, 'bannedAt', 'BannedAt'),
      createdAt: pickNullableString(d, 'createdAt', 'CreatedAt'),
      stats: {
        ownedWorkspaces: pickNumber(stats, 'ownedWorkspaces', 'OwnedWorkspaces'),
        memberWorkspaces: pickNumber(stats, 'memberWorkspaces', 'MemberWorkspaces'),
        totalProjects: pickNumber(stats, 'totalProjects', 'TotalProjects'),
        bespokeAsClient: pickNumber(stats, 'bespokeAsClient', 'BespokeAsClient'),
        bespokeAsReporter: pickNumber(stats, 'bespokeAsReporter', 'BespokeAsReporter'),
        unreadNotifications: pickNumber(stats, 'unreadNotifications', 'UnreadNotifications'),
      },
      workspaces: workspaces.map((w) => ({
        workspaceId: pickNumber(w, 'workspaceId', 'WorkspaceId'),
        name: pickString(w, 'name', 'Name'),
        membershipRole: pickString(w, 'membershipRole', 'MembershipRole'),
        isOwner: pickField(w, 'isOwner', 'IsOwner') === true,
        projectCount: pickNumber(w, 'projectCount', 'ProjectCount'),
        subscriptionPlan: pickNullableString(w, 'subscriptionPlan', 'SubscriptionPlan'),
        subscriptionStatus: pickNullableString(w, 'subscriptionStatus', 'SubscriptionStatus'),
        createdAt: pickNullableString(w, 'createdAt', 'CreatedAt'),
      })),
      bespokeRequests: bespokeRequests.map((r) => ({
        requestId: pickNumber(r, 'requestId', 'RequestId'),
        title: pickString(r, 'title', 'Title'),
        status: pickString(r, 'status', 'Status'),
        involvement: pickString(r, 'involvement', 'Involvement'),
        submittedAt: pickNullableString(r, 'submittedAt', 'SubmittedAt'),
      })),
      recentPayments: recentPayments.map((p) => ({
        paymentId: pickNumber(p, 'paymentId', 'PaymentId'),
        amount: Number(pickField(p, 'amount', 'Amount') ?? 0),
        status: pickNullableString(p, 'status', 'Status'),
        type: pickNullableString(p, 'type', 'Type'),
        planName: pickNullableString(p, 'planName', 'PlanName'),
        createdAt: pickNullableString(p, 'createdAt', 'CreatedAt'),
      })),
    };
  },

  getBespokeRequests: async (): Promise<PortalBespokeRequest[]> => {
    const res = await axiosClient.get<unknown[]>('/api/admin/bespoke');
    return (res.data as Record<string, unknown>[]).map(mapPortalRequest);
  },

  getReporters: async () => {
    const res = await axiosClient.get<unknown[]>('/api/admin/reporters');
    return (res.data as Record<string, unknown>[]).map((r) => ({
      userId: pickNumber(r, 'userId', 'UserId'),
      fullName: pickString(r, 'fullName', 'FullName'),
      email: pickString(r, 'email', 'Email'),
    }));
  },

  assignReporter: async (requestId: number, reporterId: number) => {
    await axiosClient.post(`/api/admin/bespoke/${requestId}/assign`, { reporterId });
  },

  getSubscriptionPlans: async () => {
    const res = await axiosClient.get<unknown[]>('/api/admin/subscription-plans');
    return (res.data ?? []).map((item) => {
      const p = item as Record<string, unknown>;
      return {
        planId: pickNumber(p, 'planId', 'PlanId'),
        name: pickString(p, 'name', 'Name'),
        price: Number(pickField(p, 'price', 'Price') ?? 0),
        priceLabel: pickString(p, 'priceLabel', 'PriceLabel'),
        aiCreditLimit: pickNumber(p, 'aiCreditLimit', 'AiCreditLimit'),
        activeSubscribers: pickNumber(p, 'activeSubscribers', 'ActiveSubscribers'),
      };
    });
  },

  updateSubscriptionPlan: async (planId: number, payload: { name: string; price: number; aiCreditLimit: number }) => {
    const res = await axiosClient.put<Record<string, unknown>>(`/api/admin/subscription-plans/${planId}`, payload);
    return res.data;
  },

  getProxies: async () => {
    const res = await axiosClient.get<unknown[]>('/api/admin/proxies');
    return (res.data ?? []).map((item) => {
      const p = item as Record<string, unknown>;
      return {
        proxyId: pickNumber(p, 'proxyId', 'ProxyId'),
        ipAddress: pickString(p, 'ipAddress', 'IpAddress'),
        port: pickNumber(p, 'port', 'Port'),
        authUser: pickNullableString(p, 'authUser', 'AuthUser'),
        status: pickNullableString(p, 'status', 'Status'),
        failCount: pickNumber(p, 'failCount', 'FailCount'),
        lastUsedAt: pickNullableString(p, 'lastUsedAt', 'LastUsedAt'),
        enabled: pickField(p, 'enabled', 'Enabled') !== false,
      };
    });
  },

  createProxy: async (payload: {
    ipAddress: string;
    port: number;
    authUser?: string;
    authPass?: string;
    status?: string;
    enabled?: boolean;
  }) => {
    const res = await axiosClient.post('/api/admin/proxies', payload);
    return res.data;
  },

  updateProxy: async (proxyId: number, payload: Record<string, unknown>) => {
    const res = await axiosClient.put(`/api/admin/proxies/${proxyId}`, payload);
    return res.data;
  },

  deleteProxy: async (proxyId: number) => {
    await axiosClient.delete(`/api/admin/proxies/${proxyId}`);
  },

  getScrapingJobs: async () => {
    const res = await axiosClient.get<unknown[]>('/api/admin/scraping-jobs');
    return (res.data ?? []).map((item) => {
      const j = item as Record<string, unknown>;
      return {
        jobId: pickString(j, 'jobId', 'JobId'),
        projectId: pickNumber(j, 'projectId', 'ProjectId'),
        projectName: pickNullableString(j, 'projectName', 'ProjectName'),
        status: pickNullableString(j, 'status', 'Status'),
        totalScraped: pickNumber(j, 'totalScraped', 'TotalScraped'),
        errorLog: pickNullableString(j, 'errorLog', 'ErrorLog'),
        proxyIp: pickNullableString(j, 'proxyIp', 'ProxyIp'),
        startedAt: pickNullableString(j, 'startedAt', 'StartedAt'),
        finishedAt: pickNullableString(j, 'finishedAt', 'FinishedAt'),
      };
    });
  },

  getFbSources: async (): Promise<FbSource[]> => {
    const res = await axiosClient.get<unknown[]>('/api/admin/fb-sources');
    return (res.data ?? []).map((item) => {
      const s = item as Record<string, unknown>;
      return {
        fbSourceId: pickNumber(s, 'fbSourceId', 'FbSourceId'),
        groupUrl: pickString(s, 'groupUrl', 'GroupUrl'),
        groupName: pickNullableString(s, 'groupName', 'GroupName'),
        status: pickNullableString(s, 'status', 'Status'),
        addedBy: pickNumber(s, 'addedBy', 'AddedBy'),
        addedByName: pickNullableString(s, 'addedByName', 'AddedByName'),
        createdAt: pickNullableString(s, 'createdAt', 'CreatedAt'),
      };
    });
  },

  createFbSource: async (payload: UpsertFbSource) => {
    const res = await axiosClient.post('/api/admin/fb-sources', payload);
    return res.data;
  },

  updateFbSource: async (fbSourceId: number, payload: UpsertFbSource) => {
    const res = await axiosClient.put(`/api/admin/fb-sources/${fbSourceId}`, payload);
    return res.data;
  },

  deleteFbSource: async (fbSourceId: number) => {
    await axiosClient.delete(`/api/admin/fb-sources/${fbSourceId}`);
  },

  getSettings: async () => {
    const res = await axiosClient.get<unknown[]>('/api/admin/settings');
    return (res.data ?? []).map((item) => {
      const s = item as Record<string, unknown>;
      return {
        settingId: pickNumber(s, 'settingId', 'SettingId'),
        settingKey: pickString(s, 'settingKey', 'SettingKey'),
        settingValue: pickNullableString(s, 'settingValue', 'SettingValue'),
        isEncrypted: pickField(s, 'isEncrypted', 'IsEncrypted') === true,
      };
    });
  },

  updateSettings: async (settings: Record<string, string | null | undefined>) => {
    const res = await axiosClient.put<unknown[]>('/api/admin/settings', { settings });
    return res.data;
  },
};

export interface GeminiTestResult {
  configured: boolean;
  success: boolean;
  modelUsed: string | null;
  message: string;
  sampleSummary: string | null;
  sampleSentiment: string | null;
}

export const geminiApi = {
  status: async () => {
    const res = await axiosClient.get<Record<string, unknown>>('/api/gemini/status');
    return {
      configured: pickField(res.data, 'configured', 'Configured') === true,
      message: pickString(res.data, 'message', 'Message'),
    };
  },

  test: async (): Promise<GeminiTestResult> => {
    const res = await axiosClient.post<Record<string, unknown>>('/api/gemini/test');
    const d = res.data;
    return {
      configured: pickField(d, 'configured', 'Configured') === true,
      success: pickField(d, 'success', 'Success') === true,
      modelUsed: pickNullableString(d, 'modelUsed', 'ModelUsed'),
      message: pickString(d, 'message', 'Message'),
      sampleSummary: pickNullableString(d, 'sampleSummary', 'SampleSummary'),
      sampleSentiment: pickNullableString(d, 'sampleSentiment', 'SampleSentiment'),
    };
  },
};

export const reporterApi = {
  getKanban: async () => {
    const res = await axiosClient.get<Record<string, unknown>>('/api/reporter/kanban');
    const d = res.data;
    const mapList = (key: string) =>
      ((pickField<unknown[]>(d, key, key.charAt(0).toUpperCase() + key.slice(1)) ?? []) as Record<string, unknown>[]).map(
        mapPortalRequest
      );
    return {
      pending: mapList('pending'),
      inProgress: mapList('inProgress'),
      completed: mapList('completed'),
    };
  },

  getRequest: async (requestId: number): Promise<PortalBespokeRequest> => {
    const res = await axiosClient.get<Record<string, unknown>>(`/api/reporter/requests/${requestId}`);
    return mapPortalRequest(res.data);
  },

  quote: async (requestId: number, payload: { agreedPrice: number; deadline?: string; note?: string }) => {
    await axiosClient.post(`/api/reporter/requests/${requestId}/quote`, payload);
  },

  startWork: async (requestId: number) => {
    await axiosClient.post(`/api/reporter/requests/${requestId}/start`);
  },

  deliver: async (requestId: number) => {
    await axiosClient.post(`/api/reporter/requests/${requestId}/deliver`);
  },

  download: async (requestId: number) => {
    const res = await axiosClient.get(`/api/reporter/requests/${requestId}/download`, { responseType: 'blob' });
    const url = window.URL.createObjectURL(new Blob([res.data]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `bespoke-report-${requestId}.html`);
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  },

  getPerformance: async () => {
    const res = await axiosClient.get<Record<string, unknown>>('/api/reporter/performance');
    const d = res.data;
    const history = (pickField<unknown[]>(d, 'history', 'History') ?? []) as Record<string, unknown>[];
    return {
      deliveredCount: pickNumber(d, 'deliveredCount', 'DeliveredCount'),
      inProgressCount: pickNumber(d, 'inProgressCount', 'InProgressCount'),
      pendingCount: pickNumber(d, 'pendingCount', 'PendingCount'),
      avgProcessingDays: pickField<number>(d, 'avgProcessingDays', 'AvgProcessingDays') ?? null,
      history: history.map(mapPortalRequest),
    };
  },

  getAnalyticsPreview: async (requestId: number) => {
    const res = await axiosClient.get<Record<string, unknown>>(`/api/reporter/requests/${requestId}/analytics-preview`);
    const d = res.data;
    return {
      projectName: pickString(d, 'projectName', 'ProjectName'),
      totalMentions: pickNumber(d, 'totalMentions', 'TotalMentions'),
      totalComments: pickNumber(d, 'totalComments', 'TotalComments'),
      negativeCount: pickNumber(d, 'negativeCount', 'NegativeCount'),
      positiveCount: pickNumber(d, 'positiveCount', 'PositiveCount'),
      nsrScore: pickField<number>(d, 'nsrScore', 'NsrScore') ?? 0,
    };
  },
};
