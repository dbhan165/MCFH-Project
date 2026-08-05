import {
  Users, MonitorPlay, Globe, Hash, Map, FileUp,
  Check, Lock, Sparkles, MessageCircle,
} from 'lucide-react';

const PLATFORMS = [
  { id: 'facebook', icon: Users, label: 'Facebook', description: 'Bài viết, bình luận công khai' },
  { id: 'youtube', icon: MonitorPlay, label: 'YouTube', description: 'Video, bình luận' },
  { id: 'threads', icon: MessageCircle, label: 'Threads', description: 'Bài viết, bình luận công khai' },
  { id: 'news', icon: Globe, label: 'Tin tức', description: 'Báo điện tử, trang tin' },
] as const;

const COMING_SOON = [
  { id: 'tiktok', icon: Hash, label: 'TikTok', description: 'Video ngắn, bình luận' },
  { id: 'maps', icon: Map, label: 'Google Maps', description: 'Đánh giá địa điểm' },
  { id: 'file', icon: FileUp, label: 'Import file', description: 'CSV / Excel tuỳ chỉnh' },
] as const;

interface SourcesStepProps {
  selectedSources: string[];
  setSelectedSources: (value: string[]) => void;
  /** maxSources từ gói đã chọn (null = unlimited) */
  maxSources: number | null;
}

const SourcesStep = ({ selectedSources, setSelectedSources, maxSources }: SourcesStepProps) => {
  const isAtLimit = maxSources != null && selectedSources.length >= maxSources;
  const isOverLimit = maxSources != null && selectedSources.length > maxSources;

  const toggleSource = (id: string) => {
    const has = selectedSources.includes(id);
    if (has) {
      setSelectedSources(selectedSources.filter((s) => s !== id));
      return;
    }
    // Đã đạt giới hạn → không cho tick thêm.
    if (maxSources != null && selectedSources.length >= maxSources) return;
    setSelectedSources([...selectedSources, id]);
  };

  const counterLabel =
    maxSources == null
      ? `${selectedSources.length} nguồn đã chọn`
      : `${selectedSources.length}/${maxSources} nguồn — giới hạn của gói`;

  return (
    <div className="animate-in fade-in slide-in-from-right-4 duration-500 space-y-6">
      <div>
        <h2 className="text-xl font-bold mb-2">Chọn nguồn dữ liệu</h2>
        <p className="text-gray-400 text-sm mb-2">
          Hệ thống sẽ cào mentions từ các nguồn bạn chọn.
        </p>
        <p
          className={`inline-flex items-center gap-2 text-xs px-3 py-1.5 rounded-full border ${
            isOverLimit
              ? 'bg-red-500/10 border-red-500/30 text-red-300'
              : isAtLimit
                ? 'bg-amber-500/10 border-amber-500/30 text-amber-200'
                : 'bg-white/5 border-white/10 text-gray-400'
          }`}
        >
          <Sparkles className="w-3.5 h-3.5" />
          {counterLabel}
        </p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        {PLATFORMS.map(({ id, icon: Icon, label, description }) => {
          const isSelected = selectedSources.includes(id);
          const disabled = !isSelected && isAtLimit;
          return (
            <button
              key={id}
              type="button"
              onClick={() => toggleSource(id)}
              disabled={disabled}
              className={`relative flex items-start gap-4 p-5 rounded-xl border text-left transition-all ${
                isSelected
                  ? 'bg-[#FF7575]/[0.07] border-[#FF7575] shadow-[0_0_0_1px_rgba(255,117,117,0.25)]'
                  : disabled
                    ? 'bg-[#151B2B]/40 border-white/5 opacity-50 cursor-not-allowed'
                    : 'bg-[#151B2B] border-white/5 hover:border-white/20'
              }`}
              title={disabled ? `Đã đạt giới hạn ${maxSources} nguồn của gói đã chọn` : undefined}
            >
              <div
                className={`w-5 h-5 mt-0.5 rounded flex items-center justify-center border shrink-0 ${
                  isSelected
                    ? 'bg-[#FF7575] border-[#FF7575]'
                    : disabled
                      ? 'border-gray-700'
                      : 'border-gray-500'
                }`}
              >
                {isSelected ? (
                  <Check className="w-3.5 h-3.5 text-white" strokeWidth={3} />
                ) : disabled ? (
                  <Lock className="w-3 h-3 text-gray-600" />
                ) : null}
              </div>
              <Icon
                className={`w-5 h-5 mt-0.5 shrink-0 ${
                  isSelected ? 'text-[#FF7575]' : disabled ? 'text-gray-600' : 'text-gray-400'
                }`}
              />
              <div className="min-w-0 flex-1">
                <p
                  className={`font-semibold text-sm ${
                    isSelected ? 'text-white' : disabled ? 'text-gray-500' : 'text-white'
                  }`}
                >
                  {label}
                </p>
                <p className="text-xs text-gray-500 mt-0.5">{description}</p>
              </div>
            </button>
          );
        })}
      </div>

      <div className="grid grid-cols-1 md:grid-cols-3 gap-4 opacity-50">
        {COMING_SOON.map(({ id, icon: Icon, label, description }) => (
          <div
            key={id}
            className="flex items-start gap-3 p-4 rounded-xl border border-white/5 bg-[#151B2B]/40 text-gray-500"
          >
            <Icon className="w-4 h-4 mt-0.5 shrink-0" />
            <div className="min-w-0 flex-1">
              <p className="text-sm font-semibold">{label}</p>
              <p className="text-[11px] text-gray-600 mt-0.5">{description}</p>
            </div>
            <span className="text-[10px] uppercase tracking-wide text-gray-600">Soon</span>
          </div>
        ))}
      </div>
    </div>
  );
};

export default SourcesStep;