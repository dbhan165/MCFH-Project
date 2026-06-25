import { useState } from 'react';
import {
  Users, Hash, Map, FileUp, Link as LinkIcon, Check, MonitorPlay, Globe,
  Loader2, AlertCircle, Sparkles, Database,
} from 'lucide-react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import KeywordStep from './KeywordStep';
import { projectApi, scrapeApi } from '../api/projectApi';
import { extractApiError } from '../utils/authStorage';
import {
  buildDataSources,
  getPrimaryKeyword,
  SCRAPABLE_PLATFORMS,
} from '../utils/onboardingHelpers';
import ScrapeProgressModal from '../components/scraping/ScrapeProgressModal';
import type { ScrapeJobStatus } from '../types/project';

type LaunchPhase = 'idle' | 'creating' | 'scraping' | 'analyzing' | 'done' | 'error';

const CreateProject = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const workspaceId = searchParams.get('wid') || '';
  const isOnboarding = searchParams.get('onboarding') === '1';

  const [currentStep, setCurrentStep] = useState(1);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [launchPhase, setLaunchPhase] = useState<LaunchPhase>('idle');
  const [errorMessage, setErrorMessage] = useState('');
  const [launchSummary, setLaunchSummary] = useState('');
  const [scrapeJob, setScrapeJob] = useState<ScrapeJobStatus | null>(null);
  const [scrapeJobId, setScrapeJobId] = useState<string | null>(null);
  const [isCancellingScrape, setIsCancellingScrape] = useState(false);

  const [campaignName, setCampaignName] = useState('');
  const [selectedSources, setSelectedSources] = useState<string[]>(['facebook', 'youtube']);
  const [targetUrl, setTargetUrl] = useState('');
  const [keywords, setKeywords] = useState('');

  const toggleSource = (sourceId: string) => {
    if (selectedSources.includes(sourceId)) {
      setSelectedSources(selectedSources.filter((id) => id !== sourceId));
    } else {
      setSelectedSources([...selectedSources, sourceId]);
    }
  };

  const hasScrapableSource = selectedSources.some((source) =>
    SCRAPABLE_PLATFORMS.includes(source as (typeof SCRAPABLE_PLATFORMS)[number])
  );

  const handleNext = () => {
    setErrorMessage('');
    if (currentStep === 2 && !hasScrapableSource) {
      setErrorMessage('Vui lòng chọn ít nhất một nền tảng: Facebook, YouTube hoặc TikTok.');
      return;
    }
    if (currentStep === 3 && !getPrimaryKeyword(keywords)) {
      setErrorMessage('Vui lòng nhập ít nhất một từ khóa.');
      return;
    }
    if (currentStep < 4) setCurrentStep(currentStep + 1);
  };

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
      setErrorMessage('Vui lòng nhập từ khóa.');
      return;
    }
    if (!hasScrapableSource) {
      setErrorMessage('Vui lòng chọn ít nhất một nền tảng để cào dữ liệu.');
      return;
    }

    setIsSubmitting(true);
    setErrorMessage('');
    setLaunchPhase('creating');

    let createdProjectId: number | null = null;

    try {
      const project = await projectApi.create(wid, {
        name: campaignName.trim(),
        searchQuery: primaryKeyword,
        enableFacebook: selectedSources.includes('facebook'),
        enableYoutube: selectedSources.includes('youtube'),
        enableTiktok: selectedSources.includes('tiktok'),
        enableMaps: selectedSources.includes('maps'),
        dataSources: buildDataSources(selectedSources, targetUrl),
      });
      createdProjectId = project.projectId;

      setLaunchPhase('scraping');
      setScrapeJob(null);
      setScrapeJobId(null);
      setIsCancellingScrape(false);
      const scrapeResult = await scrapeApi.byKeyword(project.projectId, (job) => {
        setScrapeJob(job);
        if (job.jobId) setScrapeJobId(job.jobId);
        if (job.phase === 'cancelling') setIsCancellingScrape(true);
      });

      if (scrapeResult.cancelled) {
        const scrapedCount =
          (scrapeResult.facebook?.length ?? 0) +
          (scrapeResult.youTube?.length ?? 0) +
          (scrapeResult.tikTok?.length ?? 0);

        if (scrapedCount === 0) {
          throw new Error(scrapeResult.message || 'Đã dừng — chưa lưu bài nào.');
        }

        setLaunchSummary(scrapeResult.message || `Đã dừng — giữ ${scrapedCount} bản ghi.`);
        setLaunchPhase('done');
        setScrapeJob(null);
        setTimeout(() => {
          navigate(`/workspace/${wid}/project/${project.projectId}`);
        }, 1800);
        return;
      }

      if (scrapeResult.errorMessage) {
        throw new Error(scrapeResult.errorMessage);
      }

      const scrapedCount =
        (scrapeResult.facebook?.length ?? 0) +
        (scrapeResult.youTube?.length ?? 0) +
        (scrapeResult.tikTok?.length ?? 0);

      if (scrapedCount === 0) {
        const detail = scrapeResult.errors?.length
          ? scrapeResult.errors.join(' ')
          : 'Không thu thập được dữ liệu. Kiểm tra Playwright, cookie Facebook (fb_cookie.json) và từ khóa.';

        setLaunchSummary(
          `Dự án đã tạo nhưng chưa có dữ liệu. ${detail} Vào dự án và bấm «Cào lại» để thử lại.`
        );
        setErrorMessage(detail);
        setLaunchPhase('done');
        setScrapeJob(null);
        setTimeout(() => {
          navigate(`/workspace/${wid}/project/${project.projectId}`);
        }, 2800);
        return;
      }

      if (scrapeResult.errors?.length) {
        setErrorMessage(scrapeResult.errors.join(' '));
      }

      setLaunchPhase('analyzing');
      const analyzeMessage = scrapeResult.analysis?.message;
      if (analyzeMessage) {
        setLaunchSummary(`${scrapeResult.message || `Đã thu thập ${scrapedCount} nguồn dữ liệu.`} ${analyzeMessage}`);
      } else {
        const analyzeResult = await projectApi.analyze(wid, project.projectId, true);
        setLaunchSummary(
          `${scrapeResult.message || `Đã thu thập ${scrapedCount} nguồn dữ liệu.`} ${analyzeResult.message}`
        );
      }
      setLaunchPhase('done');
      setScrapeJob(null);

      setTimeout(() => {
        navigate(`/workspace/${wid}/project/${project.projectId}`);
      }, 1800);
    } catch (error) {
      setLaunchPhase('error');
      setScrapeJob(null);
      const message = extractApiError(error, 'Không thể khởi chạy dự án. Vui lòng thử lại.');
      setErrorMessage(
        createdProjectId
          ? `${message} Dự án đã được tạo — bạn có thể vào workspace và thử «Cào lại».`
          : message
      );
      if (createdProjectId) {
        setLaunchSummary(`Dự án #${createdProjectId} đã lưu trong workspace.`);
      }
    } finally {
      setIsSubmitting(false);
    }
  };

  const isStep1Valid = campaignName.trim() !== '';

  const phaseLabel: Record<LaunchPhase, string> = {
    idle: '',
    creating: 'Đang tạo dự án...',
    scraping: 'Bot đang cào dữ liệu từ các nền tảng...',
    analyzing: 'AI đang phân tích sentiment...',
    done: 'Hoàn tất! Đang chuyển đến dashboard...',
    error: 'Có lỗi xảy ra',
  };

  return (
    <div className="min-h-screen bg-[#050A15] text-white font-sans flex items-center justify-center p-6 selection:bg-[#FF7575] selection:text-white relative overflow-hidden">
      <ScrapeProgressModal
        open={launchPhase === 'scraping' || launchPhase === 'analyzing'}
        projectName={campaignName || 'Dự án mới'}
        job={scrapeJob}
        isCancelling={isCancellingScrape}
        onCancel={
          launchPhase === 'scraping' && scrapeJobId
            ? async () => {
                if (!scrapeJobId || isCancellingScrape) return;
                setIsCancellingScrape(true);
                try {
                  await scrapeApi.cancelJob(scrapeJobId);
                } catch {
                  setIsCancellingScrape(false);
                }
              }
            : undefined
        }
      />
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
              Chọn nền tảng, nhập URL/từ khóa — hệ thống sẽ tự động cào dữ liệu và phân tích bằng AI.
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
          {['THÔNG TIN', 'NGUỒN DỮ LIỆU', 'TỪ KHÓA', 'KHỞI CHẠY'].map((label, index) => {
            const step = index + 1;
            return (
              <div key={label} className={`flex items-center gap-3 bg-[#0A101D] ${index === 0 ? 'pr-4' : index === 3 ? 'pl-4' : 'px-4'}`}>
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
            <div className="animate-in fade-in slide-in-from-right-4 duration-500">
              <h2 className="text-xl font-bold mb-2">Nguồn thu thập</h2>
              <p className="text-gray-400 text-sm mb-6">
                Chọn nền tảng cần cào. Giai đoạn này hỗ trợ <strong className="text-white">Facebook, YouTube, TikTok</strong>.
              </p>
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
                {[
                  { id: 'facebook', icon: Users, label: 'FACEBOOK' },
                  { id: 'youtube', icon: MonitorPlay, label: 'YOUTUBE' },
                  { id: 'tiktok', icon: Hash, label: 'TIKTOK' },
                ].map(({ id, icon: Icon, label }) => (
                  <button
                    key={id}
                    type="button"
                    onClick={() => toggleSource(id)}
                    className={`flex items-center gap-4 p-5 rounded-xl border text-left transition-all ${
                      selectedSources.includes(id)
                        ? 'bg-[#FF7575]/5 border-[#FF7575]'
                        : 'bg-[#151B2B] border-white/5 hover:border-white/20'
                    }`}
                  >
                    <div className={`w-5 h-5 rounded flex items-center justify-center border ${selectedSources.includes(id) ? 'bg-[#FF7575] border-[#FF7575]' : 'border-gray-500'}`}>
                      {selectedSources.includes(id) && <Check className="w-3.5 h-3.5 text-white" strokeWidth={3} />}
                    </div>
                    <Icon className={`w-5 h-5 ${selectedSources.includes(id) ? 'text-[#FF7575]' : 'text-gray-400'}`} />
                    <span className="font-semibold text-sm">{label}</span>
                  </button>
                ))}
              </div>

              <div className="grid grid-cols-1 md:grid-cols-2 gap-4 opacity-60">
                {[
                  { id: 'maps', icon: Map, label: 'GOOGLE MAPS' },
                  { id: 'browser', icon: Globe, label: 'TRÌNH DUYỆT' },
                  { id: 'file', icon: FileUp, label: 'IMPORT FILE' },
                ].map(({ id, icon: Icon, label }) => (
                  <div key={id} className="flex items-center gap-4 p-4 rounded-xl border border-white/5 bg-[#151B2B]/50 text-gray-500">
                    <Icon className="w-5 h-5" />
                    <span className="text-sm font-semibold">{label}</span>
                    <span className="ml-auto text-xs">Sắp ra mắt</span>
                  </div>
                ))}
              </div>

              <div className="pt-6 mt-4 border-t border-white/5">
                <label className="block text-xs font-bold text-gray-500 uppercase mb-3">
                  URL Group / Fanpage Facebook (khuyến nghị khi cào FB)
                </label>
                <div className="relative">
                  <LinkIcon className="absolute left-4 top-1/2 -translate-y-1/2 h-5 w-5 text-gray-400" />
                  <input
                    type="url"
                    value={targetUrl}
                    onChange={(e) => setTargetUrl(e.target.value)}
                    placeholder="https://www.facebook.com/groups/... hoặc fanpage..."
                    className="w-full pl-12 pr-4 py-4 bg-[#151B2B] border border-white/5 text-white placeholder-gray-500 rounded-xl focus:outline-none focus:border-[#FF7575]"
                  />
                </div>
              </div>
            </div>
          )}

          {currentStep === 3 && <KeywordStep keywords={keywords} setKeywords={setKeywords} />}

          {currentStep === 4 && (
            <div className="animate-in fade-in slide-in-from-right-4 duration-500 flex flex-col items-center justify-center py-8">
              {launchPhase === 'idle' ? (
                <>
                  <div className="w-16 h-16 rounded-full bg-[#FF7575]/10 flex items-center justify-center mb-6">
                    <Sparkles className="w-8 h-8 text-[#FF7575]" />
                  </div>
                  <h2 className="text-2xl font-bold mb-2">Sẵn sàng khởi chạy!</h2>
                  <p className="text-gray-400 text-center max-w-lg mb-6">
                    Dự án <strong className="text-white">"{campaignName}"</strong> sẽ được tạo, cào dữ liệu từ{' '}
                    {selectedSources.filter((s) => SCRAPABLE_PLATFORMS.includes(s as typeof SCRAPABLE_PLATFORMS[number])).join(', ')}{' '}
                    với từ khóa <strong className="text-white">"{getPrimaryKeyword(keywords)}"</strong>, sau đó AI phân tích sentiment.
                  </p>
                  <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 w-full max-w-2xl">
                    <div className="p-4 bg-[#151B2B] rounded-xl border border-white/5 text-center text-sm text-gray-400">
                      <Database className="w-5 h-5 mx-auto mb-2 text-[#00B4D8]" /> Lưu project vào DB
                    </div>
                    <div className="p-4 bg-[#151B2B] rounded-xl border border-white/5 text-center text-sm text-gray-400">
                      <Globe className="w-5 h-5 mx-auto mb-2 text-[#FF7575]" /> Cào Facebook / YouTube / TikTok
                    </div>
                    <div className="p-4 bg-[#151B2B] rounded-xl border border-white/5 text-center text-sm text-gray-400">
                      <Sparkles className="w-5 h-5 mx-auto mb-2 text-emerald-400" /> AI phân tích sentiment
                    </div>
                  </div>
                </>
              ) : (
                <div className="text-center space-y-4">
                  {launchPhase === 'error' ? (
                    <AlertCircle className="w-12 h-12 mx-auto text-red-400" />
                  ) : (
                    <Loader2 className={`w-12 h-12 mx-auto animate-spin ${launchPhase === 'done' ? 'text-emerald-400' : 'text-[#FF7575]'}`} />
                  )}
                  <h2 className="text-xl font-bold">{phaseLabel[launchPhase]}</h2>
                  {launchSummary && <p className="text-gray-400 text-sm max-w-md mx-auto">{launchSummary}</p>}
                </div>
              )}
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
                disabled={(currentStep === 1 && !isStep1Valid) || isSubmitting}
                className="bg-[#FF7575] hover:bg-[#ff6262] text-white px-8 py-3 rounded-lg text-sm font-semibold disabled:opacity-50"
              >
                Tiếp tục
              </button>
            ) : (
              <button
                onClick={handleSubmit}
                disabled={isSubmitting || launchPhase === 'done'}
                className="bg-blue-600 hover:bg-blue-500 text-white px-8 py-3 rounded-lg text-sm font-bold disabled:opacity-70 flex items-center gap-2"
              >
                {isSubmitting ? (
                  <>
                    <Loader2 className="w-4 h-4 animate-spin" />
                    Đang xử lý...
                  </>
                ) : (
                  'Khởi chạy Chiến dịch'
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
