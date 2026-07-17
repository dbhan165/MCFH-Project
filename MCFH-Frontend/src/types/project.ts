export interface Project {
  projectId: number;
  workspaceId: number;
  name: string;
  description: string | null;
  searchQuery: string | null;
  dataSourceCount: number;
  enableFacebook: boolean;
  enableYoutube: boolean;
  enableTiktok: boolean;
  enableMaps: boolean;
  enableNews?: boolean;
  createdAt: string | null;
}

export interface CreateProjectPayload {
  name: string;
  description?: string;
  searchQuery: string;
  enableFacebook?: boolean;
  enableYoutube?: boolean;
  enableTiktok?: boolean;
  enableMaps?: boolean;
  dataSources?: Array<{ platform: string; targetUrl?: string | null }>;
}

export interface AiAnalysisProgress {
  isAnalyzing: boolean;
  progressPercent: number;
}

export interface AnalyzeProjectResult {
  projectId: number;
  analyzedCount: number;
  skippedCount: number;
  totalFeedbacks: number;
  message: string;
}

export interface ProjectOverviewStats {
  projectId: number;
  projectName: string;
  totalMentions: number;
  totalComments: number;
  analyzedCount: number;
  pendingAnalysisCount: number;
  nsrScore: number;
  positiveCount: number;
  negativeCount: number;
  neutralCount: number;
  platformBreakdown: Record<string, number>;
}

export interface MentionTag {
  tagId: number;
  name: string;
  color: string | null;
}

export interface ProjectMention {
  feedbackId: number;
  authorName: string | null;
  platform: string;
  content: string;
  sentiment: string | null;
  confidenceScore: number | null;
  originalUrl: string | null;
  commentsCount: number;
  scrapedAt: string | null;
  postedAt: string | null;
  aiSummary: string | null;
  comments: string[];
  isAnalyzed: boolean;
  analyzedAt: string | null;
  tags: MentionTag[];
  isSentimentOverridden: boolean;
  isCrisisAlert: boolean;
}

export interface SentimentSummary {
  total: number;
  positive: number;
  negative: number;
  neutral: number;
  unanalyzed: number;
  positivePercent: number;
  negativePercent: number;
  neutralPercent: number;
  nsrScore: number;
}

export interface ProjectInfluencer {
  id: string;
  name: string;
  platform: string;
  handleUrl: string | null;
  mentions: number;
  totalComments: number;
  shareOfVoice: number;
  influenceScore: number;
  dominantSentiment: string | null;
  positiveCount: number;
  negativeCount: number;
  neutralCount: number;
  followers: number | null;
}

export interface InfluencerAnalytics {
  totalMentions: number;
  uniqueInfluencers: number;
  influencers: ProjectInfluencer[];
}

export interface ChannelStats {
  platform: string;
  label: string;
  mentions: number;
  mentionShare: number;
  totalComments: number;
  commentShare: number;
  positive: number;
  negative: number;
  neutral: number;
  unanalyzed: number;
  nsrScore: number;
  positivePercent: number;
  negativePercent: number;
  neutralPercent: number;
}

export interface ChannelComparison {
  totalMentions: number;
  totalComments: number;
  channels: ChannelStats[];
}

export interface AspectStats {
  key: string;
  label: string;
  totalMentions: number;
  positive: number;
  negative: number;
  neutral: number;
  positivePercent: number;
  negativePercent: number;
  neutralPercent: number;
}

export interface AspectAnalysis {
  totalAnalyzedMentions: number;
  totalAspectHits: number;
  topPositiveAspect: string | null;
  topNegativeAspect: string | null;
  aspects: AspectStats[];
}

export interface ReportTemplate {
  key: string;
  name: string;
  description: string;
  format: string;
  typeLabel: string;
}

export interface ReportFile {
  reportId: string;
  name: string;
  type: string;
  typeLabel: string;
  createdAt: string;
  createdBy: string;
  status: string;
  fileSizeBytes: number;
  rowCount: number;
}

export interface ReportCenter {
  totalReports: number;
  lastGeneratedAt: string | null;
  templates: ReportTemplate[];
  reports: ReportFile[];
}

export interface BespokeRequestItem {
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
  modules: string[];
  dateFrom: string | null;
  dateTo: string | null;
  format: string;
  agreedPrice: number | null;
  hasDeliverable: boolean;
  deliverableReportId: number | null;
}

export interface ReporterOption {
  userId: number;
  fullName: string;
  email: string;
}

export interface BespokeCenter {
  userSystemRole: string;
  requests: BespokeRequestItem[];
  reporters: ReporterOption[];
}

export interface CreateBespokePayload {
  title: string;
  keyword: string;
  packageType: string;
  requirements?: string;
  dateFrom?: string;
  dateTo?: string;
  modules: string[];
  format: string;
}

export interface ScrapeResult {
  keyword?: string;
  message?: string;
  errorMessage?: string;
  errors?: string[];
  facebook?: unknown[];
  youTube?: unknown[];
  tikTok?: unknown[];
  analysis?: AnalyzeProjectResult;
}

export interface ScrapePlatformProgress {
  platform: string;
  label: string;
  status: 'pending' | 'running' | 'done' | 'error' | 'skipped' | string;
  count: number;
  message?: string | null;
}

export interface ScrapeJobStatus {
  jobId: string;
  projectId: number;
  status: 'running' | 'completed' | 'failed' | string;
  phase?: string | null;
  phaseMessage?: string | null;
  platforms: ScrapePlatformProgress[];
  result?: ScrapeResult | null;
  errorMessage?: string | null;
}

export interface ScrapeJobStart {
  jobId: string;
  projectId: number;
}
