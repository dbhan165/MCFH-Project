export interface SystemProxy {
  proxyId: number;
  ipAddress: string;
  port: number;
  authUser?: string | null;
  status?: string | null;
  failCount: number;
  lastUsedAt?: string | null;
  enabled: boolean;
}

export interface UpsertSystemProxy {
  ipAddress: string;
  port: number;
  authUser?: string;
  authPass?: string;
  status?: string;
  enabled: boolean;
}

export interface ScrapingJob {
  jobId: string;
  projectId: number;
  projectName?: string | null;
  sourceId?: number | null;
  status?: string | null;
  totalScraped: number;
  errorLog?: string | null;
  proxyIp?: string | null;
  startedAt?: string | null;
  finishedAt?: string | null;
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
