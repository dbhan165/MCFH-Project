import axiosClient from './axiosClient';
import type {
  AnalyzeProjectResult,
  AspectAnalysis,
  ChannelComparison,
  CreateProjectPayload,
  InfluencerAnalytics,
  Project,
  ProjectMention,
  ProjectOverviewStats,
  ScrapeResult,
  ScrapeJobStatus,
  ScrapeJobStart,
  SentimentSummary,
  ReportCenter,
  ReportFile,
  BespokeCenter,
  BespokeRequestItem,
  CreateBespokePayload,
} from '../types/project';
import { pickField, pickNullableString, pickNumber, pickString } from '../utils/normalizeApi';

function normalizeProject(data: Record<string, unknown>): Project {
  return {
    projectId: pickNumber(data, 'projectId', 'ProjectId'),
    workspaceId: pickNumber(data, 'workspaceId', 'WorkspaceId'),
    name: pickString(data, 'name', 'Name'),
    description: pickNullableString(data, 'description', 'Description'),
    searchQuery: pickNullableString(data, 'searchQuery', 'SearchQuery'),
    dataSourceCount: pickNumber(data, 'dataSourceCount', 'DataSourceCount'),
    enableFacebook: Boolean(pickField(data, 'enableFacebook', 'EnableFacebook')),
    enableYoutube: Boolean(pickField(data, 'enableYoutube', 'EnableYoutube')),
    enableTiktok: Boolean(pickField(data, 'enableTiktok', 'EnableTiktok')),
    enableMaps: Boolean(pickField(data, 'enableMaps', 'EnableMaps')),
    createdAt: pickNullableString(data, 'createdAt', 'CreatedAt'),
  };
}

function normalizeAnalyzeResult(data: Record<string, unknown>): AnalyzeProjectResult {
  return {
    projectId: pickNumber(data, 'projectId', 'ProjectId'),
    analyzedCount: pickNumber(data, 'analyzedCount', 'AnalyzedCount'),
    skippedCount: pickNumber(data, 'skippedCount', 'SkippedCount'),
    totalFeedbacks: pickNumber(data, 'totalFeedbacks', 'TotalFeedbacks'),
    message: pickString(data, 'message', 'Message'),
  };
}

export const projectApi = {
  getProjects: async (workspaceId: number): Promise<Project[]> => {
    const response = await axiosClient.get<unknown[]>(
      `/api/workspaces/${workspaceId}/projects/extended`
    );
    return (response.data ?? []).map((item) => normalizeProject(item as Record<string, unknown>));
  },

  getById: async (workspaceId: number, projectId: number): Promise<Project> => {
    const response = await axiosClient.get<Record<string, unknown>>(
      `/api/workspaces/${workspaceId}/projects/${projectId}/detail`
    );
    return normalizeProject(response.data);
  },

  create: async (workspaceId: number, payload: CreateProjectPayload): Promise<Project> => {
    const hasExtended =
      payload.enableFacebook != null ||
      payload.enableYoutube != null ||
      payload.enableTiktok != null ||
      payload.enableMaps != null ||
      (payload.dataSources?.length ?? 0) > 0;

    const url = hasExtended
      ? `/api/workspaces/${workspaceId}/projects/with-sources`
      : `/api/workspaces/${workspaceId}/projects`;

    const body = hasExtended
      ? {
          name: payload.name,
          description: payload.description,
          searchQuery: payload.searchQuery,
          enableFacebook: payload.enableFacebook ?? false,
          enableYoutube: payload.enableYoutube ?? false,
          enableTiktok: payload.enableTiktok ?? false,
          enableMaps: payload.enableMaps ?? false,
          dataSources: (payload.dataSources ?? []).map((s) => ({
            platform: s.platform,
            targetUrl: s.targetUrl ?? null,
          })),
        }
      : payload;

    const response = await axiosClient.post<Record<string, unknown>>(url, body);
    return normalizeProject(response.data);
  },

  delete: async (workspaceId: number, projectId: number): Promise<void> => {
    await axiosClient.delete(`/api/workspaces/${workspaceId}/projects/${projectId}`);
  },

  update: async (
    workspaceId: number,
    projectId: number,
    payload: {
      name: string;
      description?: string | null;
      searchQuery?: string | null;
      enableFacebook?: boolean;
      enableYoutube?: boolean;
      enableTiktok?: boolean;
      enableMaps?: boolean;
    }
  ): Promise<Project> => {
    const response = await axiosClient.put<Record<string, unknown>>(
      `/api/workspaces/${workspaceId}/projects/${projectId}/extended`,
      {
        name: payload.name,
        description: payload.description,
        searchQuery: payload.searchQuery,
        enableFacebook: payload.enableFacebook,
        enableYoutube: payload.enableYoutube,
        enableTiktok: payload.enableTiktok,
        enableMaps: payload.enableMaps,
      }
    );
    return normalizeProject(response.data);
  },

  analyze: async (workspaceId: number, projectId: number, force = true): Promise<AnalyzeProjectResult> => {
    const response = await axiosClient.post<Record<string, unknown>>(
      `/api/workspaces/${workspaceId}/projects/${projectId}/analyze`,
      null,
      { params: { force }, timeout: 120_000 }
    );
    return normalizeAnalyzeResult(response.data);
  },

  getOverview: async (workspaceId: number, projectId: number): Promise<ProjectOverviewStats> => {
    const response = await axiosClient.get<Record<string, unknown>>(
      `/api/workspaces/${workspaceId}/projects/${projectId}/analytics/overview`
    );
    const d = response.data;
    return {
      projectId: pickNumber(d, 'projectId', 'ProjectId'),
      projectName: pickString(d, 'projectName', 'ProjectName'),
      totalMentions: pickNumber(d, 'totalMentions', 'TotalMentions'),
      totalComments: pickNumber(d, 'totalComments', 'TotalComments'),
      analyzedCount: pickNumber(d, 'analyzedCount', 'AnalyzedCount'),
      pendingAnalysisCount: pickNumber(d, 'pendingAnalysisCount', 'PendingAnalysisCount'),
      nsrScore: Number(pickField(d, 'nsrScore', 'NsrScore') ?? 0),
      positiveCount: pickNumber(d, 'positiveCount', 'PositiveCount'),
      negativeCount: pickNumber(d, 'negativeCount', 'NegativeCount'),
      neutralCount: pickNumber(d, 'neutralCount', 'NeutralCount'),
      platformBreakdown: (pickField<Record<string, number>>(d, 'platformBreakdown', 'PlatformBreakdown') ?? {}),
    };
  },

  getMentions: async (
    workspaceId: number,
    projectId: number,
    filters?: {
      platform?: string;
      sentiment?: string;
      search?: string;
      dateFrom?: string;
      dateTo?: string;
    }
  ): Promise<ProjectMention[]> => {
    const response = await axiosClient.get<unknown[]>(
      `/api/workspaces/${workspaceId}/projects/${projectId}/analytics/mentions`,
      { params: filters }
    );
    return (response.data ?? []).map((item) => {
      const d = item as Record<string, unknown>;
      return {
        feedbackId: pickNumber(d, 'feedbackId', 'FeedbackId'),
        authorName: pickNullableString(d, 'authorName', 'AuthorName'),
        platform: pickString(d, 'platform', 'Platform'),
        content: pickString(d, 'content', 'Content'),
        sentiment: pickNullableString(d, 'sentiment', 'Sentiment'),
        confidenceScore: pickField<number>(d, 'confidenceScore', 'ConfidenceScore') ?? null,
        originalUrl: pickNullableString(d, 'originalUrl', 'OriginalUrl'),
        commentsCount: pickNumber(d, 'commentsCount', 'CommentsCount'),
        scrapedAt: pickNullableString(d, 'scrapedAt', 'ScrapedAt'),
        postedAt: pickNullableString(d, 'postedAt', 'PostedAt'),
        aiSummary: pickNullableString(d, 'aiSummary', 'AiSummary'),
        comments: (pickField<string[]>(d, 'comments', 'Comments') ?? []).filter(Boolean),
        isAnalyzed:
          pickField(d, 'isAnalyzed', 'IsAnalyzed') === true ||
          pickNullableString(d, 'sentiment', 'Sentiment') != null,
        analyzedAt: pickNullableString(d, 'analyzedAt', 'AnalyzedAt'),
      };
    });
  },

  getSentiment: async (workspaceId: number, projectId: number): Promise<SentimentSummary> => {
    const response = await axiosClient.get<Record<string, unknown>>(
      `/api/workspaces/${workspaceId}/projects/${projectId}/analytics/sentiment`
    );
    const d = response.data;
    return {
      total: pickNumber(d, 'total', 'Total'),
      positive: pickNumber(d, 'positive', 'Positive'),
      negative: pickNumber(d, 'negative', 'Negative'),
      neutral: pickNumber(d, 'neutral', 'Neutral'),
      unanalyzed: pickNumber(d, 'unanalyzed', 'Unanalyzed'),
      positivePercent: Number(pickField(d, 'positivePercent', 'PositivePercent') ?? 0),
      negativePercent: Number(pickField(d, 'negativePercent', 'NegativePercent') ?? 0),
      neutralPercent: Number(pickField(d, 'neutralPercent', 'NeutralPercent') ?? 0),
      nsrScore: Number(pickField(d, 'nsrScore', 'NsrScore') ?? 0),
    };
  },

  getInfluencers: async (workspaceId: number, projectId: number): Promise<InfluencerAnalytics> => {
    const response = await axiosClient.get<Record<string, unknown>>(
      `/api/workspaces/${workspaceId}/projects/${projectId}/analytics/influencers`
    );
    const d = response.data;
    const rawList = (pickField<unknown[]>(d, 'influencers', 'Influencers') ?? []) as Record<string, unknown>[];

    return {
      totalMentions: pickNumber(d, 'totalMentions', 'TotalMentions'),
      uniqueInfluencers: pickNumber(d, 'uniqueInfluencers', 'UniqueInfluencers'),
      influencers: rawList.map((item) => ({
        id: pickString(item, 'id', 'Id'),
        name: pickString(item, 'name', 'Name'),
        platform: pickString(item, 'platform', 'Platform'),
        handleUrl: pickNullableString(item, 'handleUrl', 'HandleUrl'),
        mentions: pickNumber(item, 'mentions', 'Mentions'),
        totalComments: pickNumber(item, 'totalComments', 'TotalComments'),
        shareOfVoice: Number(pickField(item, 'shareOfVoice', 'ShareOfVoice') ?? 0),
        influenceScore: Number(pickField(item, 'influenceScore', 'InfluenceScore') ?? 0),
        dominantSentiment: pickNullableString(item, 'dominantSentiment', 'DominantSentiment'),
        positiveCount: pickNumber(item, 'positiveCount', 'PositiveCount'),
        negativeCount: pickNumber(item, 'negativeCount', 'NegativeCount'),
        neutralCount: pickNumber(item, 'neutralCount', 'NeutralCount'),
        followers: pickField<number>(item, 'followers', 'Followers') ?? null,
      })),
    };
  },

  getChannels: async (workspaceId: number, projectId: number): Promise<ChannelComparison> => {
    const response = await axiosClient.get<Record<string, unknown>>(
      `/api/workspaces/${workspaceId}/projects/${projectId}/analytics/channels`
    );
    const d = response.data;
    const rawList = (pickField<unknown[]>(d, 'channels', 'Channels') ?? []) as Record<string, unknown>[];

    return {
      totalMentions: pickNumber(d, 'totalMentions', 'TotalMentions'),
      totalComments: pickNumber(d, 'totalComments', 'TotalComments'),
      channels: rawList.map((item) => ({
        platform: pickString(item, 'platform', 'Platform'),
        label: pickString(item, 'label', 'Label'),
        mentions: pickNumber(item, 'mentions', 'Mentions'),
        mentionShare: Number(pickField(item, 'mentionShare', 'MentionShare') ?? 0),
        totalComments: pickNumber(item, 'totalComments', 'TotalComments'),
        commentShare: Number(pickField(item, 'commentShare', 'CommentShare') ?? 0),
        positive: pickNumber(item, 'positive', 'Positive'),
        negative: pickNumber(item, 'negative', 'Negative'),
        neutral: pickNumber(item, 'neutral', 'Neutral'),
        unanalyzed: pickNumber(item, 'unanalyzed', 'Unanalyzed'),
        nsrScore: Number(pickField(item, 'nsrScore', 'NsrScore') ?? 0),
        positivePercent: Number(pickField(item, 'positivePercent', 'PositivePercent') ?? 0),
        negativePercent: Number(pickField(item, 'negativePercent', 'NegativePercent') ?? 0),
        neutralPercent: Number(pickField(item, 'neutralPercent', 'NeutralPercent') ?? 0),
      })),
    };
  },

  getAspects: async (workspaceId: number, projectId: number): Promise<AspectAnalysis> => {
    const response = await axiosClient.get<Record<string, unknown>>(
      `/api/workspaces/${workspaceId}/projects/${projectId}/analytics/aspects`
    );
    const d = response.data;
    const rawList = (pickField<unknown[]>(d, 'aspects', 'Aspects') ?? []) as Record<string, unknown>[];

    return {
      totalAnalyzedMentions: pickNumber(d, 'totalAnalyzedMentions', 'TotalAnalyzedMentions'),
      totalAspectHits: pickNumber(d, 'totalAspectHits', 'TotalAspectHits'),
      topPositiveAspect: pickNullableString(d, 'topPositiveAspect', 'TopPositiveAspect'),
      topNegativeAspect: pickNullableString(d, 'topNegativeAspect', 'TopNegativeAspect'),
      aspects: rawList.map((item) => ({
        key: pickString(item, 'key', 'Key'),
        label: pickString(item, 'label', 'Label'),
        totalMentions: pickNumber(item, 'totalMentions', 'TotalMentions'),
        positive: pickNumber(item, 'positive', 'Positive'),
        negative: pickNumber(item, 'negative', 'Negative'),
        neutral: pickNumber(item, 'neutral', 'Neutral'),
        positivePercent: Number(pickField(item, 'positivePercent', 'PositivePercent') ?? 0),
        negativePercent: Number(pickField(item, 'negativePercent', 'NegativePercent') ?? 0),
        neutralPercent: Number(pickField(item, 'neutralPercent', 'NeutralPercent') ?? 0),
      })),
    };
  },

  getReports: async (workspaceId: number, projectId: number): Promise<ReportCenter> => {
    const response = await axiosClient.get<Record<string, unknown>>(
      `/api/workspaces/${workspaceId}/projects/${projectId}/reports`
    );
    const d = response.data;
    const templates = (pickField<unknown[]>(d, 'templates', 'Templates') ?? []) as Record<string, unknown>[];
    const reports = (pickField<unknown[]>(d, 'reports', 'Reports') ?? []) as Record<string, unknown>[];

    return {
      totalReports: pickNumber(d, 'totalReports', 'TotalReports'),
      lastGeneratedAt: pickNullableString(d, 'lastGeneratedAt', 'LastGeneratedAt'),
      templates: templates.map((t) => ({
        key: pickString(t, 'key', 'Key'),
        name: pickString(t, 'name', 'Name'),
        description: pickString(t, 'description', 'Description'),
        format: pickString(t, 'format', 'Format'),
        typeLabel: pickString(t, 'typeLabel', 'TypeLabel'),
      })),
      reports: reports.map((r) => ({
        reportId: pickString(r, 'reportId', 'ReportId'),
        name: pickString(r, 'name', 'Name'),
        type: pickString(r, 'type', 'Type'),
        typeLabel: pickString(r, 'typeLabel', 'TypeLabel'),
        createdAt: pickString(r, 'createdAt', 'CreatedAt'),
        createdBy: pickString(r, 'createdBy', 'CreatedBy'),
        status: pickString(r, 'status', 'Status'),
        fileSizeBytes: pickNumber(r, 'fileSizeBytes', 'FileSizeBytes'),
        rowCount: pickNumber(r, 'rowCount', 'RowCount'),
      })),
    };
  },

  generateReport: async (workspaceId: number, projectId: number, type: string): Promise<ReportFile> => {
    const response = await axiosClient.post<Record<string, unknown>>(
      `/api/workspaces/${workspaceId}/projects/${projectId}/reports/generate`,
      { type }
    );
    const r = response.data;
    return {
      reportId: pickString(r, 'reportId', 'ReportId'),
      name: pickString(r, 'name', 'Name'),
      type: pickString(r, 'type', 'Type'),
      typeLabel: pickString(r, 'typeLabel', 'TypeLabel'),
      createdAt: pickString(r, 'createdAt', 'CreatedAt'),
      createdBy: pickString(r, 'createdBy', 'CreatedBy'),
      status: pickString(r, 'status', 'Status'),
      fileSizeBytes: pickNumber(r, 'fileSizeBytes', 'FileSizeBytes'),
      rowCount: pickNumber(r, 'rowCount', 'RowCount'),
    };
  },

  downloadReport: async (workspaceId: number, projectId: number, reportId: string, fileName: string) => {
    const response = await axiosClient.get(
      `/api/workspaces/${workspaceId}/projects/${projectId}/reports/${reportId}/download`,
      { responseType: 'blob' }
    );
    const url = window.URL.createObjectURL(new Blob([response.data]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', fileName);
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  },

  getBespokeCenter: async (workspaceId: number, projectId: number): Promise<BespokeCenter> => {
    const response = await axiosClient.get<Record<string, unknown>>(
      `/api/workspaces/${workspaceId}/projects/${projectId}/bespoke`
    );
    const d = response.data;
    const requests = (pickField<unknown[]>(d, 'requests', 'Requests') ?? []) as Record<string, unknown>[];
    const reporters = (pickField<unknown[]>(d, 'reporters', 'Reporters') ?? []) as Record<string, unknown>[];

    return {
      userSystemRole: pickString(d, 'userSystemRole', 'UserSystemRole'),
      requests: requests.map(mapBespokeRequest),
      reporters: reporters.map((r) => ({
        userId: pickNumber(r, 'userId', 'UserId'),
        fullName: pickString(r, 'fullName', 'FullName'),
        email: pickString(r, 'email', 'Email'),
      })),
    };
  },

  createBespokeRequest: async (
    workspaceId: number,
    projectId: number,
    payload: CreateBespokePayload
  ): Promise<BespokeRequestItem> => {
    const response = await axiosClient.post<Record<string, unknown>>(
      `/api/workspaces/${workspaceId}/projects/${projectId}/bespoke`,
      payload
    );
    return mapBespokeRequest(response.data);
  },

  assignBespokeReporter: async (
    workspaceId: number,
    projectId: number,
    requestId: number,
    reporterId: number
  ): Promise<BespokeRequestItem> => {
    const response = await axiosClient.post<Record<string, unknown>>(
      `/api/workspaces/${workspaceId}/projects/${projectId}/bespoke/${requestId}/assign`,
      { reporterId }
    );
    return mapBespokeRequest(response.data);
  },

  startBespokeWork: async (workspaceId: number, projectId: number, requestId: number) => {
    const response = await axiosClient.post<Record<string, unknown>>(
      `/api/workspaces/${workspaceId}/projects/${projectId}/bespoke/${requestId}/start`
    );
    return mapBespokeRequest(response.data);
  },

  deliverBespokeReport: async (workspaceId: number, projectId: number, requestId: number) => {
    const response = await axiosClient.post<Record<string, unknown>>(
      `/api/workspaces/${workspaceId}/projects/${projectId}/bespoke/${requestId}/deliver`
    );
    return mapBespokeRequest(response.data);
  },

  downloadBespokeReport: async (workspaceId: number, projectId: number, requestId: number) => {
    const response = await axiosClient.get(
      `/api/workspaces/${workspaceId}/projects/${projectId}/bespoke/${requestId}/download`,
      { responseType: 'blob' }
    );
    const url = window.URL.createObjectURL(new Blob([response.data]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', `bespoke-report-${requestId}.html`);
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  },

  getMentionFilters: async (workspaceId: number, projectId: number) => {
    const response = await axiosClient.get<unknown[]>(
      `/api/workspaces/${workspaceId}/projects/${projectId}/mention-filters`
    );
    return (response.data ?? []).map((item) => {
      const d = item as Record<string, unknown>;
      const config = (pickField<Record<string, unknown>>(d, 'config', 'Config') ?? {}) as Record<string, unknown>;
      return {
        filterId: pickNumber(d, 'filterId', 'FilterId'),
        projectId: pickNumber(d, 'projectId', 'ProjectId'),
        name: pickString(d, 'name', 'Name'),
        config: {
          platform: pickNullableString(config, 'platform', 'Platform'),
          sentiment: pickNullableString(config, 'sentiment', 'Sentiment'),
          search: pickNullableString(config, 'search', 'Search'),
          dateFrom: pickNullableString(config, 'dateFrom', 'DateFrom'),
          dateTo: pickNullableString(config, 'dateTo', 'DateTo'),
        },
        createdAt: pickNullableString(d, 'createdAt', 'CreatedAt'),
      };
    });
  },

  saveMentionFilter: async (
    workspaceId: number,
    projectId: number,
    payload: { name: string; config: Record<string, string | null | undefined> }
  ) => {
    const response = await axiosClient.post(
      `/api/workspaces/${workspaceId}/projects/${projectId}/mention-filters`,
      payload
    );
    return response.data;
  },

  deleteMentionFilter: async (workspaceId: number, projectId: number, filterId: number) => {
    await axiosClient.delete(
      `/api/workspaces/${workspaceId}/projects/${projectId}/mention-filters/${filterId}`
    );
  },

  deleteMention: async (workspaceId: number, projectId: number, feedbackId: number) => {
    await axiosClient.delete(
      `/api/workspaces/${workspaceId}/projects/${projectId}/analytics/mentions/${feedbackId}`
    );
  },

  analyzeMention: async (
    workspaceId: number,
    projectId: number,
    feedbackId: number
  ): Promise<AnalyzeProjectResult> => {
    const response = await axiosClient.post<Record<string, unknown>>(
      `/api/workspaces/${workspaceId}/projects/${projectId}/analytics/mentions/${feedbackId}/analyze`,
      null,
      { timeout: 120_000 }
    );
    return normalizeAnalyzeResult(response.data);
  },
};

function mapBespokeRequest(r: Record<string, unknown>): BespokeRequestItem {
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
    modules: (pickField<string[]>(r, 'modules', 'Modules') ?? []).filter(Boolean),
    dateFrom: pickNullableString(r, 'dateFrom', 'DateFrom'),
    dateTo: pickNullableString(r, 'dateTo', 'DateTo'),
    format: pickString(r, 'format', 'Format') || 'html',
    hasDeliverable: pickField(r, 'hasDeliverable', 'HasDeliverable') === true,
    deliverableReportId: pickField<number>(r, 'deliverableReportId', 'DeliverableReportId') ?? null,
  };
}

export const scrapeApi = {
  startByKeyword: async (projectId: number, postedSinceDays?: number | null): Promise<ScrapeJobStart> => {
    const response = await axiosClient.post<Record<string, unknown>>(`/api/scrape/by-keyword`, null, {
      params: {
        projectId,
        ...(postedSinceDays != null && postedSinceDays > 0 ? { postedSinceDays } : {}),
      },
      timeout: 30_000,
    });
    const d = response.data;
    return {
      jobId: pickString(d, 'jobId', 'JobId'),
      projectId: pickNumber(d, 'projectId', 'ProjectId'),
    };
  },

  getJobStatus: async (jobId: string): Promise<ScrapeJobStatus> => {
    const response = await axiosClient.get<Record<string, unknown>>(`/api/scrape/jobs/${jobId}`);
    return normalizeScrapeJobStatus(response.data);
  },

  cancelJob: async (jobId: string): Promise<void> => {
    await axiosClient.post(`/api/scrape/jobs/${jobId}/cancel`, null, { timeout: 15_000 });
  },

  waitForJob: async (
    jobId: string,
    onProgress?: (status: ScrapeJobStatus) => void,
    pollMs = 1500
  ): Promise<ScrapeJobStatus> => {
    const deadline = Date.now() + 600_000;
    while (Date.now() < deadline) {
      const status = await scrapeApi.getJobStatus(jobId);
      onProgress?.(status);
      if (
        status.status === 'completed' ||
        status.status === 'failed' ||
        status.status === 'cancelled'
      ) {
        return status;
      }
      await new Promise((resolve) => setTimeout(resolve, pollMs));
    }
    throw new Error('Cào dữ liệu quá thời gian chờ (10 phút). Vui lòng thử lại.');
  },

  byKeyword: async (
    projectId: number,
    onProgress?: (status: ScrapeJobStatus) => void,
    postedSinceDays?: number | null
  ): Promise<ScrapeResult & { jobId?: string; cancelled?: boolean }> => {
    const { jobId } = await scrapeApi.startByKeyword(projectId, postedSinceDays);
    onProgress?.({
      jobId,
      projectId,
      status: 'running',
      phase: 'starting',
      phaseMessage: 'Đang khởi động bot cào dữ liệu...',
      platforms: [],
    });
    const finalJob = await scrapeApi.waitForJob(jobId, onProgress);
    const result = finalJob.result;
    if (!result) {
      return {
        errorMessage: finalJob.errorMessage ?? 'Không nhận được kết quả cào dữ liệu.',
        errors: [],
        jobId,
        cancelled: finalJob.status === 'cancelled',
      };
    }
    if (finalJob.errorMessage) {
      result.errorMessage = finalJob.errorMessage;
    }
    return { ...result, jobId, cancelled: finalJob.status === 'cancelled' };
  },
};

function normalizeScrapeJobStatus(d: Record<string, unknown>): ScrapeJobStatus {
  const platformsRaw = (pickField<unknown[]>(d, 'platforms', 'Platforms') ?? []) as Record<string, unknown>[];
  return {
    jobId: pickString(d, 'jobId', 'JobId'),
    projectId: pickNumber(d, 'projectId', 'ProjectId'),
    status: pickString(d, 'status', 'Status') || 'running',
    phase: pickNullableString(d, 'phase', 'Phase'),
    phaseMessage: pickNullableString(d, 'phaseMessage', 'PhaseMessage'),
    errorMessage: pickNullableString(d, 'errorMessage', 'ErrorMessage'),
    platforms: platformsRaw.map((p) => ({
      platform: pickString(p, 'platform', 'Platform'),
      label: pickString(p, 'label', 'Label'),
      status: pickString(p, 'status', 'Status') || 'pending',
      count: pickNumber(p, 'count', 'Count'),
      message: pickNullableString(p, 'message', 'Message'),
    })),
    result: pickField<ScrapeResult>(d, 'result', 'Result') ?? undefined,
  };
}
