import type { ProjectMention, SentimentSummary } from '../types/project';
import { SENTIMENT_COLORS, SENTIMENT_LABELS, getPlatformDisplayName } from './sentimentHelpers';

export interface PieSlice {
  key: string;
  name: string;
  value: number;
  color: string;
  percent: number;
}

export interface PlatformSentimentRow {
  platform: string;
  label: string;
  positive: number;
  negative: number;
  neutral: number;
  total: number;
}

export interface TrendPoint {
  date: string;
  label: string;
  positive: number;
  negative: number;
  neutral: number;
  total: number;
}

export function buildPieSlices(summary: SentimentSummary): PieSlice[] {
  const analyzed = summary.positive + summary.negative + summary.neutral;
  const base = analyzed || 1;

  const slices: PieSlice[] = [
    {
      key: 'positive',
      name: SENTIMENT_LABELS.positive,
      value: summary.positive,
      color: SENTIMENT_COLORS.positive,
      percent: Math.round((summary.positive / base) * 100),
    },
    {
      key: 'negative',
      name: SENTIMENT_LABELS.negative,
      value: summary.negative,
      color: SENTIMENT_COLORS.negative,
      percent: Math.round((summary.negative / base) * 100),
    },
    {
      key: 'neutral',
      name: SENTIMENT_LABELS.neutral,
      value: summary.neutral,
      color: SENTIMENT_COLORS.neutral,
      percent: Math.round((summary.neutral / base) * 100),
    },
  ];

  return slices.filter((s) => s.value > 0);
}

export function buildPlatformRows(mentions: ProjectMention[]): PlatformSentimentRow[] {
  const map = new Map<string, PlatformSentimentRow>();

  for (const m of mentions) {
    const platform = m.platform.toLowerCase();
    const row = map.get(platform) ?? {
      platform,
      label: getPlatformDisplayName(platform),
      positive: 0,
      negative: 0,
      neutral: 0,
      total: 0,
    };

    row.total++;
    const s = m.sentiment?.toLowerCase();
    if (s === 'positive') row.positive++;
    else if (s === 'negative') row.negative++;
    else if (s === 'neutral') row.neutral++;

    map.set(platform, row);
  }

  return Array.from(map.values()).sort((a, b) => b.total - a.total);
}

export function buildTrendPoints(mentions: ProjectMention[]): TrendPoint[] {
  const map = new Map<string, TrendPoint>();

  for (const m of mentions) {
    const sourceDate = m.postedAt || m.scrapedAt;
    if (!sourceDate) continue;
    const d = new Date(sourceDate);
    if (Number.isNaN(d.getTime())) continue;

    const date = d.toISOString().slice(0, 10);
    const point = map.get(date) ?? {
      date,
      label: d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' }),
      positive: 0,
      negative: 0,
      neutral: 0,
      total: 0,
    };

    point.total++;
    const s = m.sentiment?.toLowerCase();
    if (s === 'positive') point.positive++;
    else if (s === 'negative') point.negative++;
    else if (s === 'neutral') point.neutral++;

    map.set(date, point);
  }

  return Array.from(map.values()).sort((a, b) => a.date.localeCompare(b.date));
}

export function getNsrTone(nsr: number): 'good' | 'bad' | 'neutral' {
  if (nsr >= 20) return 'good';
  if (nsr <= -20) return 'bad';
  return 'neutral';
}
