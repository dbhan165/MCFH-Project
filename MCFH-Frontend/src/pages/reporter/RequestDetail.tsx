import { useCallback, useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import {
  FileText,
  ClipboardList,
  Loader2,
  AlertCircle,
  Download,
  UploadCloud,
  CheckCircle2,
} from 'lucide-react';
import ReporterLayout from '../../components/reporter/ReporterLayout';
import { reporterApi } from '../../api/portalApi';
import type { PortalBespokeRequest } from '../../api/portalApi';
import { extractApiError } from '../../utils/authStorage';

export default function RequestDetail() {
  const { id } = useParams();
  const requestId = Number(id);
  const navigate = useNavigate();

  const [request, setRequest] = useState<PortalBespokeRequest | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isUploading, setIsUploading] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [file, setFile] = useState<File | null>(null);

  const load = useCallback(async () => {
    if (!requestId || Number.isNaN(requestId)) return;
    setIsLoading(true);
    setErrorMessage('');
    try {
      const data = await reporterApi.getRequest(requestId);
      setRequest(data);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải yêu cầu.'));
    } finally {
      setIsLoading(false);
    }
  }, [requestId]);

  useEffect(() => {
    load();
  }, [load]);

  const handleDownload = async () => {
    if (!requestId) return;
    try {
      await reporterApi.download(requestId);
      await load();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải báo cáo hệ thống.'));
    }
  };

  const handleUpload = async () => {
    if (!requestId || !file) {
      setErrorMessage('Vui lòng chọn file báo cáo đã chỉnh sửa.');
      return;
    }
    setIsUploading(true);
    setErrorMessage('');
    try {
      await reporterApi.uploadRevision(requestId, file);
      setSuccessMessage('Đã upload bản sửa thành công.');
      setFile(null);
      await load();
      setTimeout(() => navigate('/reporter/tasks'), 1200);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể upload bản sửa.'));
    } finally {
      setIsUploading(false);
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

  const canUpload =
    request.status === 'pending' ||
    request.status === 'assigned' ||
    request.status === 'in_progress' ||
    request.status === 'revision_requested';
  const priceLabel =
    request.agreedPrice != null ? `${request.agreedPrice.toLocaleString('vi-VN')} VND` : null;

  return (
    <ReporterLayout activeTopNav="reports">
      <div className="min-h-screen bg-[#f8fafc] -m-6 p-6 font-sans">
        <div className="mb-6">
          <div className="text-[11px] text-slate-500 font-medium flex items-center gap-1">
            <button type="button" onClick={() => navigate('/reporter/tasks')} className="hover:text-slate-700">
              Bảng công việc
            </button>
            <span className="text-slate-400">&gt;</span>
            <span className="text-slate-600 font-semibold">Yêu cầu #{request.requestId}</span>
          </div>
          <h1 className="text-2xl font-bold text-[#0f172a] mt-2 tracking-tight">{request.title}</h1>
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
          <div className="mb-4 flex items-center gap-2 text-sm text-emerald-700 bg-emerald-50 border border-emerald-100 rounded-lg px-4 py-3">
            <CheckCircle2 className="w-4 h-4" />
            {successMessage}
          </div>
        )}

        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
          <div className="lg:col-span-7 space-y-6">
            <div className="bg-white rounded-2xl border border-slate-200 p-6 shadow-sm">
              <div className="flex items-center gap-2.5 border-b border-slate-100 pb-4 mb-5">
                <ClipboardList className="w-5 h-5 text-[#0f172a]" />
                <h2 className="text-md font-bold text-[#0f172a]">Yêu cầu của khách</h2>
              </div>

              <div className="text-[13.5px] leading-relaxed text-[#334155] space-y-4">
                <p>{request.requirements || 'Khách hàng chưa ghi mô tả chi tiết.'}</p>

                {request.revisionFeedback && (
                  <div className="rounded-xl border border-pink-200 bg-pink-50 p-4">
                    <p className="text-xs font-bold text-pink-700 mb-1">Chi tiết cần chỉnh sửa</p>
                    <p className="text-sm text-pink-900 whitespace-pre-wrap">{request.revisionFeedback}</p>
                  </div>
                )}

                <div className="pt-4 border-t border-dashed border-slate-200 text-xs text-gray-500 space-y-1">
                  <p>Khách hàng: {request.clientName ?? '—'}</p>
                  {request.keyword && <p>Keyword: {request.keyword}</p>}
                  {request.packageType && (
                    <p>
                      Gói: {request.packageType === 'pro' ? 'Nâng cao' : 'Cơ bản'}
                      {priceLabel ? ` (${priceLabel})` : ''}
                    </p>
                  )}
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
          </div>

          <div className="lg:col-span-5 space-y-6">
            <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
              <div className="flex items-center gap-2.5 bg-[#f0f4f8] px-6 py-4 border-b border-slate-200">
                <FileText className="w-5 h-5 text-[#0f172a]" />
                <h2 className="text-md font-bold text-[#0f172a]">Báo cáo hệ thống (bản nháp)</h2>
              </div>
              <div className="p-6 space-y-3">
                <p className="text-sm text-slate-500">
                  Tải file hệ thống đã tạo sẵn về máy, chỉnh sửa thủ công rồi upload lại. Sau khi tải, đơn sẽ chuyển sang «Đang xử lý».
                </p>
                <button
                  type="button"
                  disabled={!request.hasDeliverable && request.status !== 'pending'}
                  onClick={handleDownload}
                  className="w-full inline-flex items-center justify-center gap-2 bg-teal-600 hover:bg-teal-700 disabled:opacity-50 text-white font-semibold py-2.5 rounded-lg text-sm"
                >
                  <Download className="w-4 h-4" />
                  {request.hasDeliverable || request.status === 'pending'
                    ? 'Tải báo cáo hệ thống'
                    : 'Chưa có báo cáo'}
                </button>
              </div>
            </div>

            <div className="bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
              <div className="flex items-center gap-2.5 bg-[#f0f4f8] px-6 py-4 border-b border-slate-200">
                <UploadCloud className="w-5 h-5 text-[#0f172a]" />
                <h2 className="text-md font-bold text-[#0f172a]">Upload bản đã chỉnh sửa</h2>
              </div>
              <div className="p-6 space-y-4">
                {canUpload ? (
                  <>
                    <p className="text-sm text-slate-500">
                      {request.status === 'assigned'
                        ? 'Nên tải báo cáo hệ thống trước, rồi upload bản đã chỉnh. Upload xong đơn sẽ vào «Đã gửi khách».'
                        : 'Upload file PDF / PPTX / HTML sau khi chỉnh sửa. Khách sẽ được thông báo khi nhận được.'}
                    </p>
                    <input
                      type="file"
                      accept=".pdf,.pptx,.ppt,.html,.htm"
                      onChange={(e) => setFile(e.target.files?.[0] ?? null)}
                      className="w-full text-sm text-slate-600 file:mr-4 file:py-2 file:px-4 file:rounded-lg file:border-0 file:text-sm file:font-semibold file:bg-teal-50 file:text-teal-700 hover:file:bg-teal-100"
                    />
                    <button
                      type="button"
                      disabled={isUploading || !file}
                      onClick={handleUpload}
                      className="w-full inline-flex items-center justify-center gap-2 bg-[#00667e] hover:bg-[#005367] disabled:opacity-50 text-white font-semibold py-2.5 rounded-lg text-sm"
                    >
                      {isUploading ? <Loader2 className="w-4 h-4 animate-spin" /> : <UploadCloud className="w-4 h-4" />}
                      Upload &amp; gửi khách
                    </button>
                  </>
                ) : (
                  <p className="text-sm text-slate-500">
                    {request.status === 'completed'
                      ? 'Đã gửi khách. Khi khách yêu cầu sửa thêm, đơn sẽ quay lại «Cần chỉnh sửa».'
                      : 'Đơn chưa sẵn sàng để upload.'}
                  </p>
                )}
              </div>
            </div>
          </div>
        </div>
      </div>
    </ReporterLayout>
  );
}
