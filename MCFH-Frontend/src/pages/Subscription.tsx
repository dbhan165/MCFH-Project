import { useEffect, useState } from 'react';
import { CheckCircle2, Clock, CreditCard, Loader2, XCircle, Zap } from 'lucide-react';
import { subscriptionApi, type BillingSummary, type PaymentHistory } from '../api/subscriptionApi';
import { workspaceApi } from '../api/workspaceApi';
import type { Workspace } from '../types/workspace';
import { extractApiError } from '../utils/authStorage';
import { formatWorkspaceDateTime } from '../utils/workspaceHelpers';

const STATUS_CONFIG: Record<string, { label: string; className: string; icon: typeof CheckCircle2 }> = {
  success: { label: 'Thành công', className: 'text-emerald-400', icon: CheckCircle2 },
  pending: { label: 'Đang chờ', className: 'text-amber-400', icon: Clock },
  failed: { label: 'Thất bại', className: 'text-red-400', icon: XCircle },
};

function getStatusConfig(status: string | null | undefined) {
  return STATUS_CONFIG[status ?? ''] ?? { label: status ?? '—', className: 'text-gray-400', icon: Clock };
}

const PAGE_SIZE = 5;

const Subscription = () => {
  const [workspaces, setWorkspaces] = useState<Workspace[]>([]);
  const [workspaceId, setWorkspaceId] = useState<number | null>(null);
  const [billing, setBilling] = useState<BillingSummary | null>(null);
  const [payments, setPayments] = useState<PaymentHistory[]>([]);
  const [currentPage, setCurrentPage] = useState(1);
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
    setCurrentPage(1);
    await loadData(wid);
  };

  const totalPages = Math.max(1, Math.ceil(payments.length / PAGE_SIZE));
  const safePage = Math.min(currentPage, totalPages);
  const paginatedPayments = payments.slice(
    (safePage - 1) * PAGE_SIZE,
    safePage * PAGE_SIZE,
  );

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

      <div className="p-8 md:p-10 w-full">
        <div className="max-w-5xl mx-auto mb-8">
          <h1 className="text-4xl font-bold tracking-tight">Hạn mức & Thanh toán</h1>
        </div>

        {errorMessage && (
          <div className="max-w-5xl mx-auto mb-6 text-red-300 bg-red-500/10 border border-red-500/30 rounded-xl p-4 text-sm">
            {errorMessage}
          </div>
        )}

        {workspaces.length === 0 ? (
          <div className="max-w-5xl mx-auto bg-[#0A101D] border border-white/10 rounded-2xl p-8 text-center">
            <p className="text-gray-400">Bạn chưa có workspace nào.</p>
          </div>
        ) : (
          <>
            {/* Phần trên: Toàn bộ giao dịch (rộng) */}
            <div className="max-w-7xl mx-auto bg-[#0A101D] border border-white/5 rounded-2xl overflow-hidden mb-8">
              <div className="p-6 border-b border-white/5 flex items-center gap-2">
                <CreditCard className="text-[#FF7575]" size={22} />
                <h3 className="text-xl font-bold">Toàn bộ giao dịch</h3>
                <span className="ml-auto text-sm text-gray-500">
                  {payments.length} giao dịch
                </span>
              </div>
              {payments.length === 0 ? (
                <p className="p-6 text-gray-500 text-sm">
                  Chưa có giao dịch nào trên các workspace của bạn.
                </p>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-left">
                    <thead>
                      <tr className="bg-white/[0.02]">
                        <th className="px-8 py-5 text-sm font-bold text-gray-400 uppercase tracking-wider">Mã hóa đơn</th>
                        <th className="px-8 py-5 text-sm font-bold text-gray-400 uppercase tracking-wider">Ngày</th>
                        <th className="px-8 py-5 text-sm font-bold text-gray-400 uppercase tracking-wider">Workspace</th>
                        <th className="px-8 py-5 text-sm font-bold text-gray-400 uppercase tracking-wider">Project</th>
                        <th className="px-8 py-5 text-sm font-bold text-gray-400 uppercase tracking-wider text-right">Số tiền</th>
                        <th className="px-8 py-5 text-sm font-bold text-gray-400 uppercase tracking-wider">Trạng thái</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-white/5">
                      {paginatedPayments.map((invoice) => {
                        const statusCfg = getStatusConfig(invoice.status);
                        const StatusIcon = statusCfg.icon;
                        return (
                          <tr key={invoice.paymentId} className="hover:bg-white/[0.02]">
                            <td className="px-8 py-5 font-mono text-base text-white">{invoice.transactionRef ?? `#${invoice.paymentId}`}</td>
                            <td className="px-8 py-5 text-base text-gray-200">{formatWorkspaceDateTime(invoice.createdAt)}</td>
                            <td className="px-8 py-5 text-base text-gray-200">{invoice.workspaceName ?? '—'}</td>
                            <td className="px-8 py-5 text-base text-gray-200">{invoice.projectName ?? '—'}</td>
                            <td className="px-8 py-5 text-right font-bold text-base text-white">{invoice.amountLabel}</td>
                            <td className="px-8 py-5">
                              <span className={`inline-flex items-center gap-2 text-base font-semibold ${statusCfg.className}`}>
                                <StatusIcon size={18} />
                                {statusCfg.label}
                              </span>
                            </td>
                          </tr>
                        );
                      })}
                    </tbody>
                  </table>
                </div>
              )}

              {payments.length > PAGE_SIZE && (
                <div className="p-4 border-t border-white/5 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
                  <span className="text-sm text-gray-500">
                    Trang {safePage} / {totalPages} · {payments.length} giao dịch
                  </span>
                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      onClick={() => setCurrentPage((p) => Math.max(1, p - 1))}
                      disabled={safePage === 1}
                      className="px-3 py-1.5 rounded-lg bg-[#151B2B] border border-white/10 text-sm text-white disabled:opacity-40 disabled:cursor-not-allowed hover:border-[#FF7575]/60 transition-colors"
                    >
                      ← Trước
                    </button>
                    {Array.from({ length: totalPages }, (_, i) => i + 1).map((p) => (
                      <button
                        key={p}
                        type="button"
                        onClick={() => setCurrentPage(p)}
                        className={`min-w-[36px] px-2 py-1.5 rounded-lg text-sm font-semibold border transition-colors ${
                          p === safePage
                            ? 'bg-[#FF7575] border-[#FF7575] text-white'
                            : 'bg-[#151B2B] border-white/10 text-gray-300 hover:border-[#FF7575]/60'
                        }`}
                      >
                        {p}
                      </button>
                    ))}
                    <button
                      type="button"
                      onClick={() => setCurrentPage((p) => Math.min(totalPages, p + 1))}
                      disabled={safePage === totalPages}
                      className="px-3 py-1.5 rounded-lg bg-[#151B2B] border border-white/10 text-sm text-white disabled:opacity-40 disabled:cursor-not-allowed hover:border-[#FF7575]/60 transition-colors"
                    >
                      Sau →
                    </button>
                  </div>
                </div>
              )}
            </div>

            {/* Phần dưới: Chi tiết workspace được chọn (hẹp hơn) */}
            <div className="max-w-5xl mx-auto">
            <div className="bg-[#0A101D] border border-white/5 rounded-2xl overflow-hidden mb-8">
              <div className="p-6 border-b border-white/5 flex flex-col sm:flex-row sm:items-center gap-3">
                <div className="flex items-center gap-2">
                  <Zap className="text-[#FF7575]" />
                  <h3 className="text-lg font-bold">
                    Chi tiết {billing?.workspaceName ?? 'workspace'}
                  </h3>
                </div>
                {workspaces.length > 1 && (
                  <select
                    value={workspaceId ?? ''}
                    onChange={(e) => handleWorkspaceChange(Number(e.target.value))}
                    className="sm:ml-auto bg-[#151B2B] border border-white/10 rounded-xl px-4 py-2 text-sm text-white focus:outline-none focus:border-[#FF7575]"
                  >
                    {workspaces.map((ws) => (
                      <option key={ws.workspaceId} value={ws.workspaceId}>
                        {ws.name}
                      </option>
                    ))}
                  </select>
                )}
              </div>
              <div className="p-6 space-y-6">
                <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
                  <InfoRow
                    label="Số Dự án"
                    value={`${(billing?.projectUsed ?? 0).toLocaleString('vi-VN')}`}
                  />
                  <InfoRow
                    label="Số Mention"
                    value={`${(billing?.mentionUsed ?? 0).toLocaleString('vi-VN')}`}
                  />
                  <InfoRow
                    label="Số Thành viên"
                    value={`${(billing?.memberUsed ?? 0).toLocaleString('vi-VN')}`}
                  />
                </div>
              </div>
            </div>

            {/* Section: Project Packages của workspace được chọn */}
            <div className="bg-[#0A101D] border border-white/5 rounded-2xl overflow-hidden">
              <div className="p-6 border-b border-white/5 flex items-center gap-2">
                <Zap className="text-[#FF7575]" />
                <h3 className="text-lg font-bold">Gói dữ liệu dự án — {billing?.workspaceName ?? ''}</h3>
              </div>
              {!billing?.projectPackages || billing.projectPackages.length === 0 ? (
                <p className="p-6 text-gray-500 text-sm">
                  Workspace này chưa mua gói Mentions nào cho các dự án.
                </p>
              ) : (
                <table className="w-full text-left">
                  <thead>
                    <tr className="bg-white/[0.02]">
                      <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase">Dự án</th>
                      <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase">Gói mua</th>
                      <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase">Mức sử dụng</th>
                      <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase">Số tiền</th>
                      <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase">Thanh toán</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-white/5">
                    {billing.projectPackages.map((pkg) => {
                      const percent = pkg.mentionsIncluded > 0
                          ? Math.min(100, Math.round((pkg.mentionsUsed / pkg.mentionsIncluded) * 100))
                          : pkg.mentionsIncluded === -1 ? 0 : 100;
                      const pkgStatusCfg = getStatusConfig(pkg.paymentStatus);
                      const PkgStatusIcon = pkgStatusCfg.icon;
                      return (
                      <tr key={pkg.packageId} className="hover:bg-white/[0.02]">
                        <td className="px-6 py-4 text-sm font-semibold">{pkg.projectName}</td>
                        <td className="px-6 py-4 text-sm text-gray-300">
                          {pkg.packageType}
                          {pkg.status === 'exhausted' && <span className="ml-2 text-xs text-amber-500 border border-amber-500/30 px-2 py-0.5 rounded-full">Đã hết</span>}
                        </td>
                        <td className="px-6 py-4">
                          <div className="flex flex-col gap-1 w-48">
                            <div className="text-xs text-gray-400 flex justify-between">
                              <span>Mentions</span>
                              <span>{pkg.mentionsUsed} / {pkg.mentionsIncluded === -1 ? '∞' : pkg.mentionsIncluded}</span>
                            </div>
                            <div className="w-full bg-[#151B2B] h-1.5 rounded-full overflow-hidden">
                              <div
                                className={`h-full ${percent > 85 ? 'bg-amber-500' : 'bg-blue-500'}`}
                                style={{ width: `${pkg.mentionsIncluded === -1 ? 0 : percent}%` }}
                              />
                            </div>
                          </div>
                        </td>
                        <td className="px-6 py-4 font-bold text-sm">{pkg.amount.toLocaleString('vi-VN')} ₫</td>
                        <td className="px-6 py-4">
                          <span className={`inline-flex items-center gap-1.5 text-sm font-semibold ${pkgStatusCfg.className}`}>
                            <PkgStatusIcon size={16} />
                            {pkgStatusCfg.label}
                          </span>
                        </td>
                      </tr>
                    )})}
                  </tbody>
                </table>
              )}
            </div>
            </div>
          </>
        )}
      </div>
    </div>
  );
};

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-xs font-bold text-gray-500 uppercase tracking-wide">{label}</span>
      <span className="text-base font-semibold text-white">{value}</span>
    </div>
  );
}

export default Subscription;
