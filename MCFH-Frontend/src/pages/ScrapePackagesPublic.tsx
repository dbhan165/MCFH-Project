import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { ArrowLeft, Loader2, Check, Package } from 'lucide-react';
import axiosClient from '../api/axiosClient';
import { pickField, pickNullableString, pickNumber, pickString } from '../utils/normalizeApi';

interface PublicPackage {
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

const ScrapePackagesPublic = () => {
  const [packages, setPackages] = useState<PublicPackage[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');

  useEffect(() => {
    const load = async () => {
      setIsLoading(true);
      setErrorMessage('');
      try {
        const res = await axiosClient.get<unknown[]>('/api/packages');
        const items = (res.data ?? []) as Record<string, unknown>[];
        setPackages(
          items.map((p) => ({
            code: pickString(p, 'code', 'Code'),
            name: pickString(p, 'name', 'Name'),
            description: pickNullableString(p, 'description', 'Description'),
            price: Number(pickField(p, 'price', 'Price') ?? 0),
            currency: pickString(p, 'currency', 'Currency') || 'VND',
            durationDays: pickNumber(p, 'durationDays', 'DurationDays'),
            maxItems: pickNumber(p, 'maxItems', 'MaxItems'),
            maxSources: pickField<number>(p, 'maxSources', 'MaxSources') ?? null,
            sortOrder: pickNumber(p, 'sortOrder', 'SortOrder'),
          }))
        );
      } catch (error) {
        setErrorMessage('Không thể tải danh sách gói. Vui lòng thử lại sau.');
      } finally {
        setIsLoading(false);
      }
    };
    load();
  }, []);

  if (isLoading) {
    return (
      <div className="min-h-screen bg-[#0A101D] flex justify-center items-center">
        <div className="flex flex-col items-center">
          <Loader2 className="w-10 h-10 text-[#FF7575] animate-spin mb-4" />
          <p className="text-[#9BA1B0] font-medium">Đang tải gói cào dữ liệu...</p>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-[#0A101D] text-white font-sans py-16 relative overflow-hidden">
      <div
        className="absolute inset-0 z-0 opacity-20 pointer-events-none"
        style={{
          backgroundImage: 'radial-gradient(#ffffff 1px, transparent 1px)',
          backgroundSize: '32px 32px',
        }}
      />

      <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 relative z-10 w-full">
        <div className="absolute top-4 left-4 sm:static sm:mb-8">
          <Link
            to="/"
            className="inline-flex items-center gap-2 text-sm font-medium text-[#9BA1B0] hover:text-white transition-colors bg-white/5 px-4 py-2 rounded-lg hover:bg-white/10 border border-white/10"
          >
            <ArrowLeft className="w-4 h-4" />
            Trở về Trang chủ
          </Link>
        </div>

        <div className="text-center max-w-3xl mx-auto mb-16">
          <h2 className="text-sm font-bold text-[#FF7575] tracking-widest uppercase mb-3 flex items-center justify-center gap-2">
            <span className="w-1.5 h-1.5 rounded-full bg-[#FF7575]"></span>
            Gói cào dữ liệu
            <span className="w-1.5 h-1.5 rounded-full bg-[#FF7575]"></span>
          </h2>
          <p className="mt-2 text-4xl font-extrabold text-white sm:text-5xl tracking-tight">
            Mua thêm gói mentions
          </p>
          <p className="mt-4 text-lg text-[#9BA1B0]">
            Chọn gói phù hợp với quy mô dự án. Thanh toán 1 lần, dùng đến khi hết hạn.
          </p>
        </div>

        {errorMessage && (
          <div className="mb-4 text-sm text-red-300 bg-red-500/10 border border-red-500/30 rounded-lg px-4 py-3 max-w-3xl mx-auto">
            {errorMessage}
          </div>
        )}

        {packages.length === 0 ? (
          <div className="text-center text-[#9BA1B0] py-12">
            Hiện chưa có gói nào được mở bán.
          </div>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-6">
            {packages.map((pkg, idx) => (
              <div
                key={pkg.code}
                className={`relative rounded-2xl flex flex-col p-6 transition-all duration-300 hover:-translate-y-2
                  bg-gradient-to-br from-[#1A2235]/80 to-[#0A101D]/80 backdrop-blur-md
                  ${idx === packages.length - 1
                    ? 'border border-[#FF7575]/60 shadow-[0_0_30px_rgba(255,117,117,0.15)] ring-1 ring-[#FF7575]/30'
                    : 'border border-white/10 hover:border-white/30 hover:shadow-2xl hover:shadow-white/5'
                  }`}
              >
                {idx === packages.length - 1 && (
                  <div className="absolute top-0 right-4 transform -translate-y-1/2">
                    <span className="bg-[#FF7575] text-white text-xs font-bold uppercase tracking-wider py-1 px-3 rounded-full shadow-lg shadow-[#FF7575]/30">
                      Khuyên dùng
                    </span>
                  </div>
                )}

                <div className="flex items-center gap-2 mb-3">
                  <Package className="w-5 h-5 text-[#FF7575]" />
                  <h3 className="text-xl font-bold text-white">{pkg.name}</h3>
                </div>
                <p className="text-[#9BA1B0] text-sm min-h-10 leading-relaxed mb-4">
                  {pkg.description || '—'}
                </p>

                <div className="mb-4 pb-4 border-b border-white/10">
                  <span className="text-3xl font-extrabold text-white">
                    {formatPrice(pkg.price, pkg.currency)}
                  </span>
                </div>

                <ul className="mb-6 space-y-2 flex-1 text-sm">
                  <li className="flex items-start">
                    <Check className="shrink-0 h-4 w-4 text-[#FF7575] mt-0.5" />
                    <span className="ml-2 text-gray-300">
                      <strong className="text-white">{pkg.maxItems.toLocaleString('vi-VN')}</strong> mentions
                    </span>
                  </li>
                  <li className="flex items-start">
                    <Check className="shrink-0 h-4 w-4 text-[#FF7575] mt-0.5" />
                    <span className="ml-2 text-gray-300">
                      Thời hạn <strong className="text-white">{pkg.durationDays}</strong> ngày
                    </span>
                  </li>
                  {pkg.maxSources != null && (
                    <li className="flex items-start">
                      <Check className="shrink-0 h-4 w-4 text-[#FF7575] mt-0.5" />
                      <span className="ml-2 text-gray-300">
                        Tối đa <strong className="text-white">{pkg.maxSources}</strong> nguồn
                      </span>
                    </li>
                  )}
                  {pkg.maxItems >= 9000 && (
                    <li className="flex items-start">
                      <Check className="shrink-0 h-4 w-4 text-[#FF7575] mt-0.5" />
                      <span className="ml-2 text-gray-300">Không giới hạn mentions thực tế</span>
                    </li>
                  )}
                </ul>

                <Link
                  to="/login"
                  className={`mt-auto w-full py-3 rounded-lg text-sm font-bold tracking-wide text-center transition-all duration-200 ${
                    idx === packages.length - 1
                      ? 'bg-[#FF7575] text-white hover:bg-[#ff6262] shadow-[0_4px_14px_0_rgba(255,117,117,0.39)]'
                      : 'bg-white/5 text-white hover:bg-white/10 border border-white/10'
                  }`}
                >
                  Mua gói này
                </Link>
              </div>
            ))}
          </div>
        )}
      </div>
    </div>
  );
};

export default ScrapePackagesPublic;
