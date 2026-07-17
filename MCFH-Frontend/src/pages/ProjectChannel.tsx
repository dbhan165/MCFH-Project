import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { useParams } from 'react-router-dom';
import {
  Share2,
  Loader2,
  AlertCircle,
  RefreshCw,
  MessageCircle,
  TrendingUp,
  TrendingDown,
  Minus,
  Crown,
  ShieldAlert,
  BarChart3,
  CheckCircle2,
} from 'lucide-react';
import { ResponsiveContainer, PieChart, Pie, Cell } from 'recharts';
import { projectApi } from '../api/projectApi';
import type { ChannelComparison, ChannelStats } from '../types/project';
import { extractApiError } from '../utils/authStorage';
import {
  formatNumber,
  getPlatformBadgeClass,
  getPlatformChartColor,
  SENTIMENT_COLORS,
} from '../utils/sentimentHelpers';

function PageMetricCard({
  icon,
  label,
  value,
  caption,
  accentColor,
}: {
  icon: ReactNode;
  label: string;
  value: string;
  caption: string;
  accentColor: string;
}) {
  return (
    <div
      className="relative overflow-hidden rounded-2xl border border-white/5 bg-gradient-to-br from-[#151B2B] via-[#141A28] to-[#101622] p-4 sm:p-5 hover:border-white/10 transition-all"
      style={{ boxShadow: `inset 3px 0 0 ${accentColor}88` }}
    >
      <div
        className="inline-flex p-2 rounded-xl border mb-3"
        style={{ background: `${accentColor}14`, borderColor: `${accentColor}33`, color: accentColor }}
      >
        {icon}
      </div>
      <p className="text-[10px] text-gray-500 uppercase tracking-[0.14em] font-semibold">{label}</p>
      <p className="text-lg font-bold text-white truncate mt-1">{value}</p>
      <p className="text-[11px] text-gray-500 mt-1.5 leading-relaxed">{caption}</p>
    </div>
  );
}

function NsrIcon({ score }: { score: number }) {
  if (score >= 10) return <TrendingUp className="w-4 h-4 text-[#00B4D8]" />;
  if (score <= -10) return <TrendingDown className="w-4 h-4 text-[#FF7575]" />;
  return <Minus className="w-4 h-4 text-yellow-500" />;
}

function getNsrColor(score: number) {
  if (score >= 10) return SENTIMENT_COLORS.positive;
  if (score <= -10) return SENTIMENT_COLORS.negative;
  return SENTIMENT_COLORS.neutral;
}

function ChannelDetailCard({ channel, isHighlighted }: { channel: ChannelStats; isHighlighted: boolean }) {
  const color = getPlatformChartColor(channel.platform);
  const analyzed = channel.mentions - channel.unanalyzed;
  const coverage = channel.mentions > 0 ? Math.round((analyzed / channel.mentions) * 100) : 0;
  const nsrColor = getNsrColor(channel.nsrScore);

  return (
    <div
      className={`relative overflow-hidden rounded-2xl border p-5 transition-all duration-200 ${
        isHighlighted ? 'border-white/20 bg-white/[0.05]' : 'border-white/5 bg-[#151B2B]/80'
      }`}
      style={{ boxShadow: `inset 3px 0 0 ${color}${isHighlighted ? '' : '88'}` }}
    >
      <div className="flex items-start justify-between gap-3">
        <div>
          <span className={`inline-flex px-2.5 py-1 rounded-lg text-xs font-bold ${getPlatformBadgeClass(channel.platform)}`}>
            {channel.label}
          </span>
          <p className="text-2xl font-black text-white tabular-nums mt-3 leading-none">{formatNumber(channel.mentions)}</p>
          <p className="text-xs text-gray-500 mt-1.5">
            {channel.mentionShare}% SOV · {formatNumber(channel.totalComments)} comments
          </p>
        </div>
        <div className="text-right shrink-0">
          {coverage === 100 ? (
            <div className="inline-flex items-center gap-1 rounded-full border border-emerald-500/20 bg-emerald-500/10 px-2.5 py-1 text-[11px] font-semibold text-emerald-400">
              <CheckCircle2 className="w-3.5 h-3.5" />
              Đủ AI
            </div>
          ) : (
            <div className="rounded-xl border border-amber-500/20 bg-amber-500/10 px-3 py-2 text-right">
              <p className="text-[10px] text-amber-300/80 uppercase">Độ phủ AI</p>
              <p className="text-lg font-bold text-amber-300 tabular-nums">{coverage}%</p>
            </div>
          )}
        </div>
      </div>

      <div className="mt-4 h-2.5 rounded-full overflow-hidden flex bg-white/5 ring-1 ring-white/5">
        {channel.positivePercent > 0 && (
          <div
            className="h-full"
            style={{
              width: `${Math.max(channel.positivePercent, 2)}%`,
              background: `linear-gradient(90deg, ${SENTIMENT_COLORS.positive}, ${SENTIMENT_COLORS.positive}cc)`,
            }}
          />
        )}
        {channel.neutralPercent > 0 && (
          <div
            className="h-full"
            style={{
              width: `${Math.max(channel.neutralPercent, 2)}%`,
              background: `linear-gradient(90deg, ${SENTIMENT_COLORS.neutral}, ${SENTIMENT_COLORS.neutral}cc)`,
            }}
          />
        )}
        {channel.negativePercent > 0 && (
          <div
            className="h-full"
            style={{
              width: `${Math.max(channel.negativePercent, 2)}%`,
              background: `linear-gradient(90deg, ${SENTIMENT_COLORS.negative}, ${SENTIMENT_COLORS.negative}cc)`,
            }}
          />
        )}
      </div>

      <div className="grid grid-cols-3 gap-2 mt-4">
        <div className="rounded-xl border border-white/5 bg-white/[0.03] px-3 py-2 text-center">
          <p className="text-[10px] text-gray-500 uppercase">Tích cực</p>
          <p className="text-sm font-bold mt-1 tabular-nums" style={{ color: SENTIMENT_COLORS.positive }}>
            {channel.positivePercent}%
          </p>
        </div>
        <div className="rounded-xl border border-white/5 bg-white/[0.03] px-3 py-2 text-center">
          <p className="text-[10px] text-gray-500 uppercase">Trung lập</p>
          <p className="text-sm font-bold mt-1 tabular-nums" style={{ color: SENTIMENT_COLORS.neutral }}>
            {channel.neutralPercent}%
          </p>
        </div>
        <div className="rounded-xl border border-white/5 bg-white/[0.03] px-3 py-2 text-center">
          <p className="text-[10px] text-gray-500 uppercase">Tiêu cực</p>
          <p className="text-sm font-bold mt-1 tabular-nums" style={{ color: SENTIMENT_COLORS.negative }}>
            {channel.negativePercent}%
          </p>
        </div>
      </div>

      <div className="mt-4 flex items-center justify-between text-sm">
        <span className="text-gray-500">NSR</span>
        <span className="font-bold tabular-nums flex items-center gap-1.5" style={{ color: nsrColor }}>
          <NsrIcon score={channel.nsrScore} />
          {channel.nsrScore > 0 ? '+' : ''}
          {channel.nsrScore}%
        </span>
      </div>
    </div>
  );
}

const ProjectChannel = () => {
  const { workspaceId, id } = useParams();
  const wid = Number(workspaceId);
  const projectId = Number(id);

  const [data, setData] = useState<ChannelComparison | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');
  const [activePlatform, setActivePlatform] = useState<string | null>(null);

  const loadData = useCallback(async () => {
    if (!wid || !projectId || Number.isNaN(wid) || Number.isNaN(projectId)) return;

    setIsLoading(true);
    setErrorMessage('');
    try {
      setData(await projectApi.getChannels(wid, projectId));
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải so sánh kênh.'));
    } finally {
      setIsLoading(false);
    }
  }, [wid, projectId]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const channels = data?.channels ?? [];
  const maxMentions = useMemo(() => Math.max(...channels.map((c) => c.mentions), 1), [channels]);
  const maxComments = useMemo(() => Math.max(...channels.map((c) => c.totalComments), 1), [channels]);

  const pieData = useMemo(
    () =>
      channels.map((channel) => ({
        ...channel,
        color: getPlatformChartColor(channel.platform),
      })),
    [channels]
  );

  const leaderBySov = useMemo(() => [...channels].sort((a, b) => b.mentionShare - a.mentionShare)[0], [channels]);
  const leaderByComments = useMemo(() => [...channels].sort((a, b) => b.commentShare - a.commentShare)[0], [channels]);
  const bestNsr = useMemo(() => [...channels].sort((a, b) => b.nsrScore - a.nsrScore)[0], [channels]);
  const highestRisk = useMemo(() => [...channels].sort((a, b) => b.negativePercent - a.negativePercent)[0], [channels]);

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center py-32 text-gray-400 gap-4">
        <div className="relative">
          <div className="absolute inset-0 rounded-full bg-purple-500/20 blur-2xl animate-pulse" />
          <Loader2 className="relative w-12 h-12 animate-spin text-purple-400" />
        </div>
        <p className="text-sm">Đang tải so sánh kênh...</p>
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

  const hasData = channels.length > 0;

  return (
    <div className="animate-in fade-in duration-500 max-w-7xl mx-auto space-y-8 pb-10">
      <div className="relative overflow-hidden rounded-3xl border border-white/5 bg-gradient-to-br from-[#151B2B] via-[#141A2C] to-[#0A101D] p-8">
        <div className="absolute -top-20 -right-16 w-64 h-64 bg-purple-500/10 rounded-full blur-3xl pointer-events-none" />
        <div className="absolute -bottom-12 -left-12 w-48 h-48 bg-blue-500/10 rounded-full blur-3xl pointer-events-none" />

        <div className="relative flex flex-col lg:flex-row lg:items-start lg:justify-between gap-6">
          <div className="max-w-3xl">
            <h2 className="text-3xl font-bold text-white flex items-center gap-3">
              <Share2 className="text-purple-400 w-8 h-8" />
              So sánh kênh
            </h2>
            <p className="text-gray-400 text-sm mt-3 leading-relaxed">
              So sánh volume, bình luận và sentiment giữa các nền tảng — gồm Tin tức.
            </p>
          </div>
          <button
            type="button"
            onClick={loadData}
            className="p-3 rounded-xl border border-white/10 bg-white/5 text-gray-400 hover:text-white hover:bg-white/10 transition-colors"
            title="Làm mới"
          >
            <RefreshCw className="w-5 h-5" />
          </button>
        </div>
      </div>

      {!hasData ? (
        <div className="rounded-3xl border border-white/5 bg-[#151B2B] p-16 text-center">
          <Share2 className="w-14 h-14 text-gray-600 mx-auto mb-4" />
          <p className="text-gray-300 font-medium">Chưa có dữ liệu kênh</p>
          <p className="text-gray-500 text-sm mt-2">
            Cần cào dữ liệu từ Facebook, YouTube, TikTok hoặc Tin tức.
          </p>
        </div>
      ) : (
        <>
          <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-3 sm:gap-4">
            <PageMetricCard
              icon={<Crown className="w-4 h-4" />}
              label="Dẫn đầu SOV"
              value={leaderBySov?.label ?? '—'}
              caption={leaderBySov ? `${leaderBySov.mentionShare}% tổng thảo luận` : 'Chưa đủ dữ liệu'}
              accentColor="#A78BFA"
            />
            <PageMetricCard
              icon={<MessageCircle className="w-4 h-4" />}
              label="Nhiều comments"
              value={leaderByComments?.label ?? '—'}
              caption={leaderByComments ? `${leaderByComments.commentShare}% bình luận` : 'Chưa đủ dữ liệu'}
              accentColor="#00B4D8"
            />
            <PageMetricCard
              icon={<TrendingUp className="w-4 h-4" />}
              label="NSR tốt nhất"
              value={bestNsr?.label ?? '—'}
              caption={bestNsr ? `${bestNsr.nsrScore > 0 ? '+' : ''}${bestNsr.nsrScore}% NSR` : 'Chưa đủ dữ liệu'}
              accentColor="#34D399"
            />
            <PageMetricCard
              icon={<ShieldAlert className="w-4 h-4" />}
              label="Cần theo dõi"
              value={highestRisk?.label ?? '—'}
              caption={highestRisk ? `${highestRisk.negativePercent}% tiêu cực` : 'Chưa đủ dữ liệu'}
              accentColor="#FF7575"
            />
          </div>

          <div className="relative overflow-hidden rounded-3xl border border-white/5 bg-gradient-to-br from-[#151B2B] via-[#12182A] to-[#0F1524] p-6">
            <div className="absolute -top-20 -right-20 w-56 h-56 rounded-full bg-purple-500/10 blur-3xl pointer-events-none" />

            <div className="relative flex flex-col lg:flex-row lg:items-start lg:justify-between gap-4 mb-6">
              <div className="flex items-start gap-3">
                <div className="mt-0.5 rounded-xl border border-white/10 bg-white/[0.04] p-2">
                  <BarChart3 className="w-5 h-5 text-purple-400" />
                </div>
                <div>
                  <h3 className="font-bold text-white text-lg">Phân bổ thảo luận</h3>
                  <p className="text-xs text-gray-500 mt-1">
                    {formatNumber(data.totalMentions)} mentions · {formatNumber(data.totalComments)} comments
                  </p>
                </div>
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

            <div className="relative grid grid-cols-1 xl:grid-cols-2 gap-8">
              <div className="flex flex-col sm:flex-row items-center gap-6">
                <div className="relative w-[200px] h-[200px] shrink-0">
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
                        dataKey="mentions"
                        stroke="rgba(255,255,255,0.05)"
                        strokeWidth={2}
                        cornerRadius={4}
                      >
                        {pieData.map((channel) => (
                          <Cell key={channel.platform} fill={channel.color} />
                        ))}
                      </Pie>
                    </PieChart>
                  </ResponsiveContainer>
                  <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
                    <div className="text-center">
                      <p className="text-2xl font-black text-white tabular-nums">{formatNumber(data.totalMentions)}</p>
                      <p className="text-[10px] text-gray-500 uppercase">Mentions</p>
                    </div>
                  </div>
                </div>
                <div className="flex-1 w-full space-y-4">
                  {pieData.map((channel) => (
                    <div
                      key={channel.platform}
                      onMouseEnter={() => setActivePlatform(channel.platform)}
                      onMouseLeave={() => setActivePlatform(null)}
                    >
                      <div className="flex justify-between items-center gap-2 mb-1.5">
                        <span className={`px-2 py-0.5 rounded-md text-xs font-semibold ${getPlatformBadgeClass(channel.platform)}`}>
                          {channel.label}
                        </span>
                        <span className="font-bold tabular-nums text-sm" style={{ color: channel.color }}>
                          {channel.mentionShare}%
                        </span>
                      </div>
                      <div className="h-2.5 rounded-full bg-white/5 overflow-hidden">
                        <div
                          className="h-full rounded-full transition-all duration-700"
                          style={{ width: `${channel.mentionShare}%`, background: channel.color }}
                        />
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              <div className="space-y-5">
                <p className="text-xs text-gray-500 font-semibold uppercase tracking-wider">Volume & tương tác</p>
                {channels.map((channel) => {
                  const color = getPlatformChartColor(channel.platform);
                  const mentionWidth = (channel.mentions / maxMentions) * 100;
                  const commentWidth = (channel.totalComments / maxComments) * 100;
                  return (
                    <div
                      key={`vol-${channel.platform}`}
                      className={`rounded-xl border px-4 py-3 transition-all ${
                        activePlatform === channel.platform ? 'border-white/15 bg-white/[0.04]' : 'border-white/5 bg-white/[0.02]'
                      }`}
                      onMouseEnter={() => setActivePlatform(channel.platform)}
                      onMouseLeave={() => setActivePlatform(null)}
                    >
                      <div className="flex justify-between items-center mb-2">
                        <span className="text-sm font-semibold text-gray-200">{channel.label}</span>
                        <span className="text-xs text-gray-500 tabular-nums">
                          {formatNumber(channel.mentions)} mentions · {formatNumber(channel.totalComments)} comments
                        </span>
                      </div>
                      <div className="space-y-1.5">
                        <div className="flex items-center gap-2">
                          <span className="text-[10px] text-gray-500 w-16">Mentions</span>
                          <div className="flex-1 h-2 rounded-full bg-white/5 overflow-hidden">
                            <div className="h-full rounded-full" style={{ width: `${mentionWidth}%`, background: color }} />
                          </div>
                        </div>
                        <div className="flex items-center gap-2">
                          <span className="text-[10px] text-gray-500 w-16">Comments</span>
                          <div className="flex-1 h-2 rounded-full bg-white/5 overflow-hidden">
                            <div
                              className="h-full rounded-full opacity-80"
                              style={{ width: `${commentWidth}%`, background: SENTIMENT_COLORS.positive }}
                            />
                          </div>
                        </div>
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {channels.map((channel) => (
              <div
                key={channel.platform}
                onMouseEnter={() => setActivePlatform(channel.platform)}
                onMouseLeave={() => setActivePlatform(null)}
              >
                <ChannelDetailCard channel={channel} isHighlighted={activePlatform === channel.platform} />
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  );
};

export default ProjectChannel;
