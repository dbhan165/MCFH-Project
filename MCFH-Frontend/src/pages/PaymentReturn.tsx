import { useEffect, useRef, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { AlertCircle, CheckCircle2, CreditCard, Loader2, XCircle } from 'lucide-react';
import { scrapeOrderApi, type ScrapeOrder } from '../api/scrapeOrderApi';
import { extractApiError } from '../utils/authStorage';

type ReturnState = 'checking' | 'paid' | 'cancelled' | 'error';

const PAID_STATUSES = ['paid', 'scraping', 'analyzing', 'completed'];

/**
 * Trang PayOS redirect về sau thanh toán. KHÔNG tin query param của PayOS —
 * chỉ poll backend (backend tự đối soát với PayOS/webhook) rồi mới chuyển trang.
 */
const PaymentReturn = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const orderId = Number(searchParams.get('orderId'));

  const [state, setState] = useState<ReturnState>('checking');
  const [order, setOrder] = useState<ScrapeOrder | null>(null);
  const [errorMessage, setErrorMessage] = useState('');
  const [retrying, setRetrying] = useState(false);
  const attemptsRef = useRef(0);

  useEffect(() => {
    if (!orderId || Number.isNaN(orderId)) {
      setState('error');
      setErrorMessage('Thiếu mã đơn hàng trên đường dẫn.');
      return;
    }

    let active = true;
    const tick = async () => {
      if (!active) return;
      try {
        const result = await scrapeOrderApi.getPaymentStatus(orderId);
        if (!active) return;
        setOrder(result);

        if (PAID_STATUSES.includes(result.status)) {
          setState('paid');
          setTimeout(() => {
            if (active) navigate(`/workspace/${result.workspaceId}/orders/${result.orderId}`);
          }, 1500);
          return;
        }
        if (result.status === 'quoted') {
          // Backend xác nhận link thanh toán đã hủy / hết hạn.
          setState('cancelled');
          return;
        }
      } catch (error) {
        if (!active) return;
        attemptsRef.current += 1;
        if (attemptsRef.current >= 5) {
          setState('error');
          setErrorMessage(extractApiError(error, 'Không kiểm tra được trạng thái thanh toán.'));
          return;
        }
      }
      if (active) setTimeout(tick, 3000);
    };
    tick();
    return () => {
      active = false;
    };
  }, [orderId, navigate]);

  const handleRetryPayment = async () => {
    setRetrying(true);
    setErrorMessage('');
    try {
      const checkout = await scrapeOrderApi.pay(orderId);
      if (PAID_STATUSES.includes(checkout.order.status) || !checkout.checkoutUrl) {
        navigate(`/workspace/${checkout.order.workspaceId}/orders/${checkout.order.orderId}`);
        return;
      }
      window.location.href = checkout.checkoutUrl;
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không tạo được thanh toán mới. Vui lòng thử lại.'));
      setRetrying(false);
    }
  };

  return (
    <div className="min-h-screen bg-[#050A15] text-white font-sans flex items-center justify-center p-6 relative overflow-hidden">
      <div
        className="absolute inset-0 z-0 opacity-10 pointer-events-none"
        style={{ backgroundImage: 'radial-gradient(#ffffff 1px, transparent 1px)', backgroundSize: '32px 32px' }}
      />

      <div className="w-full max-w-lg bg-[#0A101D] border border-white/5 rounded-2xl p-10 relative z-10 shadow-2xl text-center space-y-6">
        {state === 'checking' && (
          <>
            <div className="w-16 h-16 rounded-full bg-[#FF7575]/10 flex items-center justify-center mx-auto">
              <CreditCard className="w-8 h-8 text-[#FF7575]" />
            </div>
            <h1 className="text-2xl font-bold">Đang xác nhận thanh toán...</h1>
            <p className="text-gray-400 text-sm">
              Hệ thống đang đối soát với PayOS. Quá trình này thường mất vài giây — vui lòng không đóng trang.
            </p>
            <Loader2 className="w-8 h-8 animate-spin text-[#FF7575] mx-auto" />
          </>
        )}

        {state === 'paid' && (
          <>
            <div className="w-16 h-16 rounded-full bg-emerald-500/10 flex items-center justify-center mx-auto">
              <CheckCircle2 className="w-8 h-8 text-emerald-400" />
            </div>
            <h1 className="text-2xl font-bold">Thanh toán thành công!</h1>
            <p className="text-gray-400 text-sm">
              Hệ thống đã bắt đầu cào dữ liệu — đang chuyển đến trang theo dõi tiến độ...
            </p>
            <Loader2 className="w-6 h-6 animate-spin text-emerald-400 mx-auto" />
          </>
        )}

        {state === 'cancelled' && (
          <>
            <div className="w-16 h-16 rounded-full bg-amber-500/10 flex items-center justify-center mx-auto">
              <XCircle className="w-8 h-8 text-amber-400" />
            </div>
            <h1 className="text-2xl font-bold">Thanh toán chưa hoàn tất</h1>
            <p className="text-gray-400 text-sm">
              Giao dịch đã bị hủy hoặc hết hạn. Đơn cào dữ liệu vẫn được giữ — bạn có thể thanh toán lại bất cứ lúc nào.
            </p>
            {errorMessage && (
              <p className="text-red-400 text-sm flex items-center justify-center gap-2">
                <AlertCircle className="w-4 h-4 shrink-0" />
                {errorMessage}
              </p>
            )}
            <div className="flex items-center justify-center gap-3 pt-2">
              <button
                onClick={handleRetryPayment}
                disabled={retrying}
                className="bg-[#FF7575] hover:bg-[#ff6262] text-white px-6 py-3 rounded-lg text-sm font-semibold disabled:opacity-60 flex items-center gap-2"
              >
                {retrying && <Loader2 className="w-4 h-4 animate-spin" />}
                Thanh toán lại
              </button>
              {order && (
                <Link
                  to={`/workspace/${order.workspaceId}/projects`}
                  className="px-6 py-3 rounded-lg text-sm font-semibold text-gray-400 hover:bg-white/5"
                >
                  Về danh sách dự án
                </Link>
              )}
            </div>
          </>
        )}

        {state === 'error' && (
          <>
            <div className="w-16 h-16 rounded-full bg-red-500/10 flex items-center justify-center mx-auto">
              <AlertCircle className="w-8 h-8 text-red-400" />
            </div>
            <h1 className="text-2xl font-bold">Không kiểm tra được thanh toán</h1>
            <p className="text-gray-400 text-sm">{errorMessage}</p>
            <p className="text-gray-500 text-xs">
              Nếu bạn đã thanh toán, đừng lo — hệ thống sẽ tự kích hoạt đơn khi nhận được xác nhận từ PayOS.
              Kiểm tra lại trạng thái đơn trong mục dự án của bạn.
            </p>
            <Link
              to="/workspaces"
              className="inline-block bg-white/5 hover:bg-white/10 text-white px-6 py-3 rounded-lg text-sm font-semibold"
            >
              Về trang chính
            </Link>
          </>
        )}
      </div>
    </div>
  );
};

export default PaymentReturn;
