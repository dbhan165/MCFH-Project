import { Tags, Calendar } from 'lucide-react';

export const SCRAPE_TIME_RANGE_OPTIONS = [
  { value: 0, label: 'Mọi thời gian', priceHint: '12 triệu ₫' },
  { value: 7, label: '1 tuần gần đây', priceHint: '1 triệu ₫' },
  { value: 30, label: '1 tháng gần đây', priceHint: '1 triệu ₫' },
  { value: 90, label: '3 tháng gần đây', priceHint: '3 triệu ₫' },
  { value: 180, label: '6 tháng gần đây', priceHint: '6 triệu ₫' },
  { value: 365, label: '1 năm gần đây', priceHint: '12 triệu ₫' },
] as const;

interface KeywordStepProps {
  keywords: string;
  setKeywords: (value: string) => void;
  postedSinceDays: number;
  setPostedSinceDays: (value: number) => void;
}

const KeywordStep = ({
  keywords,
  setKeywords,
  postedSinceDays,
  setPostedSinceDays,
}: KeywordStepProps) => {
  return (
    <div className="animate-in fade-in slide-in-from-right-4 duration-500 space-y-6">
      <div>
        <h2 className="text-xl font-bold mb-2">Cấu hình Bộ từ khóa (Keywords)</h2>
        <p className="text-gray-400 text-sm mb-6">
          Thiết lập các từ khóa chính và khoảng thời gian đăng bài để AI của MCFH lọc dữ liệu chính xác nhất.
        </p>
      </div>

      <div className="bg-[#151B2B] border border-white/5 rounded-xl p-6">
        <label className="flex items-center gap-2 text-sm font-bold text-white mb-3">
          <Tags className="w-4 h-4 text-[#FF7575]" />
          Từ khóa chính (Bắt buộc)
        </label>
        <p className="text-xs text-gray-500 mb-4">
          Hệ thống sẽ chỉ thu thập các bài viết có chứa ít nhất 1 trong các từ khóa này.
        </p>
        <textarea
          rows={5}
          value={keywords}
          onChange={(e) => setKeywords(e.target.value)}
          placeholder="Nhập từ khóa, phân cách nhau bằng dấu phẩy (Ví dụ: thức ăn cho mèo, royal canin, pate cún...)"
          className="w-full p-4 bg-[#0A101D] border border-white/10 text-white placeholder-gray-600 rounded-lg focus:outline-none focus:border-[#FF7575] focus:ring-1 focus:ring-[#FF7575] transition-all resize-none"
        />
      </div>

      <div className="bg-[#151B2B] border border-white/5 rounded-xl p-6">
        <label className="flex items-center gap-2 text-sm font-bold text-white mb-3">
          <Calendar className="w-4 h-4 text-[#00B4D8]" />
          Khoảng thời gian đăng bài
        </label>
        <p className="text-xs text-gray-500 mb-4">
          Chỉ cào bài/video đăng trong khoảng thời gian này. Mỗi nền tảng sẽ lấy tối thiểu 5 bài phù hợp (nếu có).
        </p>
        <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
          {SCRAPE_TIME_RANGE_OPTIONS.map((opt) => (
            <button
              key={opt.value}
              type="button"
              onClick={() => setPostedSinceDays(opt.value)}
              className={`px-4 py-3 rounded-lg border text-sm font-medium text-left transition-all ${
                postedSinceDays === opt.value
                  ? 'border-[#00B4D8] bg-[#00B4D8]/10 text-white'
                  : 'border-white/10 bg-[#0A101D] text-gray-400 hover:border-white/20 hover:text-gray-200'
              }`}
            >
              <span className="block">{opt.label}</span>
              <span className="block text-xs mt-1 text-[#FF7575] font-semibold">{opt.priceHint}</span>
            </button>
          ))}
        </div>
      </div>
    </div>
  );
};

export default KeywordStep;
