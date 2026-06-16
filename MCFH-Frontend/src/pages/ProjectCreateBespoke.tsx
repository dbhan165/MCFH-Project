import { useState } from 'react';
import { Link, useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, FileText, CheckCircle2, Settings2, Download } from 'lucide-react';

const ProjectCreateBespoke = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setIsSubmitting(true);
    
    // Giả lập gọi API tạo báo cáo mất 1.5s
    setTimeout(() => {
      setIsSubmitting(false);
      navigate(`/project/${id}/reports`);
    }, 1500);
  };

  return (
    <div className="animate-in fade-in duration-500 max-w-4xl mx-auto space-y-6">
      
      {/* Nút Back */}
      <Link to={`/project/${id}/reports`} className="flex items-center gap-2 text-gray-400 hover:text-white transition-colors w-fit text-sm font-semibold">
        <ArrowLeft size={16} /> Quay lại Report Center
      </Link>

      <div className="mb-8">
        <h2 className="text-3xl font-bold flex items-center gap-2 text-white mb-2">
          <Settings2 className="text-[#FF7575]" size={32} />
          Khởi tạo Bespoke Report
        </h2>
        <p className="text-gray-400">Tùy chỉnh các trường dữ liệu và mốc thời gian để xuất báo cáo phân tích theo nhu cầu riêng của bạn.</p>
      </div>

      <form onSubmit={handleSubmit} className="bg-[#151B2B] border border-white/5 rounded-2xl p-8 shadow-xl">
        
        <div className="space-y-8">
          {/* Tên báo cáo */}
          <div>
            <label className="block text-sm font-bold text-gray-300 mb-2">Tên báo cáo</label>
            <input 
              type="text" 
              required
              placeholder="VD: Báo cáo xử lý khủng hoảng tháng 6..." 
              className="w-full px-4 py-3 bg-[#0A101D] border border-white/10 rounded-xl focus:outline-none focus:border-[#FF7575] focus:ring-1 focus:ring-[#FF7575] text-white transition-all"
            />
          </div>

          {/* Chọn mốc thời gian */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div>
              <label className="block text-sm font-bold text-gray-300 mb-2">Từ ngày</label>
              <input type="date" required className="w-full px-4 py-3 bg-[#0A101D] border border-white/10 rounded-xl text-white focus:border-[#FF7575] focus:outline-none [color-scheme:dark]" />
            </div>
            <div>
              <label className="block text-sm font-bold text-gray-300 mb-2">Đến ngày</label>
              <input type="date" required className="w-full px-4 py-3 bg-[#0A101D] border border-white/10 rounded-xl text-white focus:border-[#FF7575] focus:outline-none [color-scheme:dark]" />
            </div>
          </div>

          {/* Chọn các Module muốn xuất */}
          <div>
            <label className="block text-sm font-bold text-gray-300 mb-3">Chọn dữ liệu phân tích (Tối thiểu 1)</label>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
              {[
                'Tổng quan Mentions & Reach', 
                'Phân tích Cảm xúc (Sentiment)', 
                'So sánh Kênh (Channel)', 
                'Hiệu suất KOLs & Influencers',
                'Phân tích Khía cạnh (Aspect)',
                'Top 100 Popular Mentions'
              ].map((item, idx) => (
                <label key={idx} className="flex items-center gap-3 p-4 bg-[#0A101D] border border-white/5 rounded-xl cursor-pointer hover:border-[#FF7575]/50 transition-colors group">
                  <input type="checkbox" defaultChecked={idx < 3} className="w-5 h-5 rounded border-gray-600 text-[#FF7575] focus:ring-[#FF7575] bg-transparent" />
                  <span className="text-sm font-medium text-gray-300 group-hover:text-white">{item}</span>
                </label>
              ))}
            </div>
          </div>

          {/* Định dạng xuất */}
          <div>
            <label className="block text-sm font-bold text-gray-300 mb-3">Định dạng file</label>
            <div className="flex gap-4">
              <label className="flex items-center gap-2 cursor-pointer">
                <input type="radio" name="format" defaultChecked className="text-[#FF7575] focus:ring-[#FF7575]" />
                <span className="text-gray-300 font-semibold">PDF Report</span>
              </label>
              <label className="flex items-center gap-2 cursor-pointer">
                <input type="radio" name="format" className="text-[#FF7575] focus:ring-[#FF7575]" />
                <span className="text-gray-300 font-semibold">Excel (Raw Data)</span>
              </label>
            </div>
          </div>
        </div>

        {/* Nút Submit */}
        <div className="mt-10 pt-6 border-t border-white/5 flex justify-end gap-4">
          <Link to={`/project/${id}/reports`} className="px-6 py-3 rounded-lg font-bold text-gray-400 hover:text-white hover:bg-white/5 transition-colors">
            Hủy bỏ
          </Link>
          <button 
            type="submit" 
            disabled={isSubmitting}
            className="flex items-center gap-2 bg-[#FF7575] hover:bg-[#ff6262] text-white px-8 py-3 rounded-lg font-bold shadow-[0_4px_14px_0_rgba(255,117,117,0.39)] transition-all disabled:opacity-50"
          >
            {isSubmitting ? 'Đang tạo...' : <><Download size={18} /> Yêu cầu khởi tạo</>}
          </button>
        </div>
      </form>
    </div>
  );
};

export default ProjectCreateBespoke;