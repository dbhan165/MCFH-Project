import { useCallback, useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { FileText, ClipboardList, Calendar, Loader2, AlertCircle } from 'lucide-react';
import ReporterLayout from '../../components/reporter/ReporterLayout';
import { reporterApi } from '../../api/portalApi';
import type { PortalBespokeRequest } from '../../api/portalApi';
import { extractApiError } from '../../utils/authStorage';

export default function QuoteDetail() {
  const { id } = useParams();
  const requestId = Number(id);
  const navigate = useNavigate();

  const [request, setRequest] = useState<PortalBespokeRequest | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState('');

  const [cost, setCost] = useState('');
  const [deadline, setDeadline] = useState('');
  const [note, setNote] = useState('');

  const load = useCallback(async () => {
    if (!requestId || Number.isNaN(requestId)) return;
    setIsLoading(true);
    setErrorMessage('');
    try {
      const data = await reporterApi.getRequest(requestId);
      setRequest(data);
      if (data.agreedPrice) setCost(String(data.agreedPrice));
      if (data.deadline) setDeadline(data.deadline.slice(0, 10));
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải yêu cầu.'));
    } finally {
      setIsLoading(false);
    }
  }, [requestId]);

  useEffect(() => {
    load();
  }, [load]);

  const handleSubmitQuote = async () => {
    if (!requestId) return;
    const price = Number(cost.replace(/[^\d]/g, ''));
    if (!price || price <= 0) {
      setErrorMessage('Vui lòng nhập chi phí hợp lệ.');
      return;
    }
    setIsSubmitting(true);
    setErrorMessage('');
    try {
      await reporterApi.quote(requestId, {
        agreedPrice: price,
        deadline: deadline || undefined,
        note: note || undefined,
      });
      setSuccessMessage('Đã gửi báo giá thành công.');
      setTimeout(() => navigate('/reporter/tasks'), 1200);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể gửi báo giá.'));
    } finally {
      setIsSubmitting(false);
    }
  };

  if (isLoading) {
    return (
      <ReporterLayout activeTopNav="reports">
        <div className="flex items-center justify-center py-32 text-gray-500 gap-3">
          <Loader2 className="w-8 h-8 animate-spin text-teal-600" />
          Đang tải...
        </div>
      </ReporterLayout>
    );
  }

  if (!request) {
    return (
      <ReporterLayout activeTopNav="reports">
        <div className="text-center py-20 text-red-600">{errorMessage || 'Không tìm thấy yêu cầu.'}</div>
      </ReporterLayout>
    );
  }

  return (
    <ReporterLayout activeTopNav="reports">
      <div className="min-h-screen bg-[#f8fafc] -m-6 p-6 font-sans">
        <div className="mb-6">
          <div className="text-[11px] text-slate-500 font-medium flex items-center gap-1">
            <span>Bảng công việc</span>
            <span className="text-slate-400">&gt;</span>
            <span className="text-slate-600 font-semibold">Yêu cầu #{request.requestId}</span>
          </div>
          <h1 className="text-2xl font-bold text-[#0f172a] mt-2 tracking-tight">
            Báo giá: {request.title}
          </h1>
          <div className="mt-2.5 inline-flex items-center gap-1.5 px-2.5 py-1 bg-[#e0f2fe] text-[#0369a1] text-[11px] font-medium rounded-full">
            <span className="w-1.5 h-1.5 bg-[#38bdf8] rounded-full" />
            {request.statusLabel}
          </div>
        </div>

        {errorMessage && (
          <div className="mb-4 flex items-center gap-2 text-sm text-red-600 bg-red-50 border border-red-100 rounded-lg px-4 py-3">
            <AlertCircle className="w-4 h-4" />
            {errorMessage}
          </div>
        )}
        {successMessage && (
          <div className="mb-4 text-sm text-emerald-700 bg-emerald-50 border border-emerald-100 rounded-lg px-4 py-3">
            {successMessage}
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
          <div className="lg:col-span-7 bg-white rounded-2xl border border-slate-200 p-6 shadow-sm">
            <div className="flex items-center gap-2.5 border-b border-slate-100 pb-4 mb-5">
              <ClipboardList className="w-5 h-5 text-[#0f172a]" />
              <h2 className="text-md font-bold text-[#0f172a]">Chi tiết Yêu cầu</h2>
            </div>

            <div className="text-[13.5px] leading-relaxed text-[#334155] space-y-4">
              <p>{request.requirements || 'Khách hàng chưa ghi mô tả chi tiết.'}</p>
              <div className="pt-4 border-t border-dashed border-slate-200 text-xs text-gray-500 space-y-1">
                <p>Dự án: {request.projectName ?? '—'}</p>
                <p>Workspace: {request.workspaceName ?? '—'}</p>
                <p>Khách hàng: {request.clientName ?? '—'}</p>
                {(request.dateFrom || request.dateTo) && (
                  <p>
                    Giai đoạn: {request.dateFrom ?? '…'} → {request.dateTo ?? '…'}
                  </p>
                )}
              </div>
              {request.modules.length > 0 && (
                <div className="flex flex-wrap gap-2 pt-2">
                  {request.modules.map((m) => (
                    <span
                      key={m}
                      className="px-3 py-1 bg-slate-100 text-slate-600 text-xs font-semibold rounded-md"
                    >
                      {m}
                    </span>
                  ))}
                </div>
              )}
            </div>
          </div>

          <div className="lg:col-span-5 bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
            <div className="flex items-center gap-2.5 bg-[#f0f4f8] px-6 py-4 border-b border-slate-200">
              <FileText className="w-5 h-5 text-[#0f172a]" />
              <h2 className="text-md font-bold text-[#0f172a]">Form Báo Giá</h2>
            </div>

            <form className="p-6 space-y-4" onSubmit={(e) => e.preventDefault()}>
              <div>
                <label className="block text-[11px] font-bold text-slate-600 mb-1.5">
                  Chi phí thực hiện (VNĐ) <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  placeholder="Ví dụ: 5000000"
                  value={cost}
                  onChange={(e) => setCost(e.target.value)}
                  className="w-full px-3 py-2 bg-[#f8fafc] border border-slate-200 rounded-lg text-sm focus:outline-none focus:border-teal-500"
                />
              </div>

              <div>
                <label className="block text-[11px] font-bold text-slate-600 mb-1.5">Hạn bàn giao</label>
                <div className="relative">
                  <input
                    type="date"
                    value={deadline}
                    onChange={(e) => setDeadline(e.target.value)}
                    className="w-full px-3 py-2 bg-[#f8fafc] border border-slate-200 rounded-lg text-sm focus:outline-none focus:border-teal-500"
                  />
                  <Calendar className="absolute right-3 top-2.5 w-4 h-4 text-slate-400 pointer-events-none" />
                </div>
              </div>

              <div>
                <label className="block text-[11px] font-bold text-slate-600 mb-1.5">Ghi chú</label>
                <textarea
                  rows={3}
                  value={note}
                  onChange={(e) => setNote(e.target.value)}
                  className="w-full px-3 py-2 bg-[#f8fafc] border border-slate-200 rounded-lg text-sm resize-none focus:outline-none focus:border-teal-500"
                />
              </div>

              <button
                type="button"
                disabled={isSubmitting}
                onClick={handleSubmitQuote}
                className="w-full flex items-center justify-center gap-2 bg-[#00667e] hover:bg-[#005367] disabled:opacity-50 text-white font-semibold py-2.5 rounded-lg text-sm transition-all"
              >
                {isSubmitting ? <Loader2 className="w-4 h-4 animate-spin" /> : null}
                Gửi Báo Giá
              </button>
            </form>
          </div>
        </div>
      </div>
    </ReporterLayout>
  );
}
