import { Package, Check, Sparkles, Globe, Layers, Calendar } from 'lucide-react';

export interface ScrapePackageOption {
  code: string;
  name: string;
  description: string | null;
  price: number;
  currency: string;
  durationDays: number;
  maxItems: number;
  maxSources: number | null;
  sortOrder: number;
}

const formatPrice = (price: number, currency: string) => {
  try {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: currency || 'VND' }).format(price);
  } catch {
    return `${price.toLocaleString('vi-VN')} ${currency || 'VND'}`;
  }
};

interface PackageStepProps {
  packages: ScrapePackageOption[];
  isLoading: boolean;
  errorMessage: string;
  selectedPackageCode: string;
  setSelectedPackageCode: (value: string) => void;
}

const PackageStep = ({
  packages,
  isLoading,
  errorMessage,
  selectedPackageCode,
  setSelectedPackageCode,
}: PackageStepProps) => {
  if (isLoading) {
    return (
      <div className="animate-in fade-in slide-in-from-right-4 duration-500 space-y-6">
        <div>
          <h2 className="text-xl font-bold mb-2">Chọn gói cào dữ liệu</h2>
          <p className="text-gray-400 text-sm mb-2">
            Mỗi gói có số lượng mentions tối đa, giới hạn nguồn và thời hạn sử dụng khác nhau.
          </p>
        </div>
        <div className="flex items-center justify-center py-16 text-gray-400 gap-3">
          <span className="w-5 h-5 rounded-full border-2 border-[#FF7575] border-t-transparent animate-spin" />
          <span className="text-sm">Đang tải danh sách gói...</span>
        </div>
      </div>
    );
  }

  if (errorMessage) {
    return (
      <div className="animate-in fade-in slide-in-from-right-4 duration-500 space-y-6">
        <div>
          <h2 className="text-xl font-bold mb-2">Chọn gói cào dữ liệu</h2>
        </div>
        <div className="text-sm text-red-300 bg-red-500/10 border border-red-500/30 rounded-lg px-4 py-3">
          {errorMessage}
        </div>
      </div>
    );
  }

  return (
    <div className="animate-in fade-in slide-in-from-right-4 duration-500 space-y-6">
      <div>
        <h2 className="text-xl font-bold mb-2">Chọn gói cào dữ liệu</h2>
        <p className="text-gray-400 text-sm mb-2">
          Mỗi gói có số lượng mentions tối đa, giới hạn nguồn và thời hạn sử dụng khác nhau.
        </p>
        <p className="text-xs text-gray-500">
          Thanh toán 1 lần, dùng đến khi hết hạn hoặc hết mentions.
        </p>
      </div>

      {packages.length === 0 ? (
        <div className="text-center text-gray-400 text-sm py-10">
          Hiện chưa có gói nào được mở bán.
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          {packages.map((pkg, idx) => {
            const isSelected = pkg.code === selectedPackageCode;
            const isFull = pkg.code.toUpperCase().startsWith('FULL');
            const isHighlighted = idx === packages.length - 1 || isFull;
            const mentionsLabel = isFull ? '∞' : pkg.maxItems.toLocaleString('vi-VN');
            const sourcesLabel = pkg.maxSources == null ? '∞' : pkg.maxSources.toString();
            return (
              <button
                key={pkg.code}
                type="button"
                onClick={() => setSelectedPackageCode(pkg.code)}
                className={`relative text-left rounded-2xl border-2 transition-all overflow-hidden ${
                  isSelected
                    ? 'border-[#FF7575] bg-gradient-to-br from-[#FF7575]/10 to-[#FF7575]/[0.02] shadow-[0_8px_30px_rgba(255,117,117,0.18)]'
                    : isHighlighted
                      ? 'border-[#FF7575]/30 bg-[#151B2B] hover:border-[#FF7575]/60'
                      : 'border-white/10 bg-[#151B2B] hover:border-white/25'
                }`}
              >
                {isHighlighted && (
                  <span className="absolute top-0 right-0 bg-[#FF7575] text-white text-[10px] font-bold uppercase tracking-wider py-1 px-3 rounded-bl-xl shadow-lg shadow-[#FF7575]/30">
                    {isFull ? 'Full' : 'Khuyên dùng'}
                  </span>
                )}

                <div className="px-5 pt-5 pb-3 flex items-center gap-2">
                  <Package className={`w-5 h-5 shrink-0 ${isSelected ? 'text-[#FF7575]' : 'text-gray-400'}`} />
                  <h3 className="font-bold text-white text-base truncate">{pkg.name}</h3>
                </div>

                <div className="px-5 pb-5">
                  <p className="text-3xl font-extrabold text-white tracking-tight">
                    {formatPrice(pkg.price, pkg.currency)}
                  </p>
                  <p className="text-[11px] text-gray-500 uppercase tracking-wide mt-1">Giá trọn gói</p>
                </div>

                <div className="px-5 pb-5 grid grid-cols-3 gap-2 border-t border-white/5 pt-4">
                  <div className="text-center">
                    <p className="text-lg font-extrabold text-[#FF7575] tabular-nums leading-none">
                      {mentionsLabel}
                    </p>
                    <p className="text-[10px] text-gray-500 uppercase tracking-wide mt-1">Mentions</p>
                  </div>
                  <div className="text-center border-x border-white/5">
                    <p className="text-lg font-extrabold text-[#00B4D8] tabular-nums leading-none">
                      {sourcesLabel}
                    </p>
                    <p className="text-[10px] text-gray-500 uppercase tracking-wide mt-1">Nguồn</p>
                  </div>
                  <div className="text-center">
                    <p className="text-lg font-extrabold text-emerald-400 tabular-nums leading-none">
                      {pkg.durationDays}
                    </p>
                    <p className="text-[10px] text-gray-500 uppercase tracking-wide mt-1">Ngày</p>
                  </div>
                </div>

                {pkg.description && (
                  <div className="px-5 pb-4">
                    <p className="text-xs text-gray-400 leading-relaxed line-clamp-2">
                      {pkg.description}
                    </p>
                  </div>
                )}

                <div
                  className={`px-5 py-3 flex items-center justify-between text-xs font-semibold ${
                    isSelected
                      ? 'bg-[#FF7575]/10 text-[#FF7575]'
                      : 'bg-white/[0.03] text-gray-500'
                  }`}
                >
                  <span className="inline-flex items-center gap-1.5">
                    <Sparkles className="w-3.5 h-3.5" />
                    {isSelected ? 'Đã chọn gói này' : 'Nhấn để chọn'}
                  </span>
                  {isSelected ? (
                    <div className="w-5 h-5 rounded-full bg-[#FF7575] flex items-center justify-center">
                      <Check className="w-3 h-3 text-white" strokeWidth={3} />
                    </div>
                  ) : (
                    <div className="flex items-center gap-1 opacity-60">
                      <Layers className="w-3 h-3" />
                      <Globe className="w-3 h-3" />
                      <Calendar className="w-3 h-3" />
                    </div>
                  )}
                </div>
              </button>
            );
          })}
        </div>
      )}
    </div>
  );
};

export default PackageStep;