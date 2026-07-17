import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useLocation, useParams } from 'react-router-dom';
import {
  LayoutDashboard,
  Activity,
  BarChart2,
  GitCompare,
  FileText,
  Search,
  Bell,
  Calendar,
  ChevronDown,
  Zap,
  Loader2,
  AlertCircle,
  ArrowLeft,
  MessageCircle,
  Sparkles,
  Trophy,
  TrendingUp,
  TrendingDown,
  Minus,
  Info,
} from 'lucide-react';
import {
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  LabelList,
} from 'recharts';
import McfhLogo from '../components/brand/McfhLogo';
import { projectApi } from '../api/projectApi';
import type { Project, ProjectMention, ProjectOverviewStats, SentimentSummary } from '../types/project';
import { extractApiError } from '../utils/authStorage';
import { buildTrendPoints } from '../utils/sentimentChartData';
import {
  formatNumber,
  getPlatformDisplayName,
  PLATFORM_SORT_ORDER,
  SENTIMENT_COLORS,
  SENTIMENT_LABELS,
} from '../utils/sentimentHelpers';

const ENTITY_A_COLOR = '#E2E8F0';
const ENTITY_B_COLOR = '#4FD1C5';

const PLATFORM_COLORS: Record<string, string> = {
  facebook: '#1877F2',
  youtube: '#FF0000',
  tiktok: '#FE2C55',
  news: '#F59E0B',
  maps: '#34A853',
};

function shortName(name: string, max = 22) {
  return name.length > max ? `${name.slice(0, max)}…` : name;
}

function getPlatformAccent(platform: string) {
  return PLATFORM_COLORS[platform.toLowerCase()] ?? '#94A3B8';
}

type TrendStats = {
  totalA: number;
  totalB: number;
  avgA: number;
  avgB: number;
  peakA: number;
  peakB: number;
  changeA: number | null;
  changeB: number | null;
};

function computeTrendStats(data: CompareTrendPoint[]): TrendStats {
  if (data.length === 0) {
    return { totalA: 0, totalB: 0, avgA: 0, avgB: 0, peakA: 0, peakB: 0, changeA: null, changeB: null };
  }

  const totalA = data.reduce((sum, point) => sum + point.entityA, 0);
  const totalB = data.reduce((sum, point) => sum + point.entityB, 0);
  const first = data[0];
  const last = data[data.length - 1];

  const pctChange = (from: number, to: number) => {
    if (from === 0 && to === 0) return 0;
    if (from === 0) return 100;
    return Math.round(((to - from) / from) * 100);
  };

  return {
    totalA,
    totalB,
    avgA: Math.round((totalA / data.length) * 10) / 10,
    avgB: Math.round((totalB / data.length) * 10) / 10,
    peakA: Math.max(...data.map((point) => point.entityA)),
    peakB: Math.max(...data.map((point) => point.entityB)),
    changeA: data.length > 1 ? pctChange(first.entityA, last.entityA) : null,
    changeB: data.length > 1 ? pctChange(first.entityB, last.entityB) : null,
  };
}

function TrendChangeBadge({ value }: { value: number | null }) {
  if (value === null) return null;

  const Icon = value > 0 ? TrendingUp : value < 0 ? TrendingDown : Minus;
  const tone =
    value > 0
      ? 'text-emerald-400 bg-emerald-500/10 border-emerald-500/20'
      : value < 0
        ? 'text-[#FF7575] bg-[#FF7575]/10 border-[#FF7575]/20'
        : 'text-gray-400 bg-white/5 border-white/10';

  return (
    <span className={`inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-[11px] font-semibold border ${tone}`}>
      <Icon size={12} />
      {value > 0 ? '+' : ''}
      {value}%
    </span>
  );
}

function ChartTooltipCard({ children }: { children: React.ReactNode }) {
  return (
    <div className="rounded-xl border border-white/10 bg-[#0A101D]/95 backdrop-blur-sm px-3 py-2.5 shadow-xl shadow-black/40">
      {children}
    </div>
  );
}

function CompareTrendTooltip({
  active,
  payload,
  label,
  entityAName,
  entityBName,
}: {
  active?: boolean;
  payload?: readonly { value?: unknown; dataKey?: string | number }[];
  label?: string;
  entityAName: string;
  entityBName: string;
}) {
  if (!active || !payload?.length) return null;

  const readValue = (key: string) =>
    Number(payload.find((item) => String(item.dataKey) === key)?.value ?? 0);
  const aVal = readValue('entityA');
  const bVal = readValue('entityB');
  const total = aVal + bVal || 1;
  const leader =
    aVal > bVal ? shortName(entityAName) : bVal > aVal ? shortName(entityBName) : 'Hòa';

  return (
    <ChartTooltipCard>
      <p className="text-[11px] font-bold text-white mb-2">{label}</p>
      <div className="space-y-1.5 text-xs">
        <div className="flex items-center justify-between gap-4">
          <span className="inline-flex items-center gap-1.5 text-gray-300">
            <span className="w-2 h-2 rounded-full" style={{ background: ENTITY_A_COLOR }} />
            {shortName(entityAName, 16)}
          </span>
          <span className="font-bold text-white tabular-nums">
            {formatNumber(aVal)} <span className="text-gray-500 font-normal">({Math.round((aVal / total) * 100)}%)</span>
          </span>
        </div>
        <div className="flex items-center justify-between gap-4">
          <span className="inline-flex items-center gap-1.5 text-gray-300">
            <span className="w-2 h-2 rounded-full" style={{ background: ENTITY_B_COLOR }} />
            {shortName(entityBName, 16)}
          </span>
          <span className="font-bold text-white tabular-nums">
            {formatNumber(bVal)} <span className="text-gray-500 font-normal">({Math.round((bVal / total) * 100)}%)</span>
          </span>
        </div>
      </div>
      <p className="text-[10px] text-gray-500 mt-2 pt-2 border-t border-white/10">
        Tổng kỳ: <span className="text-white font-semibold">{formatNumber(aVal + bVal)}</span> mentions · Dẫn:{' '}
        <span className="text-[#4FD1C5] font-semibold">{leader}</span>
      </p>
    </ChartTooltipCard>
  );
}

type CompareEntitySnapshot = {
  projectId: number;
  name: string;
  keyword: string;
  nsrScore: number;
  totalVolume: number;
  totalComments: number;
  analyzedCount: number;
  positivePercent: number;
  negativePercent: number;
  neutralPercent: number;
  platformVolumes: Record<string, number>;
};

type CompareViewModel = {
  entityA: CompareEntitySnapshot;
  entityB: CompareEntitySnapshot;
  volumeDifferential: number;
  nsrDifferential: number;
  sovA: number;
  sovB: number;
  sovIndex: number;
};

type CompareTrendPoint = {
  label: string;
  entityA: number;
  entityB: number;
};

const DATE_RANGES = [
  { label: '7 ngày qua', value: 7 },
  { label: '30 ngày qua', value: 30 },
  { label: '90 ngày qua', value: 90 },
] as const;

type CompareNavState = {
  compareWith?: number;
};

function formatNsr(value: number) {
  return `${value > 0 ? '+' : ''}${value}%`;
}

function getDateFrom(days: number) {
  const d = new Date();
  d.setDate(d.getDate() - days);
  return d.toISOString().slice(0, 10);
}

function buildSnapshot(
  project: Project,
  overview: ProjectOverviewStats,
  sentiment: SentimentSummary
): CompareEntitySnapshot {
  const platformVolumes: Record<string, number> = {};
  for (const [platform, count] of Object.entries(overview.platformBreakdown)) {
    platformVolumes[platform.toLowerCase()] = count;
  }

  return {
    projectId: project.projectId,
    name: project.name,
    keyword: project.searchQuery ?? '—',
    nsrScore: overview.nsrScore,
    totalVolume: overview.totalMentions,
    totalComments: overview.totalComments,
    analyzedCount: overview.analyzedCount,
    positivePercent: sentiment.positivePercent,
    negativePercent: sentiment.negativePercent,
    neutralPercent: sentiment.neutralPercent,
    platformVolumes,
  };
}

function buildViewModel(a: CompareEntitySnapshot, b: CompareEntitySnapshot): CompareViewModel {
  const totalSov = a.totalVolume + b.totalVolume || 1;
  const sovA = Math.round((a.totalVolume / totalSov) * 100);
  const sovB = 100 - sovA;
  const sovIndex = b.totalVolume > 0 ? Number((a.totalVolume / b.totalVolume).toFixed(1)) : 0;
  const volumeDifferential =
    b.totalVolume > 0 ? Math.round(((a.totalVolume - b.totalVolume) / b.totalVolume) * 100) : 0;
  const nsrDifferential = a.nsrScore - b.nsrScore;

  return { entityA: a, entityB: b, volumeDifferential, nsrDifferential, sovA, sovB, sovIndex };
}

function sortPlatforms(platforms: Iterable<string>) {
  const order = PLATFORM_SORT_ORDER as readonly string[];
  return [...platforms].sort((a, b) => {
    const ia = order.indexOf(a);
    const ib = order.indexOf(b);
    if (ia === -1 && ib === -1) return a.localeCompare(b);
    if (ia === -1) return 1;
    if (ib === -1) return -1;
    return ia - ib;
  });
}

function buildComparisonTrend(
  leftMentions: ProjectMention[],
  rightMentions: ProjectMention[],
  mode: 'daily' | 'weekly'
): CompareTrendPoint[] {
  const leftDaily = buildTrendPoints(leftMentions);
  const rightDaily = buildTrendPoints(rightMentions);

  if (mode === 'daily') {
    const dates = new Set([...leftDaily.map((p) => p.date), ...rightDaily.map((p) => p.date)]);
    const leftMap = new Map(leftDaily.map((p) => [p.date, p.total]));
    const rightMap = new Map(rightDaily.map((p) => [p.date, p.total]));

    return Array.from(dates)
      .sort()
      .map((date) => {
        const d = new Date(date);
        return {
          label: d.toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' }),
          entityA: leftMap.get(date) ?? 0,
          entityB: rightMap.get(date) ?? 0,
        };
      });
  }

  const bucket = (mentions: ProjectMention[]) => {
    const map = new Map<string, number>();
    for (const m of mentions) {
      const sourceDate = m.postedAt || m.scrapedAt;
      if (!sourceDate) continue;
      const d = new Date(sourceDate);
      if (Number.isNaN(d.getTime())) continue;
      const weekStart = new Date(d);
      weekStart.setDate(d.getDate() - d.getDay());
      const key = weekStart.toISOString().slice(0, 10);
      map.set(key, (map.get(key) ?? 0) + 1);
    }
    return map;
  };

  const leftWeek = bucket(leftMentions);
  const rightWeek = bucket(rightMentions);
  const keys = new Set([...leftWeek.keys(), ...rightWeek.keys()]);

  return Array.from(keys)
    .sort()
    .map((key) => ({
      label: `Tuần ${new Date(key).toLocaleDateString('vi-VN', { day: '2-digit', month: '2-digit' })}`,
      entityA: leftWeek.get(key) ?? 0,
      entityB: rightWeek.get(key) ?? 0,
    }));
}

function ComparisonSidebar({ workspaceId, projectId }: { workspaceId: number; projectId: number }) {
  const base = `/workspace/${workspaceId}/project/${projectId}`;
  const navItems = [
    { path: base, icon: LayoutDashboard, label: 'Overview', exact: true },
    { path: `${base}/mentions`, icon: Activity, label: 'Data Stream' },
    { path: `${base}/aspect`, icon: BarChart2, label: 'Aspect Analysis' },
    { path: `${base}/comparison`, icon: GitCompare, label: 'Comparison', active: true },
    { path: `${base}/reports`, icon: FileText, label: 'B2B Reports' },
  ];

  return (
    <aside className="hidden lg:flex w-64 xl:w-72 bg-[#0A101D] border-r border-white/5 flex-col shrink-0 h-full">
      <div className="h-20 flex items-center px-6 border-b border-white/5 shrink-0">
        <McfhLogo
          linkTo="/workspaces"
          size={34}
          subtitle="Kinetic Enterprise"
          textClassName="text-white text-lg"
          subtitleClassName="text-[10px] text-gray-500 font-semibold tracking-[0.22em] uppercase"
        />
      </div>
      <nav className="flex-1 px-4 py-6 space-y-1 overflow-y-auto">
        {navItems.map((item) => {
          const Icon = item.icon;
          return (
            <Link
              key={item.label}
              to={item.path}
              className={`flex items-center gap-3 px-4 py-3 rounded-xl text-sm font-medium transition-all border ${
                item.active
                  ? 'bg-white/[0.06] text-white border-white/10'
                  : 'text-gray-400 border-transparent hover:text-white hover:bg-white/[0.03]'
              }`}
            >
              <Icon size={18} className={item.active ? 'text-[#FF7575]' : undefined} />
              {item.label}
            </Link>
          );
        })}
      </nav>
      <div className="p-4 border-t border-white/5 space-y-3 shrink-0">
        <Link
          to={`/create-project?workspaceId=${workspaceId}`}
          className="flex items-center justify-center gap-2 w-full py-3 rounded-xl bg-[#FF7575] hover:bg-[#ff6262] text-white text-sm font-bold transition-colors"
        >
          New Analysis
        </Link>
      </div>
    </aside>
  );
}

function ComparisonHeader() {
  return (
    <header className="h-16 lg:h-20 bg-[#0A101D]/80 backdrop-blur-md border-b border-white/5 px-4 lg:px-8 flex items-center gap-4 shrink-0 z-20">
      <div className="flex-1 max-w-xl mx-auto hidden md:block">
        <div className="relative">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-500" />
          <input
            type="search"
            placeholder="Global search..."
            className="w-full bg-white/[0.04] border border-white/10 rounded-xl pl-10 pr-4 py-2.5 text-sm text-white placeholder:text-gray-500 focus:outline-none focus:border-white/20"
          />
        </div>
      </div>
      <div className="flex items-center gap-2 lg:gap-4 ml-auto">
        <button type="button" className="p-2 text-gray-400 hover:text-white relative">
          <Bell size={20} />
          <span className="absolute top-1.5 right-1.5 w-2 h-2 bg-[#FF7575] rounded-full" />
        </button>
        <div className="w-9 h-9 rounded-full bg-gradient-to-br from-[#FF7575] to-[#00B4D8] ring-2 ring-white/10" />
      </div>
    </header>
  );
}

function EntitySelect({
  label,
  value,
  options,
  onChange,
  accentColor,
}: {
  label: string;
  value: number | null;
  options: Project[];
  onChange: (projectId: number) => void;
  accentColor: string;
}) {
  return (
    <div className="relative min-w-[160px] flex-1 max-w-[280px]">
      <label className="sr-only">{label}</label>
      <select
        value={value ?? ''}
        onChange={(e) => onChange(Number(e.target.value))}
        className="w-full appearance-none rounded-xl border border-white/10 bg-[#151B2B] text-white text-sm font-semibold pl-4 pr-10 py-3 focus:outline-none focus:border-white/25 cursor-pointer"
        style={{ boxShadow: `inset 3px 0 0 ${accentColor}` }}
      >
        {options.map((project) => (
          <option key={project.projectId} value={project.projectId} className="bg-[#151B2B]">
            {project.name}
          </option>
        ))}
      </select>
      <ChevronDown className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-500 pointer-events-none" />
    </div>
  );
}

function DualValue({
  left,
  right,
  leftColor = ENTITY_A_COLOR,
  rightColor = ENTITY_B_COLOR,
  size = 'lg',
}: {
  left: string | number;
  right: string | number;
  leftColor?: string;
  rightColor?: string;
  size?: 'lg' | 'md';
}) {
  const sizeClass = size === 'lg' ? 'text-3xl sm:text-4xl' : 'text-2xl';
  return (
    <div className="flex items-center gap-3 sm:gap-4 flex-wrap">
      <span className={`${sizeClass} font-black tabular-nums`} style={{ color: leftColor }}>
        {left}
      </span>
      <span className="text-gray-600 font-light text-xl">/</span>
      <span className={`${sizeClass} font-black tabular-nums`} style={{ color: rightColor }}>
        {right}
      </span>
    </div>
  );
}

function NsrScoreCard({ model }: { model: CompareViewModel }) {
  const total = Math.abs(model.entityA.nsrScore) + Math.abs(model.entityB.nsrScore) || 1;
  const leftWidth = (Math.abs(model.entityA.nsrScore) / total) * 100;
  const leader =
    model.nsrDifferential > 0
      ? model.entityA.name
      : model.nsrDifferential < 0
        ? model.entityB.name
        : null;

  return (
    <div className="rounded-2xl border border-white/5 bg-[#151B2B] p-5 sm:p-6 flex flex-col">
      <p className="text-[10px] font-bold text-gray-500 uppercase tracking-[0.18em] mb-4">NSR Score Index</p>
      <DualValue
        left={formatNsr(model.entityA.nsrScore)}
        right={formatNsr(model.entityB.nsrScore)}
      />
      <div className="mt-5 h-2.5 rounded-full overflow-hidden flex bg-white/5">
        <div className="h-full transition-all duration-700" style={{ width: `${leftWidth}%`, background: ENTITY_A_COLOR }} />
        <div className="h-full flex-1 transition-all duration-700" style={{ background: ENTITY_B_COLOR }} />
      </div>
      <div className="flex justify-between mt-2 text-[11px] text-gray-500">
        <span>{model.entityA.name}</span>
        <span>{model.entityB.name}</span>
      </div>
      {leader && (
        <p className="text-xs text-[#4FD1C5] mt-3">
          {leader} dẫn trước {Math.abs(model.nsrDifferential)} điểm NSR
        </p>
      )}
    </div>
  );
}

function TotalVolumeCard({ model }: { model: CompareViewModel }) {
  const isUnderperforming = model.volumeDifferential < 0;

  return (
    <div className="rounded-2xl border border-white/5 bg-[#151B2B] p-5 sm:p-6 flex flex-col">
      <p className="text-[10px] font-bold text-gray-500 uppercase tracking-[0.18em] mb-4">Total Volume</p>
      <DualValue
        left={formatNumber(model.entityA.totalVolume)}
        right={formatNumber(model.entityB.totalVolume)}
        size="md"
      />
      <div className="mt-4 flex flex-wrap items-center gap-2">
        <span
          className={`text-xs font-bold uppercase tracking-wide ${
            isUnderperforming ? 'text-[#FF7575]' : 'text-[#4FD1C5]'
          }`}
        >
          {model.volumeDifferential > 0 ? '+' : ''}
          {model.volumeDifferential}% differential
        </span>
        {isUnderperforming && (
          <span className="text-[10px] font-bold uppercase tracking-wider text-[#FF7575]/80 bg-[#FF7575]/10 px-2 py-0.5 rounded">
            Underperforming
          </span>
        )}
      </div>
      <p className="text-[11px] text-gray-500 mt-3">
        {formatNumber(model.entityA.totalComments)} vs {formatNumber(model.entityB.totalComments)} bình luận
      </p>
    </div>
  );
}

function SentimentStackBar({
  positive,
  neutral,
  negative,
}: {
  positive: number;
  neutral: number;
  negative: number;
}) {
  const segments = [
    { key: 'positive', value: positive, color: SENTIMENT_COLORS.positive },
    { key: 'neutral', value: neutral, color: SENTIMENT_COLORS.neutral },
    { key: 'negative', value: negative, color: SENTIMENT_COLORS.negative },
  ].filter((segment) => segment.value > 0);

  if (segments.length === 0) {
    return <div className="h-3 rounded-full bg-white/5" />;
  }

  return (
    <div className="h-3 rounded-full overflow-hidden flex bg-white/5">
      {segments.map((segment) => (
        <div
          key={segment.key}
          className="h-full transition-all duration-700 relative group"
          style={{ width: `${segment.value}%`, background: segment.color }}
          title={`${SENTIMENT_LABELS[segment.key as keyof typeof SENTIMENT_LABELS]} ${segment.value}%`}
        />
      ))}
    </div>
  );
}

function SentimentChip({
  type,
  percent,
}: {
  type: 'positive' | 'neutral' | 'negative';
  percent: number;
}) {
  const color = SENTIMENT_COLORS[type];
  const label = SENTIMENT_LABELS[type];

  return (
    <span className="inline-flex items-center gap-1 text-[10px] font-medium text-gray-400">
      <span className="w-2 h-2 rounded-full shrink-0" style={{ background: color }} />
      <span className="tabular-nums">
        <span className="text-white font-semibold">{percent}%</span> {label}
      </span>
    </span>
  );
}

function SentimentEquityCard({ model }: { model: CompareViewModel }) {
  const entities = [
    { entity: model.entityA, accent: ENTITY_A_COLOR },
    { entity: model.entityB, accent: ENTITY_B_COLOR },
  ];

  const sentimentLeader =
    model.entityA.positivePercent > model.entityB.positivePercent
      ? {
          name: model.entityA.name,
          color: ENTITY_A_COLOR,
          lead: model.entityA.positivePercent - model.entityB.positivePercent,
        }
      : model.entityB.positivePercent > model.entityA.positivePercent
        ? {
            name: model.entityB.name,
            color: ENTITY_B_COLOR,
            lead: model.entityB.positivePercent - model.entityA.positivePercent,
          }
        : null;

  const nsrLeader =
    model.nsrDifferential > 0
      ? { name: model.entityA.name, color: ENTITY_A_COLOR, nsr: model.entityA.nsrScore }
      : model.nsrDifferential < 0
        ? { name: model.entityB.name, color: ENTITY_B_COLOR, nsr: model.entityB.nsrScore }
        : null;

  return (
    <div className="rounded-2xl border border-white/5 bg-[#151B2B] p-5 sm:p-6 flex flex-col h-full">
      <div className="flex items-start justify-between gap-3 mb-1">
        <div>
          <p className="text-sm font-bold text-white">Phân bổ cảm xúc</p>
          <p className="text-xs text-gray-500 mt-1 leading-relaxed">
            Tỷ lệ mentions tích cực / trung lập / tiêu cực sau khi phân tích AI
          </p>
        </div>
        <div
          className="shrink-0 w-8 h-8 rounded-lg bg-white/[0.04] flex items-center justify-center text-gray-500"
          title="Mỗi thanh = 100% mentions đã phân tích của dự án"
        >
          <Info size={15} />
        </div>
      </div>

      <div className="flex flex-wrap gap-3 mt-3 mb-4 text-[10px]">
        {(['positive', 'neutral', 'negative'] as const).map((type) => (
          <span key={type} className="inline-flex items-center gap-1.5 text-gray-400">
            <span className="w-2.5 h-2.5 rounded-sm" style={{ background: SENTIMENT_COLORS[type] }} />
            {SENTIMENT_LABELS[type]}
          </span>
        ))}
      </div>

      <div className="space-y-4 flex-1">
        {entities.map(({ entity, accent }) => (
          <div
            key={entity.projectId}
            className="rounded-xl border border-white/5 bg-[#0A101D]/50 p-3.5"
          >
            <div className="flex items-start justify-between gap-2 mb-3">
              <div className="min-w-0">
                <div className="flex items-center gap-2">
                  <span className="w-2 h-2 rounded-full shrink-0" style={{ background: accent }} />
                  <p className="text-xs font-semibold text-white truncate" title={entity.name}>
                    {shortName(entity.name)}
                  </p>
                </div>
                <p className="text-[10px] text-gray-500 mt-0.5 truncate">#{entity.keyword}</p>
              </div>
              <div className="text-right shrink-0">
                <p
                  className={`text-sm font-black tabular-nums ${
                    entity.nsrScore >= 20
                      ? 'text-emerald-400'
                      : entity.nsrScore <= -20
                        ? 'text-[#FF7575]'
                        : 'text-yellow-400'
                  }`}
                >
                  NSR {formatNsr(entity.nsrScore)}
                </p>
                <p className="text-[10px] text-gray-500">chỉ số tổng hợp</p>
              </div>
            </div>

            <SentimentStackBar
              positive={entity.positivePercent}
              neutral={entity.neutralPercent}
              negative={entity.negativePercent}
            />

            <div className="flex flex-wrap gap-x-3 gap-y-1.5 mt-2.5">
              <SentimentChip type="positive" percent={entity.positivePercent} />
              <SentimentChip type="neutral" percent={entity.neutralPercent} />
              <SentimentChip type="negative" percent={entity.negativePercent} />
            </div>
          </div>
        ))}
      </div>

      <div className="mt-4 rounded-xl border border-white/5 bg-white/[0.02] overflow-hidden">
        <div className="grid grid-cols-3 text-[10px] font-bold uppercase tracking-wider text-gray-500 px-3 py-2 border-b border-white/5">
          <span>Chỉ số</span>
          <span className="text-center truncate" style={{ color: ENTITY_A_COLOR }}>
            {shortName(model.entityA.name, 10)}
          </span>
          <span className="text-center truncate" style={{ color: ENTITY_B_COLOR }}>
            {shortName(model.entityB.name, 10)}
          </span>
        </div>
        {(
          [
            { key: 'positive', label: SENTIMENT_LABELS.positive, color: SENTIMENT_COLORS.positive, a: model.entityA.positivePercent, b: model.entityB.positivePercent, higherIsBetter: true },
            { key: 'neutral', label: SENTIMENT_LABELS.neutral, color: SENTIMENT_COLORS.neutral, a: model.entityA.neutralPercent, b: model.entityB.neutralPercent, higherIsBetter: false },
            { key: 'negative', label: SENTIMENT_LABELS.negative, color: SENTIMENT_COLORS.negative, a: model.entityA.negativePercent, b: model.entityB.negativePercent, higherIsBetter: false },
          ] as const
        ).map((row) => {
          const aWins = row.higherIsBetter ? row.a > row.b : row.a < row.b;
          const bWins = row.higherIsBetter ? row.b > row.a : row.b < row.a;
          return (
            <div key={row.key} className="grid grid-cols-3 items-center px-3 py-2 text-xs border-b border-white/5 last:border-0">
              <span className="inline-flex items-center gap-1.5 text-gray-400">
                <span className="w-2 h-2 rounded-full" style={{ background: row.color }} />
                {row.label}
              </span>
              <span className={`text-center font-bold tabular-nums ${aWins ? 'text-white' : 'text-gray-500'}`}>
                {row.a}%
                {aWins && row.a !== row.b ? ' ✓' : ''}
              </span>
              <span className={`text-center font-bold tabular-nums ${bWins ? 'text-white' : 'text-gray-500'}`}>
                {row.b}%
                {bWins && row.a !== row.b ? ' ✓' : ''}
              </span>
            </div>
          );
        })}
      </div>

      <div
        className={`mt-3 rounded-xl px-3 py-2.5 flex items-center gap-2 text-xs ${
          sentimentLeader || nsrLeader
            ? 'bg-emerald-500/10 border border-emerald-500/20 text-emerald-300'
            : 'bg-white/[0.03] border border-white/5 text-gray-400'
        }`}
      >
        <Trophy size={14} className="shrink-0" />
        {sentimentLeader ? (
          <span>
            <span className="font-bold" style={{ color: sentimentLeader.color }}>
              {shortName(sentimentLeader.name)}
            </span>{' '}
            tích cực hơn{' '}
            <span className="font-bold text-white">{sentimentLeader.lead} điểm %</span>
            {nsrLeader && (
              <>
                {' '}
                · NSR <span className="font-bold text-white">{formatNsr(nsrLeader.nsr)}</span>
              </>
            )}
          </span>
        ) : (
          <span>
            Hai dự án có tỷ lệ tích cực <span className="font-bold text-white">bằng nhau</span> (
            {model.entityA.positivePercent}%)
          </span>
        )}
      </div>
    </div>
  );
}

function MetricMiniCard({
  icon,
  label,
  left,
  right,
}: {
  icon: React.ReactNode;
  label: string;
  left: string;
  right: string;
}) {
  return (
    <div className="rounded-2xl border border-white/5 bg-[#151B2B] p-4 flex items-start gap-3">
      <div className="w-9 h-9 rounded-lg bg-white/[0.04] flex items-center justify-center text-[#FF7575] shrink-0">
        {icon}
      </div>
      <div className="min-w-0">
        <p className="text-[10px] font-bold text-gray-500 uppercase tracking-wider">{label}</p>
        <p className="text-sm font-semibold text-white mt-1 truncate">
          <span style={{ color: ENTITY_A_COLOR }}>{left}</span>
          <span className="text-gray-600 mx-1.5">vs</span>
          <span style={{ color: ENTITY_B_COLOR }}>{right}</span>
        </p>
      </div>
    </div>
  );
}

function ShareOfVoiceCard({ model }: { model: CompareViewModel }) {
  const totalMentions = model.entityA.totalVolume + model.entityB.totalVolume;
  const pieData = [
    {
      key: 'a',
      name: model.entityA.name,
      shortLabel: shortName(model.entityA.name),
      value: model.entityA.totalVolume,
      percent: model.sovA,
      color: ENTITY_A_COLOR,
    },
    {
      key: 'b',
      name: model.entityB.name,
      shortLabel: shortName(model.entityB.name),
      value: model.entityB.totalVolume,
      percent: model.sovB,
      color: ENTITY_B_COLOR,
    },
  ];

  const leader =
    model.entityA.totalVolume > model.entityB.totalVolume
      ? { name: model.entityA.name, color: ENTITY_A_COLOR, lead: model.entityA.totalVolume - model.entityB.totalVolume }
      : model.entityB.totalVolume > model.entityA.totalVolume
        ? { name: model.entityB.name, color: ENTITY_B_COLOR, lead: model.entityB.totalVolume - model.entityA.totalVolume }
        : null;

  return (
    <div className="rounded-2xl border border-white/5 bg-[#151B2B] p-5 sm:p-6 flex flex-col h-full">
      <div className="flex items-start justify-between gap-3 mb-1">
        <div>
          <p className="text-sm font-bold text-white">Thị phần đề cập (SOV)</p>
          <p className="text-xs text-gray-500 mt-1 leading-relaxed">
            Mỗi thương hiệu chiếm bao nhiêu % tổng mentions trong khoảng thời gian đã chọn
          </p>
        </div>
        <div className="shrink-0 w-8 h-8 rounded-lg bg-white/[0.04] flex items-center justify-center text-gray-500" title="Share of Voice = mentions của dự án / tổng mentions cả hai">
          <Info size={15} />
        </div>
      </div>

      <div className="mt-4 rounded-xl bg-[#0A101D]/60 border border-white/5 px-3 py-2.5">
        <div className="flex items-center justify-between text-[11px] text-gray-500 mb-1.5">
          <span>Tổng cộng</span>
          <span className="text-white font-bold tabular-nums">{formatNumber(totalMentions)} mentions</span>
        </div>
        <div className="h-4 rounded-full overflow-hidden flex bg-white/5">
          <div
            className="h-full flex items-center justify-center text-[10px] font-bold text-[#0A101D] transition-all duration-700 min-w-[2rem]"
            style={{ width: `${model.sovA}%`, background: ENTITY_A_COLOR }}
          >
            {model.sovA >= 12 ? `${model.sovA}%` : ''}
          </div>
          <div
            className="h-full flex items-center justify-center text-[10px] font-bold text-[#0A101D] transition-all duration-700 min-w-[2rem]"
            style={{ width: `${model.sovB}%`, background: ENTITY_B_COLOR }}
          >
            {model.sovB >= 12 ? `${model.sovB}%` : ''}
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-[1fr_1.1fr] gap-4 mt-5 flex-1">
        <div className="relative mx-auto w-full max-w-[200px] aspect-square">
          <ResponsiveContainer width="100%" height="100%">
            <PieChart>
              <Pie
                data={pieData}
                cx="50%"
                cy="50%"
                innerRadius="65%"
                outerRadius="85%"
                paddingAngle={2}
                startAngle={90}
                endAngle={-270}
                dataKey="value"
                stroke="rgba(255,255,255,0.05)"
                strokeWidth={2}
                cornerRadius={4}
              >
                {pieData.map((slice) => (
                  <Cell key={slice.key} fill={slice.color} />
                ))}
                <LabelList
                  dataKey="percent"
                  position="outside"
                  formatter={(value) => `${value}%`}
                  style={{ fill: '#94A3B8', fontSize: 11, fontWeight: 600 }}
                />
              </Pie>
            </PieChart>
          </ResponsiveContainer>
          <div className="absolute inset-0 flex flex-col items-center justify-center pointer-events-none px-4 text-center">
            <span className="text-[10px] text-gray-500 uppercase tracking-wider font-semibold">Tỷ lệ</span>
            <span className="text-2xl font-black text-white tabular-nums leading-tight">
              {model.sovA}:{model.sovB}
            </span>
            <span className="text-[10px] text-gray-500 mt-0.5">A : B</span>
          </div>
        </div>

        <div className="space-y-3">
          {pieData.map((slice) => (
            <div key={slice.key} className="rounded-xl border border-white/5 bg-white/[0.02] p-3">
              <div className="flex items-center gap-2 mb-2">
                <span className="w-2.5 h-2.5 rounded-full shrink-0" style={{ background: slice.color }} />
                <p className="text-xs font-semibold text-white truncate" title={slice.name}>
                  {slice.shortLabel}
                </p>
              </div>
              <div className="flex items-end justify-between gap-2">
                <div>
                  <p className="text-2xl font-black text-white tabular-nums leading-none">{formatNumber(slice.value)}</p>
                  <p className="text-[10px] text-gray-500 mt-1">mentions</p>
                </div>
                <div className="text-right">
                  <p className="text-lg font-bold tabular-nums" style={{ color: slice.color }}>
                    {slice.percent}%
                  </p>
                  <p className="text-[10px] text-gray-500">thị phần</p>
                </div>
              </div>
              <div className="mt-2 h-1.5 rounded-full bg-white/5 overflow-hidden">
                <div className="h-full rounded-full transition-all duration-700" style={{ width: `${slice.percent}%`, background: slice.color }} />
              </div>
            </div>
          ))}
        </div>
      </div>

      <div
        className={`mt-4 rounded-xl px-3 py-2.5 flex items-center gap-2 text-xs ${
          leader
            ? 'bg-[#4FD1C5]/10 border border-[#4FD1C5]/20 text-[#4FD1C5]'
            : 'bg-white/[0.03] border border-white/5 text-gray-400'
        }`}
      >
        <Trophy size={14} className="shrink-0" style={leader ? { color: leader.color } : undefined} />
        {leader ? (
          <span>
            <span className="font-bold" style={{ color: leader.color }}>
              {shortName(leader.name)}
            </span>{' '}
            dẫn trước <span className="font-bold text-white">{formatNumber(leader.lead)} mentions</span> (
            {model.sovIndex}x lần đối thủ)
          </span>
        ) : (
          <span>
            Hai dự án <span className="font-bold text-white">cân bằng</span> — mỗi bên {model.sovA}% thị phần (
            {formatNumber(model.entityA.totalVolume)} mentions)
          </span>
        )}
      </div>
    </div>
  );
}

function PlatformDistributionCard({
  model,
  platforms,
}: {
  model: CompareViewModel;
  platforms: string[];
}) {
  const rows = platforms.map((platform) => {
    const aVal = model.entityA.platformVolumes[platform] ?? 0;
    const bVal = model.entityB.platformVolumes[platform] ?? 0;
    const total = aVal + bVal || 1;
    const maxVal = Math.max(aVal, bVal, 1);
    return {
      platform,
      label: getPlatformDisplayName(platform),
      accent: getPlatformAccent(platform),
      aVal,
      bVal,
      total,
      aPct: Math.round((aVal / total) * 100),
      bPct: Math.round((bVal / total) * 100),
      aBar: (aVal / maxVal) * 100,
      bBar: (bVal / maxVal) * 100,
      leader: aVal > bVal ? 'a' : bVal > aVal ? 'b' : 'tie',
    };
  });

  const totalA = rows.reduce((sum, row) => sum + row.aVal, 0);
  const totalB = rows.reduce((sum, row) => sum + row.bVal, 0);
  const grandTotal = totalA + totalB;

  return (
    <div className="rounded-2xl border border-white/5 bg-[#151B2B] p-5 sm:p-6 flex flex-col h-full">
      <div className="mb-1">
        <p className="text-sm font-bold text-white">Phân bổ theo nền tảng</p>
        <p className="text-xs text-gray-500 mt-1 leading-relaxed">
          So sánh số mentions trên từng kênh — thanh dài hơn = nhiều đề cập hơn
        </p>
      </div>

      <div className="mt-4 grid grid-cols-2 gap-2 text-center">
        <div className="rounded-lg bg-white/[0.03] border border-white/5 px-2 py-2">
          <p className="text-[10px] text-gray-500 truncate" title={model.entityA.name}>
            {shortName(model.entityA.name, 14)}
          </p>
          <p className="text-lg font-black tabular-nums mt-0.5" style={{ color: ENTITY_A_COLOR }}>
            {formatNumber(totalA)}
          </p>
        </div>
        <div className="rounded-lg bg-white/[0.03] border border-white/5 px-2 py-2">
          <p className="text-[10px] text-gray-500 truncate" title={model.entityB.name}>
            {shortName(model.entityB.name, 14)}
          </p>
          <p className="text-lg font-black tabular-nums mt-0.5" style={{ color: ENTITY_B_COLOR }}>
            {formatNumber(totalB)}
          </p>
        </div>
      </div>

      {rows.length === 0 ? (
        <p className="text-sm text-gray-500 py-10 text-center flex-1">Chưa có dữ liệu theo nền tảng.</p>
      ) : (
        <div className="space-y-4 mt-5 flex-1">
          {rows.map((row) => (
            <div key={row.platform} className="rounded-xl border border-white/5 bg-[#0A101D]/40 p-3.5">
              <div className="flex items-center justify-between gap-2 mb-3">
                <div className="flex items-center gap-2 min-w-0">
                  <span
                    className="w-7 h-7 rounded-lg flex items-center justify-center text-[10px] font-black text-white shrink-0"
                    style={{ background: `${row.accent}22`, color: row.accent, border: `1px solid ${row.accent}44` }}
                  >
                    {row.label.slice(0, 2).toUpperCase()}
                  </span>
                  <div className="min-w-0">
                    <p className="text-sm font-semibold text-white">{row.label}</p>
                    <p className="text-[10px] text-gray-500">{formatNumber(row.total)} mentions trên kênh này</p>
                  </div>
                </div>
                {row.leader !== 'tie' && (
                  <span className="text-[10px] font-bold uppercase tracking-wide px-2 py-0.5 rounded bg-white/5 text-gray-400 shrink-0">
                    {row.leader === 'a' ? shortName(model.entityA.name, 10) : shortName(model.entityB.name, 10)} dẫn
                  </span>
                )}
              </div>

              <div className="space-y-2.5">
                <div>
                  <div className="flex items-center justify-between text-[11px] mb-1">
                    <span className="text-gray-400 truncate" style={{ color: ENTITY_A_COLOR }}>
                      {shortName(model.entityA.name, 14)}
                    </span>
                    <span className="tabular-nums font-semibold text-white shrink-0 ml-2">
                      {formatNumber(row.aVal)}{' '}
                      <span className="text-gray-500 font-normal">({row.aPct}%)</span>
                    </span>
                  </div>
                  <div className="h-2.5 rounded-full bg-white/5 overflow-hidden">
                    <div
                      className="h-full rounded-full transition-all duration-700"
                      style={{ width: `${row.aBar}%`, background: ENTITY_A_COLOR }}
                    />
                  </div>
                </div>

                <div>
                  <div className="flex items-center justify-between text-[11px] mb-1">
                    <span className="text-gray-400 truncate" style={{ color: ENTITY_B_COLOR }}>
                      {shortName(model.entityB.name, 14)}
                    </span>
                    <span className="tabular-nums font-semibold text-white shrink-0 ml-2">
                      {formatNumber(row.bVal)}{' '}
                      <span className="text-gray-500 font-normal">({row.bPct}%)</span>
                    </span>
                  </div>
                  <div className="h-2.5 rounded-full bg-white/5 overflow-hidden">
                    <div
                      className="h-full rounded-full transition-all duration-700"
                      style={{ width: `${row.bBar}%`, background: ENTITY_B_COLOR }}
                    />
                  </div>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}

      {grandTotal > 0 && (
        <p className="text-[11px] text-gray-500 mt-4 pt-3 border-t border-white/5 text-center">
          Tổng cộng <span className="text-white font-semibold">{formatNumber(grandTotal)}</span> mentions trên{' '}
          {rows.length} nền tảng
        </p>
      )}
    </div>
  );
}

function VolumeTrendCard({
  trendData,
  entityAName,
  entityBName,
  mode,
  onModeChange,
}: {
  trendData: CompareTrendPoint[];
  entityAName: string;
  entityBName: string;
  mode: 'daily' | 'weekly';
  onModeChange: (mode: 'daily' | 'weekly') => void;
}) {
  const stats = useMemo(() => computeTrendStats(trendData), [trendData]);
  const modeLabel = mode === 'daily' ? 'Theo ngày' : 'Theo tuần';

  return (
    <div className="rounded-2xl border border-white/5 bg-[#151B2B] p-5 sm:p-6">
      <div className="flex flex-col lg:flex-row lg:items-start lg:justify-between gap-4 mb-5">
        <div>
          <p className="text-sm font-bold text-white">Xu hướng lượng đề cập</p>
          <p className="text-xs text-gray-500 mt-1 leading-relaxed max-w-xl">
            Biểu đồ cột so sánh trực tiếp số mentions theo {mode === 'daily' ? 'ngày' : 'tuần'}. Di chuột vào cột để
            xem chi tiết từng kỳ.
          </p>
        </div>
        <div className="inline-flex rounded-lg border border-white/10 bg-white/[0.03] p-1 self-start shrink-0">
          {(
            [
              { key: 'daily' as const, label: 'Theo ngày' },
              { key: 'weekly' as const, label: 'Theo tuần' },
            ] as const
          ).map(({ key, label }) => (
            <button
              key={key}
              type="button"
              onClick={() => onModeChange(key)}
              className={`px-3 py-1.5 rounded-md text-xs font-semibold transition-all ${
                mode === key ? 'bg-white/10 text-white' : 'text-gray-500 hover:text-gray-300'
              }`}
            >
              {label}
            </button>
          ))}
        </div>
      </div>

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-2 mb-5">
        {[
          {
            label: `Tổng (${shortName(entityAName, 12)})`,
            value: formatNumber(stats.totalA),
            sub: `TB ${stats.avgA}/kỳ`,
            color: ENTITY_A_COLOR,
            change: stats.changeA,
          },
          {
            label: `Tổng (${shortName(entityBName, 12)})`,
            value: formatNumber(stats.totalB),
            sub: `TB ${stats.avgB}/kỳ`,
            color: ENTITY_B_COLOR,
            change: stats.changeB,
          },
          {
            label: 'Cao nhất A / B',
            value: `${formatNumber(stats.peakA)} / ${formatNumber(stats.peakB)}`,
            sub: 'Một kỳ nhiều nhất',
            color: '#fff',
            change: null,
          },
          {
            label: 'Chênh lệch tổng',
            value: `${stats.totalA >= stats.totalB ? '+' : ''}${formatNumber(stats.totalA - stats.totalB)}`,
            sub: stats.totalA === stats.totalB ? 'Bằng nhau' : stats.totalA > stats.totalB ? `${shortName(entityAName, 10)} nhiều hơn` : `${shortName(entityBName, 10)} nhiều hơn`,
            color: stats.totalA === stats.totalB ? '#94A3B8' : stats.totalA > stats.totalB ? ENTITY_A_COLOR : ENTITY_B_COLOR,
            change: null,
          },
        ].map((item) => (
          <div key={item.label} className="rounded-xl border border-white/5 bg-[#0A101D]/50 px-3 py-2.5">
            <p className="text-[10px] text-gray-500 truncate" title={item.label}>
              {item.label}
            </p>
            <p className="text-lg font-black tabular-nums mt-0.5" style={{ color: item.color }}>
              {item.value}
            </p>
            <div className="flex items-center justify-between gap-1 mt-0.5">
              <p className="text-[10px] text-gray-500 truncate">{item.sub}</p>
              {item.change !== null && <TrendChangeBadge value={item.change} />}
            </div>
          </div>
        ))}
      </div>

      {trendData.length === 0 ? (
        <div className="h-[320px] flex flex-col items-center justify-center text-sm text-gray-500 border border-dashed border-white/10 rounded-xl gap-2">
          <BarChart2 size={28} className="text-gray-600" />
          <p>Chưa đủ dữ liệu theo thời gian để vẽ xu hướng.</p>
          <p className="text-xs text-gray-600">Thử chọn khoảng thời gian rộng hơn hoặc cào thêm dữ liệu.</p>
        </div>
      ) : (
        <div className="rounded-xl border border-white/5 bg-[#0A101D]/50 px-2 sm:px-4 pt-4 pb-2">
          <ResponsiveContainer width="100%" height={320}>
            <BarChart data={trendData} margin={{ top: 16, right: 8, left: 0, bottom: 8 }} barGap={4} barCategoryGap="24%">
              <CartesianGrid strokeDasharray="4 6" stroke="rgba(255,255,255,0.06)" vertical={false} />
              <XAxis
                dataKey="label"
                tick={{ fill: '#9CA3AF', fontSize: 11, fontWeight: 500 }}
                axisLine={{ stroke: 'rgba(255,255,255,0.08)' }}
                tickLine={false}
                interval={0}
                angle={trendData.length > 6 ? -25 : 0}
                textAnchor={trendData.length > 6 ? 'end' : 'middle'}
                height={trendData.length > 6 ? 48 : 32}
              />
              <YAxis
                tick={{ fill: '#6B7280', fontSize: 11 }}
                axisLine={false}
                tickLine={false}
                width={40}
                allowDecimals={false}
                label={{
                  value: 'Số mentions',
                  angle: -90,
                  position: 'insideLeft',
                  offset: 12,
                  style: { fill: '#6B7280', fontSize: 10 },
                }}
              />
              <Tooltip
                cursor={{ fill: 'rgba(255,255,255,0.04)' }}
                content={(props) => (
                  <CompareTrendTooltip
                    active={props.active}
                    payload={props.payload as readonly { value?: unknown; dataKey?: string | number }[] | undefined}
                    label={props.label != null ? String(props.label) : undefined}
                    entityAName={entityAName}
                    entityBName={entityBName}
                  />
                )}
              />
              <Bar dataKey="entityA" name={shortName(entityAName)} fill={ENTITY_A_COLOR} radius={[6, 6, 0, 0]} maxBarSize={48}>
                <LabelList
                  dataKey="entityA"
                  position="top"
                  formatter={(value) => (Number(value) > 0 ? formatNumber(Number(value)) : '')}
                  style={{ fill: ENTITY_A_COLOR, fontSize: 11, fontWeight: 700 }}
                />
              </Bar>
              <Bar dataKey="entityB" name={shortName(entityBName)} fill={ENTITY_B_COLOR} radius={[6, 6, 0, 0]} maxBarSize={48}>
                <LabelList
                  dataKey="entityB"
                  position="top"
                  formatter={(value) => (Number(value) > 0 ? formatNumber(Number(value)) : '')}
                  style={{ fill: ENTITY_B_COLOR, fontSize: 11, fontWeight: 700 }}
                />
              </Bar>
            </BarChart>
          </ResponsiveContainer>
        </div>
      )}

      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 mt-4 pt-4 border-t border-white/5">
        <div className="flex flex-wrap gap-4 text-xs">
          <span className="inline-flex items-center gap-2 text-gray-300">
            <span className="w-3 h-3 rounded-sm" style={{ background: ENTITY_A_COLOR }} />
            <span className="font-medium">{shortName(entityAName)}</span>
            <span className="text-gray-500">· {formatNumber(stats.totalA)} tổng</span>
          </span>
          <span className="inline-flex items-center gap-2 text-gray-300">
            <span className="w-3 h-3 rounded-sm" style={{ background: ENTITY_B_COLOR }} />
            <span className="font-medium">{shortName(entityBName)}</span>
            <span className="text-gray-500">· {formatNumber(stats.totalB)} tổng</span>
          </span>
        </div>
        <p className="text-[11px] text-gray-500">
          Chế độ: <span className="text-gray-300 font-medium">{modeLabel}</span>
          {trendData.length > 0 && (
            <>
              {' '}
              · {trendData.length} {mode === 'daily' ? 'ngày' : 'tuần'} có dữ liệu
            </>
          )}
        </p>
      </div>
    </div>
  );
}

const ProjectComparison = () => {
  const { workspaceId, id } = useParams();
  const location = useLocation();
  const compareWithFromNav = (location.state as CompareNavState | null)?.compareWith ?? null;

  const wid = Number(workspaceId);
  const currentProjectId = Number(id);

  const [projects, setProjects] = useState<Project[]>([]);
  const [leftId, setLeftId] = useState<number | null>(null);
  const [rightId, setRightId] = useState<number | null>(null);
  const [dateRange, setDateRange] = useState(30);
  const [trendMode, setTrendMode] = useState<'daily' | 'weekly'>('daily');
  const [isLoading, setIsLoading] = useState(true);
  const [isStatsLoading, setIsStatsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');

  const [leftSnapshot, setLeftSnapshot] = useState<CompareEntitySnapshot | null>(null);
  const [rightSnapshot, setRightSnapshot] = useState<CompareEntitySnapshot | null>(null);
  const [leftMentions, setLeftMentions] = useState<ProjectMention[]>([]);
  const [rightMentions, setRightMentions] = useState<ProjectMention[]>([]);

  const loadProjects = useCallback(async () => {
    if (!wid || Number.isNaN(wid)) {
      setIsLoading(false);
      return;
    }

    setIsLoading(true);
    setErrorMessage('');
    try {
      const list = await projectApi.getProjects(wid);
      setProjects(list);

      const defaultLeft =
        list.find((p) => p.projectId === currentProjectId)?.projectId ?? list[0]?.projectId ?? null;
      const defaultRight =
        compareWithFromNav && list.some((p) => p.projectId === compareWithFromNav)
          ? compareWithFromNav
          : list.find((p) => p.projectId !== defaultLeft)?.projectId ?? list[1]?.projectId ?? null;

      setLeftId(defaultLeft);
      setRightId(defaultRight);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải danh sách dự án.'));
    } finally {
      setIsLoading(false);
    }
  }, [wid, currentProjectId, compareWithFromNav]);

  useEffect(() => {
    loadProjects();
  }, [loadProjects]);

  const loadComparisonData = useCallback(async () => {
    if (!wid || !leftId || !rightId || leftId === rightId) {
      setLeftSnapshot(null);
      setRightSnapshot(null);
      setLeftMentions([]);
      setRightMentions([]);
      return;
    }

    const leftProject = projects.find((p) => p.projectId === leftId);
    const rightProject = projects.find((p) => p.projectId === rightId);
    if (!leftProject || !rightProject) return;

    setIsStatsLoading(true);
    setErrorMessage('');

    const dateFrom = getDateFrom(dateRange);
    const mentionFilters = { dateFrom };

    try {
      const [leftOverview, leftSentiment, rightOverview, rightSentiment, leftMentionsData, rightMentionsData] =
        await Promise.all([
          projectApi.getOverview(wid, leftId),
          projectApi.getSentiment(wid, leftId),
          projectApi.getOverview(wid, rightId),
          projectApi.getSentiment(wid, rightId),
          projectApi.getMentions(wid, leftId, mentionFilters),
          projectApi.getMentions(wid, rightId, mentionFilters),
        ]);

      setLeftSnapshot(buildSnapshot(leftProject, leftOverview, leftSentiment));
      setRightSnapshot(buildSnapshot(rightProject, rightOverview, rightSentiment));
      setLeftMentions(leftMentionsData);
      setRightMentions(rightMentionsData);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải dữ liệu so sánh.'));
      setLeftSnapshot(null);
      setRightSnapshot(null);
      setLeftMentions([]);
      setRightMentions([]);
    } finally {
      setIsStatsLoading(false);
    }
  }, [wid, leftId, rightId, projects, dateRange]);

  useEffect(() => {
    if (!isLoading && projects.length >= 2) {
      loadComparisonData();
    }
  }, [isLoading, projects.length, loadComparisonData]);

  const model = useMemo<CompareViewModel | null>(() => {
    if (!leftSnapshot || !rightSnapshot) return null;
    return buildViewModel(leftSnapshot, rightSnapshot);
  }, [leftSnapshot, rightSnapshot]);

  const platformList = useMemo(() => {
    if (!model) return [];
    const keys = new Set([
      ...Object.keys(model.entityA.platformVolumes),
      ...Object.keys(model.entityB.platformVolumes),
    ]);
    return sortPlatforms(keys);
  }, [model]);

  const trendData = useMemo(
    () => buildComparisonTrend(leftMentions, rightMentions, trendMode),
    [leftMentions, rightMentions, trendMode]
  );

  const hasNoData = model && model.entityA.totalVolume === 0 && model.entityB.totalVolume === 0;

  if (isLoading) {
    return (
      <div className="min-h-screen bg-[#050A15] flex items-center justify-center text-gray-400 gap-3">
        <Loader2 className="w-8 h-8 animate-spin text-[#FF7575]" />
        <span>Đang tải so sánh...</span>
      </div>
    );
  }

  if (projects.length < 2) {
    return (
      <div className="min-h-screen bg-[#050A15] flex flex-col items-center justify-center text-gray-400 gap-4 px-4">
        <AlertCircle className="w-10 h-10 text-amber-400" />
        <p>Cần ít nhất 2 dự án trong workspace để so sánh.</p>
        <Link to={`/workspace/${wid}/projects`} className="text-[#FF7575] hover:underline text-sm">
          Quay lại danh sách dự án
        </Link>
      </div>
    );
  }

  return (
    <div className="h-screen w-full bg-[#050A15] text-white font-sans flex overflow-hidden selection:bg-[#FF7575] selection:text-white">
      {!Number.isNaN(wid) && !Number.isNaN(currentProjectId) ? (
        <ComparisonSidebar workspaceId={wid} projectId={currentProjectId} />
      ) : null}

      <div className="flex-1 flex flex-col min-w-0 h-full overflow-hidden relative">
        <div
          className="absolute inset-0 z-0 opacity-[0.03] pointer-events-none"
          style={{
            backgroundImage: 'radial-gradient(#ffffff 1px, transparent 1px)',
            backgroundSize: '32px 32px',
          }}
        />

        <ComparisonHeader />

        <div className="flex-1 overflow-y-auto p-4 sm:p-6 lg:p-8 relative z-10">
          <div className="max-w-7xl mx-auto space-y-6 pb-24">
            <Link
              to={`/workspace/${wid}/projects`}
              className="inline-flex items-center gap-2 text-sm text-gray-400 hover:text-white transition-colors"
            >
              <ArrowLeft size={16} />
              Quay lại danh sách dự án
            </Link>

            <div>
              <h1 className="text-2xl sm:text-3xl font-bold text-white">So sánh Đối thủ Cạnh tranh</h1>
              <p className="text-sm text-gray-500 mt-1">Competitor Benchmarking Intelligence — dữ liệu thực từ DB</p>
            </div>

            {errorMessage && (
              <div className="bg-red-500/10 border border-red-500/20 text-red-300 px-4 py-3 rounded-xl text-sm flex items-center gap-2">
                <AlertCircle className="w-4 h-4 shrink-0" />
                {errorMessage}
              </div>
            )}

            <div className="flex flex-col xl:flex-row xl:items-center gap-4 flex-wrap">
              <div className="flex items-center gap-3 flex-1 min-w-0">
                <EntitySelect
                  label="Dự án A"
                  value={leftId}
                  options={projects}
                  onChange={setLeftId}
                  accentColor={ENTITY_A_COLOR}
                />
                <span className="text-xs font-bold text-gray-500 uppercase tracking-widest shrink-0">VS</span>
                <EntitySelect
                  label="Dự án B"
                  value={rightId}
                  options={projects.filter((p) => p.projectId !== leftId)}
                  onChange={setRightId}
                  accentColor={ENTITY_B_COLOR}
                />
              </div>

              <div className="relative">
                <Calendar className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-500 pointer-events-none" />
                <select
                  value={dateRange}
                  onChange={(e) => setDateRange(Number(e.target.value))}
                  className="appearance-none rounded-xl border border-white/10 bg-[#151B2B] pl-10 pr-10 py-2.5 text-sm text-white focus:outline-none focus:border-white/20 cursor-pointer"
                >
                  {DATE_RANGES.map((range) => (
                    <option key={range.value} value={range.value} className="bg-[#151B2B]">
                      {range.label}
                    </option>
                  ))}
                </select>
                <ChevronDown className="absolute right-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-500 pointer-events-none" />
              </div>
            </div>

            {model && (
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 text-xs">
                <div className="rounded-xl border border-white/5 bg-white/[0.02] px-4 py-3">
                  <span className="text-gray-500">Dự án A · </span>
                  <span className="text-white font-medium">{model.entityA.name}</span>
                  <span className="text-gray-500 ml-2">#{model.entityA.keyword}</span>
                </div>
                <div className="rounded-xl border border-white/5 bg-white/[0.02] px-4 py-3">
                  <span className="text-gray-500">Dự án B · </span>
                  <span className="text-white font-medium">{model.entityB.name}</span>
                  <span className="text-gray-500 ml-2">#{model.entityB.keyword}</span>
                </div>
              </div>
            )}

            {isStatsLoading ? (
              <div className="flex items-center justify-center py-16 text-gray-400 gap-3">
                <Loader2 className="w-6 h-6 animate-spin text-[#FF7575]" />
                <span className="text-sm">Đang tải chỉ số NSR từ dữ liệu đã cào...</span>
              </div>
            ) : model ? (
              <>
                {hasNoData && (
                  <div className="bg-amber-500/10 border border-amber-500/20 text-amber-200 px-4 py-3 rounded-xl text-sm">
                    Hai dự án chưa có mentions trong khoảng thời gian đã chọn. Thử mở rộng bộ lọc hoặc cào lại dữ liệu.
                  </div>
                )}

                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <NsrScoreCard model={model} />
                  <TotalVolumeCard model={model} />
                  <SentimentEquityCard model={model} />
                </div>

                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                  <MetricMiniCard
                    icon={<MessageCircle size={16} />}
                    label="Bình luận"
                    left={formatNumber(model.entityA.totalComments)}
                    right={formatNumber(model.entityB.totalComments)}
                  />
                  <MetricMiniCard
                    icon={<Sparkles size={16} />}
                    label="Đã phân tích AI"
                    left={formatNumber(model.entityA.analyzedCount)}
                    right={formatNumber(model.entityB.analyzedCount)}
                  />
                </div>

                <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                  <ShareOfVoiceCard model={model} />
                  <PlatformDistributionCard model={model} platforms={platformList} />
                </div>

                <VolumeTrendCard
                  trendData={trendData}
                  entityAName={model.entityA.name}
                  entityBName={model.entityB.name}
                  mode={trendMode}
                  onModeChange={setTrendMode}
                />
              </>
            ) : null}
          </div>
        </div>

        <button
          type="button"
          className="fixed bottom-8 right-8 z-30 w-14 h-14 rounded-full bg-[#FF7575] hover:bg-[#ff6262] text-white shadow-lg shadow-[#FF7575]/30 flex items-center justify-center transition-transform hover:scale-105"
          title="Quick action"
        >
          <Zap className="w-6 h-6" />
        </button>
      </div>
    </div>
  );
};

export default ProjectComparison;
