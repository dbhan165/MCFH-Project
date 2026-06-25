import { useCallback, useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Clock, Loader2, CheckCircle, ExternalLink } from 'lucide-react';
import ReporterLayout from '../../components/reporter/ReporterLayout';
import { reporterApi } from '../../api/portalApi';
import type { PortalBespokeRequest } from '../../api/portalApi';
import { extractApiError } from '../../utils/authStorage';
import { formatWorkspaceDate } from '../../utils/workspaceHelpers';

export default function MyPerformance() {
  const [performance, setPerformance] = useState<Awaited<ReturnType<typeof reporterApi.getPerformance>> | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');

  const load = useCallback(async () => {
    setIsLoading(true);
    try {
      setPerformance(await reporterApi.getPerformance());
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải hiệu suất.'));
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const history = performance?.history ?? [];

  return (
    <ReporterLayout activeTopNav="performance">
      <div className="min-h-screen bg-[#f8fafc] -m-6 p-6 font-sans text-[#1e293b]">
        <h2 className="text-xl lg:text-2xl font-bold text-[#0f172a] mb-6">
          Thống kê Hiệu suất Cá nhân
        </h2>

        {errorMessage && (
          <div className="mb-4 text-sm text-red-600 bg-red-50 rounded-lg px-4 py-3">{errorMessage}</div>
        )}

        {isLoading ? (
          <div className="flex justify-center py-20">
            <Loader2 className="w-8 h-8 animate-spin text-teal-600" />
          </div>
        ) : (
          <>
            <div className="grid grid-cols-1 md:grid-cols-3 gap-5 mb-6">
              <div className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm">
                <span className="text-sm font-semibold text-gray-500">Báo cáo đã bàn giao</span>
                <p className="text-3xl font-bold text-[#0f172a] mt-2">{performance?.deliveredCount ?? 0}</p>
              </div>
              <div className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm">
                <span className="text-sm font-semibold text-gray-500">Đang xử lý</span>
                <p className="text-3xl font-bold text-[#0f172a] mt-2">{performance?.inProgressCount ?? 0}</p>
              </div>
              <div className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm">
                <span className="text-sm font-semibold text-gray-500">Thời gian TB</span>
                <p className="text-3xl font-bold text-[#0f172a] mt-2">
                  {performance?.avgProcessingDays != null ? `${performance.avgProcessingDays} ngày` : '—'}
                </p>
              </div>
            </div>

            <div className="bg-white border border-gray-200 rounded-xl overflow-hidden shadow-sm">
              <div className="px-6 py-4 border-b border-gray-100 font-bold">Lịch sử đơn hàng</div>
              <table className="w-full text-sm">
                <thead className="bg-gray-50 text-gray-500 text-xs uppercase">
                  <tr>
                    <th className="px-6 py-3 text-left">ID</th>
                    <th className="px-6 py-3 text-left">Tiêu đề</th>
                    <th className="px-6 py-3 text-left">Trạng thái</th>
                    <th className="px-6 py-3 text-right">Thao tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {history.length === 0 ? (
                    <tr>
                      <td colSpan={4} className="px-6 py-12 text-center text-gray-400">
                        Chưa có đơn nào
                      </td>
                    </tr>
                  ) : (
                    history.map((req: PortalBespokeRequest) => (
                      <tr key={req.requestId} className="hover:bg-gray-50">
                        <td className="px-6 py-4 font-mono text-xs">#{req.requestId}</td>
                        <td className="px-6 py-4 font-medium">{req.title}</td>
                        <td className="px-6 py-4">
                          <span className="text-xs font-semibold px-2 py-1 rounded-full bg-gray-100">
                            {req.statusLabel}
                          </span>
                        </td>
                        <td className="px-6 py-4 text-right">
                          {req.status === 'completed' && req.hasDeliverable ? (
                            <button
                              type="button"
                              onClick={() => reporterApi.download(req.requestId)}
                              className="text-teal-600 text-xs font-semibold hover:underline"
                            >
                              Tải báo cáo
                            </button>
                          ) : (
                            <Link
                              to={`/reporter/requests/${req.requestId}`}
                              className="text-teal-600 text-xs font-semibold hover:underline inline-flex items-center gap-1"
                            >
                              Chi tiết <ExternalLink className="w-3 h-3" />
                            </Link>
                          )}
                        </td>
                      </tr>
                    ))
                  )}
                </tbody>
              </table>
            </div>
          </>
        )}
      </div>
    </ReporterLayout>
  );
}
