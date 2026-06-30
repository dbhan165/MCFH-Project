import axiosClient from './axiosClient';
import { pickField, pickNumber, pickNullableString, pickString } from '../utils/normalizeApi';
import type { ScrapeJobStatus } from '../types/project';

export type ScrapeQuote = {
  postedSinceDays: number;
  timeRangeLabel: string;
  price: number;
  priceLabel: string;
  estimatedMinutes: number;
  estimatedDeliveryLabel: string;
};

export type ScrapeOrder = {
  orderId: number;
  workspaceId: number;
  projectId: number;
  projectName: string;
  keyword: string;
  postedSinceDays: number;
  timeRangeLabel: string;
  quotedPrice: number;
  priceLabel: string;
  status: string;
  statusLabel: string;
  progressPercent: number;
  statusMessage?: string | null;
  scrapeJobId?: string | null;
  estimatedReportAt?: string | null;
  reportReadyAt?: string | null;
  createdAt: string;
  paidAt?: string | null;
  completedAt?: string | null;
  scrapeJob?: ScrapeJobStatus | null;
};

function normalizeQuote(data: Record<string, unknown>): ScrapeQuote {
  return {
    postedSinceDays: pickNumber(data, 'postedSinceDays', 'PostedSinceDays'),
    timeRangeLabel: pickString(data, 'timeRangeLabel', 'TimeRangeLabel'),
    price: pickNumber(data, 'price', 'Price'),
    priceLabel: pickString(data, 'priceLabel', 'PriceLabel'),
    estimatedMinutes: pickNumber(data, 'estimatedMinutes', 'EstimatedMinutes'),
    estimatedDeliveryLabel: pickString(data, 'estimatedDeliveryLabel', 'EstimatedDeliveryLabel'),
  };
}

function normalizeJob(data: Record<string, unknown> | undefined): ScrapeJobStatus | null {
  if (!data) return null;
  const platforms = (pickField(data, 'platforms', 'Platforms') as unknown[]) ?? [];
  return {
    jobId: pickString(data, 'jobId', 'JobId'),
    projectId: pickNumber(data, 'projectId', 'ProjectId'),
    status: pickString(data, 'status', 'Status'),
    phase: pickNullableString(data, 'phase', 'Phase') ?? undefined,
    phaseMessage: pickNullableString(data, 'phaseMessage', 'PhaseMessage') ?? undefined,
    platforms: platforms.map((p) => {
      const row = p as Record<string, unknown>;
      return {
        platform: pickString(row, 'platform', 'Platform'),
        label: pickString(row, 'label', 'Label'),
        status: pickString(row, 'status', 'Status'),
        count: pickNumber(row, 'count', 'Count'),
        message: pickNullableString(row, 'message', 'Message') ?? undefined,
      };
    }),
    errorMessage: pickNullableString(data, 'errorMessage', 'ErrorMessage') ?? undefined,
  };
}

function normalizeOrder(data: Record<string, unknown>): ScrapeOrder {
  const jobRaw = pickField(data, 'scrapeJob', 'ScrapeJob') as Record<string, unknown> | undefined;
  return {
    orderId: pickNumber(data, 'orderId', 'OrderId'),
    workspaceId: pickNumber(data, 'workspaceId', 'WorkspaceId'),
    projectId: pickNumber(data, 'projectId', 'ProjectId'),
    projectName: pickString(data, 'projectName', 'ProjectName'),
    keyword: pickString(data, 'keyword', 'Keyword'),
    postedSinceDays: pickNumber(data, 'postedSinceDays', 'PostedSinceDays'),
    timeRangeLabel: pickString(data, 'timeRangeLabel', 'TimeRangeLabel'),
    quotedPrice: pickNumber(data, 'quotedPrice', 'QuotedPrice'),
    priceLabel: pickString(data, 'priceLabel', 'PriceLabel'),
    status: pickString(data, 'status', 'Status'),
    statusLabel: pickString(data, 'statusLabel', 'StatusLabel'),
    progressPercent: pickNumber(data, 'progressPercent', 'ProgressPercent'),
    statusMessage: pickNullableString(data, 'statusMessage', 'StatusMessage'),
    scrapeJobId: pickNullableString(data, 'scrapeJobId', 'ScrapeJobId'),
    estimatedReportAt: pickNullableString(data, 'estimatedReportAt', 'EstimatedReportAt'),
    reportReadyAt: pickNullableString(data, 'reportReadyAt', 'ReportReadyAt'),
    createdAt: pickString(data, 'createdAt', 'CreatedAt'),
    paidAt: pickNullableString(data, 'paidAt', 'PaidAt'),
    completedAt: pickNullableString(data, 'completedAt', 'CompletedAt'),
    scrapeJob: normalizeJob(jobRaw),
  };
}

export const scrapeOrderApi = {
  async getQuote(postedSinceDays: number): Promise<ScrapeQuote> {
    const { data } = await axiosClient.get('/api/scrape-orders/quote', {
      params: { postedSinceDays },
    });
    return normalizeQuote(data as Record<string, unknown>);
  },

  async create(payload: {
    workspaceId: number;
    projectId: number;
    keyword: string;
    postedSinceDays: number;
  }): Promise<ScrapeOrder> {
    const { data } = await axiosClient.post('/api/scrape-orders', payload);
    return normalizeOrder(data as Record<string, unknown>);
  },

  async pay(orderId: number): Promise<ScrapeOrder> {
    const { data } = await axiosClient.post(`/api/scrape-orders/${orderId}/pay`);
    return normalizeOrder(data as Record<string, unknown>);
  },

  async get(orderId: number): Promise<ScrapeOrder> {
    const { data } = await axiosClient.get(`/api/scrape-orders/${orderId}`);
    return normalizeOrder(data as Record<string, unknown>);
  },

  async list(workspaceId?: number, projectId?: number): Promise<ScrapeOrder[]> {
    const { data } = await axiosClient.get('/api/scrape-orders', {
      params: { workspaceId, projectId },
    });
    const rows = Array.isArray(data) ? data : [];
    return rows.map((row) => normalizeOrder(row as Record<string, unknown>));
  },

  pollOrder(
    orderId: number,
    onUpdate: (order: ScrapeOrder) => void,
    intervalMs = 5000
  ): () => void {
    let active = true;
    const tick = async () => {
      if (!active || document.visibilityState === 'hidden') {
        if (active) setTimeout(tick, intervalMs);
        return;
      }
      try {
        const order = await scrapeOrderApi.get(orderId);
        onUpdate(order);
        if (order.status === 'completed' || order.status === 'failed') return;
      } catch {
        /* ignore transient errors */
      }
      if (active) setTimeout(tick, intervalMs);
    };
    tick();
    return () => {
      active = false;
    };
  },
};
