import { useEffect, useState } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { ArrowLeft, Check, Loader2, Sparkles } from 'lucide-react';
import { subscriptionApi, type BillingSummary, type SubscriptionPlan } from '../api/subscriptionApi';
import { workspaceApi } from '../api/workspaceApi';
import type { Workspace } from '../types/workspace';
import { extractApiError } from '../utils/authStorage';
import { useAppModal } from '../contexts/AppModalContext';

const SubscriptionUpgrade = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const { alert, confirm } = useAppModal();

  const [plans, setPlans] = useState<SubscriptionPlan[]>([]);
  const [workspaces, setWorkspaces] = useState<Workspace[]>([]);
  const [billing, setBilling] = useState<BillingSummary | null>(null);
  const [workspaceId, setWorkspaceId] = useState<number | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [subscribingPlanId, setSubscribingPlanId] = useState<number | null>(null);
  const [errorMessage, setErrorMessage] = useState('');

  const loadBilling = async (wid: number) => {
    const data = await subscriptionApi.getBilling(wid);
    setBilling(data);
  };

  useEffect(() => {
    const init = async () => {
      setIsLoading(true);
      setErrorMessage('');
      try {
        const [planList, wsList] = await Promise.all([
          subscriptionApi.getPlans(),
          workspaceApi.getMyWorkspaces(),
        ]);
        setPlans(planList);
        setWorkspaces(wsList);

        const paramWid = Number(searchParams.get('workspaceId'));
        const initialWid =
          !Number.isNaN(paramWid) && wsList.some((w) => w.workspaceId === paramWid)
            ? paramWid
            : wsList[0]?.workspaceId ?? null;

        setWorkspaceId(initialWid);
        if (initialWid) await loadBilling(initialWid);
      } catch (error) {
        setErrorMessage(extractApiError(error, 'Không thể tải bảng giá.'));
      } finally {
        setIsLoading(false);
      }
    };
    init();
  }, [searchParams]);

  const handleWorkspaceChange = async (wid: number) => {
    setWorkspaceId(wid);
    try {
      await loadBilling(wid);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải gói workspace.'));
    }
  };

  const handleSubscribe = async (plan: SubscriptionPlan) => {
    if (!workspaceId) {
      await alert({ title: 'Chưa chọn workspace', message: 'Tạo workspace trước khi nâng cấp gói.', type: 'warning' });
      return;
    }

    if (billing?.planId === plan.planId) {
      await alert({ title: 'Đang dùng gói này', message: `Workspace đã ở gói ${plan.name}.`, type: 'info' });
      return;
    }

    const ok = await confirm({
      title: plan.price > 0 ? 'Xác nhận nâng cấp' : 'Chuyển sang gói miễn phí',
      message:
        plan.price > 0
          ? `Áp dụng gói «${plan.name}» (${plan.priceLabel}/tháng) cho workspace «${billing?.workspaceName ?? ''}»? Hệ thống ghi nhận thanh toán demo ngay lập tức.`
          : `Chuyển workspace «${billing?.workspaceName ?? ''}» về gói «${plan.name}»?`,
      confirmText: plan.price > 0 ? 'Nâng cấp' : 'Xác nhận',
    });
    if (!ok) return;

    setSubscribingPlanId(plan.planId);
    setErrorMessage('');
    try {
      const updated = await subscriptionApi.subscribe(workspaceId, plan.planId);
      setBilling(updated);
      await alert({
        title: 'Đã cập nhật gói cước',
        message: `Workspace «${updated.workspaceName}» hiện dùng gói ${updated.planName}.`,
        type: 'success',
      });
      navigate('/subscription');
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể đăng ký gói.'));
    } finally {
      setSubscribingPlanId(null);
    }
  };

  if (isLoading) {
    return (
      <div className="flex flex-col h-full text-white bg-[#050A15] items-center justify-center">
        <Loader2 className="w-8 h-8 animate-spin text-[#FF7575]" />
      </div>
    );
  }

  return (
    <div className="flex flex-col h-full text-white bg-[#050A15] overflow-y-auto">
      <header className="h-20 bg-[#0A101D]/80 backdrop-blur-md border-b border-white/5 flex items-center px-8 sticky top-0 z-20 shrink-0 gap-4">
        <Link
          to="/subscription"
          className="inline-flex items-center gap-2 text-sm font-semibold text-gray-400 hover:text-white transition-colors"
        >
          <ArrowLeft size={16} />
          Quay lại Billing
        </Link>
        <div className="text-gray-300 font-medium tracking-wide">Nâng cấp gói cước</div>
      </header>

      <div className="p-8 md:p-10 max-w-6xl mx-auto w-full">
        <div className="mb-8">
          <h1 className="text-3xl font-bold tracking-tight mb-2">Chọn gói cho workspace</h1>
          <p className="text-gray-400 text-sm max-w-2xl">
            Gói cước áp dụng theo từng workspace — giới hạn dự án, mentions, thành viên và AI credits.
            Thanh toán demo được ghi vào lịch sử ngay sau khi xác nhận.
          </p>
        </div>

        {errorMessage && (
          <div className="mb-6 text-red-300 bg-red-500/10 border border-red-500/30 rounded-xl p-4 text-sm">
            {errorMessage}
          </div>
        )}

        {workspaces.length === 0 ? (
          <div className="bg-[#0A101D] border border-white/10 rounded-2xl p-8 text-center">
            <p className="text-gray-400 mb-4">Bạn chưa có workspace nào.</p>
            <Link to="/create-workspace" className="text-[#FF7575] font-semibold hover:underline">
              Tạo workspace đầu tiên
            </Link>
          </div>
        ) : (
          <>
            <div className="mb-8 flex flex-col sm:flex-row sm:items-end gap-4">
              <label className="flex-1 max-w-md">
                <span className="block text-xs font-bold text-gray-500 uppercase mb-2">Workspace</span>
                <select
                  value={workspaceId ?? ''}
                  onChange={(e) => handleWorkspaceChange(Number(e.target.value))}
                  className="w-full bg-[#151B2B] border border-white/10 rounded-xl px-4 py-3 text-white focus:outline-none focus:border-[#FF7575]"
                >
                  {workspaces.map((ws) => (
                    <option key={ws.workspaceId} value={ws.workspaceId}>
                      {ws.name}
                    </option>
                  ))}
                </select>
              </label>
              {billing && (
                <div className="text-sm text-gray-400">
                  Gói hiện tại:{' '}
                  <span className="text-white font-semibold">{billing.planName}</span>
                  {billing.expiryDate && (
                    <span className="ml-2">— hết hạn {new Date(billing.expiryDate).toLocaleDateString('vi-VN')}</span>
                  )}
                </div>
              )}
            </div>

            <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
              {plans.map((plan) => {
                const isCurrent = billing?.planId === plan.planId;
                const isBusy = subscribingPlanId === plan.planId;

                return (
                  <div
                    key={plan.planId}
                    className={`relative rounded-2xl flex flex-col p-6 border transition-all ${
                      isCurrent
                        ? 'border-emerald-500/50 bg-emerald-500/5'
                        : plan.isPopular
                          ? 'border-[#FF7575]/50 bg-gradient-to-br from-[#1A2235]/80 to-[#0A101D]/80'
                          : 'border-white/10 bg-[#0A101D]'
                    }`}
                  >
                    {isCurrent && (
                      <span className="absolute top-4 right-4 text-xs font-bold text-emerald-400 uppercase">
                        Đang dùng
                      </span>
                    )}
                    {plan.isPopular && !isCurrent && (
                      <span className="absolute top-0 right-4 -translate-y-1/2 bg-[#FF7575] text-white text-xs font-bold px-3 py-1 rounded-full flex items-center gap-1">
                        <Sparkles size={12} /> Khuyên dùng
                      </span>
                    )}

                    <h3 className="text-xl font-bold mb-1">{plan.name}</h3>
                    <p className="text-gray-500 text-sm mb-4 min-h-[40px]">{plan.description}</p>
                    <div className="mb-4 pb-4 border-b border-white/10">
                      <span className="text-3xl font-extrabold">{plan.priceLabel}</span>
                      {plan.price > 0 && <span className="text-gray-500 ml-1">/tháng</span>}
                    </div>

                    <ul className="space-y-2.5 mb-6 flex-1 text-sm text-gray-300">
                      {plan.features.map((f) => (
                        <li key={f} className="flex items-start gap-2">
                          <Check size={16} className="text-[#FF7575] shrink-0 mt-0.5" />
                          {f}
                        </li>
                      ))}
                      <li className="flex items-start gap-2 text-gray-500">
                        <Check size={16} className="text-gray-600 shrink-0 mt-0.5" />
                        {plan.aiCreditLimit.toLocaleString('vi-VN')} AI credits/tháng
                      </li>
                    </ul>

                    <button
                      type="button"
                      disabled={isCurrent || isBusy || subscribingPlanId !== null}
                      onClick={() => handleSubscribe(plan)}
                      className={`w-full py-3 rounded-xl text-sm font-bold transition-colors disabled:opacity-50 ${
                        plan.isPopular && !isCurrent
                          ? 'bg-[#FF7575] hover:bg-[#ff6262] text-white'
                          : 'bg-white/10 hover:bg-white/15 text-white border border-white/10'
                      }`}
                    >
                      {isBusy ? (
                        <span className="inline-flex items-center gap-2 justify-center">
                          <Loader2 size={16} className="animate-spin" /> Đang xử lý...
                        </span>
                      ) : isCurrent ? (
                        'Gói hiện tại'
                      ) : (
                        plan.buttonText || 'Chọn gói này'
                      )}
                    </button>
                  </div>
                );
              })}
            </div>
          </>
        )}
      </div>
    </div>
  );
};

export default SubscriptionUpgrade;
