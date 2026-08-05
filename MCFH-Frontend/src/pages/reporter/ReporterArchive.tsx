import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { AlertCircle, Download, ExternalLink, Loader2, Search } from 'lucide-react';
import ReporterLayout from '../../components/reporter/ReporterLayout';
import { reporterApi } from '../../api/portalApi';
import type { PortalBespokeRequest } from '../../api/portalApi';
import { extractApiError } from '../../utils/authStorage';
import { formatWorkspaceDate } from '../../utils/workspaceHelpers';

export default function ReporterArchive() {
  const [items, setItems] = useState<PortalBespokeRequest[]>([]);
  const [search, setSearch] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [downloadingId, setDownloadingId] = useState<number | null>(null);
  const [errorMessage, setErrorMessage] = useState('');

  const load = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage('');
    try {
      const kanban = await reporterApi.getKanban();
      setItems(kanban.completed);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải kho lưu trữ.'));
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return items;
    return items.filter(
      (r) =>
        r.title.toLowerCase().includes(q) ||
        String(r.requestId).includes(q) ||
        (r.clientName ?? '').toLowerCase().includes(q)
    );
  }, [items, search]);

  const handleDownload = async (requestId: number) => {
    setDownloadingId(requestId);
    setErrorMessage('');
    try {
      await reporterApi.download(requestId);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải báo cáo.'));
    } finally {
      setDownloadingId(null);
    }
  };

  return (
    <ReporterLayout>
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-8">
        <div>
          <h2 className="text-xl lg:text-2xl font-bold text-[#111827]">Archive</h2>
          <p className="text-sm text-[#78716c] mt-1">Các báo cáo đã bàn giao cho khách</p>
        </div>
        <div className="relative w-full sm:w-72">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-stone-400" />
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Tìm theo mã, tiêu đề, khách..."
            className="w-full bg-white border border-stone-200 rounded-lg pl-10 pr-4 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-rose-500/20 focus:border-rose-400"
          />
        </div>
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
          <p className="text-sm">Đang tải archive...</p>
        </div>
      ) : (
        <div className="bg-white border border-stone-200 rounded-xl overflow-hidden shadow-sm">
          <div className="px-6 py-4 border-b border-stone-100 flex items-center justify-between">
            <span className="text-sm font-semibold text-stone-500">
              {filtered.length} / {items.length} đơn đã hoàn thành
            </span>
            <button
              type="button"
              onClick={load}
              className="text-sm font-semibold text-[#e11d48] hover:underline"
            >
              Làm mới
            </button>
          </div>

          {filtered.length === 0 ? (
            <p className="px-6 py-16 text-center text-sm text-stone-400">
              {items.length === 0 ? 'Chưa có báo cáo nào trong archive.' : 'Không có kết quả khớp tìm kiếm.'}
            </p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead className="bg-stone-50 text-stone-500 text-xs uppercase">
                  <tr>
                    <th className="px-6 py-3 text-left">ID</th>
                    <th className="px-6 py-3 text-left">Tiêu đề</th>
                    <th className="px-6 py-3 text-left">Khách hàng</th>
                    <th className="px-6 py-3 text-left">Ngày giao</th>
                    <th className="px-6 py-3 text-right">Thao tác</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-stone-100">
                  {filtered.map((req) => (
                    <tr key={req.requestId} className="hover:bg-stone-50">
                      <td className="px-6 py-4 font-mono text-xs">#{req.requestId}</td>
                      <td className="px-6 py-4 font-medium text-[#111827]">{req.title}</td>
                      <td className="px-6 py-4 text-stone-600">{req.clientName ?? '—'}</td>
                      <td className="px-6 py-4 text-stone-500">
                        {formatWorkspaceDate(req.submittedAt ?? req.assignedAt)}
                      </td>
                      <td className="px-6 py-4 text-right space-x-3">
                        {req.hasDeliverable && (
                          <button
                            type="button"
                            disabled={downloadingId === req.requestId}
                            onClick={() => handleDownload(req.requestId)}
                            className="inline-flex items-center gap-1 text-[#e11d48] text-xs font-semibold hover:underline disabled:opacity-50"
                          >
                            {downloadingId === req.requestId ? (
                              <Loader2 className="w-3 h-3 animate-spin" />
                            ) : (
                              <Download className="w-3 h-3" />
                            )}
                            Tải lại
                          </button>
                        )}
                        <Link
                          to={`/reporter/requests/${req.requestId}`}
                          className="inline-flex items-center gap-1 text-stone-600 text-xs font-semibold hover:underline"
                        >
                          Chi tiết <ExternalLink className="w-3 h-3" />
                        </Link>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}
    </ReporterLayout>
  );
}
