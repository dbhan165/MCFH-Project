import { useCallback, useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  Users,
  ExternalLink,
  Search,
  Loader2,
  AlertCircle,
  RefreshCw,
  Crown,
  MessageCircle,
  TrendingUp,
  Filter,
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
} from 'recharts';
import { projectApi } from '../api/projectApi';
import type { InfluencerAnalytics, ProjectInfluencer } from '../types/project';
import { extractApiError } from '../utils/authStorage';
import { buildSovSlices, formatFollowers, getAvatarColor, getAvatarInitials } from '../utils/influencerHelpers';
import {
  formatNumber,
  getPlatformBadgeClass,
  getPlatformDisplayName,
  getSentimentBadgeClass,
  getSentimentLabel,
  MENTION_PLATFORMS,
  type MentionPlatformFilter,
} from '../utils/sentimentHelpers';

const CHART_TOOLTIP = {
  backgroundColor: '#0A101D',
  border: '1px solid rgba(255,255,255,0.08)',
  borderRadius: 12,
  padding: '10px 14px',
};

function SovTooltip({
  active,
  payload,
}: {
  active?: boolean;
  payload?: Array<{ payload: { name: string; value: number; mentions: number } }>;
}) {
  if (!active || !payload?.[0]) return null;
  const item = payload[0].payload;
  return (
    <div style={CHART_TOOLTIP}>
      <p className="text-sm font-semibold text-white">{item.name}</p>
      <p className="text-xs text-gray-400 mt-1">
        SOV {item.value}% · {item.mentions} mentions
      </p>
    </div>
  );
}

function Avatar({ name, id }: { name: string; id: string }) {
  const initials = getAvatarInitials(name);
  const bg = getAvatarColor(id);
  return (
    <div
      className="w-11 h-11 rounded-full flex items-center justify-center text-sm font-bold text-white shrink-0 border border-white/10"
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

  const rankedInfluencers = useMemo(() => sortInfluencers(filtered), [filtered]);
  const hasActiveFilter = Boolean(search.trim()) || platformFilter !== 'all';
  const activeList = hasActiveFilter ? rankedInfluencers : sortInfluencers(data?.influencers ?? []);

  const sovSlices = useMemo(() => buildSovSlices(activeList), [activeList]);
  const topSov = activeList[0];
  const topByComments = [...activeList].sort((a, b) => b.totalComments - a.totalComments)[0];
  const topByInfluence = [...activeList].sort((a, b) => b.influenceScore - a.influenceScore)[0];
  const followerCoverage = activeList.filter((item) => (item.followers ?? 0) > 0).length;
  const profileCoverage = activeList.filter((item) => item.handleUrl).length;
  const totalComments = activeList.reduce((sum, item) => sum + item.totalComments, 0);
  const avgMentions = activeList.length > 0 ? Math.round((activeList.reduce((sum, item) => sum + item.mentions, 0) / activeList.length) * 10) / 10 : 0;

  const sovBarData = activeList.slice(0, 8).map((kol) => ({
    name: kol.name.length > 18 ? `${kol.name.slice(0, 16)}…` : kol.name,
    fullName: kol.name,
    sov: kol.shareOfVoice,
    mentions: kol.mentions,
  }));

  const sentimentBarData = activeList.slice(0, 6).map((kol) => ({
    name: kol.name.length > 16 ? `${kol.name.slice(0, 14)}…` : kol.name,
    fullName: kol.name,
    positive: kol.positiveCount,
    neutral: kol.neutralCount,
    negative: kol.negativeCount,
  }));

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
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-white/5 border border-white/10 text-xs text-gray-400 mb-4">
              <Crown className="w-3.5 h-3.5 text-[#00B4D8]" />
              Creator voice mapping
            </div>
            <h2 className="text-3xl font-bold text-white flex items-center gap-3">
              <Users className="text-[#00B4D8] w-8 h-8" />
              KOLs & Influencers
            </h2>
            <p className="text-gray-400 text-sm mt-3 leading-relaxed">
              Màn hình này trả lời 3 câu hỏi: ai đang tạo nhiều tiếng nói nhất, ai tạo nhiều bình luận nhất, và ai đang
              mang sentiment tích cực / tiêu cực rõ nhất trong tập dữ liệu hiện tại.
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
          <Users className="w-14 h-14 text-gray-600 mx-auto mb-4" />
          <p className="text-gray-300 font-medium">Chua phat hien KOL nao</p>
          <p className="text-gray-500 text-sm mt-2 max-w-md mx-auto">
            Cần dữ liệu Facebook, YouTube, TikTok hoặc News để hệ thống nhóm tác giả / kênh và tính share of voice.
          </p>
        </div>
      ) : (
        <>
          <div className="grid grid-cols-2 xl:grid-cols-5 gap-4">
            <div className="rounded-2xl border border-white/5 bg-[#151B2B] p-5">
              <p className="text-xs text-gray-500 uppercase tracking-wide font-semibold">Creator hiển thị</p>
              <p className="text-3xl font-bold text-white mt-2 tabular-nums">{formatNumber(activeList.length)}</p>
              <p className="text-xs text-gray-500 mt-2">Sau bộ lọc hiện tại</p>
            </div>
            <div className="rounded-2xl border border-white/5 bg-[#151B2B] p-5">
              <p className="text-xs text-gray-500 uppercase tracking-wide font-semibold">Tổng comments</p>
              <p className="text-3xl font-bold text-white mt-2 tabular-nums">{formatNumber(totalComments)}</p>
              <p className="text-xs text-gray-500 mt-2">Bình luận gom từ creator đang hiển thị</p>
            </div>
            <div className="rounded-2xl border border-white/5 bg-[#151B2B] p-5">
              <p className="text-xs text-gray-500 uppercase tracking-wide font-semibold">TB mentions / creator</p>
              <p className="text-3xl font-bold text-[#00B4D8] mt-2 tabular-nums">{avgMentions}</p>
              <p className="text-xs text-gray-500 mt-2">Muc do tap trung thao luan</p>
            </div>
            <div className="rounded-2xl border border-white/5 bg-[#151B2B] p-5">
              <p className="text-xs text-gray-500 uppercase tracking-wide font-semibold">Co follower data</p>
              <p className="text-3xl font-bold text-yellow-400 mt-2 tabular-nums">{formatNumber(followerCoverage)}</p>
              <p className="text-xs text-gray-500 mt-2">
                {activeList.length > 0 ? Math.round((followerCoverage / activeList.length) * 100) : 0}% số creator đang hiển thị
              </p>
            </div>
            <div className="rounded-2xl border border-white/5 bg-[#151B2B] p-5">
              <p className="text-xs text-gray-500 uppercase tracking-wide font-semibold">Co link profile</p>
              <p className="text-3xl font-bold text-violet-400 mt-2 tabular-nums">{formatNumber(profileCoverage)}</p>
              <p className="text-xs text-gray-500 mt-2">Phục vụ click sang bài / kênh gốc</p>
            </div>
          </div>

          <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
            <div className="rounded-3xl border border-white/5 bg-[#151B2B] p-6">
              <p className="text-xs text-[#00B4D8] font-semibold uppercase tracking-wide mb-3">Dẫn đầu share of voice</p>
              <p className="text-lg font-bold text-white">{topSov?.name ?? 'Không có dữ liệu'}</p>
              <p className="text-sm text-gray-400 mt-2">
                {topSov ? `${topSov.shareOfVoice}% SOV · ${topSov.mentions} mentions` : 'Không đủ dữ liệu để xếp hạng.'}
              </p>
            </div>

            <div className="rounded-3xl border border-white/5 bg-[#151B2B] p-6">
              <p className="text-xs text-yellow-400 font-semibold uppercase tracking-wide mb-3">Tạo nhiều bình luận nhất</p>
              <p className="text-lg font-bold text-white">{topByComments?.name ?? 'Không có dữ liệu'}</p>
              <p className="text-sm text-gray-400 mt-2">
                {topByComments
                  ? `${formatNumber(topByComments.totalComments)} comments tu ${topByComments.mentions} mentions`
                  : 'Không đủ dữ liệu để xếp hạng.'}
              </p>
            </div>

            <div className="rounded-3xl border border-white/5 bg-[#151B2B] p-6">
              <p className="text-xs text-violet-400 font-semibold uppercase tracking-wide mb-3">Diem anh huong cao nhat</p>
              <p className="text-lg font-bold text-white">{topByInfluence?.name ?? 'Không có dữ liệu'}</p>
              <p className="text-sm text-gray-400 mt-2">
                {topByInfluence
                  ? `Score ${topByInfluence.influenceScore} · xu hướng ${topByInfluence.dominantSentiment ? getSentimentLabel(topByInfluence.dominantSentiment) : 'chưa rõ'}`
                  : 'Không đủ dữ liệu để xếp hạng.'}
              </p>
            </div>
          </div>

          <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
            <div className="bg-[#151B2B] border border-white/5 rounded-3xl p-6">
              <h3 className="font-bold text-white mb-1">Share of Voice</h3>
              <p className="text-xs text-gray-500 mb-4">Tỉ lệ thảo luận của từng creator trong tập dữ liệu đang xem</p>

              {activeList.length === 0 ? (
                <div className="h-[260px] flex items-center justify-center rounded-2xl border border-dashed border-white/10 text-sm text-gray-500">
                  Không có creator nào khớp bộ lọc hiện tại.
                </div>
              ) : (
                <div className="flex flex-col md:flex-row items-center gap-6">
                  <div className="relative w-full max-w-[240px] h-[240px]">
                    <ResponsiveContainer width="100%" height="100%">
                      <PieChart>
                        <Pie
                          data={sovSlices}
                          cx="50%"
                          cy="50%"
                          innerRadius={62}
                          outerRadius={100}
                          paddingAngle={2}
                          dataKey="value"
                          stroke="none"
                          animationDuration={800}
                        >
                          {sovSlices.map((slice) => (
                            <Cell key={slice.name} fill={slice.color} />
                          ))}
                        </Pie>
                        <Tooltip content={<SovTooltip />} />
                      </PieChart>
                    </ResponsiveContainer>
                    <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
                      <div className="text-center">
                        <p className="text-2xl font-black text-white">{activeList.length}</p>
                        <p className="text-[10px] text-gray-500 uppercase tracking-wide">creators</p>
                      </div>
                    </div>
                  </div>

                  <div className="flex-1 w-full space-y-3">
                    {sovSlices.map((slice) => (
                      <div key={slice.name}>
                        <div className="flex justify-between text-sm mb-1">
                          <span className="text-gray-300 truncate pr-2">{slice.name}</span>
                          <span className="font-bold tabular-nums shrink-0" style={{ color: slice.color }}>
                            {slice.value}%
                          </span>
                        </div>
                        <div className="w-full bg-[#0A101D] h-2 rounded-full overflow-hidden">
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

            <div className="bg-[#151B2B] border border-white/5 rounded-3xl p-6">
              <h3 className="font-bold text-white flex items-center gap-2 mb-1">
                <TrendingUp className="w-5 h-5 text-[#00B4D8]" />
                Xep hang SOV
              </h3>
              <p className="text-xs text-gray-500 mb-4">Top creator theo share of voice trong tập hiện tại</p>

              <ResponsiveContainer width="100%" height={280}>
                <BarChart data={sovBarData} margin={{ top: 4, right: 8, left: -12, bottom: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.04)" vertical={false} />
                  <XAxis
                    dataKey="name"
                    tick={{ fill: '#6B7280', fontSize: 10 }}
                    axisLine={false}
                    tickLine={false}
                    interval={0}
                    angle={-20}
                    textAnchor="end"
                    height={56}
                  />
                  <YAxis tick={{ fill: '#6B7280', fontSize: 11 }} axisLine={false} tickLine={false} unit="%" />
                  <Tooltip
                    content={({ active, payload }) => {
                      if (!active || !payload?.[0]) return null;
                      const row = payload[0].payload as { fullName: string; sov: number; mentions: number };
                      return (
                        <div style={CHART_TOOLTIP}>
                          <p className="text-sm font-semibold text-white">{row.fullName}</p>
                          <p className="text-xs text-gray-400 mt-1">
                            SOV {row.sov}% · {row.mentions} mentions
                          </p>
                        </div>
                      );
                    }}
                    cursor={{ fill: 'rgba(255,255,255,0.03)' }}
                  />
                  <Bar dataKey="sov" name="SOV" fill="#00B4D8" radius={[6, 6, 0, 0]} maxBarSize={48} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </div>

          <div className="bg-[#151B2B] border border-white/5 rounded-3xl p-6">
            <h3 className="font-bold text-white mb-1">Phân bố sentiment của top creator</h3>
            <p className="text-xs text-gray-500 mb-4">Cho biết creator nào đang tạo lượng nhắc tích cực / tiêu cực rõ nhất</p>

            {sentimentBarData.length === 0 ? (
              <div className="h-[260px] flex items-center justify-center rounded-2xl border border-dashed border-white/10 text-sm text-gray-500">
                Không có dữ liệu creator khớp bộ lọc hiện tại.
              </div>
            ) : (
              <ResponsiveContainer width="100%" height={280}>
                <BarChart data={sentimentBarData} margin={{ top: 8, right: 8, left: -12, bottom: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.04)" vertical={false} />
                  <XAxis
                    dataKey="name"
                    tick={{ fill: '#6B7280', fontSize: 10 }}
                    axisLine={false}
                    tickLine={false}
                    interval={0}
                    angle={-20}
                    textAnchor="end"
                    height={56}
                  />
                  <YAxis allowDecimals={false} tick={{ fill: '#6B7280', fontSize: 11 }} axisLine={false} tickLine={false} />
                  <Tooltip
                    content={({ active, payload }) => {
                      if (!active || !payload?.length) return null;
                      const row = payload[0].payload as { fullName: string; positive: number; neutral: number; negative: number };
                      return (
                        <div style={CHART_TOOLTIP}>
                          <p className="text-sm font-semibold text-white">{row.fullName}</p>
                          <p className="text-xs text-[#00B4D8] mt-1">Tích cực: {row.positive}</p>
                          <p className="text-xs text-yellow-400">Trung lập: {row.neutral}</p>
                          <p className="text-xs text-[#FF7575]">Tiêu cực: {row.negative}</p>
                        </div>
                      );
                    }}
                    cursor={{ fill: 'rgba(255,255,255,0.03)' }}
                  />
                  <Bar dataKey="positive" name="Tích cực" stackId="s" fill="#00B4D8" />
                  <Bar dataKey="neutral" name="Trung lập" stackId="s" fill="#EAB308" />
                  <Bar dataKey="negative" name="Tiêu cực" stackId="s" fill="#FF7575" radius={[6, 6, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </div>

          <div className="bg-[#151B2B] border border-white/5 rounded-3xl overflow-hidden">
            <div className="p-6 border-b border-white/5 flex flex-col lg:flex-row lg:items-center justify-between gap-4">
              <div>
                <h3 className="font-bold text-white">Danh sách creator</h3>
                <p className="text-xs text-gray-500 mt-1">
                  {activeList.length} kết quả · xếp hạng đã được sắp xếp theo SOV, comments, rồi đến mentions
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
                    className="pl-9 pr-4 py-2.5 bg-[#0A101D] border border-white/10 rounded-xl text-sm text-white focus:border-[#00B4D8] focus:outline-none w-56"
                  />
                </div>
                <div className="flex items-center gap-1 p-1 bg-[#0A101D] rounded-xl border border-white/5">
                  <Filter className="w-4 h-4 text-gray-500 ml-2 shrink-0" />
                  {MENTION_PLATFORMS.map((platform) => (
                    <button
                      key={platform}
                      type="button"
                      onClick={() => setPlatformFilter(platform)}
                      className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-colors ${
                        platformFilter === platform
                          ? 'bg-[#00B4D8]/20 text-[#00B4D8]'
                          : 'text-gray-500 hover:text-gray-300'
                      }`}
                    >
                      {platform === 'all' ? 'Tất cả' : getPlatformDisplayName(platform)}
                    </button>
                  ))}
                </div>
              </div>
            </div>

            <div className="overflow-x-auto">
              <table className="w-full text-left">
                <thead>
                  <tr className="bg-white/[0.02] border-b border-white/5">
                    <th className="px-6 py-4 text-xs font-bold text-gray-500 uppercase tracking-wider">#</th>
                    <th className="px-6 py-4 text-xs font-bold text-gray-500 uppercase tracking-wider">Creator</th>
                    <th className="px-6 py-4 text-xs font-bold text-gray-500 uppercase tracking-wider">Nen tang</th>
                    <th className="px-6 py-4 text-xs font-bold text-gray-500 uppercase tracking-wider text-right">SOV</th>
                    <th className="px-6 py-4 text-xs font-bold text-gray-500 uppercase tracking-wider text-right">Mentions</th>
                    <th className="px-6 py-4 text-xs font-bold text-gray-500 uppercase tracking-wider text-right">
                      <span className="inline-flex items-center gap-1 justify-end">
                        <MessageCircle className="w-3.5 h-3.5" /> Comments
                      </span>
                    </th>
                    <th className="px-6 py-4 text-xs font-bold text-gray-500 uppercase tracking-wider text-center">Sentiment</th>
                    <th className="px-6 py-4 text-xs font-bold text-gray-500 uppercase tracking-wider text-right">Score</th>
                    <th className="px-6 py-4 text-xs font-bold text-gray-500 uppercase tracking-wider text-right">Link</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-white/5">
                  {activeList.length === 0 ? (
                    <tr>
                      <td colSpan={9} className="px-6 py-12 text-center text-gray-500 text-sm">
                        Không tìm thấy creator phù hợp bộ lọc.
                      </td>
                    </tr>
                  ) : (
                    activeList.map((kol, index) => <InfluencerRow key={kol.id} kol={kol} rank={index + 1} />)
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </>
      )}
    </div>
  );
};

function InfluencerRow({ kol, rank }: { kol: ProjectInfluencer; rank: number }) {
  return (
    <tr className="hover:bg-white/[0.02] transition-colors group">
      <td className="px-6 py-4">
        <span
          className={`inline-flex w-7 h-7 items-center justify-center rounded-lg text-xs font-bold ${
            rank <= 3 ? 'bg-[#00B4D8]/20 text-[#00B4D8]' : 'bg-white/5 text-gray-500'
          }`}
        >
          {rank}
        </span>
      </td>
      <td className="px-6 py-4">
        <div className="flex items-center gap-3 min-w-[220px]">
          <Avatar name={kol.name} id={kol.id} />
          <div className="min-w-0">
            <p className="font-bold text-white truncate">{kol.name}</p>
            <p className="text-xs text-gray-500">
              {formatFollowers(kol.followers)} followers · {kol.handleUrl ? 'có profile link' : 'thiếu profile link'}
            </p>
          </div>
        </div>
      </td>
      <td className="px-6 py-4">
        <span className={`inline-block px-2.5 py-1 rounded-lg text-xs font-semibold ${getPlatformBadgeClass(kol.platform)}`}>
          {getPlatformDisplayName(kol.platform)}
        </span>
      </td>
      <td className="px-6 py-4 text-right">
        <span className="text-sm font-bold text-[#00B4D8] tabular-nums">{kol.shareOfVoice}%</span>
      </td>
      <td className="px-6 py-4 text-right text-sm font-semibold text-white tabular-nums">{kol.mentions}</td>
      <td className="px-6 py-4 text-right text-sm text-gray-400 tabular-nums">{formatNumber(kol.totalComments)}</td>
      <td className="px-6 py-4 text-center">
        {kol.dominantSentiment ? (
          <div className="inline-flex flex-col items-center gap-1">
            <span className={`inline-block px-2.5 py-1 rounded-full text-xs font-semibold border ${getSentimentBadgeClass(kol.dominantSentiment)}`}>
              {getSentimentLabel(kol.dominantSentiment)}
            </span>
            <span className="text-[10px] text-gray-500">
              +{kol.positiveCount} / ={kol.neutralCount} / -{kol.negativeCount}
            </span>
          </div>
        ) : (
          <span className="text-xs text-gray-600">--</span>
        )}
      </td>
      <td className="px-6 py-4 text-right">
        <span className="text-sm font-bold text-white tabular-nums">{kol.influenceScore}</span>
      </td>
      <td className="px-6 py-4 text-right">
        {kol.handleUrl ? (
          <a
            href={kol.handleUrl}
            target="_blank"
            rel="noopener noreferrer"
            className="inline-flex p-2 rounded-lg text-gray-500 hover:text-[#00B4D8] hover:bg-[#00B4D8]/10 transition-colors"
          >
            <ExternalLink size={16} />
          </a>
        ) : (
          <span className="text-gray-700">--</span>
        )}
      </td>
    </tr>
  );
}

export default ProjectInfluencers;
