import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { CheckCircle2, CreditCard, Loader2, Zap } from 'lucide-react';
import { subscriptionApi, type BillingSummary, type PaymentHistory } from '../api/subscriptionApi';
import { workspaceApi } from '../api/workspaceApi';
import type { Workspace } from '../types/workspace';
import { extractApiError } from '../utils/authStorage';
import { formatWorkspaceDateTime } from '../utils/workspaceHelpers';

const Subscription = () => {
  const [workspaces, setWorkspaces] = useState<Workspace[]>([]);
  const [workspaceId, setWorkspaceId] = useState<number | null>(null);
  const [billing, setBilling] = useState<BillingSummary | null>(null);
  const [payments, setPayments] = useState<PaymentHistory[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');

  const loadData = async (wid?: number) => {
    setIsLoading(true);
    setErrorMessage('');
    try {
      const [billingData, paymentData] = await Promise.all([
        subscriptionApi.getBilling(wid),
        subscriptionApi.getPayments(),
      ]);
      setBilling(billingData);
      setPayments(paymentData);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải thông tin gói cước.'));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    const init = async () => {
      try {
        const wsList = await workspaceApi.getMyWorkspaces();
        setWorkspaces(wsList);
        const first = wsList[0]?.workspaceId ?? null;
        setWorkspaceId(first);
        if (first) await loadData(first);
        else setIsLoading(false);
      } catch (error) {
        setErrorMessage(extractApiError(error, 'Không thể tải workspace.'));
        setIsLoading(false);
      }
    };
    init();
  }, []);

  const handleWorkspaceChange = async (wid: number) => {
    setWorkspaceId(wid);
    await loadData(wid);
  };

  const statusLabel =
    billing?.status === 'active'
      ? 'Đang hoạt động'
      : billing?.status === 'free'
        ? 'Miễn phí'
        : billing?.status ?? '—';

  if (isLoading) {
    return (
      <div className="flex flex-col h-full text-white bg-[#050A15] items-center justify-center">
        <Loader2 className="w-8 h-8 animate-spin text-[#FF7575]" />
      </div>
    );
  }

  return (
    <div className="flex flex-col h-full text-white bg-[#050A15] overflow-y-auto">
      <header className="h-20 bg-[#0A101D]/80 backdrop-blur-md border-b border-white/5 flex items-center px-8 sticky top-0 z-20 shrink-0">
        <div className="text-gray-300 font-medium tracking-wide">Quản lý Tài chính</div>
      </header>

      <div className="p-8 md:p-10 max-w-5xl mx-auto w-full">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 mb-8">
          <h1 className="text-4xl font-bold tracking-tight">Gói cước (Billing)</h1>
          {workspaces.length > 1 && (
            <select
              value={workspaceId ?? ''}
              onChange={(e) => handleWorkspaceChange(Number(e.target.value))}
              className="bg-[#151B2B] border border-white/10 rounded-xl px-4 py-2.5 text-sm text-white focus:outline-none focus:border-[#FF7575]"
            >
              {workspaces.map((ws) => (
                <option key={ws.workspaceId} value={ws.workspaceId}>
                  {ws.name}
                </option>
              ))}
            </select>
          )}
        </div>

        {errorMessage && (
          <div className="mb-6 text-red-300 bg-red-500/10 border border-red-500/30 rounded-xl p-4 text-sm">
            {errorMessage}
          </div>
        )}

        {workspaces.length === 0 ? (
          <div className="bg-[#0A101D] border border-white/10 rounded-2xl p-8 text-center">
            <p className="text-gray-400 mb-4">Tạo workspace để quản lý gói cước và hạn mức.</p>
            <Link to="/create-workspace" className="text-[#FF7575] font-semibold hover:underline">
              Tạo workspace
            </Link>
          </div>
        ) : (
          <>
            <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 mb-8">
              <div className="lg:col-span-2 bg-gradient-to-br from-[#0A101D] to-[#151B2B] border border-[#FF7575]/30 rounded-2xl p-8 relative overflow-hidden">
                <div className="absolute top-0 right-0 p-8 opacity-10">
                  <Zap size={100} />
                </div>
                <div className="relative z-10">
                  <div className="flex flex-wrap items-center gap-2 mb-4">
                    <span className="inline-block px-3 py-1 bg-[#FF7575]/20 text-[#FF7575] rounded-full text-xs font-bold uppercase border border-[#FF7575]/30">
                      Gói hiện tại
                    </span>
                    <span className="inline-block px-3 py-1 bg-white/5 text-gray-300 rounded-full text-xs font-semibold">
                      {statusLabel}
                    </span>
                  </div>
                  <h2 className="text-3xl font-bold mb-1">{billing?.planName ?? 'Khởi động'}</h2>
                  <p className="text-gray-500 text-sm mb-1">{billing?.workspaceName}</p>
                  <p className="text-gray-400 mb-6">{billing?.renewalNote ?? 'Chưa có gói trả phí.'}</p>
                  <Link
                    to={workspaceId ? `/subscription/upgrade?workspaceId=${workspaceId}` : '/subscription/upgrade'}
                    className="inline-flex bg-[#FF7575] hover:bg-[#ff6262] text-white px-6 py-3 rounded-lg font-bold transition-colors"
                  >
                    Nâng cấp / đổi gói
                  </Link>
                </div>
              </div>

              <div className="bg-[#0A101D] border border-white/5 rounded-2xl p-8">
                <h3 className="text-lg font-bold mb-6">Mức sử dụng tài nguyên</h3>
                <div className="space-y-6">
                  <UsageBar label="Dự án" used={billing?.projectUsed ?? 0} limit={billing?.projectLimit ?? 1} color="bg-[#00B4D8]" />
                  <UsageBar label="Mentions" used={billing?.mentionUsed ?? 0} limit={billing?.mentionLimit ?? 1000} color="bg-yellow-500" />
                  <UsageBar label="Thành viên" used={billing?.memberUsed ?? 0} limit={billing?.memberLimit ?? 5} color="bg-emerald-500" />
                  <UsageBar label="AI Credits" used={billing?.aiCreditUsed ?? 0} limit={billing?.aiCreditLimit ?? 100} color="bg-violet-500" />
                </div>
              </div>
            </div>

            <div className="bg-[#0A101D] border border-white/5 rounded-2xl overflow-hidden">
              <div className="p-6 border-b border-white/5 flex items-center gap-2">
                <CreditCard className="text-[#FF7575]" />
                <h3 className="text-lg font-bold">Lịch sử thanh toán</h3>
              </div>
              {payments.length === 0 ? (
                <p className="p-6 text-gray-500 text-sm">
                  Chưa có giao dịch. Nâng cấp gói để thấy hóa đơn demo tại đây.
                </p>
              ) : (
                <table className="w-full text-left">
                  <thead>
                    <tr className="bg-white/[0.02]">
                      <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase">Mã hóa đơn</th>
                      <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase">Ngày</th>
                      <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase">Gói</th>
                      <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase">Số tiền</th>
                      <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase">Trạng thái</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-white/5">
                    {payments.map((invoice) => (
                      <tr key={invoice.paymentId} className="hover:bg-white/[0.02]">
                        <td className="px-6 py-4 font-mono text-sm">{invoice.transactionRef ?? `#${invoice.paymentId}`}</td>
                        <td className="px-6 py-4 text-gray-300">{formatWorkspaceDateTime(invoice.createdAt)}</td>
                        <td className="px-6 py-4 text-gray-300">{invoice.planName ?? '—'}</td>
                        <td className="px-6 py-4 font-bold">{invoice.amountLabel}</td>
                        <td className="px-6 py-4">
                          <span className="flex items-center gap-1.5 text-emerald-500 text-sm font-semibold">
                            <CheckCircle2 size={16} /> {invoice.status === 'success' ? 'Thành công' : invoice.status}
                          </span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </div>
          </>
        )}
      </div>
    </div>
  );
};

function UsageBar({
  label,
  used,
  limit,
  color,
}: {
  label: string;
  used: number;
  limit: number;
  color: string;
}) {
  const percent = limit > 0 ? Math.min(100, Math.round((used / limit) * 100)) : 0;
  const isNearLimit = percent >= 85;
  return (
    <div>
      <div className="flex justify-between text-sm mb-2">
        <span className="text-gray-400">{label}</span>
        <span className={`font-bold ${isNearLimit ? 'text-amber-400' : ''}`}>
          {used.toLocaleString('vi-VN')} / {limit.toLocaleString('vi-VN')}
        </span>
      </div>
      <div className="w-full bg-[#151B2B] h-2 rounded-full overflow-hidden">
        <div className={`${color} h-full transition-all`} style={{ width: `${percent}%` }} />
      </div>
    </div>
  );
}

export default Subscription;
