import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import {
  ArrowLeft, CheckCircle2, Clock, FileText, Loader2, AlertCircle,
} from 'lucide-react';
import { scrapeOrderApi, type ScrapeOrder } from '../api/scrapeOrderApi';

function formatDateTime(iso?: string | null) {
  if (!iso) return '—';
  const d = new Date(iso);
  if (Number.isNaN(d.getTime())) return iso;
  return d.toLocaleString('vi-VN', { hour: '2-digit', minute: '2-digit', day: '2-digit', month: '2-digit', year: 'numeric' });
}

const ScrapeOrderTracking = () => {
  const { workspaceId, orderId } = useParams<{ workspaceId: string; orderId: string }>();
  const navigate = useNavigate();
  const [order, setOrder] = useState<ScrapeOrder | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const oid = Number(orderId);
    if (!oid || Number.isNaN(oid)) {
      setError('Đơn không hợp lệ.');
      setLoading(false);
      return;
    }

    let stop = () => {};
    scrapeOrderApi
      .get(oid)
      .then((o) => {
        setOrder(o);
        setLoading(false);
        if (o.status !== 'completed' && o.status !== 'failed') {
          stop = scrapeOrderApi.pollOrder(oid, setOrder);
        }
      })
      .catch(() => {
        setError('Không tải được thông tin đơn cào dữ liệu.');
        setLoading(false);
      });

    return () => stop();
  }, [orderId]);

  const isActive = order && !['completed', 'failed', 'quoted'].includes(order.status);
  const isDone = order?.status === 'completed';
  const isFailed = order?.status === 'failed';

  return (
    <div className="min-h-screen bg-[#050A15] text-white font-sans p-6">
      <div className="max-w-2xl mx-auto">
        <Link
          to={`/workspace/${workspaceId}/projects`}
          className="inline-flex items-center gap-2 text-sm text-gray-400 hover:text-white mb-8"
        >
          <ArrowLeft className="w-4 h-4" />
          Về danh sách dự án
        </Link>

        {loading && (
          <div className="flex flex-col items-center py-20 text-gray-400">
            <Loader2 className="w-10 h-10 animate-spin text-[#FF7575] mb-4" />
            Đang tải tiến độ...
          </div>
        )}

        {error && (
          <div className="bg-red-500/10 border border-red-500/30 rounded-xl p-6 flex gap-3 text-red-300">
            <AlertCircle className="w-5 h-5 shrink-0" />
            {error}
          </div>
        )}

        {order && !loading && (
          <div className="bg-[#0A101D] border border-white/10 rounded-2xl overflow-hidden shadow-2xl">
            <div className="px-8 py-6 border-b border-white/5">
              <div className="flex items-start gap-4">
                {isDone ? (
                  <CheckCircle2 className="w-10 h-10 text-emerald-400 shrink-0" />
                ) : isFailed ? (
                  <AlertCircle className="w-10 h-10 text-red-400 shrink-0" />
                ) : (
                  <Loader2 className="w-10 h-10 text-[#FF7575] animate-spin shrink-0" />
                )}
                <div className="min-w-0 flex-1">
                  <p className="text-xs font-bold text-[#FF7575] uppercase tracking-wider mb-1">
                    Đơn cào #{order.orderId}
                  </p>
                  <h1 className="text-2xl font-bold truncate">{order.projectName}</h1>
                  <p className="text-gray-400 text-sm mt-1">
                    Từ khóa: <span className="text-white">{order.keyword}</span>
                    {' · '}
                    {order.timeRangeLabel}
                  </p>
                </div>
              </div>

              <div className="mt-6">
                <div className="flex justify-between text-sm mb-2">
                  <span className="text-gray-400">{order.statusLabel}</span>
                  <span className="font-bold text-[#00B4D8]">{order.progressPercent}%</span>
                </div>
                <div className="h-3 rounded-full bg-white/5 overflow-hidden">
                  <div
                    className={`h-full rounded-full transition-all duration-700 ease-out ${
                      isFailed ? 'bg-red-500' : isDone ? 'bg-emerald-500' : 'bg-gradient-to-r from-[#FF7575] to-[#00B4D8]'
                    }`}
                    style={{ width: `${Math.min(100, Math.max(0, order.progressPercent))}%` }}
                  />
                </div>
                {order.statusMessage && (
                  <p className="text-sm text-gray-300 mt-3">{order.statusMessage}</p>
                )}
              </div>
            </div>

            <div className="px-8 py-6 grid grid-cols-1 sm:grid-cols-2 gap-4 text-sm">
              <div className="bg-[#151B2B] rounded-xl p-4 border border-white/5">
                <div className="flex items-center gap-2 text-gray-500 mb-1">
                  <Clock className="w-4 h-4" />
                  Báo cáo dự kiến
                </div>
                <p className="font-semibold">{formatDateTime(order.estimatedReportAt)}</p>
              </div>
              <div className="bg-[#151B2B] rounded-xl p-4 border border-white/5">
                <div className="flex items-center gap-2 text-gray-500 mb-1">
                  <FileText className="w-4 h-4" />
                  Đã thanh toán
                </div>
                <p className="font-semibold">{order.priceLabel}</p>
              </div>
            </div>

            {isActive && order.scrapeJob?.platforms && order.scrapeJob.platforms.length > 0 && (
              <div className="px-8 pb-4 space-y-2">
                <p className="text-xs font-bold text-gray-500 uppercase">Chi tiết nền tảng</p>
                {order.scrapeJob.platforms.map((p) => (
                  <div key={p.platform} className="flex justify-between text-sm bg-[#151B2B] rounded-lg px-4 py-2 border border-white/5">
                    <span>{p.label}</span>
                    <span className="text-gray-400">{p.message ?? p.status}</span>
                  </div>
                ))}
              </div>
            )}

            {isActive && (
              <div className="px-8 pb-6">
                <p className="text-xs text-gray-500 bg-amber-500/10 border border-amber-500/20 text-amber-200/90 rounded-lg px-4 py-3">
                  Bạn có thể đóng trang này — hệ thống vẫn tiếp tục cào và phân tích. Quay lại bất cứ lúc nào để xem % tiến độ.
                </p>
              </div>
            )}

            {isDone && (
              <div className="px-8 pb-8 flex flex-col sm:flex-row gap-3">
                <button
                  type="button"
                  onClick={() => navigate(`/workspace/${workspaceId}/project/${order.projectId}/reports`)}
                  className="flex-1 py-3 rounded-xl bg-[#FF7575] hover:bg-[#ff6262] font-semibold text-sm"
                >
                  Xem báo cáo
                </button>
                <button
                  type="button"
                  onClick={() => navigate(`/workspace/${workspaceId}/project/${order.projectId}`)}
                  className="flex-1 py-3 rounded-xl border border-white/20 hover:bg-white/5 font-semibold text-sm"
                >
                  Vào dashboard dự án
                </button>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default ScrapeOrderTracking;
