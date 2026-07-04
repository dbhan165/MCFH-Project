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
import { formatNumber, SENTIMENT_COLORS, getPlatformDisplayName } from '../utils/sentimentHelpers';
import { buildPieSlices, buildTrendPoints, getNsrTone, type PieSlice } from '../utils/sentimentChartData';

const CHART_TOOLTIP_STYLE = {
  backgroundColor: '#0A101D',
  border: '1px solid rgba(255,255,255,0.08)',
  borderRadius: 12,
  boxShadow: '0 12px 40px rgba(0,0,0,0.45)',
  padding: '10px 14px',
};

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

function ChartTooltip({
  active,
  payload,
  label,
}: {
  active?: boolean;
  payload?: Array<{ name?: string; value?: number; color?: string }>;
  label?: string;
}) {
  if (!active || !payload?.length) return null;
  return (
    <div style={CHART_TOOLTIP_STYLE}>
      {label ? <p className="text-xs text-gray-500 mb-2">{label}</p> : null}
      {payload.map((item) => (
        <div key={item.name} className="flex items-center gap-2 text-sm text-white">
          <span className="w-2.5 h-2.5 rounded-full" style={{ background: item.color }} />
          <span className="text-gray-400">{item.name}:</span>
          <span className="font-semibold">{item.value ?? 0}</span>
        </div>
      ))}
    </div>
  );
}

function PieTooltip({ active, payload }: { active?: boolean; payload?: Array<{ payload: PieSlice }> }) {
  if (!active || !payload?.[0]) return null;
  const item = payload[0].payload;
  return (
    <div style={CHART_TOOLTIP_STYLE}>
      <p className="text-sm font-semibold text-white">{item.name}</p>
      <p className="text-xs text-gray-400 mt-1">
        {item.value} mentions da phan tich · {item.percent}%
      </p>
    </div>
  );
}

function MetricCard({
  label,
  value,
  caption,
  accentClass,
}: {
  label: string;
  value: string;
  caption: string;
  accentClass: string;
}) {
  return (
    <div className="rounded-2xl border border-white/5 bg-[#151B2B] p-5">
      <p className="text-xs text-gray-500 uppercase tracking-wide font-semibold">{label}</p>
      <p className={`text-3xl font-bold mt-2 tabular-nums ${accentClass}`}>{value}</p>
      <p className="text-xs text-gray-500 mt-2">{caption}</p>
    </div>
  );
}

function PlatformCard({ item }: { item: PlatformInsight }) {
  return (
    <div className="rounded-2xl border border-white/5 bg-[#151B2B] p-5">
      <div className="flex items-center justify-between gap-3">
        <div>
          <p className="text-sm font-semibold text-white">{item.label}</p>
          <p className="text-xs text-gray-500 mt-1">
            {formatNumber(item.total)} mentions · {item.share}% tong luong thao luan
          </p>
        </div>
        <div className="text-right">
          <p className="text-xs text-gray-500 uppercase tracking-wide">Độ phủ AI</p>
          <p className="text-lg font-bold text-white tabular-nums">
            {item.total > 0 ? Math.round((item.analyzed / item.total) * 100) : 0}%
          </p>
        </div>
      </div>

      <div className="grid grid-cols-3 gap-3 mt-4 text-center">
        <div className="rounded-xl bg-[#0A101D] border border-white/5 px-3 py-3">
          <p className="text-[11px] text-gray-500 uppercase tracking-wide">Tích cực</p>
          <p className="text-lg font-bold text-[#00B4D8] tabular-nums mt-1">{item.positiveRate}%</p>
        </div>
        <div className="rounded-xl bg-[#0A101D] border border-white/5 px-3 py-3">
          <p className="text-[11px] text-gray-500 uppercase tracking-wide">Trung lập</p>
          <p className="text-lg font-bold text-yellow-400 tabular-nums mt-1">
            {item.analyzed > 0 ? Math.round((item.neutral / item.analyzed) * 100) : 0}%
          </p>
        </div>
        <div className="rounded-xl bg-[#0A101D] border border-white/5 px-3 py-3">
          <p className="text-[11px] text-gray-500 uppercase tracking-wide">Tiêu cực</p>
          <p className="text-lg font-bold text-[#FF7575] tabular-nums mt-1">{item.negativeRate}%</p>
        </div>
      </div>

      <div className="mt-4 space-y-2">
        <div>
          <div className="flex items-center justify-between text-xs text-gray-400 mb-1">
            <span>Độ phủ phân tích</span>
            <span className="tabular-nums">
              {formatNumber(item.analyzed)} / {formatNumber(item.total)}
            </span>
          </div>
          <div className="h-2 rounded-full bg-[#0A101D] overflow-hidden">
            <div
              className="h-full rounded-full bg-white/70"
              style={{ width: `${item.total > 0 ? (item.analyzed / item.total) * 100 : 0}%` }}
            />
          </div>
        </div>

        {item.pending > 0 ? (
          <p className="text-xs text-amber-300">
            Còn {formatNumber(item.pending)} mention chưa phân tích AI trên kênh này.
          </p>
        ) : (
          <p className="text-xs text-gray-500">Kênh này đã có độ phủ phân tích đầy đủ.</p>
        )}
      </div>
    </div>
  );
}

function getDominantSentiment(summary: SentimentSummary) {
  const candidates = [
    { key: 'positive', label: 'Tích cực', value: summary.positive, color: '#00B4D8' },
    { key: 'negative', label: 'Tiêu cực', value: summary.negative, color: '#FF7575' },
    { key: 'neutral', label: 'Trung lập', value: summary.neutral, color: '#EAB308' },
  ];
  return [...candidates].sort((a, b) => b.value - a.value)[0];
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

  const platformChartData = useMemo(
    () =>
      platformInsights.map((item) => ({
        label: item.label,
        positive: item.positive,
        neutral: item.neutral,
        negative: item.negative,
      })),
    [platformInsights]
  );

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
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-white/5 border border-white/10 text-xs text-gray-400 mb-4">
              <Sparkles className="w-3.5 h-3.5 text-[#FF7575]" />
              AI sentiment summary
            </div>
            <h2 className="text-3xl font-bold text-white flex items-center gap-3">
              <PieChartIcon className="text-[#FF7575] w-8 h-8" />
              Sentiment Chart
            </h2>
            <p className="text-gray-400 text-sm mt-3 leading-relaxed">
              Đọc nhanh thái độ cộng đồng từ {formatNumber(summary.total)} mention đã thu thập. Biểu đồ này ưu tiên bài đã
              phân tích AI, và xu hướng theo ngày ưu tiên ngày đăng bài, fallback sang ngày hệ thống cào dữ liệu.
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
                <span className="text-xs uppercase tracking-wider text-gray-500 font-semibold">NSR score</span>
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
          <div className="grid grid-cols-2 xl:grid-cols-5 gap-4">
            <MetricCard
              label="Tổng mentions"
              value={formatNumber(summary.total)}
              caption="Lượng thảo luận đã thu thập"
              accentClass="text-white"
            />
            <MetricCard
              label="Đã phân tích"
              value={formatNumber(analyzedCount)}
              caption={`Độ phủ ${coveragePercent}% trên tổng số mentions`}
              accentClass="text-[#00B4D8]"
            />
            <MetricCard
              label="Cảm xúc chủ đạo"
              value={`${dominant.label} ${dominant.value}`}
              caption="Loại sentiment xuất hiện nhiều nhất"
              accentClass="text-white"
            />
            <MetricCard
              label="Tiêu cực"
              value={`${summary.negativePercent}%`}
              caption={`${formatNumber(summary.negative)} mention tiêu cực`}
              accentClass="text-[#FF7575]"
            />
            <MetricCard
              label="Chưa phân tích"
              value={formatNumber(summary.unanalyzed)}
              caption="Cần chạy thêm AI để đủ độ phủ"
              accentClass="text-yellow-400"
            />
          </div>

          <div className="grid grid-cols-1 xl:grid-cols-5 gap-6">
            <div className="xl:col-span-2 bg-[#151B2B] border border-white/5 rounded-3xl p-6 flex flex-col">
              <div className="flex items-start justify-between gap-4">
                <div>
                  <h3 className="font-bold text-white">Tỉ lệ sentiment đã phân tích</h3>
                  <p className="text-xs text-gray-500 mt-1">Chỉ tính trên nhóm bài đã có kết quả AI / rule-based</p>
                </div>
                <div className="text-right">
                  <p className="text-xs text-gray-500 uppercase tracking-wide">Độ phủ</p>
                  <p className="text-lg font-bold text-white tabular-nums">{coveragePercent}%</p>
                </div>
              </div>

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
                        <p className="text-4xl font-black text-white tabular-nums">{dominant.value}</p>
                        <p className="text-xs mt-1" style={{ color: dominant.color }}>
                          {dominant.label} đang chiếm ưu thế
                        </p>
                      </div>
                    </div>
                  </>
                )}
              </div>

              <div className="space-y-3 mt-4">
                {pieData.map((slice) => (
                  <div key={slice.key} className="rounded-2xl bg-[#0A101D] border border-white/5 p-3">
                    <div className="flex items-center justify-between text-sm">
                      <div className="flex items-center gap-2">
                        <span className="w-3 h-3 rounded-full" style={{ background: slice.color }} />
                        <span className="text-gray-300">{slice.name}</span>
                      </div>
                      <span className="font-bold text-white tabular-nums">{slice.percent}%</span>
                    </div>
                    <p className="text-xs text-gray-500 mt-1">{formatNumber(slice.value)} mentions trong nhóm đã phân tích</p>
                  </div>
                ))}
              </div>
            </div>

            <div className="xl:col-span-3 bg-[#151B2B] border border-white/5 rounded-3xl p-6">
              <div className="flex items-start justify-between gap-4 mb-6">
                <div>
                  <h3 className="font-bold text-white flex items-center gap-2">
                    <Activity className="w-5 h-5 text-[#FF7575]" />
                    Xu hướng theo thời gian
                  </h3>
                  <p className="text-xs text-gray-500 mt-1">Ưu tiên ngày đăng bài, fallback ngày cào dữ liệu nếu thiếu metadata</p>
                </div>
              </div>

              {trendPoints.length < 2 ? (
                <div className="h-[320px] flex flex-col items-center justify-center text-gray-500 text-sm border border-dashed border-white/10 rounded-2xl bg-[#0A101D]/50">
                  <Activity className="w-10 h-10 text-gray-600 mb-3" />
                  Cần thêm dữ liệu nhiều ngày để hiển thị xu hướng.
                </div>
              ) : (
                <ResponsiveContainer width="100%" height={320}>
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
                        <stop offset="0%" stopColor={SENTIMENT_COLORS.neutral} stopOpacity={0.25} />
                        <stop offset="100%" stopColor={SENTIMENT_COLORS.neutral} stopOpacity={0} />
                      </linearGradient>
                    </defs>
                    <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.04)" vertical={false} />
                    <XAxis dataKey="label" tick={{ fill: '#6B7280', fontSize: 11 }} axisLine={false} tickLine={false} />
                    <YAxis allowDecimals={false} tick={{ fill: '#6B7280', fontSize: 11 }} axisLine={false} tickLine={false} />
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
                  ? `${topSupportPlatform.positiveRate}% số bài đã phân tích trên kênh này nghiêng về hướng tích cực.`
                  : 'Cần thêm mention đã phân tích để đưa ra kết luận rõ ràng hơn.'}
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
                  ? `${topRiskPlatform.negativeRate}% sentiment đã phân tích trên kênh này là tiêu cực. Nên vào Mentions để đọc bài gốc và comment liên quan.`
                  : 'Dữ liệu hiện tại chưa cho thấy một kênh nổi bật về rủi ro sentiment.'}
              </p>
            </div>

            <div className="rounded-3xl border border-white/10 bg-[#151B2B] p-6">
              <p className="text-xs text-gray-400 font-semibold uppercase tracking-wide mb-3">Gợi ý hành động</p>
              <p className="text-lg font-bold text-white">
                {summary.unanalyzed > 0 ? 'Tăng độ phủ AI trước khi đọc xu hướng' : 'Có thể tin hơn vào xu hướng hiện tại'}
              </p>
              <p className="text-sm text-gray-400 mt-2 leading-relaxed">
                {summary.unanalyzed > 0
                  ? `Còn ${formatNumber(summary.unanalyzed)} mention chưa phân tích, nên các tỉ lệ hiện tại chưa phản ánh toàn bộ tập dữ liệu.`
                  : 'Toàn bộ dữ liệu đã có phân tích sentiment, phù hợp để tiếp tục so sánh theo kênh và đọc insight.'}
              </p>
            </div>
          </div>

          <div className="bg-[#151B2B] border border-white/5 rounded-3xl p-6">
            <div className="flex items-center gap-2 mb-1">
              <BarChart3 className="w-5 h-5 text-[#00B4D8]" />
              <h3 className="font-bold text-white">So sánh theo nền tảng</h3>
            </div>
            <p className="text-xs text-gray-500 mb-6">Kết hợp lượng thảo luận, độ phủ AI và tỉ lệ sentiment trên từng kênh</p>

            {platformInsights.length === 0 ? (
              <p className="text-gray-500 text-sm py-8 text-center">Chưa có dữ liệu theo nền tảng.</p>
            ) : (
              <>
                <ResponsiveContainer width="100%" height={Math.max(220, platformChartData.length * 76)}>
                  <BarChart data={platformChartData} layout="vertical" margin={{ top: 0, right: 24, left: 8, bottom: 0 }}>
                    <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.04)" horizontal={false} />
                    <XAxis type="number" allowDecimals={false} tick={{ fill: '#6B7280', fontSize: 11 }} axisLine={false} tickLine={false} />
                    <YAxis
                      type="category"
                      dataKey="label"
                      width={100}
                      tick={{ fill: '#D1D5DB', fontSize: 12, fontWeight: 600 }}
                      axisLine={false}
                      tickLine={false}
                    />
                    <Tooltip content={<ChartTooltip />} cursor={{ fill: 'rgba(255,255,255,0.03)' }} />
                    <Bar dataKey="positive" name="Tích cực" stackId="s" fill={SENTIMENT_COLORS.positive} />
                    <Bar dataKey="neutral" name="Trung lập" stackId="s" fill={SENTIMENT_COLORS.neutral} />
                    <Bar dataKey="negative" name="Tiêu cực" stackId="s" fill={SENTIMENT_COLORS.negative} radius={[0, 6, 6, 0]} />
                  </BarChart>
                </ResponsiveContainer>

                <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 mt-6">
                  {platformInsights.map((item) => (
                    <PlatformCard key={item.platform} item={item} />
                  ))}
                </div>
              </>
            )}
          </div>
        </>
      )}
    </div>
  );
};

export default ProjectSentiment;
