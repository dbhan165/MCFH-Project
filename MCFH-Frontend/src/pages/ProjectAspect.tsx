import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { useParams } from 'react-router-dom';
import {
  BarChart2,
  CheckCircle2,
  AlertCircle,
  Loader2,
  RefreshCw,
  Target,
  Layers,
  Hash,
  Sparkles,
  ThumbsUp,
  ThumbsDown,
} from 'lucide-react';
import { projectApi } from '../api/projectApi';
import type { AspectAnalysis, AspectStats } from '../types/project';
import { extractApiError } from '../utils/authStorage';
import { formatNumber, SENTIMENT_COLORS } from '../utils/sentimentHelpers';

function PageMetricCard({
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
    <div className="relative overflow-hidden rounded-2xl border border-white/5 bg-gradient-to-br from-[#151B2B] via-[#141A28] to-[#101622] p-4 sm:p-5 hover:border-white/10 transition-all group">
      <div
        className="absolute top-0 inset-x-0 h-px opacity-70"
        style={{ background: `linear-gradient(90deg, transparent, ${accentColor}, transparent)` }}
      />
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
        className={`text-2xl font-black tabular-nums mt-1 leading-none ${valueClass}`}
        style={valueClass ? undefined : { color: accentColor }}
      >
        {value}
      </p>
      {progress !== undefined && (
        <div className="mt-3 h-1.5 rounded-full bg-white/5 overflow-hidden">
          <div
            className="h-full rounded-full"
            style={{
              width: `${Math.min(100, progress)}%`,
              background: `linear-gradient(90deg, ${accentColor}, ${accentColor}88)`,
            }}
          />
        </div>
      )}
      <p className="text-[11px] text-gray-500 mt-2 leading-relaxed">{caption}</p>
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
  const colors = {
    positive: { accent: SENTIMENT_COLORS.positive, border: 'border-[#00B4D8]/25' },
    negative: { accent: SENTIMENT_COLORS.negative, border: 'border-[#FF7575]/25' },
    neutral: { accent: SENTIMENT_COLORS.neutral, border: 'border-yellow-500/25' },
  };
  const style = colors[tone];

  return (
    <div
      className={`relative overflow-hidden rounded-2xl border bg-gradient-to-br from-[#151B2B] to-[#101622] p-5 ${style.border}`}
      style={{ boxShadow: `inset 3px 0 0 ${style.accent}` }}
    >
      <div className="flex items-start gap-4">
        <div
          className="p-3 rounded-xl border shrink-0"
          style={{ background: `${style.accent}14`, borderColor: `${style.accent}33`, color: style.accent }}
        >
          {icon}
        </div>
        <div className="min-w-0">
          <p className="text-[10px] uppercase tracking-wider font-semibold" style={{ color: style.accent }}>
            {title}
          </p>
          <p className="text-lg font-bold text-white mt-1 leading-snug">{value}</p>
          <p className="text-xs text-gray-500 mt-1.5 leading-relaxed">{detail}</p>
        </div>
      </div>
    </div>
  );
}

function getAspectTone(aspect: AspectStats): 'positive' | 'negative' | 'mixed' {
  if (aspect.positivePercent >= aspect.negativePercent + 15) return 'positive';
  if (aspect.negativePercent >= aspect.positivePercent + 15) return 'negative';
  return 'mixed';
}

function getToneColor(tone: 'positive' | 'negative' | 'mixed') {
  if (tone === 'positive') return SENTIMENT_COLORS.positive;
  if (tone === 'negative') return SENTIMENT_COLORS.negative;
  return SENTIMENT_COLORS.neutral;
}

function getToneLabel(tone: 'positive' | 'negative' | 'mixed') {
  if (tone === 'positive') return 'Thiên khen';
  if (tone === 'negative') return 'Thiên chê';
  return 'Tranh luận';
}

function AspectCard({ aspect, totalAspectHits, maxMentions }: { aspect: AspectStats; totalAspectHits: number; maxMentions: number }) {
  const tone = getAspectTone(aspect);
  const accent = getToneColor(tone);
  const share = totalAspectHits > 0 ? Math.round((aspect.totalMentions / totalAspectHits) * 100) : 0;
  const balance = aspect.positive - aspect.negative;
  const volumeWidth = maxMentions > 0 ? (aspect.totalMentions / maxMentions) * 100 : 0;

  return (
    <article
      className="relative overflow-hidden rounded-2xl border border-white/5 bg-gradient-to-br from-[#151B2B]/90 to-[#101622] p-5 hover:border-white/10 transition-all"
      style={{ boxShadow: `inset 3px 0 0 ${accent}88` }}
    >
      <div className="flex items-start justify-between gap-3">
        <div className="min-w-0">
          <p className="text-lg font-bold text-white">{aspect.label}</p>
          <p className="text-xs text-gray-500 mt-1">
            {formatNumber(aspect.totalMentions)} mentions · {share}% tổng lượt nhắc
          </p>
        </div>
        <span
          className="shrink-0 px-2.5 py-1 rounded-full text-[11px] font-semibold border"
          style={{
            color: accent,
            background: `${accent}14`,
            borderColor: `${accent}33`,
          }}
        >
          {getToneLabel(tone)}
        </span>
      </div>

      <div className="mt-4">
        <div className="flex justify-between text-[10px] text-gray-500 mb-1">
          <span>Mức độ được nhắc</span>
          <span className="tabular-nums">{formatNumber(aspect.totalMentions)}</span>
        </div>
        <div className="h-2 rounded-full bg-white/5 overflow-hidden">
          <div
            className="h-full rounded-full transition-all duration-500"
            style={{ width: `${Math.max(volumeWidth, aspect.totalMentions > 0 ? 4 : 0)}%`, background: accent }}
          />
        </div>
      </div>

      <div className="mt-4 h-2.5 rounded-full overflow-hidden flex bg-white/5 ring-1 ring-white/5">
        {aspect.positivePercent > 0 && (
          <div
            className="h-full"
            style={{
              width: `${Math.max(aspect.positivePercent, 2)}%`,
              background: `linear-gradient(90deg, ${SENTIMENT_COLORS.positive}, ${SENTIMENT_COLORS.positive}cc)`,
            }}
          />
        )}
        {aspect.neutralPercent > 0 && (
          <div
            className="h-full"
            style={{
              width: `${Math.max(aspect.neutralPercent, 2)}%`,
              background: `linear-gradient(90deg, ${SENTIMENT_COLORS.neutral}, ${SENTIMENT_COLORS.neutral}cc)`,
            }}
          />
        )}
        {aspect.negativePercent > 0 && (
          <div
            className="h-full"
            style={{
              width: `${Math.max(aspect.negativePercent, 2)}%`,
              background: `linear-gradient(90deg, ${SENTIMENT_COLORS.negative}, ${SENTIMENT_COLORS.negative}cc)`,
            }}
          />
        )}
      </div>

      <div className="grid grid-cols-3 gap-2 mt-4">
        {[
          { label: 'Tích cực', count: aspect.positive, pct: aspect.positivePercent, color: SENTIMENT_COLORS.positive },
          { label: 'Trung lập', count: aspect.neutral, pct: aspect.neutralPercent, color: SENTIMENT_COLORS.neutral },
          { label: 'Tiêu cực', count: aspect.negative, pct: aspect.negativePercent, color: SENTIMENT_COLORS.negative },
        ].map((item) => (
          <div key={item.label} className="rounded-xl border border-white/5 bg-white/[0.03] px-3 py-2.5 text-center">
            <p className="text-[10px] text-gray-500 uppercase">{item.label}</p>
            <p className="text-sm font-bold mt-1 tabular-nums" style={{ color: item.color }}>
              {item.pct}%
            </p>
            <p className="text-[10px] text-gray-500 mt-0.5 tabular-nums">{formatNumber(item.count)}</p>
          </div>
        ))}
      </div>

      <p className="mt-4 text-xs text-gray-500 leading-relaxed">
        {balance > 0
          ? `Nghiêng tích cực (+${balance} mentions).`
          : balance < 0
            ? `Nghiêng tiêu cực (${balance} mentions).`
            : 'Cân bằng giữa khen và chê.'}
      </p>
    </article>
  );
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

  const mostDiscussed = rankedAspects[0] ?? null;
  const maxMentions = mostDiscussed?.totalMentions ?? 1;
  const avgHitsPerMention =
    data && data.totalAnalyzedMentions > 0
      ? Math.round((data.totalAspectHits / data.totalAnalyzedMentions) * 10) / 10
      : 0;
  const topShare = data && data.totalAspectHits > 0 && mostDiscussed
    ? Math.round((mostDiscussed.totalMentions / data.totalAspectHits) * 100)
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
            <h2 className="text-3xl font-bold text-white flex items-center gap-3">
              <BarChart2 className="text-emerald-400 w-8 h-8" />
              Khía cạnh
            </h2>
            <p className="text-gray-400 text-sm mt-3 leading-relaxed">
              Chủ đề được nhắc nhiều nhất và sentiment theo từng khía cạnh — từ MXH và Tin tức.
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
        <div className="rounded-3xl border border-white/5 bg-[#151B2B] p-16 text-center">
          <Target className="w-14 h-14 text-gray-600 mx-auto mb-4" />
          <p className="text-gray-300 font-medium">Chưa phát hiện khía cạnh nào</p>
          <p className="text-gray-500 text-sm mt-2 max-w-md mx-auto">
            Cần mentions từ MXH hoặc bài báo nhắc đến giá, chất lượng, dịch vụ, lương thưởng, môi trường...
          </p>
        </div>
      ) : (
        <>
          <div className="grid grid-cols-2 xl:grid-cols-4 gap-3 sm:gap-4">
            <PageMetricCard
              icon={<Layers className="w-4 h-4" />}
              label="Khía cạnh"
              value={formatNumber(rankedAspects.length)}
              caption="Chủ đề được phát hiện"
              accentColor="#34D399"
            />
            <PageMetricCard
              icon={<Hash className="w-4 h-4" />}
              label="Mentions đã xét"
              value={formatNumber(data.totalAnalyzedMentions)}
              caption="Dùng để tính khía cạnh"
              accentColor="#00B4D8"
            />
            <PageMetricCard
              icon={<Sparkles className="w-4 h-4" />}
              label="Lượt nhắc"
              value={formatNumber(data.totalAspectHits)}
              caption="Tổng số lần khớp chủ đề"
              accentColor="white"
            />
            <PageMetricCard
              icon={<BarChart2 className="w-4 h-4" />}
              label="Nhắc / mention"
              value={String(avgHitsPerMention)}
              caption="Độ phong phú chủ đề trung bình"
              accentColor="#EAB308"
              progress={Math.min(100, avgHitsPerMention * 20)}
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <HighlightCard
              title="Được khen nhiều"
              value={data.topPositiveAspect ?? '—'}
              detail="Xu hướng tích cực nổi bật"
              tone="positive"
              icon={<ThumbsUp className="w-5 h-5" />}
            />
            <HighlightCard
              title="Bị phàn nàn nhiều"
              value={data.topNegativeAspect ?? '—'}
              detail="Xu hướng tiêu cực nổi bật"
              tone="negative"
              icon={<ThumbsDown className="w-5 h-5" />}
            />
            <HighlightCard
              title="Được nhắc nhiều nhất"
              value={mostDiscussed?.label ?? '—'}
              detail={
                mostDiscussed
                  ? `${formatNumber(mostDiscussed.totalMentions)} mentions · ${topShare}% tổng nhắc`
                  : 'Chưa đủ dữ liệu'
              }
              tone="neutral"
              icon={<CheckCircle2 className="w-5 h-5" />}
            />
          </div>

          <div className="relative overflow-hidden rounded-3xl border border-white/5 bg-gradient-to-br from-[#151B2B] via-[#12182A] to-[#0F1524] p-6">
            <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-6">
              <div>
                <h3 className="font-bold text-white">Bảng xếp hạng khía cạnh</h3>
                <p className="text-xs text-gray-500 mt-1">Sắp xếp theo mức độ được nhắc đến</p>
              </div>
              <div className="flex flex-wrap gap-2">
                {[
                  { label: 'Tích cực', color: SENTIMENT_COLORS.positive },
                  { label: 'Trung lập', color: SENTIMENT_COLORS.neutral },
                  { label: 'Tiêu cực', color: SENTIMENT_COLORS.negative },
                ].map((item) => (
                  <span
                    key={item.label}
                    className="inline-flex items-center gap-1.5 rounded-full border border-white/10 bg-white/[0.03] px-3 py-1.5 text-[11px] text-gray-300"
                  >
                    <span className="w-2 h-2 rounded-full" style={{ background: item.color }} />
                    {item.label}
                  </span>
                ))}
              </div>
            </div>

            <div className="space-y-4">
              {rankedAspects.map((aspect, idx) => {
                const tone = getAspectTone(aspect);
                const accent = getToneColor(tone);
                const volumeWidth = (aspect.totalMentions / maxMentions) * 100;
                return (
                  <div key={aspect.key} className="rounded-xl border border-white/5 bg-white/[0.02] px-4 py-3.5">
                    <div className="flex flex-wrap items-center justify-between gap-2 mb-2.5">
                      <div className="flex items-center gap-3 min-w-0">
                        <span className="text-xs font-bold text-gray-500 w-5 tabular-nums">{idx + 1}</span>
                        <span className="text-sm font-bold text-white">{aspect.label}</span>
                        <span
                          className="text-[10px] font-semibold px-2 py-0.5 rounded-full border"
                          style={{ color: accent, background: `${accent}14`, borderColor: `${accent}33` }}
                        >
                          {getToneLabel(tone)}
                        </span>
                      </div>
                      <span className="text-sm font-bold tabular-nums" style={{ color: accent }}>
                        {formatNumber(aspect.totalMentions)} mentions
                      </span>
                    </div>
                    <div className="h-2 rounded-full bg-white/5 overflow-hidden mb-2">
                      <div className="h-full rounded-full" style={{ width: `${volumeWidth}%`, background: accent }} />
                    </div>
                    <div className="h-2 rounded-full overflow-hidden flex bg-white/5">
                      {aspect.positivePercent > 0 && (
                        <div style={{ width: `${aspect.positivePercent}%`, background: SENTIMENT_COLORS.positive }} className="h-full" />
                      )}
                      {aspect.neutralPercent > 0 && (
                        <div style={{ width: `${aspect.neutralPercent}%`, background: SENTIMENT_COLORS.neutral }} className="h-full" />
                      )}
                      {aspect.negativePercent > 0 && (
                        <div style={{ width: `${aspect.negativePercent}%`, background: SENTIMENT_COLORS.negative }} className="h-full" />
                      )}
                    </div>
                    <div className="flex flex-wrap gap-x-4 gap-y-1 mt-2 text-[11px] tabular-nums">
                      <span style={{ color: SENTIMENT_COLORS.positive }}>+{aspect.positivePercent}% ({aspect.positive})</span>
                      <span style={{ color: SENTIMENT_COLORS.neutral }}>○{aspect.neutralPercent}% ({aspect.neutral})</span>
                      <span style={{ color: SENTIMENT_COLORS.negative }}>-{aspect.negativePercent}% ({aspect.negative})</span>
                    </div>
                  </div>
                );
              })}
            </div>
          </div>

          <div className="rounded-3xl border border-white/5 bg-[#151B2B]/80 p-6">
            <h3 className="font-bold text-white mb-5">Chi tiết từng khía cạnh</h3>
            <div className="grid grid-cols-1 xl:grid-cols-2 gap-4">
              {rankedAspects.map((aspect) => (
                <AspectCard
                  key={aspect.key}
                  aspect={aspect}
                  totalAspectHits={data.totalAspectHits}
                  maxMentions={maxMentions}
                />
              ))}
            </div>
          </div>
        </>
      )}
    </div>
  );
};

export default ProjectAspect;
