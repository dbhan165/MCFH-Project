import { useCallback, useEffect, useMemo, useState, useId, type ReactNode } from 'react';
import { useParams } from 'react-router-dom';
import {
  PieChart as PieChartIcon,
  TrendingUp,
  TrendingDown,
  Minus,
  Loader2,
  AlertCircle,
  RefreshCw,
  BarChart3,
  Activity,
  CheckCircle2,
  Sparkles,
  Layers,
} from 'lucide-react';
import {
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  Tooltip,
  XAxis,
  YAxis,
  CartesianGrid,
  AreaChart,
  Area,
} from 'recharts';
import { projectApi } from '../api/projectApi';
import type { ProjectMention, SentimentSummary } from '../types/project';
import { extractApiError } from '../utils/authStorage';
import {
  formatNumber,
  SENTIMENT_COLORS,
  getPlatformDisplayName,
  getPlatformBadgeClass,
  getPlatformChartColor,
} from '../utils/sentimentHelpers';
import { buildPieSlices, buildTrendPoints, getNsrTone, type PieSlice, type TrendPoint } from '../utils/sentimentChartData';

type PlatformInsight = {
  platform: string;
  label: string;
  total: number;
  analyzed: number;
  pending: number;
  positive: number;
  negative: number;
  neutral: number;
  positiveRate: number;
  negativeRate: number;
  share: number;
};

type DominantSentiment = {
  key: string;
  label: string;
  value: number;
  percent: number;
  color: string;
};

function getDominantSentiment(summary: SentimentSummary): DominantSentiment {
  const candidates: DominantSentiment[] = [
    {
      key: 'positive',
      label: 'Tích cực',
      value: summary.positive,
      percent: summary.positivePercent,
      color: SENTIMENT_COLORS.positive,
    },
    {
      key: 'negative',
      label: 'Tiêu cực',
      value: summary.negative,
      percent: summary.negativePercent,
      color: SENTIMENT_COLORS.negative,
    },
    {
      key: 'neutral',
      label: 'Trung lập',
      value: summary.neutral,
      percent: summary.neutralPercent,
      color: SENTIMENT_COLORS.neutral,
    },
  ];
  return [...candidates].sort((a, b) => b.value - a.value)[0];
}

function SentimentRatioCard({
  pieData,
  dominant,
  analyzedCount,
  coveragePercent,
}: {
  pieData: PieSlice[];
  dominant: DominantSentiment;
  analyzedCount: number;
  coveragePercent: number;
}) {
  const chartId = useId().replace(/:/g, '');
  const [activeKey, setActiveKey] = useState<string | null>(null);
  const orderedSlices = useMemo(() => {
    const order = ['positive', 'neutral', 'negative'];
    return order
      .map((key) => pieData.find((slice) => slice.key === key))
      .filter((slice): slice is PieSlice => Boolean(slice));
  }, [pieData]);
  const focusSlice = useMemo(
    () => (activeKey ? pieData.find((slice) => slice.key === activeKey) ?? null : null),
    [activeKey, pieData]
  );
  const centerColor = focusSlice?.color ?? dominant.color;
  const centerPercent = focusSlice?.percent ?? dominant.percent;
  const centerLabel = focusSlice?.name ?? dominant.label;
  const centerValue = focusSlice?.value ?? dominant.value;
  const centerCaption = focusSlice ? 'Đang xem' : 'Chủ đạo';

  if (pieData.length === 0) {
    return (
      <div className="xl:col-span-2 rounded-3xl border border-white/5 bg-[#151B2B] p-6 flex items-center justify-center min-h-[420px]">
        <p className="text-gray-500 text-sm">Chưa có bài nào được phân tích sentiment.</p>
      </div>
    );
  }

  return (
    <div className="xl:col-span-2 relative overflow-hidden rounded-3xl border border-white/5 bg-gradient-to-br from-[#151B2B] via-[#12182A] to-[#0F1524] p-6 flex flex-col">
      <div className="absolute -top-16 -right-16 w-48 h-48 rounded-full bg-[#00B4D8]/10 blur-3xl pointer-events-none" />
      <div className="absolute -bottom-12 -left-12 w-40 h-40 rounded-full bg-[#FF7575]/10 blur-3xl pointer-events-none" />

      <div className="relative flex items-start justify-between gap-4">
        <div>
          <h3 className="font-bold text-white">Tỉ lệ sentiment</h3>
          <p className="text-xs text-gray-500 mt-1">
            {formatNumber(analyzedCount)} mentions đã phân tích
          </p>
        </div>
        <div className="rounded-2xl border border-white/10 bg-white/[0.03] px-4 py-2 text-right">
          <p className="text-[10px] text-gray-500 uppercase tracking-wider">Độ phủ</p>
          <p className="text-xl font-black text-white tabular-nums">{coveragePercent}%</p>
        </div>
      </div>

      <div className="relative mx-auto mt-2 w-full max-w-[300px] aspect-square">
        <div
          className="absolute inset-[12%] rounded-full opacity-40 blur-2xl pointer-events-none"
          style={{
            background: `radial-gradient(circle, ${centerColor}55 0%, transparent 70%)`,
          }}
        />

        <ResponsiveContainer width="100%" height="100%">
          <PieChart>
            <defs>
              {pieData.map((slice) => (
                <linearGradient key={slice.key} id={`${chartId}-grad-${slice.key}`} x1="0" y1="0" x2="1" y2="1">
                  <stop offset="0%" stopColor={slice.color} stopOpacity={1} />
                  <stop offset="100%" stopColor={slice.color} stopOpacity={0.72} />
                </linearGradient>
              ))}
              <filter id={`${chartId}-glow`} x="-20%" y="-20%" width="140%" height="140%">
                <feGaussianBlur stdDeviation="2" result="blur" />
                <feMerge>
                  <feMergeNode in="blur" />
                  <feMergeNode in="SourceGraphic" />
                </feMerge>
              </filter>
            </defs>
            <Pie
              data={pieData}
              cx="50%"
              cy="50%"
              innerRadius="62%"
              outerRadius="82%"
              paddingAngle={2}
              minAngle={4}
              dataKey="value"
              stroke="rgba(255,255,255,0.14)"
              strokeWidth={2}
              cornerRadius={6}
              animationDuration={800}
            >
              {pieData.map((slice) => (
                <Cell
                  key={slice.key}
                  fill={`url(#${chartId}-grad-${slice.key})`}
                  onMouseEnter={() => setActiveKey(slice.key)}
                  onMouseLeave={() => setActiveKey(null)}
                  style={{
                    filter: activeKey === slice.key ? `url(#${chartId}-glow)` : undefined,
                    opacity: activeKey && activeKey !== slice.key ? 0.45 : 1,
                    transition: 'opacity 200ms ease',
                    cursor: 'pointer',
                  }}
                />
              ))}
            </Pie>
          </PieChart>
        </ResponsiveContainer>

        <div className="absolute inset-0 z-10 flex items-center justify-center pointer-events-none">
          <div
            className="w-[58%] h-[58%] max-w-[168px] max-h-[168px] rounded-full flex flex-col items-center justify-center text-center px-3 transition-all duration-200"
            style={{
              background: 'radial-gradient(circle at 50% 30%, rgba(255,255,255,0.06) 0%, rgba(10,16,29,0.95) 55%)',
              border: '1px solid rgba(255,255,255,0.08)',
              boxShadow: 'inset 0 1px 0 rgba(255,255,255,0.06), 0 8px 32px rgba(0,0,0,0.35)',
            }}
          >
            <span className="text-[10px] uppercase tracking-[0.2em] text-gray-500 font-semibold">{centerCaption}</span>
            <p className="text-4xl font-black tabular-nums leading-none mt-1" style={{ color: centerColor }}>
              {centerPercent}%
            </p>
            <p className="text-sm font-semibold text-white mt-1.5">{centerLabel}</p>
            <p className="text-[11px] text-gray-500 mt-1">{formatNumber(centerValue)} mentions</p>
          </div>
        </div>
      </div>

      <div className="relative mt-5">
        <div className="h-2.5 rounded-full overflow-hidden flex bg-white/5 ring-1 ring-white/5">
          {orderedSlices.map((slice) => (
            <div
              key={slice.key}
              title={`${slice.name} ${slice.percent}%`}
              className="h-full transition-all duration-500 first:rounded-l-full last:rounded-r-full"
              style={{
                width: `${Math.max(slice.percent, slice.value > 0 ? 2 : 0)}%`,
                background: `linear-gradient(90deg, ${slice.color}, ${slice.color}cc)`,
                opacity: activeKey && activeKey !== slice.key ? 0.35 : 1,
              }}
            />
          ))}
        </div>
        <div className="flex justify-between mt-2 text-[10px] text-gray-500 px-0.5">
          {orderedSlices.map((slice) => (
            <span key={slice.key}>{slice.name}</span>
          ))}
        </div>
      </div>

      <div className="relative grid grid-cols-3 gap-2.5 mt-5">
        {pieData.map((slice) => {
          const isActive = activeKey === slice.key;
          return (
            <button
              key={slice.key}
              type="button"
              onMouseEnter={() => setActiveKey(slice.key)}
              onMouseLeave={() => setActiveKey(null)}
              onFocus={() => setActiveKey(slice.key)}
              onBlur={() => setActiveKey(null)}
              className={`rounded-2xl p-3.5 text-left transition-all duration-200 border ${
                isActive
                  ? 'bg-white/[0.06] border-white/15 scale-[1.02]'
                  : 'bg-[#0A101D]/70 border-white/5 hover:border-white/10'
              }`}
            >
              <div className="flex items-center gap-2 mb-2">
                <span
                  className="w-2.5 h-2.5 rounded-full shrink-0"
                  style={{
                    background: slice.color,
                    boxShadow: isActive ? `0 0 10px ${slice.color}` : undefined,
                  }}
                />
                <span className="text-xs text-gray-400 font-medium">{slice.name}</span>
              </div>
              <p className="text-2xl font-black text-white tabular-nums leading-none">{slice.percent}%</p>
              <p className="text-[11px] text-gray-500 mt-1.5 tabular-nums">{formatNumber(slice.value)} mentions</p>
              <div className="mt-2.5 h-1 rounded-full bg-white/5 overflow-hidden">
                <div
                  className="h-full rounded-full transition-all duration-500"
                  style={{ width: `${slice.percent}%`, background: slice.color }}
                />
              </div>
            </button>
          );
        })}
      </div>
    </div>
  );
}

const TREND_SERIES = [
  { key: 'positive', name: 'Tích cực', color: SENTIMENT_COLORS.positive },
  { key: 'negative', name: 'Tiêu cực', color: SENTIMENT_COLORS.negative },
  { key: 'neutral', name: 'Trung lập', color: SENTIMENT_COLORS.neutral },
] as const;

type TrendSeriesKey = (typeof TREND_SERIES)[number]['key'];

function TrendTooltip({
  active,
  payload,
  label,
}: {
  active?: boolean;
  payload?: Array<{ name?: string; value?: number; color?: string; dataKey?: string }>;
  label?: string;
}) {
  if (!active || !payload?.length) return null;
  const total = payload.reduce((sum, item) => sum + (Number(item.value) || 0), 0);

  return (
    <div
      className="rounded-xl border border-white/10 bg-[#0A101D]/95 backdrop-blur-sm px-4 py-3 shadow-2xl min-w-[160px]"
      style={{ pointerEvents: 'none' }}
    >
      <p className="text-xs font-semibold text-white mb-2">{label}</p>
      <div className="space-y-1.5">
        {payload.map((item) => (
          <div key={item.dataKey ?? item.name} className="flex items-center justify-between gap-4 text-sm">
            <div className="flex items-center gap-2">
              <span className="w-2 h-2 rounded-full shrink-0" style={{ background: item.color }} />
              <span className="text-gray-400">{item.name}</span>
            </div>
            <span className="font-semibold text-white tabular-nums">{item.value ?? 0}</span>
          </div>
        ))}
      </div>
      <p className="text-xs text-gray-500 mt-2 pt-2 border-t border-white/10">
        Tổng: <span className="text-white font-semibold">{total}</span> mentions
      </p>
    </div>
  );
}

function SentimentTrendCard({ trendPoints }: { trendPoints: TrendPoint[] }) {
  const chartId = useId().replace(/:/g, '');
  const [hiddenSeries, setHiddenSeries] = useState<Set<TrendSeriesKey>>(new Set());

  const stats = useMemo(() => {
    const peak = [...trendPoints].sort((a, b) => b.total - a.total)[0];
    const totalMentions = trendPoints.reduce((sum, point) => sum + point.total, 0);
    return { peak, totalMentions, dayCount: trendPoints.length };
  }, [trendPoints]);

  const toggleSeries = (key: TrendSeriesKey) => {
    setHiddenSeries((prev) => {
      const next = new Set(prev);
      if (next.has(key)) next.delete(key);
      else next.add(key);
      return next;
    });
  };

  if (trendPoints.length < 2) {
    return (
      <div className="xl:col-span-3 rounded-3xl border border-white/5 bg-[#151B2B] p-6">
        <div className="flex items-start gap-3 mb-6">
          <Activity className="w-5 h-5 text-[#FF7575] shrink-0 mt-0.5" />
          <div>
            <h3 className="font-bold text-white">Xu hướng theo thời gian</h3>
            <p className="text-xs text-gray-500 mt-1">Theo ngày đăng bài hoặc ngày cào dữ liệu</p>
          </div>
        </div>
        <div className="h-[320px] flex flex-col items-center justify-center text-gray-500 text-sm border border-dashed border-white/10 rounded-2xl bg-[#0A101D]/50">
          <Activity className="w-10 h-10 text-gray-600 mb-3" />
          Cần thêm dữ liệu nhiều ngày để hiển thị xu hướng.
        </div>
      </div>
    );
  }

  return (
    <div className="xl:col-span-3 relative overflow-hidden rounded-3xl border border-white/5 bg-gradient-to-br from-[#151B2B] via-[#12182A] to-[#0F1524] p-6">
      <div className="absolute -top-20 -left-16 w-56 h-56 rounded-full bg-[#FF7575]/8 blur-3xl pointer-events-none" />
      <div className="absolute -bottom-16 -right-12 w-48 h-48 rounded-full bg-[#00B4D8]/8 blur-3xl pointer-events-none" />

      <div className="relative flex flex-col lg:flex-row lg:items-start lg:justify-between gap-4 mb-5">
        <div>
          <h3 className="font-bold text-white flex items-center gap-2">
            <Activity className="w-5 h-5 text-[#FF7575]" />
            Xu hướng theo thời gian
          </h3>
          <p className="text-xs text-gray-500 mt-1">
            {stats.dayCount} ngày · {formatNumber(stats.totalMentions)} mentions
            {stats.peak ? ` · đỉnh ${stats.peak.label} (${stats.peak.total})` : ''}
          </p>
        </div>

        <div className="flex flex-wrap gap-2">
          {TREND_SERIES.map((series) => {
            const hidden = hiddenSeries.has(series.key);
            return (
              <button
                key={series.key}
                type="button"
                onClick={() => toggleSeries(series.key)}
                className={`inline-flex items-center gap-2 px-3 py-1.5 rounded-full text-xs font-semibold border transition-all ${
                  hidden
                    ? 'border-white/10 bg-white/[0.02] text-gray-500'
                    : 'border-white/15 bg-white/[0.05] text-white'
                }`}
              >
                <span
                  className="w-2 h-2 rounded-full shrink-0 transition-opacity"
                  style={{ background: series.color, opacity: hidden ? 0.35 : 1 }}
                />
                {series.name}
              </button>
            );
          })}
        </div>
      </div>

      <div className="relative rounded-2xl border border-white/5 bg-[#0A101D]/40 px-2 pt-4 pb-1">
        <ResponsiveContainer width="100%" height={340}>
          <AreaChart data={trendPoints} margin={{ top: 8, right: 12, left: 4, bottom: 4 }}>
            <defs>
              {TREND_SERIES.map((series) => (
                <linearGradient key={series.key} id={`${chartId}-${series.key}`} x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor={series.color} stopOpacity={0.28} />
                  <stop offset="100%" stopColor={series.color} stopOpacity={0} />
                </linearGradient>
              ))}
            </defs>
            <CartesianGrid strokeDasharray="4 6" stroke="rgba(255,255,255,0.05)" vertical={false} />
            <XAxis
              dataKey="label"
              tick={{ fill: '#9CA3AF', fontSize: 10 }}
              axisLine={false}
              tickLine={false}
              minTickGap={28}
              interval="preserveStartEnd"
              dy={6}
            />
            <YAxis
              allowDecimals={false}
              tick={{ fill: '#6B7280', fontSize: 10 }}
              axisLine={false}
              tickLine={false}
              width={28}
            />
            <Tooltip
              content={<TrendTooltip />}
              cursor={{ stroke: 'rgba(255,255,255,0.15)', strokeWidth: 1, strokeDasharray: '4 4' }}
              wrapperStyle={{ zIndex: 30, outline: 'none' }}
              offset={18}
            />
            {TREND_SERIES.map((series) => (
              <Area
                key={series.key}
                type="monotone"
                dataKey={series.key}
                name={series.name}
                hide={hiddenSeries.has(series.key)}
                stroke={series.color}
                fill={`url(#${chartId}-${series.key})`}
                strokeWidth={2.5}
                dot={false}
                activeDot={{
                  r: 4,
                  stroke: series.color,
                  strokeWidth: 2,
                  fill: '#0A101D',
                }}
                isAnimationActive
                animationDuration={700}
              />
            ))}
          </AreaChart>
        </ResponsiveContainer>
      </div>
    </div>
  );
}

function SentimentMetricCard({
  icon,
  label,
  value,
  caption,
  accentColor,
  progress,
}: {
  icon: ReactNode;
  label: string;
  value: string;
  caption: string;
  accentColor: string;
  progress?: number;
}) {
  const valueClass = accentColor === 'white' ? 'text-white' : '';

  return (
    <div className="relative overflow-hidden rounded-2xl border border-white/5 bg-gradient-to-br from-[#151B2B] via-[#141A28] to-[#101622] p-4 sm:p-5 transition-all duration-200 hover:border-white/10 hover:shadow-lg hover:shadow-black/20 group">
      <div
        className="absolute top-0 inset-x-0 h-px opacity-70"
        style={{ background: `linear-gradient(90deg, transparent, ${accentColor}, transparent)` }}
      />
      <div
        className="absolute -top-10 -right-10 w-28 h-28 rounded-full opacity-[0.12] blur-2xl pointer-events-none transition-opacity group-hover:opacity-20"
        style={{ background: accentColor }}
      />

      <div className="relative">
        <div
          className="inline-flex p-2 rounded-xl border mb-3"
          style={{
            background: `${accentColor === 'white' ? '#ffffff' : accentColor}12`,
            borderColor: `${accentColor === 'white' ? '#ffffff' : accentColor}28`,
            color: accentColor === 'white' ? '#E2E8F0' : accentColor,
          }}
        >
          {icon}
        </div>

        <p className="text-[10px] text-gray-500 uppercase tracking-[0.14em] font-semibold">{label}</p>
        <p
          className={`text-2xl sm:text-[1.75rem] font-black tabular-nums mt-1 leading-none ${valueClass}`}
          style={valueClass ? undefined : { color: accentColor }}
        >
          {value}
        </p>

        {progress !== undefined && (
          <div className="mt-3 h-1.5 rounded-full bg-white/5 ring-1 ring-white/5 overflow-hidden">
            <div
              className="h-full rounded-full transition-all duration-700"
              style={{
                width: `${Math.min(100, Math.max(0, progress))}%`,
                background: `linear-gradient(90deg, ${accentColor}, ${accentColor}88)`,
              }}
            />
          </div>
        )}

        <p className="text-[11px] text-gray-500 mt-2 leading-relaxed">{caption}</p>
      </div>
    </div>
  );
}

const SENTIMENT_LEGEND = [
  { key: 'positive', label: 'Tích cực', color: SENTIMENT_COLORS.positive },
  { key: 'neutral', label: 'Trung lập', color: SENTIMENT_COLORS.neutral },
  { key: 'negative', label: 'Tiêu cực', color: SENTIMENT_COLORS.negative },
] as const;

function getNeutralRate(item: PlatformInsight) {
  return item.analyzed > 0 ? Math.round((item.neutral / item.analyzed) * 100) : 0;
}

function getCoveragePercent(item: PlatformInsight) {
  return item.total > 0 ? Math.round((item.analyzed / item.total) * 100) : 0;
}

function PlatformDetailCard({ item, isHighlighted }: { item: PlatformInsight; isHighlighted: boolean }) {
  const neutralRate = getNeutralRate(item);
  const coverage = getCoveragePercent(item);
  const platformColor = getPlatformChartColor(item.platform);
  const segments = [
    {
      key: 'positive',
      label: 'Tích cực',
      rate: item.positiveRate,
      count: item.positive,
      color: SENTIMENT_COLORS.positive,
    },
    {
      key: 'neutral',
      label: 'Trung lập',
      rate: neutralRate,
      count: item.neutral,
      color: SENTIMENT_COLORS.neutral,
    },
    {
      key: 'negative',
      label: 'Tiêu cực',
      rate: item.negativeRate,
      count: item.negative,
      color: SENTIMENT_COLORS.negative,
    },
  ];

  return (
    <div
      className={`relative overflow-hidden rounded-2xl border p-5 transition-all duration-200 ${
        isHighlighted
          ? 'border-white/20 bg-white/[0.05]'
          : 'border-white/5 bg-[#151B2B]/80'
      }`}
      style={{ boxShadow: `inset ${isHighlighted ? 4 : 3}px 0 0 ${platformColor}${isHighlighted ? '' : '88'}` }}
    >
      <div className="flex items-start justify-between gap-3">
        <div>
          <span className={`inline-flex px-2.5 py-1 rounded-lg text-xs font-bold ${getPlatformBadgeClass(item.platform)}`}>
            {item.label}
          </span>
          <p className="text-2xl font-black text-white tabular-nums mt-3 leading-none">{formatNumber(item.total)}</p>
          <p className="text-xs text-gray-500 mt-1.5">
            {item.share}% tổng thảo luận · {formatNumber(item.analyzed)} đã phân tích
          </p>
        </div>
        <div className="text-right shrink-0">
          {coverage === 100 ? (
            <div className="inline-flex items-center gap-1.5 rounded-full border border-emerald-500/20 bg-emerald-500/10 px-2.5 py-1 text-[11px] font-semibold text-emerald-400">
              <CheckCircle2 className="w-3.5 h-3.5" />
              Đủ phân tích
            </div>
          ) : (
            <div className="rounded-xl border border-amber-500/20 bg-amber-500/10 px-3 py-2 text-right">
              <p className="text-[10px] text-amber-300/80 uppercase tracking-wide">Độ phủ AI</p>
              <p className="text-lg font-bold text-amber-300 tabular-nums">{coverage}%</p>
            </div>
          )}
        </div>
      </div>

      <div className="mt-4 h-2.5 rounded-full overflow-hidden flex bg-white/5 ring-1 ring-white/5">
        {segments.map((segment) =>
          segment.rate > 0 ? (
            <div
              key={segment.key}
              className="h-full transition-all duration-500 first:rounded-l-full last:rounded-r-full"
              style={{
                width: `${Math.max(segment.rate, 2)}%`,
                background: `linear-gradient(90deg, ${segment.color}, ${segment.color}cc)`,
              }}
              title={`${segment.label}: ${segment.rate}%`}
            />
          ) : null
        )}
      </div>

      <div className="grid grid-cols-3 gap-2 mt-4">
        {segments.map((segment) => (
          <div
            key={segment.key}
            className="rounded-xl border border-white/5 bg-white/[0.03] px-3 py-2.5 text-center sm:text-left"
          >
            <div className="flex items-center justify-center sm:justify-start gap-1.5">
              <span className="w-2 h-2 rounded-full shrink-0" style={{ background: segment.color }} />
              <span className="text-[10px] text-gray-500 uppercase tracking-wide font-semibold">{segment.label}</span>
            </div>
            <p className="text-lg font-bold tabular-nums mt-1" style={{ color: segment.color }}>
              {segment.rate}%
            </p>
            <p className="text-[11px] text-gray-500 tabular-nums">{formatNumber(segment.count)} mentions</p>
          </div>
        ))}
      </div>

      {item.pending > 0 ? (
        <p className="text-xs text-amber-300 mt-3 leading-relaxed">
          Còn {formatNumber(item.pending)} mention chưa phân tích AI trên kênh này.
        </p>
      ) : null}
    </div>
  );
}

function PlatformComparisonPanel({ platformInsights }: { platformInsights: PlatformInsight[] }) {
  const [activePlatform, setActivePlatform] = useState<string | null>(null);
  const maxTotal = useMemo(() => Math.max(...platformInsights.map((item) => item.total), 1), [platformInsights]);

  if (platformInsights.length === 0) {
    return (
      <div className="rounded-3xl border border-white/5 bg-[#151B2B] p-6">
        <p className="text-gray-500 text-sm py-8 text-center">Chưa có dữ liệu theo nền tảng.</p>
      </div>
    );
  }

  return (
    <div className="relative overflow-hidden rounded-3xl border border-white/5 bg-gradient-to-br from-[#151B2B] via-[#12182A] to-[#0F1524] p-6">
      <div className="absolute -top-20 -right-20 w-56 h-56 rounded-full bg-[#00B4D8]/10 blur-3xl pointer-events-none" />
      <div className="absolute -bottom-16 -left-16 w-48 h-48 rounded-full bg-pink-500/10 blur-3xl pointer-events-none" />

      <div className="relative flex flex-col lg:flex-row lg:items-start lg:justify-between gap-4 mb-6">
        <div className="flex items-start gap-3">
          <div className="mt-0.5 rounded-xl border border-white/10 bg-white/[0.04] p-2">
            <BarChart3 className="w-5 h-5 text-[#00B4D8]" />
          </div>
          <div>
            <h3 className="font-bold text-white text-lg">So sánh theo nền tảng</h3>
            <p className="text-xs text-gray-500 mt-1">Lượng thảo luận và sentiment theo từng kênh</p>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          {SENTIMENT_LEGEND.map((item) => (
            <span
              key={item.key}
              className="inline-flex items-center gap-1.5 rounded-full border border-white/10 bg-white/[0.03] px-3 py-1.5 text-[11px] font-medium text-gray-300"
            >
              <span className="w-2 h-2 rounded-full" style={{ background: item.color }} />
              {item.label}
            </span>
          ))}
          <span className="inline-flex items-center gap-1.5 rounded-full border border-white/10 bg-white/[0.03] px-3 py-1.5 text-[11px] font-medium text-gray-400">
            <span className="w-2 h-2 rounded-full bg-white/20" />
            Chưa phân tích
          </span>
        </div>
      </div>

      <div className="relative space-y-3 mb-8">
        {platformInsights.map((item) => {
          const neutralRate = getNeutralRate(item);
          const coverage = getCoveragePercent(item);
          const barWidthPercent = (item.total / maxTotal) * 100;
          const isActive = activePlatform === item.platform;
          const analyzedShare = item.total > 0 ? (item.analyzed / item.total) * 100 : 0;
          const pendingShare = item.total > 0 ? (item.pending / item.total) * 100 : 0;

          return (
            <div
              key={item.platform}
              className={`rounded-2xl border px-4 py-3.5 transition-all duration-200 ${
                isActive ? 'border-white/15 bg-white/[0.04]' : 'border-white/5 bg-white/[0.02]'
              }`}
              onMouseEnter={() => setActivePlatform(item.platform)}
              onMouseLeave={() => setActivePlatform(null)}
            >
              <div className="flex flex-wrap items-center justify-between gap-2 mb-2.5">
                <div className="flex flex-wrap items-center gap-2.5">
                  <span className={`px-2.5 py-1 rounded-lg text-xs font-bold ${getPlatformBadgeClass(item.platform)}`}>
                    {item.label}
                  </span>
                  <span className="text-sm text-gray-400">
                    <span className="text-white font-semibold tabular-nums">{formatNumber(item.total)}</span> mentions
                    <span className="mx-2 text-white/15">·</span>
                    <span className="tabular-nums">{item.share}%</span> SOV
                  </span>
                </div>
                <span className="text-[11px] text-gray-500 tabular-nums">
                  AI {coverage}% · {formatNumber(item.analyzed)}/{formatNumber(item.total)}
                </span>
              </div>

              <div className="relative h-8 rounded-xl bg-white/[0.04] ring-1 ring-white/5 overflow-hidden">
                <div
                  className="absolute inset-y-0 left-0 flex overflow-hidden rounded-xl transition-all duration-500"
                  style={{
                    width: `${Math.max(barWidthPercent, item.total > 0 ? 12 : 0)}%`,
                  }}
                >
                  {item.analyzed > 0 ? (
                    <div className="flex h-full min-w-0" style={{ width: `${analyzedShare}%` }}>
                      {item.positive > 0 ? (
                        <div
                          className="h-full"
                          style={{ flex: item.positive, background: SENTIMENT_COLORS.positive }}
                          title={`Tích cực: ${formatNumber(item.positive)}`}
                        />
                      ) : null}
                      {item.neutral > 0 ? (
                        <div
                          className="h-full"
                          style={{ flex: item.neutral, background: SENTIMENT_COLORS.neutral }}
                          title={`Trung lập: ${formatNumber(item.neutral)}`}
                        />
                      ) : null}
                      {item.negative > 0 ? (
                        <div
                          className="h-full"
                          style={{ flex: item.negative, background: SENTIMENT_COLORS.negative }}
                          title={`Tiêu cực: ${formatNumber(item.negative)}`}
                        />
                      ) : null}
                    </div>
                  ) : null}
                  {item.pending > 0 ? (
                    <div
                      className="h-full bg-[repeating-linear-gradient(135deg,rgba(255,255,255,0.08)_0px,rgba(255,255,255,0.08)_4px,transparent_4px,transparent_8px)]"
                      style={{ width: `${pendingShare}%` }}
                      title={`Chưa phân tích: ${formatNumber(item.pending)}`}
                    />
                  ) : null}
                </div>
              </div>

              <div className="flex flex-wrap gap-x-4 gap-y-1 mt-2 text-[11px] tabular-nums">
                <span style={{ color: SENTIMENT_COLORS.positive }}>
                  Tích cực {item.positiveRate}% ({formatNumber(item.positive)})
                </span>
                <span style={{ color: SENTIMENT_COLORS.neutral }}>
                  Trung lập {neutralRate}% ({formatNumber(item.neutral)})
                </span>
                <span style={{ color: SENTIMENT_COLORS.negative }}>
                  Tiêu cực {item.negativeRate}% ({formatNumber(item.negative)})
                </span>
              </div>
            </div>
          );
        })}
      </div>

      <div className="relative grid grid-cols-1 lg:grid-cols-2 gap-4">
        {platformInsights.map((item) => (
          <div
            key={`card-${item.platform}`}
            onMouseEnter={() => setActivePlatform(item.platform)}
            onMouseLeave={() => setActivePlatform(null)}
          >
            <PlatformDetailCard item={item} isHighlighted={activePlatform === item.platform} />
          </div>
        ))}
      </div>
    </div>
  );
}

const ProjectSentiment = () => {
  const { workspaceId, id } = useParams();
  const wid = Number(workspaceId);
  const projectId = Number(id);

  const [summary, setSummary] = useState<SentimentSummary | null>(null);
  const [mentions, setMentions] = useState<ProjectMention[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');

  const loadSentiment = useCallback(async () => {
    if (!wid || !projectId || Number.isNaN(wid) || Number.isNaN(projectId)) return;

    setIsLoading(true);
    setErrorMessage('');
    try {
      const [sentimentData, mentionsData] = await Promise.all([
        projectApi.getSentiment(wid, projectId),
        projectApi.getMentions(wid, projectId),
      ]);
      setSummary(sentimentData);
      setMentions(mentionsData);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải phân tích sentiment.'));
    } finally {
      setIsLoading(false);
    }
  }, [wid, projectId]);

  useEffect(() => {
    loadSentiment();
  }, [loadSentiment]);

  const pieData = useMemo(() => (summary ? buildPieSlices(summary) : []), [summary]);
  const trendPoints = useMemo(() => buildTrendPoints(mentions).slice(-12), [mentions]);

  const platformInsights = useMemo<PlatformInsight[]>(() => {
    const totalMentions = mentions.length || 1;
    const map = new Map<string, PlatformInsight>();

    for (const mention of mentions) {
      const platform = mention.platform.toLowerCase();
      const current = map.get(platform) ?? {
        platform,
        label: getPlatformDisplayName(platform),
        total: 0,
        analyzed: 0,
        pending: 0,
        positive: 0,
        negative: 0,
        neutral: 0,
        positiveRate: 0,
        negativeRate: 0,
        share: 0,
      };

      current.total += 1;
      if (!mention.sentiment) {
        current.pending += 1;
      } else {
        current.analyzed += 1;
        const sentiment = mention.sentiment.toLowerCase();
        if (sentiment === 'positive') current.positive += 1;
        else if (sentiment === 'negative') current.negative += 1;
        else current.neutral += 1;
      }

      map.set(platform, current);
    }

    return Array.from(map.values())
      .map((item) => ({
        ...item,
        share: Math.round((item.total / totalMentions) * 100),
        positiveRate: item.analyzed > 0 ? Math.round((item.positive / item.analyzed) * 100) : 0,
        negativeRate: item.analyzed > 0 ? Math.round((item.negative / item.analyzed) * 100) : 0,
      }))
      .sort((a, b) => b.total - a.total);
  }, [mentions]);

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center py-32 text-gray-400 gap-4">
        <div className="relative">
          <div className="absolute inset-0 rounded-full bg-[#FF7575]/20 blur-2xl animate-pulse" />
          <Loader2 className="relative w-12 h-12 animate-spin text-[#FF7575]" />
        </div>
        <p className="text-sm">Đang tải phân tích sentiment...</p>
      </div>
    );
  }

  if (errorMessage) {
    return (
      <div className="bg-red-500/10 border border-red-500/20 text-red-300 p-4 rounded-xl flex items-center gap-3 text-sm max-w-3xl mx-auto">
        <AlertCircle className="w-5 h-5 shrink-0" />
        {errorMessage}
      </div>
    );
  }

  if (!summary) return null;

  const hasData = summary.total > 0;
  const analyzedCount = summary.positive + summary.negative + summary.neutral;
  const coveragePercent = summary.total > 0 ? Math.round((analyzedCount / summary.total) * 100) : 0;
  const dominant = getDominantSentiment(summary);
  const nsrTone = getNsrTone(summary.nsrScore);
  const nsrIcon =
    nsrTone === 'good' ? (
      <TrendingUp className="w-5 h-5 text-[#00B4D8]" />
    ) : nsrTone === 'bad' ? (
      <TrendingDown className="w-5 h-5 text-[#FF7575]" />
    ) : (
      <Minus className="w-5 h-5 text-yellow-500" />
    );
  const topRiskPlatform = [...platformInsights]
    .filter((item) => item.analyzed > 0)
    .sort((a, b) => b.negativeRate - a.negativeRate)[0];
  const topSupportPlatform = [...platformInsights]
    .filter((item) => item.analyzed > 0)
    .sort((a, b) => b.positiveRate - a.positiveRate)[0];

  return (
    <div className="animate-in fade-in duration-500 max-w-7xl mx-auto space-y-8 pb-10">
      <div className="relative overflow-hidden rounded-3xl border border-white/5 bg-gradient-to-br from-[#151B2B] via-[#12182A] to-[#0A101D] p-8">
        <div className="absolute -top-24 -right-24 w-72 h-72 bg-[#FF7575]/10 rounded-full blur-3xl pointer-events-none" />
        <div className="absolute -bottom-16 -left-16 w-56 h-56 bg-[#00B4D8]/10 rounded-full blur-3xl pointer-events-none" />

        <div className="relative flex flex-col lg:flex-row lg:items-start lg:justify-between gap-6">
          <div className="max-w-3xl">
            <h2 className="text-3xl font-bold text-white flex items-center gap-3">
              <PieChartIcon className="text-[#FF7575] w-8 h-8" />
              Sentiment
            </h2>
            <p className="text-gray-400 text-sm mt-3 leading-relaxed">
              Thái độ cộng đồng từ {formatNumber(summary.total)} mentions đã thu thập.
            </p>
          </div>

          <div className="flex items-center gap-4">
            <div
              className={`rounded-2xl border px-6 py-4 min-w-[170px] text-center backdrop-blur-sm ${
                nsrTone === 'good'
                  ? 'border-[#00B4D8]/30 bg-[#00B4D8]/5'
                  : nsrTone === 'bad'
                    ? 'border-[#FF7575]/30 bg-[#FF7575]/5'
                    : 'border-yellow-500/30 bg-yellow-500/5'
              }`}
            >
              <div className="flex items-center justify-center gap-2 mb-1">
                {nsrIcon}
                <span className="text-xs uppercase tracking-wider text-gray-500 font-semibold">Chỉ số NSR</span>
              </div>
              <p
                className={`text-4xl font-black tabular-nums ${
                  nsrTone === 'good' ? 'text-[#00B4D8]' : nsrTone === 'bad' ? 'text-[#FF7575]' : 'text-yellow-400'
                }`}
              >
                {summary.nsrScore > 0 ? '+' : ''}
                {summary.nsrScore}%
              </p>
            </div>

            <button
              type="button"
              onClick={loadSentiment}
              className="p-3 rounded-xl border border-white/10 bg-white/5 text-gray-400 hover:text-white hover:bg-white/10 transition-colors"
              title="Làm mới"
            >
              <RefreshCw className="w-5 h-5" />
            </button>
          </div>
        </div>
      </div>

      {!hasData ? (
        <div className="bg-[#151B2B] border border-white/5 rounded-3xl p-16 text-center">
          <div className="w-20 h-20 mx-auto mb-6 rounded-2xl bg-white/5 flex items-center justify-center">
            <PieChartIcon className="w-10 h-10 text-gray-600" />
          </div>
          <p className="text-gray-300 font-medium">Chưa có dữ liệu sentiment</p>
          <p className="text-gray-500 text-sm mt-2">Cào dữ liệu và chạy phân tích AI trước để xem xu hướng thái độ cộng đồng.</p>
        </div>
      ) : (
        <>
          <div className="grid grid-cols-2 xl:grid-cols-5 gap-3 sm:gap-4">
            <SentimentMetricCard
              icon={<Layers className="w-4 h-4" />}
              label="Tổng mentions"
              value={formatNumber(summary.total)}
              caption="Thảo luận từ MXH và Tin tức"
              accentColor="white"
            />
            <SentimentMetricCard
              icon={<CheckCircle2 className="w-4 h-4" />}
              label="Đã phân tích"
              value={formatNumber(analyzedCount)}
              caption={`${coveragePercent}% độ phủ AI trên tổng mentions`}
              accentColor={SENTIMENT_COLORS.positive}
              progress={coveragePercent}
            />
            <SentimentMetricCard
              icon={<Activity className="w-4 h-4" />}
              label="Cảm xúc chủ đạo"
              value={`${dominant.percent}%`}
              caption={`${dominant.label} · ${formatNumber(dominant.value)} mentions`}
              accentColor={dominant.color}
            />
            <SentimentMetricCard
              icon={<TrendingDown className="w-4 h-4" />}
              label="Tiêu cực"
              value={`${summary.negativePercent}%`}
              caption={`${formatNumber(summary.negative)} mention tiêu cực`}
              accentColor={SENTIMENT_COLORS.negative}
              progress={summary.negativePercent}
            />
            <SentimentMetricCard
              icon={<Sparkles className="w-4 h-4" />}
              label="Chưa phân tích"
              value={formatNumber(summary.unanalyzed)}
              caption={summary.unanalyzed > 0 ? 'Chạy AI để tăng độ phủ' : 'Đã phân tích đủ 100%'}
              accentColor={SENTIMENT_COLORS.neutral}
            />
          </div>

          <div className="grid grid-cols-1 xl:grid-cols-5 gap-6">
            <SentimentRatioCard
              pieData={pieData}
              dominant={dominant}
              analyzedCount={analyzedCount}
              coveragePercent={coveragePercent}
            />

            <SentimentTrendCard trendPoints={trendPoints} />
          </div>

          <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
            <div className="rounded-3xl border border-white/5 bg-[#151B2B] p-6">
              <p className="text-xs text-[#00B4D8] font-semibold uppercase tracking-wide mb-3">Điểm tương đối tốt</p>
              <p className="text-lg font-bold text-white">
                {topSupportPlatform
                  ? `${topSupportPlatform.label} đang có tỉ lệ tích cực cao nhất`
                  : 'Chưa đủ dữ liệu để xác định kênh tốt nhất'}
              </p>
              <p className="text-sm text-gray-400 mt-2 leading-relaxed">
                {topSupportPlatform
                  ? `${topSupportPlatform.positiveRate}% mentions trên kênh này là tích cực.`
                  : 'Cần thêm dữ liệu đã phân tích.'}
              </p>
            </div>

            <div className="rounded-3xl border border-[#FF7575]/20 bg-[#151B2B] p-6">
              <p className="text-xs text-[#FF7575] font-semibold uppercase tracking-wide mb-3">Điểm cần cảnh báo</p>
              <p className="text-lg font-bold text-white">
                {topRiskPlatform
                  ? `${topRiskPlatform.label} có tỉ lệ tiêu cực cao nhất`
                  : 'Chưa phát hiện kênh rủi ro rõ ràng'}
              </p>
              <p className="text-sm text-gray-400 mt-2 leading-relaxed">
                {topRiskPlatform
                  ? `${topRiskPlatform.negativeRate}% mentions trên kênh này là tiêu cực.`
                  : 'Chưa có kênh rủi ro nổi bật.'}
              </p>
            </div>

            <div className="rounded-3xl border border-white/10 bg-[#151B2B] p-6">
              <p className="text-xs text-gray-400 font-semibold uppercase tracking-wide mb-3">Gợi ý hành động</p>
              <p className="text-lg font-bold text-white">
                {summary.unanalyzed > 0 ? 'Tăng độ phủ AI trước khi đọc xu hướng' : 'Có thể tin hơn vào xu hướng hiện tại'}
              </p>
              <p className="text-sm text-gray-400 mt-2 leading-relaxed">
                {summary.unanalyzed > 0
                  ? `Còn ${formatNumber(summary.unanalyzed)} mentions chưa phân tích.`
                  : 'Toàn bộ mentions đã có sentiment.'}
              </p>
            </div>
          </div>

          <PlatformComparisonPanel platformInsights={platformInsights} />
        </>
      )}
    </div>
  );
};

export default ProjectSentiment;
