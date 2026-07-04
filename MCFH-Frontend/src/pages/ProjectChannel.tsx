import { useCallback, useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  Share2,
  Loader2,
  AlertCircle,
  RefreshCw,
  MessageCircle,
  BarChart3,
  TrendingUp,
  TrendingDown,
  Minus,
  Layers,
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
  Legend,
} from 'recharts';
import { projectApi } from '../api/projectApi';
import type { ChannelComparison, ChannelStats } from '../types/project';
import { extractApiError } from '../utils/authStorage';
import { formatNumber, getPlatformBadgeClass, getPlatformChartColor, SENTIMENT_COLORS } from '../utils/sentimentHelpers';

const TOOLTIP_STYLE = {
  backgroundColor: '#0A101D',
  border: '1px solid rgba(255,255,255,0.08)',
  borderRadius: 12,
  padding: '10px 14px',
};

function ShareTooltip({
  active,
  payload,
}: {
  active?: boolean;
  payload?: Array<{ payload: { label: string; mentionShare: number; mentions: number } }>;
}) {
  if (!active || !payload?.[0]) return null;
  const item = payload[0].payload;
  return (
    <div style={TOOLTIP_STYLE}>
      <p className="text-sm font-semibold text-white">{item.label}</p>
      <p className="text-xs text-gray-400 mt-1">
        {item.mentions} mentions · {item.mentionShare}% SOV
      </p>
    </div>
  );
}

function NsrIcon({ score }: { score: number }) {
  if (score >= 10) return <TrendingUp className="w-4 h-4 text-[#00B4D8]" />;
  if (score <= -10) return <TrendingDown className="w-4 h-4 text-[#FF7575]" />;
  return <Minus className="w-4 h-4 text-yellow-500" />;
}

const ProjectChannel = () => {
  const { workspaceId, id } = useParams();
  const wid = Number(workspaceId);
  const projectId = Number(id);

  const [data, setData] = useState<ChannelComparison | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');

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

  const pieData = useMemo(
    () =>
      channels.map((channel) => ({
        ...channel,
        color: getPlatformChartColor(channel.platform),
      })),
    [channels]
  );

  const volumeData = useMemo(
    () =>
      channels.map((channel) => ({
        label: channel.label,
        Mentions: channel.mentions,
        Comments: channel.totalComments,
      })),
    [channels]
  );

  const sentimentRateData = useMemo(
    () =>
      channels.map((channel) => ({
        label: channel.label,
        'Tích cực %': channel.positivePercent,
        'Trung lập %': channel.neutralPercent,
        'Tiêu cực %': channel.negativePercent,
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
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-white/5 border border-white/10 text-xs text-gray-400 mb-4">
              <Layers className="w-3.5 h-3.5 text-purple-400" />
              Cross-platform comparison
            </div>
            <h2 className="text-3xl font-bold text-white flex items-center gap-3">
              <Share2 className="text-purple-400 w-8 h-8" />
              Channel Comparison
            </h2>
            <p className="text-gray-400 text-sm mt-3 leading-relaxed">
              Màn hình này giúp user trả lời 4 câu hỏi: kênh nào đang tạo nhiều volume nhất, kênh nào sinh bình luận nhất,
              kênh nào có sentiment tốt nhất, và kênh nào cần ưu tiên theo dõi do rủi ro.
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
        <div className="bg-[#151B2B] border border-white/5 rounded-3xl p-16 text-center">
          <Share2 className="w-14 h-14 text-gray-600 mx-auto mb-4" />
          <p className="text-gray-300 font-medium">Chưa có dữ liệu kênh</p>
          <p className="text-gray-500 text-sm mt-2">Cần cào dữ liệu từ Facebook, YouTube, TikTok hoặc News để bắt đầu so sánh.</p>
        </div>
      ) : (
        <>
          <div className="grid grid-cols-1 xl:grid-cols-4 gap-4">
            <SignalCard
              title="Dẫn đầu SOV"
              value={leaderBySov?.label ?? 'Không có'}
              detail={leaderBySov ? `${leaderBySov.mentionShare}% share of voice` : 'Không đủ dữ liệu'}
              accent="text-purple-400"
            />
            <SignalCard
              title="Nhiều comments nhất"
              value={leaderByComments?.label ?? 'Không có'}
              detail={leaderByComments ? `${leaderByComments.commentShare}% tổng comments` : 'Không đủ dữ liệu'}
              accent="text-[#00B4D8]"
            />
            <SignalCard
              title="NSR tot nhat"
              value={bestNsr?.label ?? 'Không có'}
              detail={bestNsr ? `${bestNsr.nsrScore > 0 ? '+' : ''}${bestNsr.nsrScore}% NSR` : 'Không đủ dữ liệu'}
              accent="text-emerald-400"
            />
            <SignalCard
              title="Cần theo dõi"
              value={highestRisk?.label ?? 'Không có'}
              detail={highestRisk ? `${highestRisk.negativePercent}% tiêu cực` : 'Không đủ dữ liệu'}
              accent="text-[#FF7575]"
            />
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-4">
            {channels.map((channel) => (
              <ChannelCard key={channel.platform} channel={channel} />
            ))}
          </div>

          <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
            <div className="bg-[#151B2B] border border-white/5 rounded-3xl p-6">
              <h3 className="font-bold text-white mb-1">Tỉ trọng thảo luận theo kênh</h3>
              <p className="text-xs text-gray-500 mb-4">Cho biết khảo sát đang được tạo nhiều nhất ở đâu</p>

              <div className="flex flex-col sm:flex-row items-center gap-6">
                <div className="relative w-[220px] h-[220px] shrink-0">
                  <ResponsiveContainer width="100%" height="100%">
                    <PieChart>
                      <Pie
                        data={pieData}
                        cx="50%"
                        cy="50%"
                        innerRadius={58}
                        outerRadius={95}
                        paddingAngle={3}
                        dataKey="mentions"
                        stroke="none"
                        animationDuration={800}
                      >
                        {pieData.map((channel) => (
                          <Cell key={channel.platform} fill={channel.color} />
                        ))}
                      </Pie>
                      <Tooltip content={<ShareTooltip />} />
                    </PieChart>
                  </ResponsiveContainer>
                  <div className="absolute inset-0 flex items-center justify-center pointer-events-none">
                    <div className="text-center">
                      <p className="text-2xl font-black text-white">{data.totalMentions}</p>
                      <p className="text-[10px] text-gray-500 uppercase">mentions</p>
                    </div>
                  </div>
                </div>

                <div className="flex-1 w-full space-y-4">
                  {pieData.map((channel) => (
                    <div key={channel.platform}>
                      <div className="flex justify-between text-sm mb-1.5">
                        <span className={`px-2 py-0.5 rounded-md text-xs font-semibold ${getPlatformBadgeClass(channel.platform)}`}>
                          {channel.label}
                        </span>
                        <span className="font-bold tabular-nums" style={{ color: channel.color }}>
                          {channel.mentionShare}%
                        </span>
                      </div>
                      <div className="w-full bg-[#0A101D] h-2.5 rounded-full overflow-hidden">
                        <div
                          className="h-full rounded-full transition-all duration-700"
                          style={{ width: `${channel.mentionShare}%`, background: channel.color }}
                        />
                      </div>
                      <p className="text-[11px] text-gray-600 mt-1">
                        {formatNumber(channel.mentions)} mentions · {formatNumber(channel.totalComments)} comments
                      </p>
                    </div>
                  ))}
                </div>
              </div>
            </div>

            <div className="bg-[#151B2B] border border-white/5 rounded-3xl p-6">
              <h3 className="font-bold text-white flex items-center gap-2 mb-1">
                <BarChart3 className="w-5 h-5 text-purple-400" />
                Luong volume va comments
              </h3>
              <p className="text-xs text-gray-500 mb-4">So sánh quy mô thảo luận và mức độ tương tác trên từng kênh</p>

              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={volumeData} margin={{ top: 8, right: 8, left: -12, bottom: 0 }}>
                  <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.04)" vertical={false} />
                  <XAxis dataKey="label" tick={{ fill: '#6B7280', fontSize: 11 }} axisLine={false} tickLine={false} />
                  <YAxis allowDecimals={false} tick={{ fill: '#6B7280', fontSize: 11 }} axisLine={false} tickLine={false} />
                  <Tooltip
                    contentStyle={TOOLTIP_STYLE}
                    labelStyle={{ color: '#9CA3AF', fontSize: 12 }}
                    itemStyle={{ color: '#fff', fontSize: 13 }}
                    cursor={{ fill: 'rgba(255,255,255,0.03)' }}
                  />
                  <Legend wrapperStyle={{ fontSize: 12, color: '#9CA3AF', paddingTop: 12 }} iconType="circle" iconSize={8} />
                  <Bar dataKey="Mentions" fill="#A78BFA" radius={[6, 6, 0, 0]} maxBarSize={40} />
                  <Bar dataKey="Comments" fill="#00B4D8" radius={[6, 6, 0, 0]} maxBarSize={40} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </div>

          <div className="bg-[#151B2B] border border-white/5 rounded-3xl p-6">
            <h3 className="font-bold text-white mb-1">Chất lượng sentiment theo kênh</h3>
            <p className="text-xs text-gray-500 mb-4">Đọc nhanh tỉ lệ tích cực / trung lập / tiêu cực để biết kênh nào cần ưu tiên xử lý</p>

            <ResponsiveContainer width="100%" height={320}>
              <BarChart data={sentimentRateData} margin={{ top: 8, right: 8, left: -12, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.04)" vertical={false} />
                <XAxis dataKey="label" tick={{ fill: '#6B7280', fontSize: 11 }} axisLine={false} tickLine={false} />
                <YAxis tick={{ fill: '#6B7280', fontSize: 11 }} axisLine={false} tickLine={false} unit="%" />
                <Tooltip
                  contentStyle={TOOLTIP_STYLE}
                  labelStyle={{ color: '#9CA3AF' }}
                  itemStyle={{ color: '#fff' }}
                  cursor={{ fill: 'rgba(255,255,255,0.03)' }}
                />
                <Legend wrapperStyle={{ fontSize: 12, color: '#9CA3AF' }} iconType="circle" iconSize={8} />
                <Bar dataKey="Tích cực %" fill={SENTIMENT_COLORS.positive} radius={[6, 6, 0, 0]} maxBarSize={38} />
                <Bar dataKey="Trung lập %" fill={SENTIMENT_COLORS.neutral} maxBarSize={38} />
                <Bar dataKey="Tiêu cực %" fill={SENTIMENT_COLORS.negative} maxBarSize={38} />
              </BarChart>
            </ResponsiveContainer>
          </div>

          <div className="bg-[#151B2B] border border-white/5 rounded-3xl overflow-hidden">
            <div className="p-6 border-b border-white/5">
              <h3 className="font-bold text-white">Bang so sanh chi tiet</h3>
              <p className="text-xs text-gray-500 mt-1">Toàn bộ KPI quan trọng cho từng kênh: volume, comment share, sentiment và độ phủ AI</p>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full text-left">
                <thead>
                  <tr className="bg-white/[0.02] border-b border-white/5">
                    {['Nền tảng', 'Mentions', '% SOV', 'Comments', '% Comments', 'Độ phủ AI', 'NSR', '% Tích cực', '% Tiêu cực'].map((header) => (
                      <th key={header} className="px-5 py-4 text-xs font-bold text-gray-500 uppercase tracking-wider whitespace-nowrap">
                        {header}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-white/5">
                  {channels.map((channel) => {
                    const analyzed = channel.mentions - channel.unanalyzed;
                    const coverage = channel.mentions > 0 ? Math.round((analyzed / channel.mentions) * 100) : 0;
                    return (
                      <tr key={channel.platform} className="hover:bg-white/[0.02] transition-colors">
                        <td className="px-5 py-4">
                          <span className={`inline-block px-2.5 py-1 rounded-lg text-xs font-semibold ${getPlatformBadgeClass(channel.platform)}`}>
                            {channel.label}
                          </span>
                        </td>
                        <td className="px-5 py-4 text-sm font-bold text-white tabular-nums">{channel.mentions}</td>
                        <td className="px-5 py-4 text-sm font-semibold tabular-nums" style={{ color: getPlatformChartColor(channel.platform) }}>
                          {channel.mentionShare}%
                        </td>
                        <td className="px-5 py-4 text-sm text-gray-300 tabular-nums">
                          <span className="inline-flex items-center gap-1">
                            <MessageCircle className="w-3.5 h-3.5 text-gray-600" />
                            {formatNumber(channel.totalComments)}
                          </span>
                        </td>
                        <td className="px-5 py-4 text-sm text-gray-400 tabular-nums">{channel.commentShare}%</td>
                        <td className="px-5 py-4 text-sm text-white tabular-nums">{coverage}%</td>
                        <td className="px-5 py-4">
                          <span
                            className={`inline-flex items-center gap-1 text-sm font-bold tabular-nums ${
                              channel.nsrScore >= 10 ? 'text-[#00B4D8]' : channel.nsrScore <= -10 ? 'text-[#FF7575]' : 'text-yellow-500'
                            }`}
                          >
                            <NsrIcon score={channel.nsrScore} />
                            {channel.nsrScore > 0 ? '+' : ''}
                            {channel.nsrScore}%
                          </span>
                        </td>
                        <td className="px-5 py-4 text-sm text-[#00B4D8] tabular-nums">{channel.positivePercent}%</td>
                        <td className="px-5 py-4 text-sm text-[#FF7575] tabular-nums">{channel.negativePercent}%</td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          </div>
        </>
      )}
    </div>
  );
};

function SignalCard({
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
      <p className={`text-2xl font-bold mt-2 ${accent}`}>{value}</p>
      <p className="text-xs text-gray-500 mt-2">{detail}</p>
    </div>
  );
}

function ChannelCard({ channel }: { channel: ChannelStats }) {
  const color = getPlatformChartColor(channel.platform);
  const analyzed = channel.mentions - channel.unanalyzed;
  const coverage = channel.mentions > 0 ? Math.round((analyzed / channel.mentions) * 100) : 0;

  return (
    <div className="relative overflow-hidden rounded-2xl border border-white/5 bg-[#151B2B] p-5">
      <div className="absolute top-0 left-0 w-full h-1" style={{ background: color }} />
      <div className="flex items-start justify-between gap-3">
        <span className={`inline-block px-2.5 py-1 rounded-lg text-xs font-semibold ${getPlatformBadgeClass(channel.platform)}`}>
          {channel.label}
        </span>
        <span className="text-xs font-bold tabular-nums" style={{ color }}>
          {channel.mentionShare}%
        </span>
      </div>
      <div className="mt-4 grid grid-cols-2 gap-3">
        <div>
          <p className="text-xs text-gray-500">Mentions</p>
          <p className="text-2xl font-bold text-white tabular-nums">{channel.mentions}</p>
        </div>
        <div>
          <p className="text-xs text-gray-500">Comments</p>
          <p className="text-2xl font-bold text-white tabular-nums">{formatNumber(channel.totalComments)}</p>
        </div>
      </div>

      <div className="mt-4 pt-4 border-t border-white/5 space-y-3">
        <div className="flex items-center justify-between">
          <span className="text-xs text-gray-500">NSR</span>
          <span
            className={`text-sm font-bold tabular-nums flex items-center gap-1 ${
              channel.nsrScore >= 10 ? 'text-[#00B4D8]' : channel.nsrScore <= -10 ? 'text-[#FF7575]' : 'text-yellow-500'
            }`}
          >
            <NsrIcon score={channel.nsrScore} />
            {channel.nsrScore > 0 ? '+' : ''}
            {channel.nsrScore}%
          </span>
        </div>

        <div>
          <div className="flex items-center justify-between text-xs text-gray-400 mb-1">
            <span>Độ phủ AI</span>
            <span className="tabular-nums">{coverage}%</span>
          </div>
          <div className="h-2 rounded-full bg-[#0A101D] overflow-hidden">
            <div className="h-full rounded-full bg-white/70" style={{ width: `${coverage}%` }} />
          </div>
        </div>

        <div className="grid grid-cols-2 gap-2 text-xs">
          <div className="rounded-xl bg-[#0A101D] border border-white/5 px-3 py-2">
            <p className="text-gray-500">Tích cực</p>
            <p className="text-[#00B4D8] font-bold mt-1">{channel.positivePercent}%</p>
          </div>
          <div className="rounded-xl bg-[#0A101D] border border-white/5 px-3 py-2">
            <p className="text-gray-500">Tiêu cực</p>
            <p className="text-[#FF7575] font-bold mt-1">{channel.negativePercent}%</p>
          </div>
        </div>
      </div>
    </div>
  );
}

export default ProjectChannel;
