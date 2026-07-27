import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Calendar, Filter, Loader2, AlertCircle } from 'lucide-react';
import ReporterLayout from '../../components/reporter/ReporterLayout';
import { reporterApi } from '../../api/portalApi';
import type { PortalBespokeRequest } from '../../api/portalApi';
import { extractApiError } from '../../utils/authStorage';
import { formatWorkspaceDate } from '../../utils/workspaceHelpers';

type ColumnKey = 'pending' | 'inProgress' | 'completed';

const COLUMNS: { key: ColumnKey; title: string; dotColor: string }[] = [
  { key: 'pending', title: 'Cần chỉnh sửa', dotColor: 'bg-pink-400' },
  { key: 'inProgress', title: 'Đang xử lý', dotColor: 'bg-amber-400' },
  { key: 'completed', title: 'Đã gửi khách', dotColor: 'bg-emerald-400' },
];

function getCardAction(req: PortalBespokeRequest): { label: string } {
  if (req.status === 'in_progress') return { label: 'Upload bản đã chỉnh sửa' };
  if (req.status === 'pending' || req.status === 'assigned' || req.status === 'revision_requested')
    return { label: 'Tải báo cáo & chỉnh sửa' };
  if (req.status === 'completed') return { label: 'Xem báo cáo đã gửi' };
  return { label: 'Xem chi tiết' };
}

const BespokeRequests = () => {
  const navigate = useNavigate();
  const [kanban, setKanban] = useState<Record<ColumnKey, PortalBespokeRequest[]>>({
    pending: [],
    inProgress: [],
    completed: [],
  });
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');

  const load = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage('');
    try {
      const data = await reporterApi.getKanban();
      setKanban(data);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải danh sách yêu cầu.'));
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  return (
    <ReporterLayout activeTopNav="reports">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-8">
        <h2 className="text-xl lg:text-2xl font-bold text-[#0f172a]">
          Yêu cầu chỉnh sửa báo cáo{' '}
          <span className="text-base font-normal text-[#64748b]">(Bespoke)</span>
        </h2>
        <button
          type="button"
          onClick={load}
          className="flex items-center gap-2 px-4 py-2.5 bg-white border border-gray-200 rounded-lg text-sm font-medium text-[#0f172a] hover:bg-gray-50 transition-colors shrink-0"
        >
          <Filter className="w-4 h-4 text-gray-500" />
          Làm mới
        </button>
      </div>

      {errorMessage && (
        <div className="mb-6 flex items-center gap-2 text-sm text-red-600 bg-red-50 border border-red-100 rounded-lg px-4 py-3">
          <AlertCircle className="w-4 h-4 shrink-0" />
          {errorMessage}
        </div>
      )}

      {isLoading ? (
        <div className="flex flex-col items-center justify-center py-24 text-gray-500 gap-3">
          <Loader2 className="w-8 h-8 animate-spin text-teal-600" />
          <p className="text-sm">Đang tải yêu cầu...</p>
        </div>
      ) : (
        <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
          {COLUMNS.map((column) => {
            const cards = kanban[column.key];
            return (
              <div key={column.key} className="flex flex-col min-h-[320px]">
                <div className="flex items-center gap-2.5 mb-4 px-1">
                  <span className={`w-2.5 h-2.5 rounded-full ${column.dotColor}`} />
                  <h3 className="text-sm font-semibold text-[#0f172a]">{column.title}</h3>
                  <span className="text-xs font-semibold text-[#64748b] bg-gray-100 px-2 py-0.5 rounded-full">
                    {cards.length}
                  </span>
                </div>

                <div className="space-y-4 flex-1">
                  {cards.length === 0 ? (
                    <p className="text-sm text-gray-400 text-center py-8 border border-dashed border-gray-200 rounded-xl">
                      Không có đơn
                    </p>
                  ) : (
                    cards.map((card) => {
                      const initials = (card.clientName ?? '?').slice(0, 2).toUpperCase();
                      const { label } = getCardAction(card);
                      return (
                        <div
                          key={card.requestId}
                          className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm hover:shadow-md transition-shadow"
                        >
                          <div className="flex items-start justify-between gap-2 mb-3">
                            <span className="text-xs font-semibold text-[#64748b]">#{card.requestId}</span>
                            <span className="text-[10px] font-bold uppercase tracking-wider px-2 py-1 rounded bg-gray-100 text-gray-600">
                              {card.statusLabel}
                            </span>
                          </div>

                          <h4 className="text-base font-bold text-[#0f172a] mb-3 leading-snug">{card.title}</h4>

                          <div className="flex items-center gap-2.5 mb-4">
                            <span className="w-8 h-8 bg-sky-100 text-sky-700 rounded-md flex items-center justify-center text-xs font-bold shrink-0">
                              {initials}
                            </span>
                            <span className="text-sm text-[#64748b]">{card.clientName ?? '—'}</span>
                          </div>

                          {card.deadline && (
                            <p className="flex items-center gap-1 text-xs text-gray-500 mb-4">
                              <Calendar className="w-3 h-3" />
                              Hạn: {formatWorkspaceDate(card.deadline)}
                            </p>
                          )}

                          <button
                            type="button"
                            onClick={() => navigate(`/reporter/requests/${card.requestId}`)}
                            className="w-full py-2.5 border-2 border-teal-600 text-teal-700 hover:bg-teal-50 rounded-lg text-sm font-semibold transition-colors"
                          >
                            {label}
                          </button>
                        </div>
                      );
                    })
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </ReporterLayout>
  );
};

export default BespokeRequests;
