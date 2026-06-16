import { Tags } from 'lucide-react';

// Định nghĩa Props để truyền dữ liệu từ thẻ cha (CreateCampaign) xuống
interface KeywordStepProps {
  keywords: string;
  setKeywords: (value: string) => void;
}

const KeywordStep = ({ keywords, setKeywords }: KeywordStepProps) => {
  return (
    <div className="animate-in fade-in slide-in-from-right-4 duration-500">
      <h2 className="text-xl font-bold mb-2">Cấu hình Bộ từ khóa (Keywords)</h2>
      <p className="text-gray-400 text-sm mb-6">
        Thiết lập các từ khóa chính để AI của MCFH nhận diện và lọc dữ liệu chính xác nhất.
      </p>
      
      {/* Khối nhập Từ khóa chính (Đã bỏ phần Loại trừ) */}
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
    </div>
  );
};

export default KeywordStep;