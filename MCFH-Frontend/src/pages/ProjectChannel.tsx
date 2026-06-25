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
  RadarChart,
  PolarGrid,
  PolarAngleAxis,
  Radar,
} from 'recharts';
import { projectApi } from '../api/projectApi';
import type { ChannelComparison, ChannelStats } from '../types/project';
import { extractApiError } from '../utils/authStorage';
import {
  formatNumber,
  getPlatformBadgeClass,
  getPlatformChartColor,
  SENTIMENT_COLORS,
} from '../utils/sentimentHelpers';

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
  payload?: { payload: { label: string; mentionShare: number; mentions: number; color: string } }[];
}) {
  if (!active || !payload?.[0]) return null;
  const item = payload[0].payload;
  return (
    <div style={TOOLTIP_STYLE}>
      <p className="text-sm font-semibold text-white">{item.label}</p>
      <p className="text-xs text-gray-400 mt-1">
        {item.mentions} mentions · {item.mentionShare}%
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

  const pieData = useMemo(
    () =>
      (data?.channels ?? []).map((ch) => ({
        ...ch,
        color: getPlatformChartColor(ch.platform),
      })),
    [data]
  );

  const volumeData = useMemo(
    () =>
      (data?.channels ?? []).map((ch) => ({
        label: ch.label,
        Mentions: ch.mentions,
        Comments: ch.totalComments,
        color: getPlatformChartColor(ch.platform),
      })),
    [data]
  );

  const sentimentStackData = useMemo(
    () =>
      (data?.channels ?? []).map((ch) => ({
        label: ch.label,
        'Tích cực': ch.positive,
        'Trung lập': ch.neutral,
        'Tiêu cực': ch.negative,
      })),
    [data]
  );

  const radarData = useMemo(
    () =>
      (data?.channels ?? []).map((ch) => ({
        platform: ch.label,
        mentions: ch.mentionShare,
        comments: ch.commentShare,
        positive: ch.positivePercent,
        nsr: Math.max(0, ch.nsrScore + 50),
      })),
    [data]
  );

  const topChannel = useMemo(() => {
    if (!data?.channels.length) return null;
    return [...data.channels].sort((a, b) => b.mentions - a.mentions)[0];
  }, [data]);

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

  const hasData = data.channels.length > 0;

  return (
    <div className="animate-in fade-in duration-500 max-w-7xl mx-auto space-y-8 pb-10">
      {/* Header */}
      <div className="relative overflow-hidden rounded-3xl border border-white/5 bg-gradient-to-br from-[#151B2B] via-[#141A2C] to-[#0A101D] p-8">
        <div className="absolute -top-20 -right-16 w-64 h-64 bg-purple-500/10 rounded-full blur-3xl pointer-events-none" />
        <div className="absolute -bottom-12 -left-12 w-48 h-48 bg-blue-500/10 rounded-full blur-3xl pointer-events-none" />

        <div className="relative flex flex-col lg:flex-row lg:items-center lg:justify-between gap-6">
          <div>
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-white/5 border border-white/10 text-xs text-gray-400 mb-4">
              <Layers className="w-3.5 h-3.5 text-purple-400" />
              Cross-Platform Analytics
            </div>
            <h2 className="text-3xl font-bold text-white flex items-center gap-3">
              <Share2 className="text-purple-400 w-8 h-8" />
              So sánh Kênh
            </h2>
            <p className="text-gray-400 text-sm mt-2 max-w-xl">
              {data.channels.length} nền tảng · {formatNumber(data.totalMentions)} mentions ·{' '}
              {formatNumber(data.totalComments)} bình luận
            </p>
          </div>

          <div className="flex items-center gap-4">
            {topChannel && (
              <div className="rounded-2xl border border-purple-500/30 bg-purple-500/5 px-6 py-4 text-center min-w-[160px]">
                <p className="text-xs text-gray-500 uppercase tracking-wide font-semibold mb-1">Kênh dẫn đầu</p>
                <p className="text-lg font-bold text-white">{topChannel.label}</p>
                <p className="text-sm text-purple-400 font-semibold tabular-nums">{topChannel.mentionShare}% SOV</p>
              </div>
            )}
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
      </div>

      {!hasData ? (
        <div className="bg-[#151B2B] border border-white/5 rounded-3xl p-16 text-center">
          <Share2 className="w-14 h-14 text-gray-600 mx-auto mb-4" />
          <p className="text-gray-300 font-medium">Chưa có dữ liệu kênh</p>
          <p className="text-gray-500 text-sm mt-2">Cào dữ liệu từ Facebook, YouTube hoặc TikTok để so sánh.</p>
        </div>
      ) : (
        <>
          {/* Channel cards */}
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            {data.channels.map((ch) => (
              <ChannelCard key={ch.platform} channel={ch} />
            ))}
          </div>

          {/* Pie + volume bar */}
          <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
            <div className="bg-[#151B2B] border border-white/5 rounded-3xl p-6">
              <h3 className="font-bold text-white mb-1">Tỷ trọng Mentions</h3>
              <p className="text-xs text-gray-500 mb-4">Phân bổ thảo luận theo nền tảng</p>

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
                        {pieData.map((ch) => (
                          <Cell key={ch.platform} fill={ch.color} />
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
                  {pieData.map((ch) => (
                    <div key={ch.platform}>
                      <div className="flex justify-between text-sm mb-1.5">
                        <span className={`px-2 py-0.5 rounded-md text-xs font-semibold ${getPlatformBadgeClass(ch.platform)}`}>
                          {ch.label}
                        </span>
                        <span className="font-bold tabular-nums" style={{ color: ch.color }}>
                          {ch.mentionShare}%
                        </span>
                      </div>
                      <div className="w-full bg-[#0A101D] h-2.5 rounded-full overflow-hidden">
                        <div
                          className="h-full rounded-full transition-all duration-700"
                          style={{ width: `${ch.mentionShare}%`, background: ch.color }}
                        />
                      </div>
                      <p className="text-[11px] text-gray-600 mt-1">
                        {formatNumber(ch.mentions)} mentions · {formatNumber(ch.totalComments)} comments
                      </p>
                    </div>
                  ))}
                </div>
              </div>
            </div>

            <div className="bg-[#151B2B] border border-white/5 rounded-3xl p-6">
              <h3 className="font-bold text-white flex items-center gap-2 mb-1">
                <BarChart3 className="w-5 h-5 text-purple-400" />
                Khối lượng thảo luận
              </h3>
              <p className="text-xs text-gray-500 mb-4">Mentions vs bình luận theo kênh</p>

              <ResponsiveContainer width="100%" height={280}>
                <BarChart data={volumeData} margin={{ top: 8, right: 8, left: -12, bottom: 0 }}>
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
                  <Tooltip
                    contentStyle={TOOLTIP_STYLE}
                    labelStyle={{ color: '#9CA3AF', fontSize: 12 }}
                    itemStyle={{ color: '#fff', fontSize: 13 }}
                    cursor={{ fill: 'rgba(255,255,255,0.03)' }}
                  />
                  <Legend
                    wrapperStyle={{ fontSize: 12, color: '#9CA3AF', paddingTop: 12 }}
                    iconType="circle"
                    iconSize={8}
                  />
                  <Bar dataKey="Mentions" fill="#A78BFA" radius={[6, 6, 0, 0]} maxBarSize={40} />
                  <Bar dataKey="Comments" fill="#00B4D8" radius={[6, 6, 0, 0]} maxBarSize={40} />
                </BarChart>
              </ResponsiveContainer>
            </div>
          </div>

          {/* Sentiment stacked + radar */}
          <div className="grid grid-cols-1 xl:grid-cols-5 gap-6">
            <div className="xl:col-span-3 bg-[#151B2B] border border-white/5 rounded-3xl p-6">
              <h3 className="font-bold text-white mb-1">Sentiment theo kênh</h3>
              <p className="text-xs text-gray-500 mb-4">Tích cực / trung lập / tiêu cực trên từng nền tảng</p>

              <ResponsiveContainer width="100%" height={300}>
                <BarChart data={sentimentStackData} margin={{ top: 8, right: 8, left: -12, bottom: 0 }}>
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
                  <Tooltip
                    contentStyle={TOOLTIP_STYLE}
                    labelStyle={{ color: '#9CA3AF' }}
                    itemStyle={{ color: '#fff' }}
                    cursor={{ fill: 'rgba(255,255,255,0.03)' }}
                  />
                  <Legend wrapperStyle={{ fontSize: 12, color: '#9CA3AF' }} iconType="circle" iconSize={8} />
                  <Bar dataKey="Tích cực" stackId="s" fill={SENTIMENT_COLORS.positive} />
                  <Bar dataKey="Trung lập" stackId="s" fill={SENTIMENT_COLORS.neutral} />
                  <Bar dataKey="Tiêu cực" stackId="s" fill={SENTIMENT_COLORS.negative} radius={[6, 6, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            </div>

            <div className="xl:col-span-2 bg-[#151B2B] border border-white/5 rounded-3xl p-6">
              <h3 className="font-bold text-white mb-1">Radar hiệu suất</h3>
              <p className="text-xs text-gray-500 mb-2">So sánh đa chiều (chuẩn hóa %)</p>

              {radarData.length < 2 ? (
                <div className="h-[280px] flex items-center justify-center text-gray-500 text-sm border border-dashed border-white/10 rounded-2xl">
                  Cần ít nhất 2 kênh để hiển thị radar.
                </div>
              ) : (
                <ResponsiveContainer width="100%" height={280}>
                  <RadarChart data={radarData} cx="50%" cy="50%" outerRadius="72%">
                    <PolarGrid stroke="rgba(255,255,255,0.08)" />
                    <PolarAngleAxis dataKey="platform" tick={{ fill: '#9CA3AF', fontSize: 11 }} />
                    <Radar
                      name="Mentions %"
                      dataKey="mentions"
                      stroke="#A78BFA"
                      fill="#A78BFA"
                      fillOpacity={0.25}
                      strokeWidth={2}
                    />
                    <Radar
                      name="Comments %"
                      dataKey="comments"
                      stroke="#00B4D8"
                      fill="#00B4D8"
                      fillOpacity={0.2}
                      strokeWidth={2}
                    />
                    <Tooltip contentStyle={TOOLTIP_STYLE} />
                    <Legend wrapperStyle={{ fontSize: 11, color: '#9CA3AF' }} iconSize={8} />
                  </RadarChart>
                </ResponsiveContainer>
              )}
            </div>
          </div>

          {/* Comparison table */}
          <div className="bg-[#151B2B] border border-white/5 rounded-3xl overflow-hidden">
            <div className="p-6 border-b border-white/5">
              <h3 className="font-bold text-white">Bảng so sánh chi tiết</h3>
              <p className="text-xs text-gray-500 mt-1">Toàn bộ chỉ số theo từng nền tảng</p>
            </div>
            <div className="overflow-x-auto">
              <table className="w-full text-left">
                <thead>
                  <tr className="bg-white/[0.02] border-b border-white/5">
                    {['Nền tảng', 'Mentions', '% SOV', 'Comments', '% Comments', 'NSR', 'Tích cực', 'Tiêu cực', 'Trung lập'].map(
                      (h) => (
                        <th
                          key={h}
                          className="px-5 py-4 text-xs font-bold text-gray-500 uppercase tracking-wider whitespace-nowrap"
                        >
                          {h}
                        </th>
                      )
                    )}
                  </tr>
                </thead>
                <tbody className="divide-y divide-white/5">
                  {data.channels.map((ch) => (
                    <tr key={ch.platform} className="hover:bg-white/[0.02] transition-colors">
                      <td className="px-5 py-4">
                        <span className={`inline-block px-2.5 py-1 rounded-lg text-xs font-semibold ${getPlatformBadgeClass(ch.platform)}`}>
                          {ch.label}
                        </span>
                      </td>
                      <td className="px-5 py-4 text-sm font-bold text-white tabular-nums">{ch.mentions}</td>
                      <td className="px-5 py-4 text-sm font-semibold tabular-nums" style={{ color: getPlatformChartColor(ch.platform) }}>
                        {ch.mentionShare}%
                      </td>
                      <td className="px-5 py-4 text-sm text-gray-300 tabular-nums">
                        <span className="inline-flex items-center gap-1">
                          <MessageCircle className="w-3.5 h-3.5 text-gray-600" />
                          {formatNumber(ch.totalComments)}
                        </span>
                      </td>
                      <td className="px-5 py-4 text-sm text-gray-400 tabular-nums">{ch.commentShare}%</td>
                      <td className="px-5 py-4">
                        <span
                          className={`inline-flex items-center gap-1 text-sm font-bold tabular-nums ${
                            ch.nsrScore >= 10 ? 'text-[#00B4D8]' : ch.nsrScore <= -10 ? 'text-[#FF7575]' : 'text-yellow-500'
                          }`}
                        >
                          <NsrIcon score={ch.nsrScore} />
                          {ch.nsrScore > 0 ? '+' : ''}
                          {ch.nsrScore}%
                        </span>
                      </td>
                      <td className="px-5 py-4 text-sm text-[#00B4D8] tabular-nums">{ch.positive}</td>
                      <td className="px-5 py-4 text-sm text-[#FF7575] tabular-nums">{ch.negative}</td>
                      <td className="px-5 py-4 text-sm text-yellow-500 tabular-nums">{ch.neutral}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </>
      )}
    </div>
  );
};

function ChannelCard({ channel }: { channel: ChannelStats }) {
  const color = getPlatformChartColor(channel.platform);

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
      <div className="mt-4 pt-4 border-t border-white/5 flex items-center justify-between">
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
    </div>
  );
}

export default ProjectChannel;
