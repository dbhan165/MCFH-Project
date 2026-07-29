import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import {
  AlertCircle,
  ArrowRight,
  ClipboardList,
  Clock,
  Loader2,
  CheckCircle2,
  PlayCircle,
} from 'lucide-react';
import ReporterLayout from '../../components/reporter/ReporterLayout';
import { reporterApi } from '../../api/portalApi';
import type { PortalBespokeRequest } from '../../api/portalApi';
import { extractApiError } from '../../utils/authStorage';

export default function ReporterDashboard() {
  const navigate = useNavigate();
  const [pending, setPending] = useState<PortalBespokeRequest[]>([]);
  const [inProgress, setInProgress] = useState<PortalBespokeRequest[]>([]);
  const [deliveredCount, setDeliveredCount] = useState(0);
  const [avgDays, setAvgDays] = useState<number | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');

  const load = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage('');
    try {
      const [kanban, performance] = await Promise.all([
        reporterApi.getKanban(),
        reporterApi.getPerformance(),
      ]);
      setPending(kanban.pending);
      setInProgress(kanban.inProgress);
      setDeliveredCount(performance.deliveredCount);
      setAvgDays(performance.avgProcessingDays);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải dashboard.'));
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const priority = useMemo(() => {
    return [...pending, ...inProgress].slice(0, 6);
  }, [pending, inProgress]);

  return (
    <ReporterLayout>
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-8">
        <div>
          <h2 className="text-xl lg:text-2xl font-bold text-[#111827]">Dashboard</h2>
          <p className="text-sm text-[#78716c] mt-1">Tổng quan công việc Reporter hôm nay</p>
        </div>
        <Link
          to="/reporter/tasks"
          className="inline-flex items-center gap-2 px-4 py-2.5 bg-[#e11d48] hover:bg-[#be123c] text-white rounded-lg text-sm font-semibold transition-colors"
        >
          Mở bảng Tasks
          <ArrowRight className="w-4 h-4" />
        </Link>
      </div>

      {errorMessage && (
        <div className="mb-6 flex items-center gap-2 text-sm text-red-600 bg-red-50 border border-red-100 rounded-lg px-4 py-3">
          <AlertCircle className="w-4 h-4 shrink-0" />
          {errorMessage}
        </div>
      )}

      {isLoading ? (
        <div className="flex flex-col items-center justify-center py-24 text-stone-500 gap-3">
          <Loader2 className="w-8 h-8 animate-spin text-[#e11d48]" />
          <p className="text-sm">Đang tải dashboard...</p>
        </div>
      ) : (
        <>
          <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4 mb-8">
            <div className="bg-white border border-stone-200 rounded-xl p-5 shadow-sm">
              <div className="flex items-center gap-2 text-stone-500 text-sm font-semibold mb-2">
                <ClipboardList className="w-4 h-4 text-[#e11d48]" />
                Cần chỉnh sửa
              </div>
              <p className="text-3xl font-bold text-[#111827]">{pending.length}</p>
            </div>
            <div className="bg-white border border-stone-200 rounded-xl p-5 shadow-sm">
              <div className="flex items-center gap-2 text-stone-500 text-sm font-semibold mb-2">
                <PlayCircle className="w-4 h-4 text-amber-500" />
                Đang xử lý
              </div>
              <p className="text-3xl font-bold text-[#111827]">{inProgress.length}</p>
            </div>
            <div className="bg-white border border-stone-200 rounded-xl p-5 shadow-sm">
              <div className="flex items-center gap-2 text-stone-500 text-sm font-semibold mb-2">
                <CheckCircle2 className="w-4 h-4 text-emerald-500" />
                Đã bàn giao
              </div>
              <p className="text-3xl font-bold text-[#111827]">{deliveredCount}</p>
            </div>
            <div className="bg-white border border-stone-200 rounded-xl p-5 shadow-sm">
              <div className="flex items-center gap-2 text-stone-500 text-sm font-semibold mb-2">
                <Clock className="w-4 h-4 text-sky-500" />
                Thời gian TB
              </div>
              <p className="text-3xl font-bold text-[#111827]">
                {avgDays != null ? `${avgDays} ngày` : '—'}
              </p>
            </div>
          </div>

          <div className="bg-white border border-stone-200 rounded-xl shadow-sm overflow-hidden">
            <div className="px-6 py-4 border-b border-stone-100 flex items-center justify-between">
              <h3 className="font-bold text-[#111827]">Ưu tiên hôm nay</h3>
              <Link to="/reporter/performance" className="text-sm font-semibold text-[#e11d48] hover:underline">
                Xem hiệu suất
              </Link>
            </div>

            {priority.length === 0 ? (
              <p className="px-6 py-12 text-center text-sm text-stone-400">
                Không có đơn đang chờ. Bạn đang trống lịch.
              </p>
            ) : (
              <ul className="divide-y divide-stone-100">
                {priority.map((req) => (
                  <li key={req.requestId}>
                    <button
                      type="button"
                      onClick={() => navigate(`/reporter/requests/${req.requestId}`)}
                      className="w-full text-left px-6 py-4 hover:bg-stone-50 transition-colors flex flex-col sm:flex-row sm:items-center gap-2 sm:gap-4"
                    >
                      <span className="text-xs font-mono text-stone-400 shrink-0">#{req.requestId}</span>
                      <div className="min-w-0 flex-1">
                        <p className="font-semibold text-[#111827] truncate">{req.title}</p>
                        <p className="text-xs text-stone-500 mt-0.5">
                          {req.clientName ?? '—'} · {req.statusLabel}
                        </p>
                      </div>
                      <ArrowRight className="w-4 h-4 text-stone-300 hidden sm:block shrink-0" />
                    </button>
                  </li>
                ))}
              </ul>
            )}
          </div>
        </>
      )}
    </ReporterLayout>
  );
}
