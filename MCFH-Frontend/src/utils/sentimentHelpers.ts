export function getSentimentLabel(sentiment: string | null | undefined) {
  switch (sentiment?.toLowerCase()) {
    case 'positive':
      return 'Tích cực';
    case 'negative':
      return 'Tiêu cực';
    case 'neutral':
      return 'Trung lập';
    default:
      return 'Chưa phân tích';
  }
}

export function getSentimentBadgeClass(sentiment: string | null | undefined) {
  switch (sentiment?.toLowerCase()) {
    case 'positive':
      return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
    case 'negative':
      return 'bg-[#FF7575]/10 text-[#FF7575] border-[#FF7575]/20';
    case 'neutral':
      return 'bg-yellow-500/10 text-yellow-500 border-yellow-500/20';
    default:
      return 'bg-white/10 text-gray-400 border-white/10';
  }
}

export function getPlatformLabel(platform: string) {
  switch ((platform || '').trim().toLowerCase()) {
    case 'facebook':
      return 'FB';
    case 'youtube':
      return 'YT';
    case 'tiktok':
      return 'TT';
    case 'news':
      return 'Tin';
    case 'threads':
      return 'TH';
    default:
      return platform.toUpperCase().slice(0, 2);
  }
}

export function getPlatformDisplayName(platform: string) {
  switch ((platform || '').trim().toLowerCase()) {
    case 'facebook':
      return 'Facebook';
    case 'youtube':
      return 'YouTube';
    case 'tiktok':
      return 'TikTok';
    case 'news':
      return 'Tin tức';
    case 'threads':
      return 'Threads';
    default:
      return platform.charAt(0).toUpperCase() + platform.slice(1);
  }
}

export const MENTION_PLATFORMS = ['all', 'youtube', 'tiktok', 'facebook', 'threads', 'news'] as const;
export type MentionPlatformFilter = (typeof MENTION_PLATFORMS)[number];

export const PLATFORM_SORT_ORDER = ['facebook', 'youtube', 'tiktok', 'threads', 'news'] as const;

export function sortByPlatformOrder<T extends { platform: string }>(
  items: T[],
  getVolume?: (item: T) => number
): T[] {
  return [...items].sort((a, b) => {
    const aIdx = PLATFORM_SORT_ORDER.indexOf(a.platform.toLowerCase() as (typeof PLATFORM_SORT_ORDER)[number]);
    const bIdx = PLATFORM_SORT_ORDER.indexOf(b.platform.toLowerCase() as (typeof PLATFORM_SORT_ORDER)[number]);
    const aOrder = aIdx >= 0 ? aIdx : 99;
    const bOrder = bIdx >= 0 ? bIdx : 99;
    if (aOrder !== bOrder) return aOrder - bOrder;
    if (getVolume) return getVolume(b) - getVolume(a);
    return 0;
  });
}

export const MENTION_SENTIMENTS = ['all', 'positive', 'negative', 'neutral', 'pending'] as const;
export type MentionSentimentFilter = (typeof MENTION_SENTIMENTS)[number];

export function getSentimentFilterLabel(filter: MentionSentimentFilter) {
  switch (filter) {
    case 'all':
      return 'Tất cả';
    case 'positive':
      return 'Tích cực';
    case 'negative':
      return 'Tiêu cực';
    case 'neutral':
      return 'Trung lập';
    case 'pending':
      return 'Chưa phân tích';
  }
}

export function getSentimentFilterBadgeClass(filter: MentionSentimentFilter) {
  switch (filter) {
    case 'positive':
      return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
    case 'negative':
      return 'bg-[#FF7575]/10 text-[#FF7575] border-[#FF7575]/20';
    case 'neutral':
      return 'bg-yellow-500/10 text-yellow-500 border-yellow-500/20';
    case 'pending':
      return 'bg-white/10 text-gray-400 border-white/10';
    default:
      return 'bg-[#FF7575]/10 text-[#FF7575] border-[#FF7575]/20';
  }
}

export function getPlatformBadgeClass(platform: string) {
  switch ((platform || '').trim().toLowerCase()) {
    case 'facebook':
      return 'bg-blue-500/10 text-blue-400 border-blue-500/20';
    case 'youtube':
      return 'bg-red-500/10 text-red-400 border-red-500/20';
    case 'tiktok':
      return 'bg-cyan-500/10 text-cyan-400 border-cyan-500/20';
    case 'news':
      return 'bg-amber-500/10 text-amber-400 border-amber-500/20';
    case 'threads':
      return 'bg-zinc-800 text-white border-zinc-700';
    default:
      return 'bg-white/10 text-gray-400 border-white/10';
  }
}

export function formatNumber(value: number) {
  return new Intl.NumberFormat('vi-VN').format(value);
}

export function buildConicGradient(positive: number, negative: number, neutral: number) {
  const p = Math.max(0, positive);
  const n = Math.max(0, negative);
  const u = Math.max(0, neutral);
  const total = p + n + u || 1;
  const pEnd = (p / total) * 100;
  const nEnd = pEnd + (n / total) * 100;
  return `conic-gradient(#00B4D8 0% ${pEnd}%, #FF7575 ${pEnd}% ${nEnd}%, #EAB308 ${nEnd}% 100%)`;
}

export const SENTIMENT_COLORS = {
  positive: '#00B4D8',
  negative: '#FF7575',
  neutral: '#EAB308',
  pending: '#64748B',
} as const;

export const SENTIMENT_LABELS = {
  positive: 'Tích cực',
  negative: 'Tiêu cực',
  neutral: 'Trung lập',
  pending: 'Chưa phân tích',
} as const;

export const PLATFORM_CHART_COLORS: Record<string, string> = {
  facebook: '#3B82F6',
  youtube: '#EF4444',
  tiktok: '#EC4899',
  news: '#F59E0B',
  threads: '#FAFAFA',
  unknown: '#64748B',
};

export function getPlatformChartColor(platform: string): string {
  return PLATFORM_CHART_COLORS[(platform || '').trim().toLowerCase()] ?? PLATFORM_CHART_COLORS.unknown;
}
