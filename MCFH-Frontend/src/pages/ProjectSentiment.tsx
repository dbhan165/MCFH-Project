import { useCallback, useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  PieChart as PieChartIcon,
  TrendingUp,
  TrendingDown,
  Minus,
  Loader2,
  AlertCircle,
  RefreshCw,
  Sparkles,
  BarChart3,
  Activity,
} from 'lucide-react';
import {
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
  Tooltip,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  AreaChart,
  Area,
} from 'recharts';
import { projectApi } from '../api/projectApi';
import type { ProjectMention, SentimentSummary } from '../types/project';
import { extractApiError } from '../utils/authStorage';
import { formatNumber, SENTIMENT_COLORS } from '../utils/sentimentHelpers';
import {
  buildPieSlices,
  buildPlatformRows,
  buildTrendPoints,
  getNsrTone,
  type PieSlice,
} from '../utils/sentimentChartData';

const CHART_TOOLTIP_STYLE = {
  backgroundColor: '#0A101D',
  border: '1px solid rgba(255,255,255,0.08)',
  borderRadius: 12,
  boxShadow: '0 12px 40px rgba(0,0,0,0.45)',
  padding: '10px 14px',
};

function ChartTooltip({
  active,
  payload,
  label,
}: {
  active?: boolean;
  payload?: { name: string; value: number; color: string }[];
  label?: string;
}) {
  if (!active || !payload?.length) return null;
  return (
    <div style={CHART_TOOLTIP_STYLE}>
      {label && <p className="text-xs text-gray-500 mb-2">{label}</p>}
      {payload.map((item) => (
        <div key={item.name} className="flex items-center gap-2 text-sm text-white">
          <span className="w-2.5 h-2.5 rounded-full" style={{ background: item.color }} />
          <span className="text-gray-400">{item.name}:</span>
          <span className="font-semibold">{item.value}</span>
        </div>
      ))}
    </div>
  );
}

function PieTooltip({ active, payload }: { active?: boolean; payload?: { payload: PieSlice }[] }) {
  if (!active || !payload?.[0]) return null;
  const item = payload[0].payload;
  return (
    <div style={CHART_TOOLTIP_STYLE}>
      <p className="text-sm font-semibold text-white">{item.name}</p>
      <p className="text-xs text-gray-400 mt-1">
        {item.value} mentions · {item.percent}%
      </p>
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
  const platformRows = useMemo(() => buildPlatformRows(mentions), [mentions]);
  const trendPoints = useMemo(() => buildTrendPoints(mentions), [mentions]);
  const analyzedCount = summary ? summary.positive + summary.negative + summary.neutral : 0;
  const nsrTone = summary ? getNsrTone(summary.nsrScore) : 'neutral';

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center py-32 text-gray-400 gap-4">
        <div className="relative">
          <div className="absolute inset-0 rounded-full bg-[#FF7575]/20 blur-2xl animate-pulse" />
          <Loader2 className="relative w-12 h-12 animate-spin text-[#FF7575]" />
        </div>
        <p className="text-sm">Đang tải biểu đồ sentiment...</p>
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
  const nsrIcon =
    nsrTone === 'good' ? (
      <TrendingUp className="w-5 h-5 text-[#00B4D8]" />
    ) : nsrTone === 'bad' ? (
      <TrendingDown className="w-5 h-5 text-[#FF7575]" />
    ) : (
      <Minus className="w-5 h-5 text-yellow-500" />
    );

  return (
    <div className="animate-in fade-in duration-500 max-w-7xl mx-auto space-y-8 pb-10">
      {/* Header */}
      <div className="relative overflow-hidden rounded-3xl border border-white/5 bg-gradient-to-br from-[#151B2B] via-[#12182A] to-[#0A101D] p-8">
        <div className="absolute -top-24 -right-24 w-72 h-72 bg-[#FF7575]/10 rounded-full blur-3xl pointer-events-none" />
        <div className="absolute -bottom-16 -left-16 w-56 h-56 bg-[#00B4D8]/10 rounded-full blur-3xl pointer-events-none" />

        <div className="relative flex flex-col lg:flex-row lg:items-center lg:justify-between gap-6">
          <div>
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-white/5 border border-white/10 text-xs text-gray-400 mb-4">
              <Sparkles className="w-3.5 h-3.5 text-[#FF7575]" />
              AI Sentiment Analytics
            </div>
            <h2 className="text-3xl font-bold text-white flex items-center gap-3">
              <PieChartIcon className="text-[#FF7575] w-8 h-8" />
              Phân tích Cảm xúc
            </h2>
            <p className="text-gray-400 text-sm mt-2 max-w-xl">
              Trực quan hóa thái độ cộng đồng từ {formatNumber(summary.total)} mentions đã thu thập
              {analyzedCount > 0 && ` · ${formatNumber(analyzedCount)} đã phân tích AI`}
            </p>
          </div>

          <div className="flex items-center gap-4">
            <div
              className={`rounded-2xl border px-6 py-4 min-w-[160px] text-center backdrop-blur-sm ${
                nsrTone === 'good'
                  ? 'border-[#00B4D8]/30 bg-[#00B4D8]/5'
                  : nsrTone === 'bad'
                    ? 'border-[#FF7575]/30 bg-[#FF7575]/5'
                    : 'border-yellow-500/30 bg-yellow-500/5'
              }`}
            >
              <div className="flex items-center justify-center gap-2 mb-1">
                {nsrIcon}
                <span className="text-xs uppercase tracking-wider text-gray-500 font-semibold">NSR Score</span>
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
          <p className="text-gray-500 text-sm mt-2">Cào dữ liệu và chạy Phân tích AI trước để xem biểu đồ.</p>
        </div>
      ) : (
        <>
          {/* Stat cards */}
          <div className="grid grid-cols-2 lg:grid-cols-4 gap-4">
            {[
              {
                label: 'Tích cực',
                value: summary.positive,
                pct: summary.positivePercent,
                color: SENTIMENT_COLORS.positive,
                glow: 'shadow-[0_0_30px_rgba(0,180,216,0.15)]',
              },
              {
                label: 'Tiêu cực',
                value: summary.negative,
                pct: summary.negativePercent,
                color: SENTIMENT_COLORS.negative,
                glow: 'shadow-[0_0_30px_rgba(255,117,117,0.15)]',
              },
              {
                label: 'Trung lập',
                value: summary.neutral,
                pct: summary.neutralPercent,
                color: SENTIMENT_COLORS.neutral,
                glow: 'shadow-[0_0_30px_rgba(234,179,8,0.12)]',
              },
              {
                label: 'Chưa phân tích',
                value: summary.unanalyzed,
                pct: summary.total > 0 ? Math.round((summary.unanalyzed / summary.total) * 100) : 0,
                color: SENTIMENT_COLORS.pending,
                glow: '',
              },
            ].map((card) => (
              <div
                key={card.label}
                className={`relative overflow-hidden rounded-2xl border border-white/5 bg-[#151B2B] p-5 ${card.glow}`}
              >
                <div
                  className="absolute top-0 left-0 w-full h-1 rounded-t-2xl"
                  style={{ background: card.color }}
                />
                <p className="text-xs text-gray-500 uppercase tracking-wide font-semibold">{card.label}</p>
                <p className="text-3xl font-bold text-white mt-2 tabular-nums">{formatNumber(card.value)}</p>
                <p className="text-sm mt-1 font-medium" style={{ color: card.color }}>
                  {card.pct}%
                </p>
              </div>
            ))}
          </div>

          {/* Donut + legend */}
          <div className="grid grid-cols-1 xl:grid-cols-5 gap-6">
            <div className="xl:col-span-2 bg-[#151B2B] border border-white/5 rounded-3xl p-6 flex flex-col">
              <h3 className="font-bold text-white mb-1">Tỷ lệ cảm xúc</h3>
              <p className="text-xs text-gray-500 mb-4">Phân bổ trên các bài đã phân tích</p>

              <div className="relative flex-1 min-h-[280px] flex items-center justify-center">
                {pieData.length === 0 ? (
                  <p className="text-gray-500 text-sm">Chưa có bài nào được phân tích sentiment.</p>
                ) : (
                  <>
                    <ResponsiveContainer width="100%" height={280}>
                      <PieChart>
                        <defs>
                          {pieData.map((slice) => (
                            <linearGradient key={slice.key} id={`grad-${slice.key}`} x1="0" y1="0" x2="1" y2="1">
                              <stop offset="0%" stopColor={slice.color} stopOpacity={1} />
                              <stop offset="100%" stopColor={slice.color} stopOpacity={0.65} />
                            </linearGradient>
                          ))}
                        </defs>
                        <Pie
                          data={pieData}
                          cx="50%"
                          cy="50%"
                          innerRadius={78}
                          outerRadius={118}
                          paddingAngle={3}
                          dataKey="value"
                          stroke="none"
                          animationBegin={0}
                          animationDuration={900}
                        >
                          {pieData.map((slice) => (
                            <Cell key={slice.key} fill={`url(#grad-${slice.key})`} />
                          ))}
                        </Pie>
                        <Tooltip content={<PieTooltip />} />
                      </PieChart>
                    </ResponsiveContainer>

                    <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
                      <div className="text-center">
                        <p className="text-4xl font-black text-white tabular-nums">{summary.positivePercent}%</p>
                        <p className="text-xs text-gray-500 mt-1">Tích cực</p>
                      </div>
                    </div>
                  </>
                )}
              </div>

              <div className="grid grid-cols-3 gap-2 mt-4">
                {pieData.map((slice) => (
                  <div
                    key={slice.key}
                    className="flex flex-col items-center p-3 rounded-xl bg-[#0A101D] border border-white/5"
                  >
                    <span className="w-3 h-3 rounded-full mb-2" style={{ background: slice.color }} />
                    <span className="text-lg font-bold text-white tabular-nums">{slice.value}</span>
                    <span className="text-[10px] text-gray-500 text-center leading-tight mt-0.5">{slice.name}</span>
                  </div>
                ))}
              </div>
            </div>

            {/* Trend area chart */}
            <div className="xl:col-span-3 bg-[#151B2B] border border-white/5 rounded-3xl p-6">
              <div className="flex items-start justify-between mb-6">
                <div>
                  <h3 className="font-bold text-white flex items-center gap-2">
                    <Activity className="w-5 h-5 text-[#FF7575]" />
                    Xu hướng theo thời gian
                  </h3>
                  <p className="text-xs text-gray-500 mt-1">Số mentions theo ngày thu thập</p>
                </div>
              </div>

              {trendPoints.length < 2 ? (
                <div className="h-[280px] flex flex-col items-center justify-center text-gray-500 text-sm border border-dashed border-white/10 rounded-2xl bg-[#0A101D]/50">
                  <Activity className="w-10 h-10 text-gray-600 mb-3" />
                  Cần thêm dữ liệu nhiều ngày để hiển thị xu hướng.
                </div>
              ) : (
                <ResponsiveContainer width="100%" height={280}>
                  <AreaChart data={trendPoints} margin={{ top: 8, right: 8, left: -16, bottom: 0 }}>
                    <defs>
                      <linearGradient id="areaPositive" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="0%" stopColor={SENTIMENT_COLORS.positive} stopOpacity={0.35} />
                        <stop offset="100%" stopColor={SENTIMENT_COLORS.positive} stopOpacity={0} />
                      </linearGradient>
                      <linearGradient id="areaNegative" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="0%" stopColor={SENTIMENT_COLORS.negative} stopOpacity={0.35} />
                        <stop offset="100%" stopColor={SENTIMENT_COLORS.negative} stopOpacity={0} />
                      </linearGradient>
                      <linearGradient id="areaNeutral" x1="0" y1="0" x2="0" y2="1">
                        <stop offset="0%" stopColor={SENTIMENT_COLORS.neutral} stopOpacity={0.3} />
                        <stop offset="100%" stopColor={SENTIMENT_COLORS.neutral} stopOpacity={0} />
                      </linearGradient>
                    </defs>
                    <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.04)" vertical={false} />
                    <XAxis
                      dataKey="label"
                      tick={{ fill: '#6B7280', fontSize: 11 }}
                      axisLine={false}
                      tickLine={false}
                    />
                    <YAxis
                      allowDecimals={false}
                      tick={{ fill: '#6B7280', fontSize: 11 }}
                      axisLine={false}
                      tickLine={false}
                    />
                    <Tooltip content={<ChartTooltip />} />
                    <Area
                      type="monotone"
                      dataKey="positive"
                      name="Tích cực"
                      stackId="1"
                      stroke={SENTIMENT_COLORS.positive}
                      fill="url(#areaPositive)"
                      strokeWidth={2}
                    />
                    <Area
                      type="monotone"
                      dataKey="negative"
                      name="Tiêu cực"
                      stackId="1"
                      stroke={SENTIMENT_COLORS.negative}
                      fill="url(#areaNegative)"
                      strokeWidth={2}
                    />
                    <Area
                      type="monotone"
                      dataKey="neutral"
                      name="Trung lập"
                      stackId="1"
                      stroke={SENTIMENT_COLORS.neutral}
                      fill="url(#areaNeutral)"
                      strokeWidth={2}
                    />
                  </AreaChart>
                </ResponsiveContainer>
              )}
            </div>
          </div>

          {/* Platform stacked bar */}
          <div className="bg-[#151B2B] border border-white/5 rounded-3xl p-6">
            <div className="flex items-center gap-2 mb-1">
              <BarChart3 className="w-5 h-5 text-[#00B4D8]" />
              <h3 className="font-bold text-white">Sentiment theo nền tảng</h3>
            </div>
            <p className="text-xs text-gray-500 mb-6">So sánh tích cực / tiêu cực / trung lập trên từng kênh</p>

            {platformRows.length === 0 ? (
              <p className="text-gray-500 text-sm py-8 text-center">Chưa có dữ liệu theo nền tảng.</p>
            ) : (
              <>
                <ResponsiveContainer width="100%" height={Math.max(200, platformRows.length * 72)}>
                  <BarChart
                    data={platformRows}
                    layout="vertical"
                    margin={{ top: 0, right: 24, left: 8, bottom: 0 }}
                    barCategoryGap="28%"
                  >
                    <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.04)" horizontal={false} />
                    <XAxis type="number" allowDecimals={false} tick={{ fill: '#6B7280', fontSize: 11 }} axisLine={false} tickLine={false} />
                    <YAxis
                      type="category"
                      dataKey="label"
                      width={88}
                      tick={{ fill: '#D1D5DB', fontSize: 12, fontWeight: 600 }}
                      axisLine={false}
                      tickLine={false}
                    />
                    <Tooltip content={<ChartTooltip />} cursor={{ fill: 'rgba(255,255,255,0.03)' }} />
                    <Bar dataKey="positive" name="Tích cực" stackId="s" fill={SENTIMENT_COLORS.positive} radius={[0, 0, 0, 0]} />
                    <Bar dataKey="neutral" name="Trung lập" stackId="s" fill={SENTIMENT_COLORS.neutral} />
                    <Bar dataKey="negative" name="Tiêu cực" stackId="s" fill={SENTIMENT_COLORS.negative} radius={[0, 6, 6, 0]} />
                  </BarChart>
                </ResponsiveContainer>

                <div className="flex flex-wrap justify-center gap-6 mt-4 pt-4 border-t border-white/5">
                  {[
                    { label: 'Tích cực', color: SENTIMENT_COLORS.positive },
                    { label: 'Trung lập', color: SENTIMENT_COLORS.neutral },
                    { label: 'Tiêu cực', color: SENTIMENT_COLORS.negative },
                  ].map((item) => (
                    <div key={item.label} className="flex items-center gap-2 text-xs text-gray-400">
                      <span className="w-3 h-3 rounded-sm" style={{ background: item.color }} />
                      {item.label}
                    </div>
                  ))}
                </div>
              </>
            )}
          </div>

          {/* Insight footer */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <div className="rounded-2xl border border-[#00B4D8]/20 bg-[#00B4D8]/5 p-5">
              <p className="text-xs text-[#00B4D8] font-semibold uppercase tracking-wide mb-2">Điểm mạnh</p>
              <p className="text-sm text-gray-300 leading-relaxed">
                {summary.positivePercent >= summary.negativePercent
                  ? `${summary.positivePercent}% nội dung mang thái độ tích cực — cộng đồng đang ủng hộ chủ đề này.`
                  : 'Tỷ lệ tích cực thấp hơn tiêu cực — cần theo dõi phản hồi gần đây.'}
              </p>
            </div>
            <div className="rounded-2xl border border-[#FF7575]/20 bg-[#FF7575]/5 p-5">
              <p className="text-xs text-[#FF7575] font-semibold uppercase tracking-wide mb-2">Rủi ro</p>
              <p className="text-sm text-gray-300 leading-relaxed">
                {summary.negative > 0
                  ? `${summary.negative} mentions tiêu cực (${summary.negativePercent}%) — xem tab Mentions để xử lý.`
                  : 'Chưa phát hiện mentions tiêu cực đáng kể.'}
              </p>
            </div>
            <div className="rounded-2xl border border-white/10 bg-white/[0.02] p-5">
              <p className="text-xs text-gray-500 font-semibold uppercase tracking-wide mb-2">Gợi ý</p>
              <p className="text-sm text-gray-300 leading-relaxed">
                {summary.unanalyzed > 0
                  ? `Còn ${summary.unanalyzed} bài chưa phân tích — chạy «Phân tích AI» trên Mentions.`
                  : 'Dữ liệu đã đầy đủ. Cào lại định kỳ để cập nhật xu hướng.'}
              </p>
            </div>
          </div>
        </>
      )}
    </div>
  );
};

export default ProjectSentiment;
