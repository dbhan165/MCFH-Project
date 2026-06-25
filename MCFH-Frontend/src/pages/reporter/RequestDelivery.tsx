import { useCallback, useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Clock, Loader2, CheckCircle2 } from 'lucide-react';
import ReporterLayout from '../../components/reporter/ReporterLayout';
import { reporterApi } from '../../api/portalApi';
import type { PortalBespokeRequest } from '../../api/portalApi';
import { extractApiError } from '../../utils/authStorage';
import { formatWorkspaceDate } from '../../utils/workspaceHelpers';

const RequestDelivery = () => {
  const { id } = useParams<{ id: string }>();
  const requestId = Number(id);
  const navigate = useNavigate();

  const [request, setRequest] = useState<PortalBespokeRequest | null>(null);
  const [analytics, setAnalytics] = useState<Awaited<ReturnType<typeof reporterApi.getAnalyticsPreview>> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isDelivering, setIsDelivering] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState('');

  const load = useCallback(async () => {
    if (!requestId || Number.isNaN(requestId)) return;
    setIsLoading(true);
    try {
      const [req, preview] = await Promise.all([
        reporterApi.getRequest(requestId),
        reporterApi.getAnalyticsPreview(requestId).catch(() => null),
      ]);
      setRequest(req);
      setAnalytics(preview);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải dữ liệu.'));
    } finally {
      setIsLoading(false);
    }
  }, [requestId]);

  useEffect(() => {
    load();
  }, [load]);

  const handleDeliver = async () => {
    setIsDelivering(true);
    setErrorMessage('');
    try {
      await reporterApi.deliver(requestId);
      setSuccessMessage('Đã nộp báo cáo chuyên sâu thành công.');
      await load();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể nộp báo cáo.'));
    } finally {
      setIsDelivering(false);
    }
  };

  if (isLoading) {
    return (
      <ReporterLayout activeTopNav="reports">
        <div className="flex justify-center py-32">
          <Loader2 className="w-8 h-8 animate-spin text-teal-600" />
        </div>
      </ReporterLayout>
    );
  }

  if (!request) {
    return (
      <ReporterLayout activeTopNav="reports">
        <p className="text-center py-20 text-red-600">{errorMessage || 'Không tìm thấy yêu cầu.'}</p>
      </ReporterLayout>
    );
  }

  const negPct =
    analytics && analytics.totalMentions > 0
      ? Math.round((analytics.negativeCount / analytics.totalMentions) * 100)
      : 0;

  return (
    <ReporterLayout activeTopNav="reports">
      <div className="flex items-center gap-2 text-sm text-[#64748b] mb-3">
        <button type="button" onClick={() => navigate('/reporter/tasks')} className="hover:text-[#0f172a]">
          Bảng công việc
        </button>
        <span>&gt;</span>
        <span>#{request.requestId}</span>
      </div>

      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4 mb-6">
        <div>
          <h2 className="text-xl lg:text-2xl font-bold text-[#0f172a]">
            {request.projectName ?? request.title}
          </h2>
          <span className="inline-flex mt-2 px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-50 text-blue-600">
            {request.statusLabel}
          </span>
        </div>
        {request.deadline && (
          <div className="flex items-center gap-2 px-4 py-2 bg-red-50 text-red-600 rounded-lg text-sm font-semibold border border-red-100">
            <Clock className="w-4 h-4" />
            Hạn: {formatWorkspaceDate(request.deadline)}
          </div>
        )}
      </div>

      {errorMessage && (
        <div className="mb-4 text-sm text-red-600 bg-red-50 rounded-lg px-4 py-3">{errorMessage}</div>
      )}
      {successMessage && (
        <div className="mb-4 text-sm text-emerald-700 bg-emerald-50 rounded-lg px-4 py-3 flex items-center gap-2">
          <CheckCircle2 className="w-4 h-4" />
          {successMessage}
        </div>
      )}

      <div className="bg-white border border-gray-200 rounded-xl p-6 shadow-sm mb-6">
        <h3 className="text-base font-bold mb-5">Tài nguyên Phân tích</h3>
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="bg-gray-50 rounded-xl p-4">
            <span className="text-xs text-gray-500">Mentions</span>
            <p className="text-2xl font-bold text-teal-600 mt-1">
              {analytics?.totalMentions?.toLocaleString('vi-VN') ?? '—'}
            </p>
          </div>
          <div className="bg-gray-50 rounded-xl p-4">
            <span className="text-xs text-gray-500">Tiêu cực</span>
            <p className="text-2xl font-bold text-red-500 mt-1">{analytics ? `${negPct}%` : '—'}</p>
          </div>
          <div className="bg-gray-50 rounded-xl p-4">
            <span className="text-xs text-gray-500">NSR</span>
            <p className="text-2xl font-bold text-[#0f172a] mt-1">
              {analytics ? `${analytics.nsrScore > 0 ? '+' : ''}${analytics.nsrScore.toFixed(1)}%` : '—'}
            </p>
          </div>
        </div>
      </div>

      <div className="bg-white border border-gray-200 rounded-xl p-6 shadow-sm">
        <h3 className="text-base font-bold mb-4">Nộp báo cáo chuyên sâu</h3>
        <p className="text-sm text-gray-500 mb-6">
          Hệ thống sẽ tự động biên soạn báo cáo HTML từ dữ liệu analytics của dự án.
        </p>
        {request.hasDeliverable ? (
          <button
            type="button"
            onClick={() => reporterApi.download(requestId)}
            className="px-6 py-3 bg-teal-600 text-white rounded-lg font-semibold text-sm"
          >
            Tải báo cáo đã nộp
          </button>
        ) : (
          <button
            type="button"
            disabled={isDelivering || request.status === 'completed'}
            onClick={handleDeliver}
            className="px-6 py-3 bg-[#00667e] hover:bg-[#005367] disabled:opacity-50 text-white rounded-lg font-semibold text-sm inline-flex items-center gap-2"
          >
            {isDelivering && <Loader2 className="w-4 h-4 animate-spin" />}
            Nộp báo cáo (tự động tạo HTML)
          </button>
        )}
      </div>
    </ReporterLayout>
  );
};

export default RequestDelivery;
