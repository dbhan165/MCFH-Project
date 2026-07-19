import { useCallback, useEffect, useMemo, useState, type ReactNode } from 'react';
import { useParams } from 'react-router-dom';
import {
  Users,
  ExternalLink,
  Search,
  Loader2,
  AlertCircle,
  RefreshCw,
  MessageCircle,
  TrendingUp,
  Filter,
  Crown,
  Star,
  Zap,
  Link2,
  UserCheck,
  BarChart3,
} from 'lucide-react';
import { ResponsiveContainer, PieChart, Pie, Cell } from 'recharts';
import { projectApi } from '../api/projectApi';
import type { InfluencerAnalytics, ProjectInfluencer } from '../types/project';
import { extractApiError } from '../utils/authStorage';
import { buildSovSlices, formatFollowers, getAvatarColor, getAvatarInitials } from '../utils/influencerHelpers';
import {
  formatNumber,
  getPlatformBadgeClass,
  getPlatformChartColor,
  getPlatformDisplayName,
  getSentimentBadgeClass,
  getSentimentLabel,
  MENTION_PLATFORMS,
  SENTIMENT_COLORS,
  type MentionPlatformFilter,
} from '../utils/sentimentHelpers';

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
    <div className="relative overflow-hidden rounded-2xl border border-white/5 bg-gradient-to-br from-[#151B2B] via-[#141A28] to-[#101622] p-4 sm:p-5 transition-all hover:border-white/10 group">
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
            className="h-full rounded-full transition-all duration-700"
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

function LeaderCard({
  rank,
  title,
  name,
  detail,
  accentColor,
}: {
  rank: 1 | 2 | 3;
  title: string;
  name: string;
  detail: string;
  accentColor: string;
}) {
  const icons = { 1: Crown, 2: Star, 3: Zap };
  const Icon = icons[rank];

  return (
    <div
      className="relative overflow-hidden rounded-2xl border border-white/5 bg-gradient-to-br from-[#151B2B] to-[#101622] p-5"
      style={{ boxShadow: `inset 3px 0 0 ${accentColor}` }}
    >
      <div className="flex items-start gap-3">
        <div
          className="p-2.5 rounded-xl border shrink-0"
          style={{ background: `${accentColor}14`, borderColor: `${accentColor}33`, color: accentColor }}
        >
          <Icon className="w-4 h-4" />
        </div>
        <div className="min-w-0">
          <p className="text-[10px] uppercase tracking-wider font-semibold" style={{ color: accentColor }}>
            {title}
          </p>
          <p className="text-lg font-bold text-white truncate mt-1">{name}</p>
          <p className="text-xs text-gray-500 mt-1.5 leading-relaxed">{detail}</p>
        </div>
      </div>
    </div>
  );
}

function Avatar({ name, id }: { name: string; id: string }) {
  const initials = getAvatarInitials(name);
  const bg = getAvatarColor(id);
  return (
    <div
      className="w-11 h-11 rounded-xl flex items-center justify-center text-sm font-bold text-white shrink-0 border border-white/10"
      style={{ background: `linear-gradient(135deg, ${bg}, ${bg}cc)` }}
    >
      {initials}
    </div>
  );
}

function sortInfluencers(rows: ProjectInfluencer[]) {
  return [...rows].sort((a, b) => {
    if (b.shareOfVoice !== a.shareOfVoice) return b.shareOfVoice - a.shareOfVoice;
    if (b.totalComments !== a.totalComments) return b.totalComments - a.totalComments;
    return b.mentions - a.mentions;
  });
}

function InfluencerCard({ kol, rank, maxSov }: { kol: ProjectInfluencer; rank: number; maxSov: number }) {
  const platformColor = getPlatformChartColor(kol.platform);
  const barWidth = maxSov > 0 ? (kol.shareOfVoice / maxSov) * 100 : 0;
  const totalSentiment = kol.positiveCount + kol.neutralCount + kol.negativeCount;

  return (
    <article
      className="rounded-2xl border border-white/5 bg-gradient-to-br from-[#151B2B]/90 to-[#101622] p-4 sm:p-5 hover:border-white/10 transition-all"
      style={{ boxShadow: `inset 3px 0 0 ${platformColor}88` }}
    >
      <div className="flex flex-col sm:flex-row sm:items-center gap-4">
        <div className="flex items-center gap-3 min-w-0 flex-1">
          <span
            className={`inline-flex w-8 h-8 items-center justify-center rounded-lg text-xs font-black shrink-0 ${
              rank <= 3 ? 'bg-[#00B4D8]/20 text-[#00B4D8]' : 'bg-white/5 text-gray-500'
            }`}
          >
            {rank}
          </span>
          <Avatar name={kol.name} id={kol.id} />
          <div className="min-w-0 flex-1">
            <div className="flex flex-wrap items-center gap-2">
              <p className="font-bold text-white truncate">{kol.name}</p>
              <span className={`px-2 py-0.5 rounded-md text-[10px] font-bold ${getPlatformBadgeClass(kol.platform)}`}>
                {getPlatformDisplayName(kol.platform)}
              </span>
            </div>
            <p className="text-[11px] text-gray-500 mt-1">
              {kol.platform === 'news'
                ? kol.handleUrl
                  ? 'Nguồn tin · có link bài'
                  : 'Nguồn tin · thiếu link'
                : `${formatFollowers(kol.followers)} followers`}
            </p>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-4 sm:gap-6 shrink-0">
          <div className="min-w-[120px]">
            <p className="text-[10px] text-gray-500 uppercase tracking-wide">SOV</p>
            <p className="text-lg font-black text-[#00B4D8] tabular-nums">{kol.shareOfVoice}%</p>
          </div>
          <div className="text-center">
            <p className="text-[10px] text-gray-500 uppercase tracking-wide">Mentions</p>
            <p className="text-lg font-bold text-white tabular-nums">{kol.mentions}</p>
          </div>
          <div className="text-center">
            <p className="text-[10px] text-gray-500 uppercase tracking-wide">Comments</p>
            <p className="text-lg font-bold text-gray-300 tabular-nums">{formatNumber(kol.totalComments)}</p>
          </div>
          <div className="text-center">
            <p className="text-[10px] text-gray-500 uppercase tracking-wide">Score</p>
            <p className="text-lg font-bold text-violet-400 tabular-nums">{kol.influenceScore}</p>
          </div>
          {kol.dominantSentiment ? (
            <span className={`px-2.5 py-1 rounded-full text-xs font-semibold border ${getSentimentBadgeClass(kol.dominantSentiment)}`}>
              {getSentimentLabel(kol.dominantSentiment)}
            </span>
          ) : null}
          {kol.handleUrl ? (
            <a
              href={kol.handleUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="p-2 rounded-lg text-gray-500 hover:text-[#00B4D8] hover:bg-[#00B4D8]/10 transition-colors"
            >
              <ExternalLink size={16} />
            </a>
          ) : null}
        </div>
      </div>

      <div className="mt-4">
        <div className="h-2 rounded-full bg-white/5 overflow-hidden">
          <div
            className="h-full rounded-full bg-gradient-to-r from-[#00B4D8] to-[#00B4D8]/70 transition-all duration-500"
            style={{ width: `${Math.max(barWidth, kol.shareOfVoice > 0 ? 4 : 0)}%` }}
          />
        </div>
      </div>

      {totalSentiment > 0 && (
        <div className="mt-3 h-1.5 rounded-full flex overflow-hidden bg-white/5">
          {kol.positiveCount > 0 && (
            <div style={{ flex: kol.positiveCount, background: SENTIMENT_COLORS.positive }} title={`Tích cực: ${kol.positiveCount}`} />
          )}
          {kol.neutralCount > 0 && (
            <div style={{ flex: kol.neutralCount, background: SENTIMENT_COLORS.neutral }} title={`Trung lập: ${kol.neutralCount}`} />
          )}
          {kol.negativeCount > 0 && (
            <div style={{ flex: kol.negativeCount, background: SENTIMENT_COLORS.negative }} title={`Tiêu cực: ${kol.negativeCount}`} />
          )}
        </div>
      )}
    </article>
  );
}

const ProjectInfluencers = () => {
  const { workspaceId, id } = useParams();
  const wid = Number(workspaceId);
  const projectId = Number(id);

  const [data, setData] = useState<InfluencerAnalytics | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');
  const [search, setSearch] = useState('');
  const [platformFilter, setPlatformFilter] = useState<MentionPlatformFilter>('all');

  const loadData = useCallback(async () => {
    if (!wid || !projectId || Number.isNaN(wid) || Number.isNaN(projectId)) return;

    setIsLoading(true);
    setErrorMessage('');
    try {
      setData(await projectApi.getInfluencers(wid, projectId));
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải dữ liệu KOL.'));
    } finally {
      setIsLoading(false);
    }
  }, [wid, projectId]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const filtered = useMemo(() => {
    if (!data) return [];
    return data.influencers.filter((kol) => {
      const matchSearch =
        !search.trim() ||
        kol.name.toLowerCase().includes(search.toLowerCase()) ||
        kol.platform.toLowerCase().includes(search.toLowerCase());
      const matchPlatform = platformFilter === 'all' || kol.platform === platformFilter;
      return matchSearch && matchPlatform;
    });
  }, [data, search, platformFilter]);

  const hasActiveFilter = Boolean(search.trim()) || platformFilter !== 'all';
  const activeList = useMemo(
    () => (hasActiveFilter ? sortInfluencers(filtered) : sortInfluencers(data?.influencers ?? [])),
    [hasActiveFilter, filtered, data?.influencers]
  );

  const sovSlices = useMemo(() => buildSovSlices(activeList), [activeList]);
  const topSov = activeList[0];
  const topByComments = [...activeList].sort((a, b) => b.totalComments - a.totalComments)[0];
  const topByInfluence = [...activeList].sort((a, b) => b.influenceScore - a.influenceScore)[0];
  const followerCoverage = activeList.filter((item) => (item.followers ?? 0) > 0).length;
  const profileCoverage = activeList.filter((item) => item.handleUrl).length;
  const totalComments = activeList.reduce((sum, item) => sum + item.totalComments, 0);
  const avgMentions =
    activeList.length > 0
      ? Math.round((activeList.reduce((sum, item) => sum + item.mentions, 0) / activeList.length) * 10) / 10
      : 0;
  const maxSov = activeList[0]?.shareOfVoice ?? 1;
  const followerPercent = activeList.length > 0 ? Math.round((followerCoverage / activeList.length) * 100) : 0;

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center py-32 text-gray-400 gap-4">
        <div className="relative">
          <div className="absolute inset-0 rounded-full bg-[#00B4D8]/20 blur-2xl animate-pulse" />
          <Loader2 className="relative w-12 h-12 animate-spin text-[#00B4D8]" />
        </div>
        <p className="text-sm">Đang tải KOLs & Influencers...</p>
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

  const hasData = data.influencers.length > 0;

  return (
    <div className="animate-in fade-in duration-500 max-w-7xl mx-auto space-y-8 pb-10">
      <div className="relative overflow-hidden rounded-3xl border border-white/5 bg-gradient-to-br from-[#151B2B] via-[#12182A] to-[#0A101D] p-8">
        <div className="absolute -top-20 -right-20 w-64 h-64 bg-[#00B4D8]/10 rounded-full blur-3xl pointer-events-none" />
        <div className="absolute -bottom-12 -left-12 w-48 h-48 bg-[#FF7575]/10 rounded-full blur-3xl pointer-events-none" />

        <div className="relative flex flex-col lg:flex-row lg:items-start lg:justify-between gap-6">
          <div className="max-w-3xl">
            <h2 className="text-3xl font-bold text-white flex items-center gap-3">
              <Users className="text-[#00B4D8] w-8 h-8" />
              KOL & Người ảnh hưởng
            </h2>
            <p className="text-gray-400 text-sm mt-3 leading-relaxed">
              Xếp hạng creator và nguồn tin theo SOV, bình luận và sentiment — gồm Tin tức.
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
          <Users className="w-14 h-14 text-gray-600 mx-auto mb-4" />
          <p className="text-gray-300 font-medium">Chưa phát hiện KOL nào</p>
          <p className="text-gray-500 text-sm mt-2 max-w-md mx-auto">
            Cần dữ liệu từ Facebook, YouTube, TikTok hoặc Tin tức.
          </p>
        </div>
      ) : (
        <>
          <div className="grid grid-cols-2 xl:grid-cols-5 gap-3 sm:gap-4">
            <PageMetricCard
              icon={<Users className="w-4 h-4" />}
              label="Creator"
              value={formatNumber(activeList.length)}
              caption="Đang hiển thị trong danh sách"
              accentColor="#00B4D8"
            />
            <PageMetricCard
              icon={<MessageCircle className="w-4 h-4" />}
              label="Bình luận"
              value={formatNumber(totalComments)}
              caption="Tổng comment đã gom"
              accentColor="white"
            />
            <PageMetricCard
              icon={<BarChart3 className="w-4 h-4" />}
              label="TB mentions"
              value={String(avgMentions)}
              caption="Trung bình mỗi creator"
              accentColor="#A78BFA"
            />
            <PageMetricCard
              icon={<UserCheck className="w-4 h-4" />}
              label="Có follower"
              value={formatNumber(followerCoverage)}
              caption={`${followerPercent}% creator có dữ liệu follower`}
              accentColor="#EAB308"
              progress={followerPercent}
            />
            <PageMetricCard
              icon={<Link2 className="w-4 h-4" />}
              label="Có link"
              value={formatNumber(profileCoverage)}
              caption="Mở được bài / kênh gốc"
              accentColor="#34D399"
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <LeaderCard
              rank={1}
              title="Dẫn đầu SOV"
              name={topSov?.name ?? '—'}
              detail={topSov ? `${topSov.shareOfVoice}% SOV · ${topSov.mentions} mentions` : 'Chưa đủ dữ liệu'}
              accentColor="#00B4D8"
            />
            <LeaderCard
              rank={2}
              title="Nhiều bình luận nhất"
              name={topByComments?.name ?? '—'}
              detail={
                topByComments
                  ? `${formatNumber(topByComments.totalComments)} comments · ${topByComments.mentions} mentions`
                  : 'Chưa đủ dữ liệu'
              }
              accentColor="#EAB308"
            />
            <LeaderCard
              rank={3}
              title="Điểm ảnh hưởng"
              name={topByInfluence?.name ?? '—'}
              detail={
                topByInfluence
                  ? `Score ${topByInfluence.influenceScore} · ${topByInfluence.dominantSentiment ? getSentimentLabel(topByInfluence.dominantSentiment) : 'chưa rõ'}`
                  : 'Chưa đủ dữ liệu'
              }
              accentColor="#A78BFA"
            />
          </div>

          <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
            <div className="relative overflow-hidden rounded-3xl border border-white/5 bg-gradient-to-br from-[#151B2B] via-[#12182A] to-[#0F1524] p-6">
              <div className="absolute -top-16 -right-16 w-40 h-40 bg-[#00B4D8]/10 rounded-full blur-3xl pointer-events-none" />
              <h3 className="font-bold text-white relative">Tỷ lệ thảo luận (SOV)</h3>
              <p className="text-xs text-gray-500 mt-1 mb-5 relative">Phần trăm thảo luận theo creator</p>

              {activeList.length === 0 ? (
                <p className="text-sm text-gray-500 py-12 text-center">Không có creator khớp bộ lọc.</p>
              ) : (
                <div className="relative flex flex-col md:flex-row items-center gap-6">
                  <div className="relative w-full max-w-[200px] h-[200px] shrink-0">
                    <ResponsiveContainer width="100%" height="100%">
                      <PieChart>
                        <Pie
                          data={sovSlices}
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
                          {sovSlices.map((slice) => (
                            <Cell key={slice.name} fill={slice.color} />
                          ))}
                        </Pie>
                      </PieChart>
                    </ResponsiveContainer>
                    <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
                      <div className="text-center">
                        <p className="text-2xl font-black text-white tabular-nums">{activeList.length}</p>
                        <p className="text-[10px] text-gray-500 uppercase tracking-wide">Creator</p>
                      </div>
                    </div>
                  </div>
                  <div className="flex-1 w-full space-y-3">
                    {sovSlices.map((slice) => (
                      <div key={slice.name}>
                        <div className="flex justify-between text-sm mb-1 gap-2">
                          <span className="text-gray-300 truncate">{slice.name}</span>
                          <span className="font-bold tabular-nums shrink-0" style={{ color: slice.color }}>
                            {slice.value}%
                          </span>
                        </div>
                        <div className="h-2 rounded-full bg-white/5 overflow-hidden">
                          <div
                            className="h-full rounded-full transition-all duration-700"
                            style={{ width: `${Math.min(slice.value, 100)}%`, background: slice.color }}
                          />
                        </div>
                      </div>
                    ))}
                  </div>
                </div>
              )}
            </div>

            <div className="relative overflow-hidden rounded-3xl border border-white/5 bg-gradient-to-br from-[#151B2B] via-[#12182A] to-[#0F1524] p-6">
              <div className="flex items-center gap-2 mb-1">
                <TrendingUp className="w-5 h-5 text-[#00B4D8]" />
                <h3 className="font-bold text-white">Xếp hạng SOV</h3>
              </div>
              <p className="text-xs text-gray-500 mb-5">Top creator theo tỉ lệ thảo luận</p>

              <div className="space-y-3">
                {activeList.slice(0, 8).map((kol, idx) => (
                  <div key={kol.id}>
                    <div className="flex items-center justify-between gap-2 mb-1.5">
                      <div className="flex items-center gap-2 min-w-0">
                        <span className="text-[10px] font-bold text-gray-500 w-4 tabular-nums">{idx + 1}</span>
                        <span className="text-sm text-gray-200 truncate">{kol.name}</span>
                      </div>
                      <span className="text-sm font-bold text-[#00B4D8] tabular-nums shrink-0">{kol.shareOfVoice}%</span>
                    </div>
                    <div className="h-2 rounded-full bg-white/5 overflow-hidden ml-6">
                      <div
                        className="h-full rounded-full bg-gradient-to-r from-[#00B4D8] to-[#00B4D8]/60"
                        style={{ width: `${maxSov > 0 ? (kol.shareOfVoice / maxSov) * 100 : 0}%` }}
                      />
                    </div>
                  </div>
                ))}
              </div>
            </div>
          </div>

          <div className="rounded-3xl border border-white/5 bg-[#151B2B]/80 p-5 sm:p-6">
            <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4 mb-5">
              <div>
                <h3 className="font-bold text-white">Danh sách creator</h3>
                <p className="text-xs text-gray-500 mt-1">
                  {formatNumber(activeList.length)} kết quả · sắp xếp theo SOV
                </p>
              </div>
              <div className="flex flex-wrap items-center gap-3">
                <div className="relative">
                  <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-500" />
                  <input
                    type="text"
                    value={search}
                    onChange={(e) => setSearch(e.target.value)}
                    placeholder="Tìm creator..."
                    className="pl-9 pr-4 py-2.5 bg-[#0A101D] border border-white/10 rounded-xl text-sm text-white focus:border-[#00B4D8]/50 focus:outline-none w-52"
                  />
                </div>
                <div className="flex flex-wrap items-center gap-1.5 p-1 bg-[#0A101D] rounded-xl border border-white/5">
                  <Filter className="w-4 h-4 text-gray-500 ml-2 shrink-0" />
                  {MENTION_PLATFORMS.map((platform) => (
                    <button
                      key={platform}
                      type="button"
                      onClick={() => setPlatformFilter(platform)}
                      className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-colors ${
                        platformFilter === platform
                          ? platform === 'all'
                            ? 'bg-[#00B4D8]/20 text-[#00B4D8]'
                            : `${getPlatformBadgeClass(platform)} border`
                          : 'text-gray-500 hover:text-gray-300'
                      }`}
                    >
                      {platform === 'all' ? 'Tất cả' : getPlatformDisplayName(platform)}
                    </button>
                  ))}
                </div>
              </div>
            </div>

            <div className="flex flex-wrap gap-2 mb-5">
              {[
                { key: 'positive', label: 'Tích cực', color: SENTIMENT_COLORS.positive },
                { key: 'neutral', label: 'Trung lập', color: SENTIMENT_COLORS.neutral },
                { key: 'negative', label: 'Tiêu cực', color: SENTIMENT_COLORS.negative },
              ].map((item) => (
                <span
                  key={item.key}
                  className="inline-flex items-center gap-1.5 text-[11px] text-gray-400 px-2.5 py-1 rounded-full border border-white/10 bg-white/[0.03]"
                >
                  <span className="w-2 h-2 rounded-full" style={{ background: item.color }} />
                  {item.label}
                </span>
              ))}
            </div>

            {activeList.length === 0 ? (
              <p className="text-center text-gray-500 text-sm py-12">Không tìm thấy creator phù hợp bộ lọc.</p>
            ) : (
              <div className="space-y-3">
                {activeList.map((kol, index) => (
                  <InfluencerCard key={kol.id} kol={kol} rank={index + 1} maxSov={maxSov} />
                ))}
              </div>
            )}
          </div>
        </>
      )}
    </div>
  );
};

export default ProjectInfluencers;
