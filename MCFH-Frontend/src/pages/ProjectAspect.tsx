import { useCallback, useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  BarChart2,
  CheckCircle2,
  AlertCircle,
  Loader2,
  RefreshCw,
  Sparkles,
  Target,
  Layers,
} from 'lucide-react';
import {
  ResponsiveContainer,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  RadarChart,
  PolarGrid,
  PolarAngleAxis,
  Radar,
} from 'recharts';
import { projectApi } from '../api/projectApi';
import type { AspectAnalysis, AspectStats } from '../types/project';
import { extractApiError } from '../utils/authStorage';
import { formatNumber, SENTIMENT_COLORS } from '../utils/sentimentHelpers';

const TOOLTIP_STYLE = {
  backgroundColor: '#0A101D',
  border: '1px solid rgba(255,255,255,0.08)',
  borderRadius: 12,
  padding: '10px 14px',
};

const ProjectAspect = () => {
  const { workspaceId, id } = useParams();
  const wid = Number(workspaceId);
  const projectId = Number(id);

  const [data, setData] = useState<AspectAnalysis | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');

  const loadData = useCallback(async () => {
    if (!wid || !projectId || Number.isNaN(wid) || Number.isNaN(projectId)) return;

    setIsLoading(true);
    setErrorMessage('');
    try {
      setData(await projectApi.getAspects(wid, projectId));
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải phân tích khía cạnh.'));
    } finally {
      setIsLoading(false);
    }
  }, [wid, projectId]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const stackData = useMemo(
    () =>
      (data?.aspects ?? []).map((a) => ({
        label: a.label,
        'Tích cực': a.positive,
        'Trung lập': a.neutral,
        'Tiêu cực': a.negative,
        total: a.totalMentions,
      })),
    [data]
  );

  const radarData = useMemo(
    () =>
      (data?.aspects ?? []).map((a) => ({
        aspect: a.label.length > 14 ? `${a.label.slice(0, 12)}…` : a.label,
        fullLabel: a.label,
        positive: a.positivePercent,
        negative: a.negativePercent,
      })),
    [data]
  );

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center py-32 text-gray-400 gap-4">
        <div className="relative">
          <div className="absolute inset-0 rounded-full bg-emerald-500/20 blur-2xl animate-pulse" />
          <Loader2 className="relative w-12 h-12 animate-spin text-emerald-400" />
        </div>
        <p className="text-sm">Đang phân tích khía cạnh...</p>
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

  if (!data) return null;

  const hasData = data.aspects.length > 0;

  return (
    <div className="animate-in fade-in duration-500 max-w-7xl mx-auto space-y-8 pb-10">
      {/* Header */}
      <div className="relative overflow-hidden rounded-3xl border border-white/5 bg-gradient-to-br from-[#151B2B] via-[#12182A] to-[#0A101D] p-8">
        <div className="absolute -top-20 -right-16 w-64 h-64 bg-emerald-500/10 rounded-full blur-3xl pointer-events-none" />
        <div className="absolute -bottom-12 -left-12 w-48 h-48 bg-[#FF7575]/10 rounded-full blur-3xl pointer-events-none" />

        <div className="relative flex flex-col lg:flex-row lg:items-center lg:justify-between gap-6">
          <div>
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-white/5 border border-white/10 text-xs text-gray-400 mb-4">
              <Sparkles className="w-3.5 h-3.5 text-emerald-400" />
              Aspect-Based Sentiment
            </div>
            <h2 className="text-3xl font-bold text-white flex items-center gap-3">
              <BarChart2 className="text-emerald-400 w-8 h-8" />
              Phân tích Khía cạnh
            </h2>
            <p className="text-gray-400 text-sm mt-2 max-w-xl">
              Đào sâu chủ đề người dùng đang bàn luận — từ {formatNumber(data.totalAnalyzedMentions)} mentions có
              nhắc khía cạnh · {formatNumber(data.totalAspectHits)} lượt phát hiện
            </p>
          </div>

          <button
            type="button"
            onClick={loadData}
            className="self-start p-3 rounded-xl border border-white/10 bg-white/5 text-gray-400 hover:text-white hover:bg-white/10 transition-colors"
            title="Làm mới"
          >
            <RefreshCw className="w-5 h-5" />
          </button>
        </div>
      </div>

      {!hasData ? (
        <div className="bg-[#151B2B] border border-white/5 rounded-3xl p-16 text-center">
          <Target className="w-14 h-14 text-gray-600 mx-auto mb-4" />
          <p className="text-gray-300 font-medium">Chưa phát hiện khía cạnh nào</p>
          <p className="text-gray-500 text-sm mt-2 max-w-md mx-auto">
            Cần mentions có nội dung/bình luận nhắc đến giá, chất lượng, dịch vụ, lương thưởng, v.v.
          </p>
        </div>
      ) : (
        <>
          {/* Highlights */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="bg-[#151B2B] border border-emerald-500/20 rounded-2xl p-6 flex items-center gap-4">
              <div className="w-14 h-14 rounded-2xl bg-emerald-500/10 flex items-center justify-center shrink-0">
                <CheckCircle2 className="text-emerald-400 w-7 h-7" />
              </div>
              <div className="min-w-0">
                <p className="text-sm text-gray-500">Khía cạnh được khen nhiều nhất</p>
                <h4 className="text-xl font-bold text-white truncate mt-1">
                  {data.topPositiveAspect ?? '—'}
                </h4>
              </div>
            </div>
            <div className="bg-[#151B2B] border border-[#FF7575]/20 rounded-2xl p-6 flex items-center gap-4">
              <div className="w-14 h-14 rounded-2xl bg-[#FF7575]/10 flex items-center justify-center shrink-0">
                <AlertCircle className="text-[#FF7575] w-7 h-7" />
              </div>
              <div className="min-w-0">
                <p className="text-sm text-gray-500">Khía cạnh bị phàn nàn nhiều nhất</p>
                <h4 className="text-xl font-bold text-white truncate mt-1">
                  {data.topNegativeAspect ?? '—'}
                </h4>
              </div>
            </div>
          </div>

          {/* Stacked bar + radar */}
          <div className="grid grid-cols-1 xl:grid-cols-5 gap-6">
            <div className="xl:col-span-3 bg-[#151B2B] border border-white/5 rounded-3xl p-6">
              <div className="flex flex-col sm:flex-row sm:items-end sm:justify-between gap-4 mb-6">
                <div>
                  <h3 className="font-bold text-white flex items-center gap-2">
                    <Layers className="w-5 h-5 text-emerald-400" />
                    Cảm xúc theo khía cạnh
                  </h3>
                  <p className="text-xs text-gray-500 mt-1">Stacked bar — tích cực / trung lập / tiêu cực</p>
                </div>
                <div className="flex gap-4 text-xs font-semibold">
                  <span className="flex items-center gap-1.5 text-gray-400">
                    <span className="w-3 h-3 rounded-sm" style={{ background: SENTIMENT_COLORS.positive }} /> Tích cực
                  </span>
                  <span className="flex items-center gap-1.5 text-gray-400">
                    <span className="w-3 h-3 rounded-sm" style={{ background: SENTIMENT_COLORS.neutral }} /> Trung lập
                  </span>
                  <span className="flex items-center gap-1.5 text-gray-400">
                    <span className="w-3 h-3 rounded-sm" style={{ background: SENTIMENT_COLORS.negative }} /> Tiêu cực
                  </span>
                </div>
              </div>

              <ResponsiveContainer width="100%" height={Math.max(280, stackData.length * 52)}>
                <BarChart
                  data={stackData}
                  layout="vertical"
                  margin={{ top: 0, right: 16, left: 8, bottom: 0 }}
                  barCategoryGap="24%"
                >
                  <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.04)" horizontal={false} />
                  <XAxis type="number" allowDecimals={false} tick={{ fill: '#6B7280', fontSize: 11 }} axisLine={false} tickLine={false} />
                  <YAxis
                    type="category"
                    dataKey="label"
                    width={120}
                    tick={{ fill: '#D1D5DB', fontSize: 11, fontWeight: 600 }}
                    axisLine={false}
                    tickLine={false}
                  />
                  <Tooltip
                    contentStyle={TOOLTIP_STYLE}
                    labelStyle={{ color: '#9CA3AF' }}
                    itemStyle={{ color: '#fff' }}
                    cursor={{ fill: 'rgba(255,255,255,0.03)' }}
                  />
                  <Bar dataKey="Tích cực" stackId="a" fill={SENTIMENT_COLORS.positive} />
                  <Bar dataKey="Trung lập" stackId="a" fill={SENTIMENT_COLORS.neutral} />
                  <Bar dataKey="Tiêu cực" stackId="a" fill={SENTIMENT_COLORS.negative} radius={[0, 6, 6, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>

            <div className="xl:col-span-2 bg-[#151B2B] border border-white/5 rounded-3xl p-6">
              <h3 className="font-bold text-white mb-1">Radar cảm xúc</h3>
              <p className="text-xs text-gray-500 mb-4">% tích cực vs tiêu cực từng khía cạnh</p>

              {radarData.length < 3 ? (
                <div className="h-[300px] flex items-center justify-center text-gray-500 text-sm border border-dashed border-white/10 rounded-2xl">
                  Cần thêm khía cạnh để hiển thị radar.
                </div>
              ) : (
                <ResponsiveContainer width="100%" height={300}>
                  <RadarChart data={radarData} cx="50%" cy="50%" outerRadius="70%">
                    <PolarGrid stroke="rgba(255,255,255,0.08)" />
                    <PolarAngleAxis dataKey="aspect" tick={{ fill: '#9CA3AF', fontSize: 10 }} />
                    <Radar
                      name="% Tích cực"
                      dataKey="positive"
                      stroke={SENTIMENT_COLORS.positive}
                      fill={SENTIMENT_COLORS.positive}
                      fillOpacity={0.25}
                      strokeWidth={2}
                    />
                    <Radar
                      name="% Tiêu cực"
                      dataKey="negative"
                      stroke={SENTIMENT_COLORS.negative}
                      fill={SENTIMENT_COLORS.negative}
                      fillOpacity={0.2}
                      strokeWidth={2}
                    />
                    <Tooltip
                      content={({ active, payload }) => {
                        if (!active || !payload?.[0]) return null;
                        const row = payload[0].payload as { fullLabel: string; positive: number; negative: number };
                        return (
                          <div style={TOOLTIP_STYLE}>
                            <p className="text-sm font-semibold text-white">{row.fullLabel}</p>
                            <p className="text-xs text-[#00B4D8] mt-1">Tích cực: {row.positive}%</p>
                            <p className="text-xs text-[#FF7575]">Tiêu cực: {row.negative}%</p>
                          </div>
                        );
                      }}
                    />
                    <Legend wrapperStyle={{ fontSize: 11, color: '#9CA3AF' }} iconSize={8} />
                  </RadarChart>
                </ResponsiveContainer>
              )}
            </div>
          </div>

          {/* Percent bars (CSS) + detail cards */}
          <div className="bg-[#151B2B] border border-white/5 rounded-3xl p-6 md:p-8">
            <h3 className="font-bold text-white mb-6">Chi tiết từng khía cạnh</h3>
            <div className="space-y-8">
              {data.aspects.map((asp) => (
                <AspectRow key={asp.key} aspect={asp} />
              ))}
            </div>
          </div>

          {/* Grouped column chart */}
          <div className="bg-[#151B2B] border border-white/5 rounded-3xl p-6">
            <h3 className="font-bold text-white mb-1">So sánh số lượng mention</h3>
            <p className="text-xs text-gray-500 mb-4">Số lần nhắc đến mỗi khía cạnh trong dữ liệu</p>

            <ResponsiveContainer width="100%" height={280}>
              <BarChart data={stackData} margin={{ top: 8, right: 8, left: -12, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.04)" vertical={false} />
                <XAxis
                  dataKey="label"
                  tick={{ fill: '#6B7280', fontSize: 10 }}
                  axisLine={false}
                  tickLine={false}
                  interval={0}
                  angle={-15}
                  textAnchor="end"
                  height={56}
                />
                <YAxis allowDecimals={false} tick={{ fill: '#6B7280', fontSize: 11 }} axisLine={false} tickLine={false} />
                <Tooltip contentStyle={TOOLTIP_STYLE} cursor={{ fill: 'rgba(255,255,255,0.03)' }} />
                <Legend wrapperStyle={{ fontSize: 12, color: '#9CA3AF' }} iconSize={8} />
                <Bar dataKey="Tích cực" fill={SENTIMENT_COLORS.positive} radius={[4, 4, 0, 0]} maxBarSize={36} />
                <Bar dataKey="Trung lập" fill={SENTIMENT_COLORS.neutral} maxBarSize={36} />
                <Bar dataKey="Tiêu cực" fill={SENTIMENT_COLORS.negative} maxBarSize={36} />
              </BarChart>
            </ResponsiveContainer>
          </div>
        </>
      )}
    </div>
  );
};

function AspectRow({ aspect }: { aspect: AspectStats }) {
  const { positivePercent, neutralPercent, negativePercent, totalMentions } = aspect;

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-2 mb-2">
        <span className="text-sm font-semibold text-gray-200">{aspect.label}</span>
        <span className="text-xs text-gray-500">{totalMentions} mentions</span>
      </div>
      <div className="w-full h-7 flex rounded-lg overflow-hidden bg-[#0A101D] ring-1 ring-white/5">
        {positivePercent > 0 && (
          <div
            style={{ width: `${positivePercent}%` }}
            className="h-full bg-[#00B4D8] flex items-center justify-center text-[10px] font-bold text-white min-w-[28px] transition-all duration-700"
          >
            {positivePercent >= 8 ? `${positivePercent}%` : ''}
          </div>
        )}
        {neutralPercent > 0 && (
          <div
            style={{ width: `${neutralPercent}%` }}
            className="h-full bg-yellow-500 flex items-center justify-center text-[10px] font-bold text-white min-w-[28px] transition-all duration-700"
          >
            {neutralPercent >= 8 ? `${neutralPercent}%` : ''}
          </div>
        )}
        {negativePercent > 0 && (
          <div
            style={{ width: `${negativePercent}%` }}
            className="h-full bg-[#FF7575] flex items-center justify-center text-[10px] font-bold text-white min-w-[28px] transition-all duration-700"
          >
            {negativePercent >= 8 ? `${negativePercent}%` : ''}
          </div>
        )}
      </div>
      <div className="flex gap-4 mt-2 text-[11px] text-gray-600">
        <span className="text-[#00B4D8]">{aspect.positive} tích cực</span>
        <span className="text-yellow-600">{aspect.neutral} trung lập</span>
        <span className="text-[#FF7575]">{aspect.negative} tiêu cực</span>
      </div>
    </div>
  );
}

export default ProjectAspect;
