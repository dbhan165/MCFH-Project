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
  switch (platform.toLowerCase()) {
    case 'facebook':
      return 'FB';
    case 'youtube':
      return 'YT';
    case 'tiktok':
      return 'TT';
    case 'news':
      return 'Tin';
    default:
      return platform.toUpperCase().slice(0, 2);
  }
}

export function getPlatformDisplayName(platform: string) {
  switch (platform.toLowerCase()) {
    case 'facebook':
      return 'Facebook';
    case 'youtube':
      return 'YouTube';
    case 'tiktok':
      return 'TikTok';
    case 'news':
      return 'Tin tức';
    default:
      return platform.charAt(0).toUpperCase() + platform.slice(1);
  }
}

export const MENTION_PLATFORMS = ['all', 'youtube', 'tiktok', 'facebook', 'news'] as const;
export type MentionPlatformFilter = (typeof MENTION_PLATFORMS)[number];

export const PLATFORM_SORT_ORDER = ['facebook', 'youtube', 'tiktok', 'news'] as const;

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
  switch (platform.toLowerCase()) {
    case 'facebook':
      return 'bg-blue-500/20 text-blue-400';
    case 'youtube':
      return 'bg-red-500/20 text-red-400';
    case 'tiktok':
      return 'bg-pink-500/20 text-pink-400';
    case 'news':
      return 'bg-amber-500/20 text-amber-400';
    default:
      return 'bg-gray-500/20 text-gray-400';
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
  unknown: '#64748B',
};

export function getPlatformChartColor(platform: string): string {
  return PLATFORM_CHART_COLORS[platform.toLowerCase()] ?? PLATFORM_CHART_COLORS.unknown;
}
