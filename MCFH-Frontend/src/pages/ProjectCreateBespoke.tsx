import { useState, useEffect } from 'react';
import { X, Download, Loader2, AlertCircle } from 'lucide-react';
import { projectApi } from '../api/projectApi';
import { extractApiError } from '../utils/authStorage';

const MODULE_OPTIONS = [
  { key: 'overview', label: 'Tổng quan Mentions & Reach' },
  { key: 'sentiment', label: 'Phân tích Cảm xúc (Sentiment)' },
  { key: 'channel', label: 'So sánh Kênh (Channel)' },
  { key: 'influencers', label: 'Hiệu suất KOLs & Influencers' },
  { key: 'aspects', label: 'Phân tích Khía cạnh (Aspect)' },
  { key: 'top_mentions', label: 'Top Mentions tiêu biểu' },
];

interface CreateBespokeModalProps {
  isOpen: boolean;
  onClose: () => void;
  wid: number;
  projectId: number;
  onSuccess: () => void;
}

const ProjectCreateBespokeModal = ({ isOpen, onClose, wid, projectId, onSuccess }: CreateBespokeModalProps) => {
  const [title, setTitle] = useState('');
  const [keyword, setKeyword] = useState('');
  const [packageType, setPackageType] = useState('basic');
  const [requirements, setRequirements] = useState('');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [modules, setModules] = useState<string[]>(['overview', 'sentiment', 'channel']);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');

  useEffect(() => {
    if (isOpen) document.body.style.overflow = 'hidden';
    else document.body.style.overflow = 'unset';
    return () => {
      document.body.style.overflow = 'unset';
    };
  }, [isOpen]);

  useEffect(() => {
    if (isOpen) {
      setTitle('');
      setKeyword('');
      setPackageType('basic');
      setRequirements('');
      setDateFrom('');
      setDateTo('');
      setModules(['overview', 'sentiment', 'channel']);
      setErrorMessage('');
      setIsSubmitting(false);
    }
  }, [isOpen]);

  if (!isOpen) return null;

  const toggleModule = (key: string) => {
    setModules((prev) => (prev.includes(key) ? prev.filter((m) => m !== key) : [...prev, key]));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!wid || !projectId || modules.length === 0) return;

    setIsSubmitting(true);
    setErrorMessage('');
    try {
      // Mock API call to avoid touching BE
      await new Promise(resolve => setTimeout(resolve, 1000));

      onSuccess();
      onClose();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể gửi yêu cầu báo cáo chuyên sâu.'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
      <div
        className="absolute inset-0 bg-black/60 backdrop-blur-sm"
        onClick={onClose}
      />
      <div className="relative bg-[#151B2B] border border-white/10 rounded-2xl w-full max-w-3xl max-h-[90vh] shadow-2xl overflow-hidden flex flex-col animate-in zoom-in-95 duration-200">
        <button
          type="button"
          onClick={onClose}
          className="absolute top-4 right-4 p-1.5 text-gray-400 hover:text-white hover:bg-white/10 rounded-lg transition-colors z-10"
        >
          <X size={20} />
        </button>

        <div className="p-6 border-b border-white/5 shrink-0">
          <h2 className="text-2xl font-bold text-white mb-2">Yêu cầu Báo cáo Chuyên sâu</h2>
          <p className="text-gray-400 text-sm">
            User gửi yêu cầu → Admin giao Reporter → Reporter biên soạn báo cáo theo nhu cầu của bạn.
          </p>
        </div>

        <div className="p-6 overflow-y-auto flex-1 custom-scrollbar">
          {errorMessage && (
            <div className="bg-red-500/10 border border-red-500/20 text-red-300 p-4 rounded-xl flex items-center gap-3 text-sm mb-6">
              <AlertCircle className="w-5 h-5 shrink-0" />
              {errorMessage}
            </div>
          )}

          <form id="create-bespoke-form" onSubmit={handleSubmit} className="space-y-6">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <label className={`flex flex-col p-4 rounded-xl border cursor-pointer transition-colors ${packageType === 'basic' ? 'border-[#FF7575] bg-[#FF7575]/10' : 'border-white/10 bg-[#0A101D] hover:border-white/20'}`} onClick={() => setPackageType('basic')}>
                <div className="flex items-center justify-between mb-2">
                  <span className="font-bold text-white">Gói Cơ Bản</span>
                  <div className={`w-4 h-4 rounded-full border flex items-center justify-center ${packageType === 'basic' ? 'border-[#FF7575]' : 'border-gray-500'}`}>
                    {packageType === 'basic' && <div className="w-2 h-2 rounded-full bg-[#FF7575]" />}
                  </div>
                </div>
                <span className="text-sm text-gray-400">Phân tích tiêu chuẩn, giới hạn 100 mention.</span>
                <span className="text-sm font-semibold text-[#FF7575] mt-2">500,000 VND</span>
              </label>

              <label className={`flex flex-col p-4 rounded-xl border cursor-pointer transition-colors ${packageType === 'pro' ? 'border-[#FF7575] bg-[#FF7575]/10' : 'border-white/10 bg-[#0A101D] hover:border-white/20'}`} onClick={() => setPackageType('pro')}>
                <div className="flex items-center justify-between mb-2">
                  <span className="font-bold text-white">Gói Nâng Cao</span>
                  <div className={`w-4 h-4 rounded-full border flex items-center justify-center ${packageType === 'pro' ? 'border-[#FF7575]' : 'border-gray-500'}`}>
                    {packageType === 'pro' && <div className="w-2 h-2 rounded-full bg-[#FF7575]" />}
                  </div>
                </div>
                <span className="text-sm text-gray-400">Phân tích sâu, giới hạn 250 mention.</span>
                <span className="text-sm font-semibold text-[#FF7575] mt-2">1,000,000 VND</span>
              </label>
            </div>

            <div>
              <label className="block text-sm font-bold text-gray-300 mb-2">Keyword / Thương hiệu cần phân tích *</label>
              <input
                type="text"
                required
                value={keyword}
                onChange={(e) => setKeyword(e.target.value)}
                placeholder="VD: Vinamilk, iPhone 15..."
                className="w-full px-4 py-3 bg-[#0A101D] border border-white/10 rounded-xl focus:outline-none focus:border-[#FF7575] text-white"
              />
            </div>

            <div>
              <label className="block text-sm font-bold text-gray-300 mb-2">Tiêu đề báo cáo *</label>
              <input
                type="text"
                required
                value={title}
                onChange={(e) => setTitle(e.target.value)}
                placeholder="VD: Báo cáo khủng hoảng truyền thông Q2/2026..."
                className="w-full px-4 py-3 bg-[#0A101D] border border-white/10 rounded-xl focus:outline-none focus:border-[#FF7575] text-white"
              />
            </div>

            <div>
              <label className="block text-sm font-bold text-gray-300 mb-2">Yêu cầu chi tiết</label>
              <textarea
                value={requirements}
                onChange={(e) => setRequirements(e.target.value)}
                rows={3}
                placeholder="Mô tả mục tiêu, đối tượng đọc, insight cần làm rõ, góc phân tích đặc biệt..."
                className="w-full px-4 py-3 bg-[#0A101D] border border-white/10 rounded-xl focus:outline-none focus:border-[#FF7575] text-white resize-none"
              />
            </div>

            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-bold text-gray-300 mb-2">Từ ngày</label>
                <input
                  type="date"
                  value={dateFrom}
                  onChange={(e) => setDateFrom(e.target.value)}
                  className="w-full px-4 py-3 bg-[#0A101D] border border-white/10 rounded-xl text-white focus:border-[#FF7575] focus:outline-none [color-scheme:dark]"
                />
              </div>
              <div>
                <label className="block text-sm font-bold text-gray-300 mb-2">Đến ngày</label>
                <input
                  type="date"
                  value={dateTo}
                  onChange={(e) => setDateTo(e.target.value)}
                  className="w-full px-4 py-3 bg-[#0A101D] border border-white/10 rounded-xl text-white focus:border-[#FF7575] focus:outline-none [color-scheme:dark]"
                />
              </div>
            </div>

            <div>
              <label className="block text-sm font-bold text-gray-300 mb-3">Module phân tích * (chọn ít nhất 1)</label>
              <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                {MODULE_OPTIONS.map((item) => (
                  <label
                    key={item.key}
                    className={`flex items-center gap-3 p-3 rounded-xl border cursor-pointer transition-colors ${modules.includes(item.key)
                      ? 'border-[#FF7575]/50 bg-[#FF7575]/5'
                      : 'border-white/5 bg-[#0A101D] hover:border-white/15'
                      }`}
                  >
                    <input
                      type="checkbox"
                      checked={modules.includes(item.key)}
                      onChange={() => toggleModule(item.key)}
                      className="w-4 h-4 rounded border-gray-600 text-[#FF7575] focus:ring-[#FF7575] bg-transparent"
                    />
                    <span className={`text-sm ${modules.includes(item.key) ? 'text-white' : 'text-gray-300'}`}>{item.label}</span>
                  </label>
                ))}
              </div>
            </div>
          </form>
        </div>

        <div className="p-6 border-t border-white/5 bg-[#0A101D] shrink-0 flex justify-end gap-4">
          <button type="button" onClick={onClose} className="px-6 py-2.5 rounded-xl font-bold text-gray-400 hover:text-white hover:bg-white/5 transition-colors">
            Hủy
          </button>
          <button
            type="submit"
            form="create-bespoke-form"
            disabled={isSubmitting || modules.length === 0}
            className="flex items-center gap-2 bg-[#FF7575] hover:bg-[#ff6262] disabled:opacity-50 text-white px-8 py-2.5 rounded-xl font-bold transition-colors"
          >
            {isSubmitting ? (
              <>
                <Loader2 className="w-4 h-4 animate-spin" /> Đang gửi...
              </>
            ) : (
              <>
                <Download size={18} /> Gửi yêu cầu
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  );
};

export default ProjectCreateBespokeModal;
