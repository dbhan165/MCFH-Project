import { useCallback, useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { Loader2, AlertCircle, BarChart3 } from 'lucide-react';
import { projectApi } from '../api/projectApi';
import type { ProjectOverviewStats } from '../types/project';
import { extractApiError } from '../utils/authStorage';
import { formatNumber, getPlatformDisplayName, getPlatformBadgeClass, sortByPlatformOrder } from '../utils/sentimentHelpers';

const ProjectOverview = () => {
  const { workspaceId, id } = useParams();
  const wid = Number(workspaceId);
  const projectId = Number(id);

  const [overview, setOverview] = useState<ProjectOverviewStats | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');

  const loadOverview = useCallback(async () => {
    if (!wid || !projectId || Number.isNaN(wid) || Number.isNaN(projectId)) return;

    setIsLoading(true);
    setErrorMessage('');
    try {
      const data = await projectApi.getOverview(wid, projectId);
      setOverview(data);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải tổng quan dự án.'));
    } finally {
      setIsLoading(false);
    }
  }, [wid, projectId]);

  useEffect(() => {
    loadOverview();
  }, [loadOverview]);

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center py-24 text-gray-400 gap-4">
        <Loader2 className="w-10 h-10 animate-spin text-[#FF7575]" />
        <p>Đang tải dữ liệu phân tích...</p>
      </div>
    );
  }

  if (errorMessage) {
    return (
      <div className="bg-red-500/10 border border-red-500/20 text-red-300 p-4 rounded-xl flex items-center gap-3 text-sm max-w-3xl mx-auto">
        <AlertCircle className="w-5 h-5" />
        {errorMessage}
      </div>
    );
  }

  if (!overview) return null;

  const platformBreakdown = sortByPlatformOrder(
    Object.entries(overview.platformBreakdown).map(([platform, count]) => ({ platform, count })),
    (item) => item.count
  );

  const cards = [
    { title: 'Tổng mentions', value: formatNumber(overview.totalMentions), sub: 'Bài đăng đã cào' },
    {
      title: 'Chỉ số NSR',
      value: `${overview.nsrScore > 0 ? '+' : ''}${Math.round(overview.nsrScore)}%`,
      sub: `${overview.positiveCount} tích cực · ${overview.negativeCount} tiêu cực`,
      accent: overview.nsrScore >= 0 ? 'text-[#00B4D8]' : 'text-[#FF7575]',
    },
    { title: 'Tổng bình luận', value: formatNumber(overview.totalComments), sub: 'Từ các nguồn đã chọn' },
    { title: 'Đã phân tích AI', value: formatNumber(overview.analyzedCount), sub: overview.pendingAnalysisCount > 0 ? `${overview.pendingAnalysisCount} chờ phân tích` : 'Hoàn tất' },
  ];

  return (
    <div className="animate-in fade-in duration-500 space-y-6 max-w-7xl mx-auto">
      <div className="mb-2">
        <h2 className="text-2xl font-bold text-white">{overview.projectName}</h2>
        <p className="text-gray-400 text-sm mt-1">Số liệu tổng hợp từ mentions đã thu thập</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-4 gap-6">
        {cards.map((card) => (
          <div key={card.title} className="bg-[#151B2B] border border-white/5 rounded-2xl p-6">
            <p className="text-sm text-gray-400 mb-2">{card.title}</p>
            <h3 className={`text-3xl font-bold ${card.accent ?? 'text-white'}`}>{card.value}</h3>
            <p className="text-xs text-gray-500 mt-2">{card.sub}</p>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div className="bg-[#151B2B] border border-white/5 rounded-2xl p-6">
          <h3 className="font-bold mb-4 flex items-center gap-2">
            <BarChart3 className="text-[#FF7575] w-5 h-5" /> Phân bổ theo nền tảng
          </h3>
          {Object.keys(overview.platformBreakdown).length === 0 ? (
            <p className="text-gray-500 text-sm">Chưa có dữ liệu. Hãy cào dữ liệu cho dự án này.</p>
          ) : (
            <div className="space-y-3">
              {platformBreakdown.map(({ platform, count }) => (
                <div key={platform} className="flex items-center justify-between p-3 bg-[#0A101D] rounded-xl border border-white/5">
                  <span className={`inline-flex px-2.5 py-1 rounded-lg text-xs font-bold ${getPlatformBadgeClass(platform)}`}>
                    {getPlatformDisplayName(platform)}
                  </span>
                  <span className="font-bold text-white tabular-nums">{formatNumber(count)} mentions</span>
                </div>
              ))}
            </div>
          )}
        </div>

        <div className="bg-[#151B2B] border border-white/5 rounded-2xl p-6">
          <h3 className="font-bold mb-4">Tóm tắt sentiment</h3>
          <div className="grid grid-cols-3 gap-4">
            <div className="text-center p-4 bg-[#0A101D] rounded-xl border border-emerald-500/20">
              <p className="text-2xl font-bold text-emerald-400">{overview.positiveCount}</p>
              <p className="text-xs text-gray-500 mt-1">Tích cực</p>
            </div>
            <div className="text-center p-4 bg-[#0A101D] rounded-xl border border-[#FF7575]/20">
              <p className="text-2xl font-bold text-[#FF7575]">{overview.negativeCount}</p>
              <p className="text-xs text-gray-500 mt-1">Tiêu cực</p>
            </div>
            <div className="text-center p-4 bg-[#0A101D] rounded-xl border border-yellow-500/20">
              <p className="text-2xl font-bold text-yellow-500">{overview.neutralCount}</p>
              <p className="text-xs text-gray-500 mt-1">Trung lập</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};

export default ProjectOverview;
