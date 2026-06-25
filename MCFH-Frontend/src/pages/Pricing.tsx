import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { ArrowLeft, Loader2 } from 'lucide-react';
import { subscriptionApi, type SubscriptionPlan } from '../api/subscriptionApi';
import McfhLogo from '../components/brand/McfhLogo';

function isLoggedIn() {
  return Boolean(localStorage.getItem('accessToken'));
}

const Pricing = () => {
  const navigate = useNavigate();
  const [plans, setPlans] = useState<SubscriptionPlan[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const loggedIn = isLoggedIn();

  useEffect(() => {
    subscriptionApi
      .getPlans()
      .then(setPlans)
      .catch(() => setPlans([]))
      .finally(() => setIsLoading(false));
  }, []);

  const handleBack = () => {
    if (loggedIn) {
      navigate('/subscription');
    } else {
      navigate('/');
    }
  };

  const ctaForPlan = (plan: SubscriptionPlan) => {
    if (loggedIn) {
      return {
        to: '/subscription/upgrade',
        label: plan.buttonText || 'Chọn gói',
      };
    }
    return {
      to: '/login',
      label: plan.price <= 0 ? 'Đăng ký miễn phí' : 'Đăng nhập để nâng cấp',
    };
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-[#0A101D] flex justify-center items-center">
        <Loader2 className="w-10 h-10 animate-spin text-[#FF7575]" />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-[#0A101D] text-white font-sans py-16 relative overflow-hidden flex items-center">
      <div
        className="absolute inset-0 z-0 opacity-20 pointer-events-none"
        style={{
          backgroundImage: 'radial-gradient(#ffffff 1px, transparent 1px)',
          backgroundSize: '32px 32px',
        }}
      />

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 relative z-10 w-full">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-6 mb-10">
          <McfhLogo linkTo={loggedIn ? '/workspaces' : '/'} size={36} textClassName="text-white text-xl" />
          <button
            type="button"
            onClick={handleBack}
            className="inline-flex items-center gap-2 text-sm font-semibold text-[#9BA1B0] hover:text-white transition-colors"
          >
            <ArrowLeft size={16} />
            {loggedIn ? 'Quay lại Billing' : 'Quay lại trang chủ'}
          </button>
        </div>

        <div className="text-center max-w-3xl mx-auto mb-16">
          <h2 className="text-sm font-bold text-[#FF7575] tracking-widest uppercase mb-3">Bảng giá dịch vụ</h2>
          <p className="mt-2 text-4xl font-extrabold sm:text-5xl tracking-tight">Chọn gói giải pháp phù hợp</p>
          <p className="mt-4 text-lg text-[#9BA1B0]">
            Nâng cấp hệ thống lắng nghe mạng xã hội MCFH — cào FB, YouTube, TikTok và phân tích AI theo workspace.
          </p>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
          {plans.map((plan) => {
            const cta = ctaForPlan(plan);
            return (
              <div
                key={plan.planId}
                className={`relative rounded-2xl flex flex-col p-8 transition-all duration-300 hover:-translate-y-2 bg-linear-to-br from-[#1A2235]/80 to-[#0A101D]/80 backdrop-blur-md ${
                  plan.isPopular
                    ? 'border border-[#FF7575]/60 shadow-[0_0_30px_rgba(255,117,117,0.15)]'
                    : 'border border-white/10 hover:border-white/30'
                }`}
              >
                {plan.isPopular && (
                  <div className="absolute top-0 right-6 transform -translate-y-1/2">
                    <span className="bg-[#FF7575] text-white text-xs font-bold uppercase py-1.5 px-4 rounded-full">
                      Khuyên dùng
                    </span>
                  </div>
                )}

                <div className="mb-6">
                  <h3 className="text-2xl font-bold">{plan.name}</h3>
                  <p className="mt-3 text-[#9BA1B0] min-h-12 text-sm">{plan.description}</p>
                </div>

                <div className="mb-6 pb-6 border-b border-white/10">
                  <span className="text-4xl font-extrabold">{plan.priceLabel}</span>
                  {plan.price > 0 && <span className="text-lg text-[#9BA1B0] ml-1">/tháng</span>}
                </div>

                <ul className="mb-8 space-y-4 flex-1">
                  {plan.features.map((feature) => (
                    <li key={feature} className="flex items-start text-sm text-gray-300">
                      <span className="text-[#FF7575] mr-2">✓</span>
                      {feature}
                    </li>
                  ))}
                </ul>

                <Link
                  to={cta.to}
                  className={`mt-auto w-full py-3.5 px-4 rounded-lg text-sm font-bold text-center transition-all ${
                    plan.isPopular
                      ? 'bg-[#FF7575] text-white hover:bg-[#ff6262]'
                      : 'bg-white/5 text-white hover:bg-white/10 border border-white/10'
                  }`}
                >
                  {cta.label}
                </Link>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};

export default Pricing;
