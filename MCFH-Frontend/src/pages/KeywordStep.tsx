import { Tags, Sparkles } from 'lucide-react';

interface KeywordStepProps {
  keywords: string;
  setKeywords: (value: string) => void;
}

const KeywordStep = ({ keywords, setKeywords }: KeywordStepProps) => {
  return (
    <div className="animate-in fade-in slide-in-from-right-4 duration-500 space-y-6">
      <div>
        <h2 className="text-xl font-bold mb-2">Từ khoá giám sát</h2>
        <p className="text-gray-400 text-sm mb-6">
          Hệ thống sẽ chỉ thu thập các bài viết có chứa ít nhất 1 trong các từ khoá này.
        </p>
      </div>

      <div className="bg-[#151B2B] border border-white/5 rounded-xl p-6">
        <label className="flex items-center gap-2 text-sm font-bold text-white mb-3">
          <Tags className="w-4 h-4 text-[#FF7575]" />
          Từ khoá chính (Bắt buộc)
        </label>
        <p className="text-xs text-gray-500 mb-4">
          Nhập nhiều từ khoá, phân cách bằng dấu phẩy. Hệ thống dùng từ khoá đầu tiên làm từ khoá chính cho đơn cào.
        </p>
        <textarea
          rows={6}
          value={keywords}
          onChange={(e) => setKeywords(e.target.value)}
          placeholder="Nhập từ khoá, phân cách nhau bằng dấu phẩy (Ví dụ: thức ăn cho mèo, royal canin, pate cún...)"
          className="w-full p-4 bg-[#0A101D] border border-white/10 text-white placeholder-gray-600 rounded-lg focus:outline-none focus:border-[#FF7575] focus:ring-1 focus:ring-[#FF7575] transition-all resize-none"
        />
      </div>

      <div className="flex items-start gap-2 text-xs text-gray-400 bg-white/[0.03] border border-white/5 rounded-lg px-4 py-3">
        <Sparkles className="w-3.5 h-3.5 text-[#FF7575] shrink-0 mt-0.5" />
        <span>
          Bước tiếp theo sẽ hiển thị báo giá chính xác cho gói đã chọn cùng với số nguồn giám sát.
        </span>
      </div>
    </div>
  );
};

export default KeywordStep;