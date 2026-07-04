import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
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
import { ResponsiveContainer, BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip, Legend } from 'recharts';
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

function getAspectTone(aspect: AspectStats): 'positive' | 'negative' | 'mixed' {
  if (aspect.positivePercent >= aspect.negativePercent + 15) return 'positive';
  if (aspect.negativePercent >= aspect.positivePercent + 15) return 'negative';
  return 'mixed';
}

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

  const rankedAspects = useMemo(
    () => [...(data?.aspects ?? [])].sort((a, b) => b.totalMentions - a.totalMentions),
    [data]
  );

  const volumeChartData = useMemo(
    () =>
      rankedAspects.map((aspect) => ({
        label: aspect.label,
        mentions: aspect.totalMentions,
      })),
    [rankedAspects]
  );

  const sentimentRateData = useMemo(
    () =>
      rankedAspects.map((aspect) => ({
        label: aspect.label,
        'Tích cực %': aspect.positivePercent,
        'Trung lập %': aspect.neutralPercent,
        'Tiêu cực %': aspect.negativePercent,
      })),
    [rankedAspects]
  );

  const mostDiscussed = rankedAspects[0] ?? null;
  const avgHitsPerMention =
    data && data.totalAnalyzedMentions > 0
      ? Math.round((data.totalAspectHits / data.totalAnalyzedMentions) * 10) / 10
      : 0;

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
      <div className="relative overflow-hidden rounded-3xl border border-white/5 bg-gradient-to-br from-[#151B2B] via-[#12182A] to-[#0A101D] p-8">
        <div className="absolute -top-20 -right-16 w-64 h-64 bg-emerald-500/10 rounded-full blur-3xl pointer-events-none" />
        <div className="absolute -bottom-12 -left-12 w-48 h-48 bg-[#FF7575]/10 rounded-full blur-3xl pointer-events-none" />

        <div className="relative flex flex-col lg:flex-row lg:items-start lg:justify-between gap-6">
          <div className="max-w-3xl">
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-white/5 border border-white/10 text-xs text-gray-400 mb-4">
              <Sparkles className="w-3.5 h-3.5 text-emerald-400" />
              Aspect-based reading
            </div>
            <h2 className="text-3xl font-bold text-white flex items-center gap-3">
              <BarChart2 className="text-emerald-400 w-8 h-8" />
              Aspect Analysis
            </h2>
            <p className="text-gray-400 text-sm mt-3 leading-relaxed">
              Màn hình này giúp user biết cộng đồng đang nhắc nhiều nhất đến điều gì, điều gì được khen, và điểm nào đang bị
              phàn nàn. Toàn bộ UI này chỉ dùng dữ liệu aspect mà backend hiện đang trả về.
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
            Cần mentions có nội dung / bình luận nhắc đến các chủ đề như giá, chất lượng, dịch vụ, lương thưởng, môi trường...
          </p>
        </div>
      ) : (
        <>
          <div className="grid grid-cols-2 xl:grid-cols-4 gap-4">
            <MetricCard title="Số khía cạnh" value={formatNumber(rankedAspects.length)} detail="Số nhóm chủ đề được backend detect" accent="text-white" />
            <MetricCard title="Mentions da xet" value={formatNumber(data.totalAnalyzedMentions)} detail="Tap mention duoc dung de tinh aspect" accent="text-[#00B4D8]" />
            <MetricCard title="Tổng aspect hits" value={formatNumber(data.totalAspectHits)} detail="Tổng lượt phát hiện trên toàn bộ corpus" accent="text-emerald-400" />
            <MetricCard title="Hits / mention" value={String(avgHitsPerMention)} detail="Mức độ phong phú chủ đề trong thảo luận" accent="text-yellow-400" />
          </div>

          <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
            <HighlightCard
              title="Được khen nhiều nhất"
              value={data.topPositiveAspect ?? 'Không có'}
              detail="Aspect có xu hướng positive nổi bật nhất hiện tại"
              tone="positive"
              icon={<CheckCircle2 className="text-emerald-400 w-7 h-7" />}
            />
            <HighlightCard
              title="Bị phàn nàn nhiều nhất"
              value={data.topNegativeAspect ?? 'Không có'}
              detail="Aspect có xu hướng negative cao nhất hiện tại"
              tone="negative"
              icon={<AlertCircle className="text-[#FF7575] w-7 h-7" />}
            />
            <HighlightCard
              title="Được nhắc đến nhiều nhất"
              value={mostDiscussed?.label ?? 'Không có'}
              detail={mostDiscussed ? `${mostDiscussed.totalMentions} mentions gắn với aspect này` : 'Không đủ dữ liệu'}
              tone="neutral"
              icon={<Layers className="text-yellow-400 w-7 h-7" />}
            />
          </div>

          <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
            <div className="bg-[#151B2B] border border-white/5 rounded-3xl p-6">
              <h3 className="font-bold text-white mb-1">Mức độ được nhắc đến</h3>
              <p className="text-xs text-gray-500 mb-4">Aspect nao dang chiem nhieu trong luong thao luan nhat</p>

              <ResponsiveContainer width="100%" height={Math.max(280, volumeChartData.length * 52)}>
                <BarChart data={volumeChartData} layout="vertical" margin={{ top: 0, right: 16, left: 8, bottom: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.04)" horizontal={false} />
                  <XAxis type="number" allowDecimals={false} tick={{ fill: '#6B7280', fontSize: 11 }} axisLine={false} tickLine={false} />
                  <YAxis
                    type="category"
                    dataKey="label"
                    width={130}
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
                  <Bar dataKey="mentions" name="Mentions" fill="#10B981" radius={[0, 6, 6, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>

            <div className="bg-[#151B2B] border border-white/5 rounded-3xl p-6">
              <h3 className="font-bold text-white mb-1">Sentiment theo tung aspect</h3>
              <p className="text-xs text-gray-500 mb-4">So sánh tỉ lệ tích cực / trung lập / tiêu cực trên từng chủ đề</p>

              <ResponsiveContainer width="100%" height={Math.max(280, sentimentRateData.length * 52)}>
                <BarChart data={sentimentRateData} layout="vertical" margin={{ top: 0, right: 16, left: 8, bottom: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.04)" horizontal={false} />
                  <XAxis type="number" tick={{ fill: '#6B7280', fontSize: 11 }} axisLine={false} tickLine={false} unit="%" />
                  <YAxis
                    type="category"
                    dataKey="label"
                    width={130}
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
                  <Legend wrapperStyle={{ fontSize: 12, color: '#9CA3AF' }} iconSize={8} />
                  <Bar dataKey="Tích cực %" fill={SENTIMENT_COLORS.positive} />
                  <Bar dataKey="Trung lập %" fill={SENTIMENT_COLORS.neutral} />
                  <Bar dataKey="Tiêu cực %" fill={SENTIMENT_COLORS.negative} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </div>

          <div className="bg-[#151B2B] border border-white/5 rounded-3xl p-6 md:p-8">
            <h3 className="font-bold text-white mb-6">Tổng hợp từng khía cạnh</h3>
            <div className="grid grid-cols-1 xl:grid-cols-2 gap-5">
              {rankedAspects.map((aspect) => (
                <AspectCard key={aspect.key} aspect={aspect} totalAspectHits={data.totalAspectHits} />
              ))}
            </div>
          </div>
        </>
      )}
    </div>
  );
};

function MetricCard({
  title,
  value,
  detail,
  accent,
}: {
  title: string;
  value: string;
  detail: string;
  accent: string;
}) {
  return (
    <div className="rounded-2xl border border-white/5 bg-[#151B2B] p-5">
      <p className="text-xs text-gray-500 uppercase tracking-wide font-semibold">{title}</p>
      <p className={`text-3xl font-bold mt-2 tabular-nums ${accent}`}>{value}</p>
      <p className="text-xs text-gray-500 mt-2">{detail}</p>
    </div>
  );
}

function HighlightCard({
  title,
  value,
  detail,
  tone,
  icon,
}: {
  title: string;
  value: string;
  detail: string;
  tone: 'positive' | 'negative' | 'neutral';
  icon: ReactNode;
}) {
  const toneClass =
    tone === 'positive'
      ? 'border-emerald-500/20'
      : tone === 'negative'
        ? 'border-[#FF7575]/20'
        : 'border-white/10';

  return (
    <div className={`bg-[#151B2B] rounded-2xl p-6 flex items-center gap-4 border ${toneClass}`}>
      <div className="w-14 h-14 rounded-2xl bg-white/5 flex items-center justify-center shrink-0">{icon}</div>
      <div className="min-w-0">
        <p className="text-sm text-gray-500">{title}</p>
        <h4 className="text-xl font-bold text-white truncate mt-1">{value}</h4>
        <p className="text-xs text-gray-500 mt-1">{detail}</p>
      </div>
    </div>
  );
}

function AspectCard({ aspect, totalAspectHits }: { aspect: AspectStats; totalAspectHits: number }) {
  const tone = getAspectTone(aspect);
  const share = totalAspectHits > 0 ? Math.round((aspect.totalMentions / totalAspectHits) * 100) : 0;
  const balance = aspect.positive - aspect.negative;

  return (
    <div className="rounded-2xl border border-white/5 bg-[#0F1524] p-5">
      <div className="flex items-start justify-between gap-4">
        <div>
          <p className="text-lg font-bold text-white">{aspect.label}</p>
          <p className="text-xs text-gray-500 mt-1">
            {aspect.totalMentions} mentions · {share}% tong aspect hits
          </p>
        </div>
        <span
          className={`px-3 py-1 rounded-full text-[11px] font-semibold ${
            tone === 'positive'
              ? 'bg-[#00B4D8]/10 text-[#00B4D8]'
              : tone === 'negative'
                ? 'bg-[#FF7575]/10 text-[#FF7575]'
                : 'bg-yellow-500/10 text-yellow-400'
          }`}
        >
          {tone === 'positive' ? 'Thiên về khen' : tone === 'negative' ? 'Thiên về chê' : 'Đang tranh luận'}
        </span>
      </div>

      <div className="mt-4 h-7 flex rounded-lg overflow-hidden bg-[#0A101D] ring-1 ring-white/5">
        {aspect.positivePercent > 0 ? (
          <div
            style={{ width: `${aspect.positivePercent}%` }}
            className="h-full bg-[#00B4D8] flex items-center justify-center text-[10px] font-bold text-white min-w-[28px]"
          >
            {aspect.positivePercent >= 8 ? `${aspect.positivePercent}%` : ''}
          </div>
        ) : null}
        {aspect.neutralPercent > 0 ? (
          <div
            style={{ width: `${aspect.neutralPercent}%` }}
            className="h-full bg-yellow-500 flex items-center justify-center text-[10px] font-bold text-white min-w-[28px]"
          >
            {aspect.neutralPercent >= 8 ? `${aspect.neutralPercent}%` : ''}
          </div>
        ) : null}
        {aspect.negativePercent > 0 ? (
          <div
            style={{ width: `${aspect.negativePercent}%` }}
            className="h-full bg-[#FF7575] flex items-center justify-center text-[10px] font-bold text-white min-w-[28px]"
          >
            {aspect.negativePercent >= 8 ? `${aspect.negativePercent}%` : ''}
          </div>
        ) : null}
      </div>

      <div className="grid grid-cols-3 gap-2 mt-4 text-xs">
        <div className="rounded-xl bg-[#0A101D] border border-white/5 px-3 py-2">
            <p className="text-gray-500">Tích cực</p>
          <p className="text-[#00B4D8] font-bold mt-1">
            {aspect.positive} · {aspect.positivePercent}%
          </p>
        </div>
        <div className="rounded-xl bg-[#0A101D] border border-white/5 px-3 py-2">
            <p className="text-gray-500">Trung lập</p>
          <p className="text-yellow-400 font-bold mt-1">
            {aspect.neutral} · {aspect.neutralPercent}%
          </p>
        </div>
        <div className="rounded-xl bg-[#0A101D] border border-white/5 px-3 py-2">
            <p className="text-gray-500">Tiêu cực</p>
          <p className="text-[#FF7575] font-bold mt-1">
            {aspect.negative} · {aspect.negativePercent}%
          </p>
        </div>
      </div>

      <div className="mt-4 text-sm text-gray-400 leading-relaxed">
        {balance > 0
          ? `Aspect nay dang nghiêng tich cuc voi chenhlech +${balance} mention so voi nhom tieu cuc.`
          : balance < 0
            ? `Aspect nay dang nghiêng tieu cuc voi chenhlech ${balance} mention, can doc ky cac mention lien quan.`
            : 'Aspect nay dang can bang giua khen va che, can doc them mentions de hieu boi canh.'}
      </div>
    </div>
  );
}

export default ProjectAspect;
