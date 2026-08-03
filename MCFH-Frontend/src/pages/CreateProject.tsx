import { useEffect, useMemo, useState } from 'react';
import {
  Loader2, AlertCircle, CreditCard, Clock, Check,
} from 'lucide-react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import axiosClient from '../api/axiosClient';
import PackageStep, { type ScrapePackageOption } from './PackageStep';
import SourcesStep from './SourcesStep';
import KeywordStep from './KeywordStep';
import { projectApi } from '../api/projectApi';
import { scrapeOrderApi, type ScrapeQuote } from '../api/scrapeOrderApi';
import { extractApiError } from '../utils/authStorage';
import { pickField, pickNullableString, pickNumber, pickString } from '../utils/normalizeApi';
import {
  buildDataSources,
  getPrimaryKeyword,
  SCRAPABLE_PLATFORMS,
} from '../utils/onboardingHelpers';

type LaunchPhase = 'idle' | 'creating' | 'paying' | 'done' | 'error';

const STEP_LABELS = ['THÔNG TIN', 'GÓI CÀO', 'NGUỒN DỮ LIỆU', 'TỪ KHOÁ & BÁO GIÁ'];

const CreateProject = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const workspaceId = searchParams.get('wid') || '';
  const isOnboarding = searchParams.get('onboarding') === '1';

  const [currentStep, setCurrentStep] = useState(1);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [launchPhase, setLaunchPhase] = useState<LaunchPhase>('idle');
  const [errorMessage, setErrorMessage] = useState('');
  const [quote, setQuote] = useState<ScrapeQuote | null>(null);
  const [quoteLoading, setQuoteLoading] = useState(false);
  const [packages, setPackages] = useState<ScrapePackageOption[]>([]);
  const [campaignName, setCampaignName] = useState('');
  const [selectedSources, setSelectedSources] = useState<string[]>([]);
  const [keywords, setKeywords] = useState('');
  const [selectedPackageCode, setSelectedPackageCode] = useState('');

  // Load package catalog 1 lần ở parent — share cho cả PackageStep (UI) và SourcesStep (maxSources).
  useEffect(() => {
    let cancelled = false;
    axiosClient
      .get<unknown[]>('/api/packages')
      .then((res) => {
        if (cancelled) return;
        const items = (res.data ?? []) as Record<string, unknown>[];
        const parsed: ScrapePackageOption[] = items
          .map((p) => ({
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
          .filter((p) => p.code)
          .sort((a, b) => a.sortOrder - b.sortOrder);
        setPackages(parsed);
        setSelectedPackageCode((current) => {
          if (current && parsed.some((p) => p.code === current)) return current;
          return parsed[0]?.code ?? '';
        });
      })
      .catch(() => {
        /* PackageStep sẽ hiển thị empty state nếu không tải được */
      });
    return () => {
      cancelled = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  const selectedPackage = useMemo(
    () => packages.find((p) => p.code === selectedPackageCode),
    [packages, selectedPackageCode]
  );
  const maxSources = selectedPackage?.maxSources ?? null;

  const toggleSource = (sourceId: string) => {
    setSelectedSources((prev) => {
      if (prev.includes(sourceId)) return prev.filter((id) => id !== sourceId);
      if (maxSources != null && prev.length >= maxSources) return prev;
      return [...prev, sourceId];
    });
  };

  const hasScrapableSource = selectedSources.some((source) =>
    SCRAPABLE_PLATFORMS.includes(source as (typeof SCRAPABLE_PLATFORMS)[number])
  );

  const isStep1Valid = campaignName.trim() !== '';
  const isStep2Valid = !!selectedPackageCode;
  const isStep3Valid =
    hasScrapableSource &&
    (maxSources == null || selectedSources.length <= maxSources);
  const isStep4Valid = !!getPrimaryKeyword(keywords);

  const stepIsValid = [isStep1Valid, isStep2Valid, isStep3Valid, isStep4Valid];

  const handleNext = () => {
    setErrorMessage('');
    if (currentStep === 2 && !selectedPackageCode) {
      setErrorMessage('Vui lòng chọn gói cào dữ liệu.');
      return;
    }
    if (currentStep === 3) {
      if (!hasScrapableSource) {
        setErrorMessage('Vui lòng chọn ít nhất một nguồn: Facebook, YouTube hoặc Tin tức.');
        return;
      }
      if (maxSources != null && selectedSources.length > maxSources) {
        setErrorMessage(
          `Gói đã chọn chỉ cho phép tối đa ${maxSources} nguồn. Hiện tại bạn đã chọn ${selectedSources.length}.`
        );
        return;
      }
    }
    if (currentStep === 4 && !getPrimaryKeyword(keywords)) {
      setErrorMessage('Vui lòng nhập ít nhất một từ khoá.');
      return;
    }
    if (currentStep < 4) setCurrentStep(currentStep + 1);
  };

  useEffect(() => {
    if (currentStep !== 4) return;
    if (!selectedPackageCode) return;
    setQuoteLoading(true);
    scrapeOrderApi
      .getQuote(selectedPackageCode)
      .then(setQuote)
      .catch(() => setQuote(null))
      .finally(() => setQuoteLoading(false));
  }, [currentStep, selectedPackageCode]);

  // Khi user đổi gói ở step 2/3: nếu số nguồn đã chọn vượt maxSources của gói mới → trim về top N.
  // Nếu chưa chọn gì và vừa vào step 3 → auto-suggest theo maxSources.
  useEffect(() => {
    if (currentStep !== 3) return;
    if (maxSources == null) return;
    if (selectedSources.length > maxSources) {
      setSelectedSources(selectedSources.slice(0, maxSources));
    } else if (selectedSources.length === 0) {
      const order = ['facebook', 'youtube', 'news'];
      setSelectedSources(order.slice(0, Math.min(maxSources, order.length)));
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [currentStep, maxSources]);

  const handleBack = () => {
    setErrorMessage('');
    if (currentStep > 1) setCurrentStep(currentStep - 1);
  };

  const handleSubmit = async () => {
    const wid = Number(workspaceId);
    const primaryKeyword = getPrimaryKeyword(keywords);

    if (!wid || Number.isNaN(wid)) {
      setErrorMessage('Workspace không hợp lệ.');
      return;
    }
    if (!primaryKeyword) {
      setErrorMessage('Vui lòng nhập từ khoá.');
      return;
    }
    if (!selectedPackageCode) {
      setErrorMessage('Vui lòng chọn gói cào dữ liệu.');
      return;
    }
    if (!hasScrapableSource) {
      setErrorMessage('Vui lòng chọn ít nhất một nguồn để cào dữ liệu.');
      return;
    }
    if (maxSources != null && selectedSources.length > maxSources) {
      setErrorMessage(
        `Gói đã chọn chỉ cho phép tối đa ${maxSources} nguồn. Hiện tại bạn đã chọn ${selectedSources.length}.`
      );
      return;
    }

    setIsSubmitting(true);
    setErrorMessage('');
    setLaunchPhase('creating');

    try {
      const project = await projectApi.create(wid, {
        name: campaignName.trim(),
        searchQuery: primaryKeyword,
        enableFacebook: selectedSources.includes('facebook'),
        enableYoutube: selectedSources.includes('youtube'),
        enableTiktok: selectedSources.includes('tiktok'),
        enableMaps: selectedSources.includes('maps'),
        dataSources: buildDataSources(selectedSources),
      });

      const order = await scrapeOrderApi.create({
        workspaceId: wid,
        projectId: project.projectId,
        keyword: primaryKeyword,
        mentionsPackage: selectedPackageCode,
      });

      setLaunchPhase('paying');
      const checkout = await scrapeOrderApi.pay(order.orderId);
      setLaunchPhase('done');

      // Đơn đã thanh toán từ trước (link cũ đã trả tiền) → vào thẳng trang theo dõi.
      const paidStatuses = ['paid', 'scraping', 'analyzing', 'completed'];
      if (paidStatuses.includes(checkout.order.status) || !checkout.checkoutUrl) {
        navigate(`/workspace/${wid}/orders/${checkout.order.orderId}`);
        return;
      }

      // Redirect sang cổng thanh toán PayOS — PayOS sẽ đưa người dùng về /payment/return.
      window.location.href = checkout.checkoutUrl;
    } catch (error) {
      setLaunchPhase('error');
      const message = extractApiError(error, 'Không thể hoàn tất thanh toán. Vui lòng thử lại.');
      setErrorMessage(message);
    } finally {
      setIsSubmitting(false);
    }
  };

  const phaseLabel: Record<LaunchPhase, string> = {
    idle: '',
    creating: 'Đang tạo dự án...',
    paying: 'Đang tạo liên kết thanh toán PayOS...',
    done: 'Đang chuyển đến cổng thanh toán PayOS...',
    error: 'Có lỗi xảy ra',
  };

  // Nút Tiếp tục ở mỗi step yêu cầu valid trước khi cho nhảy.
  const canAdvance = !isSubmitting && stepIsValid[currentStep - 1];

  return (
    <div className="min-h-screen bg-[#050A15] text-white font-sans flex items-center justify-center p-6 selection:bg-[#FF7575] selection:text-white relative overflow-hidden">
      <div
        className="absolute inset-0 z-0 opacity-10 pointer-events-none"
        style={{ backgroundImage: 'radial-gradient(#ffffff 1px, transparent 1px)', backgroundSize: '32px 32px' }}
      />

      <div className="w-full max-w-5xl bg-[#0A101D] border border-white/5 rounded-2xl p-8 md:p-12 relative z-10 shadow-2xl">
        <div className="mb-10">
          <h1 className="text-3xl md:text-4xl font-bold tracking-tight">
            {isOnboarding ? 'Bước 2: Tạo Dự án Giám sát' : 'Khởi tạo Dự án Giám sát'}
          </h1>
          {isOnboarding && (
            <p className="text-gray-400 mt-2 text-sm">
              Chọn gói, nguồn và từ khoá — hệ thống sẽ tự động cào dữ liệu và phân tích bằng AI.
            </p>
          )}
        </div>

        {errorMessage && (
          <div className="mb-6 bg-red-500/10 border border-red-500/20 text-red-300 p-4 rounded-xl flex items-center gap-3 text-sm">
            <AlertCircle className="w-5 h-5 shrink-0" />
            {errorMessage}
          </div>
        )}

        <div className="flex items-center justify-between mb-12 relative">
          <div className="absolute top-1/2 left-0 w-full h-px bg-white/10 -z-10 -translate-y-1/2" />
          {STEP_LABELS.map((label, index) => {
            const step = index + 1;
            return (
              <div key={label} className={`flex items-center gap-3 bg-[#0A101D] ${index === 0 ? 'pr-4' : index === STEP_LABELS.length - 1 ? 'pl-4' : 'px-4'}`}>
                <div className={`w-10 h-10 rounded-full flex items-center justify-center font-bold transition-colors ${
                  currentStep >= step ? 'bg-[#FF7575]/10 border-2 border-[#FF7575] text-[#FF7575]' : 'bg-white/5 border-2 border-white/10 text-gray-500'
                }`}>
                  {currentStep > step ? <Check className="w-5 h-5" /> : step}
                </div>
                <span className={`font-bold tracking-wide text-xs sm:text-sm hidden sm:block ${currentStep >= step ? 'text-[#FF7575]' : 'text-gray-500'}`}>
                  {label}
                </span>
              </div>
            );
          })}
        </div>

        <div className="space-y-8 min-h-[350px]">
          {currentStep === 1 && (
            <div className="animate-in fade-in slide-in-from-right-4 duration-500">
              <h2 className="text-xl font-bold mb-2">Thông tin Dự án</h2>
              <p className="text-gray-400 text-sm mb-8">Đặt tên chiến dịch giám sát của bạn.</p>
              <div className="bg-[#151B2B] border border-white/5 rounded-xl p-6">
                <label className="block text-sm font-bold text-white mb-2">Tên Dự án</label>
                <input
                  type="text"
                  value={campaignName}
                  onChange={(e) => setCampaignName(e.target.value)}
                  placeholder="VD: Giám sát phản hồi PetCareHub..."
                  className="w-full px-4 py-3 bg-[#0A101D] border border-white/10 text-white placeholder-gray-600 rounded-lg focus:outline-none focus:border-[#FF7575]"
                  autoFocus
                />
              </div>
            </div>
          )}

          {currentStep === 2 && (
            <PackageStep
              packages={packages}
              isLoading={false}
              errorMessage=""
              selectedPackageCode={selectedPackageCode}
              setSelectedPackageCode={setSelectedPackageCode}
            />
          )}

          {currentStep === 3 && (
            <SourcesStep
              selectedSources={selectedSources}
              setSelectedSources={setSelectedSources}
              maxSources={maxSources}
            />
          )}

          {currentStep === 4 && (
            <div className="animate-in fade-in slide-in-from-right-4 duration-500 space-y-6">
              <KeywordStep keywords={keywords} setKeywords={setKeywords} />

              <div className="bg-[#151B2B] border border-white/10 rounded-xl p-6">
                <div className="flex items-center gap-2 mb-4">
                  <CreditCard className="w-5 h-5 text-[#FF7575]" />
                  <h3 className="font-bold">Báo giá dự kiến</h3>
                </div>

                {launchPhase === 'idle' ? (
                  quoteLoading ? (
                    <div className="flex justify-center py-8">
                      <Loader2 className="w-8 h-8 animate-spin text-[#FF7575]" />
                    </div>
                  ) : quote ? (
                    <div className="space-y-4">
                      <div className="flex justify-between items-center">
                        <span className="text-gray-400">Dự án</span>
                        <span className="font-semibold text-right">{campaignName.trim()}</span>
                      </div>
                      <div className="flex justify-between items-center">
                        <span className="text-gray-400">Gói cào</span>
                        <span className="font-semibold text-right">
                          {quote.packageLabel}
                          <span className="block text-xs text-gray-500 font-mono mt-0.5">
                            {quote.mentionsPackage}
                          </span>
                        </span>
                      </div>
                      <div className="flex justify-between items-center">
                        <span className="text-gray-400">Số nguồn giám sát</span>
                        <span className="font-semibold text-white">{selectedSources.length}</span>
                      </div>
                      <div className="flex justify-between items-center">
                        <span className="text-gray-400">Số mentions tối đa</span>
                        <span className="font-semibold text-white">
                          {quote.projectHasFullUnlimited
                            ? 'Không giới hạn'
                            : quote.mentionsIncluded.toLocaleString('vi-VN')}
                        </span>
                      </div>
                      <div className="flex justify-between items-center pt-3 border-t border-white/5">
                        <span className="text-gray-400">Phí cào dữ liệu</span>
                        <span className="text-2xl font-bold text-[#FF7575]">{quote.priceLabel}</span>
                      </div>
                      <div className="flex items-start gap-2 text-sm text-gray-400 pt-2">
                        <Clock className="w-4 h-4 text-[#00B4D8] shrink-0 mt-0.5" />
                        <span>
                          Sau thanh toán, báo cáo dự kiến sẵn sàng trong{' '}
                          <strong className="text-white">{quote.estimatedDeliveryLabel}</strong>.
                          Bạn có thể rời trang và theo dõi % tiến độ bất cứ lúc nào.
                        </span>
                      </div>
                    </div>
                  ) : (
                    <p className="text-center text-red-400 text-sm">
                      Không tải được báo giá. Vui lòng quay lại bước chọn gói.
                    </p>
                  )
                ) : (
                  <div className="text-center space-y-4 py-6">
                    {launchPhase === 'error' ? (
                      <AlertCircle className="w-10 h-10 mx-auto text-red-400" />
                    ) : (
                      <Loader2
                        className={`w-10 h-10 mx-auto animate-spin ${
                          launchPhase === 'done' ? 'text-emerald-400' : 'text-[#FF7575]'
                        }`}
                      />
                    )}
                    <p className="text-sm font-semibold">{phaseLabel[launchPhase]}</p>
                  </div>
                )}

                {launchPhase === 'idle' && quote && (
                  <p className="text-xs text-center text-gray-500 mt-4">
                    Thanh toán an toàn qua PayOS — quét mã VietQR hoặc chuyển khoản ngân hàng.
                    Dữ liệu chỉ được cào sau khi thanh toán thành công.
                  </p>
                )}
              </div>
            </div>
          )}
        </div>

        <div className="pt-8 flex justify-between items-center border-t border-white/5 mt-8">
          <div>
            {currentStep === 1 ? (
              <Link
                to={isOnboarding ? '/create-workspace?onboarding=1' : `/workspace/${workspaceId}/projects`}
                className="px-6 py-3 rounded-lg text-sm font-semibold text-gray-400 hover:bg-white/5"
              >
                {isOnboarding ? 'Quay lại' : 'Hủy bỏ'}
              </Link>
            ) : (
              <button onClick={handleBack} disabled={isSubmitting} className="px-6 py-3 rounded-lg text-sm font-semibold text-gray-400 hover:bg-white/5 disabled:opacity-50">
                Quay lại
              </button>
            )}
          </div>

          <div>
            {currentStep < 4 ? (
              <button
                onClick={handleNext}
                disabled={!canAdvance}
                className="bg-[#FF7575] hover:bg-[#ff6262] text-white px-8 py-3 rounded-lg text-sm font-semibold disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Tiếp tục
              </button>
            ) : (
              <button
                onClick={handleSubmit}
                disabled={isSubmitting || launchPhase === 'done' || quoteLoading || !quote}
                className="bg-blue-600 hover:bg-blue-500 text-white px-8 py-3 rounded-lg text-sm font-bold disabled:opacity-70 flex items-center gap-2"
              >
                {isSubmitting ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin" />
                    Đang xử lý...
                  </>
                ) : (
                  <>
                    <CreditCard className="w-4 h-4" />
                    Thanh toán & Bắt đầu cào
                  </>
                )}
              </button>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

export default CreateProject;