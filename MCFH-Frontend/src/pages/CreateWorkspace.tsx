import { useState } from 'react';
import { Building2, ArrowLeft, CheckCircle2, Users } from 'lucide-react';
import { Link, useNavigate } from 'react-router-dom';

const CreateWorkspace = () => {
  const navigate = useNavigate();
  const [workspaceName, setWorkspaceName] = useState('');
  const [industry, setIndustry] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!workspaceName.trim()) return;

    setIsSubmitting(true);
    try {
      // Giả lập API lưu trữ dữ liệu
      await new Promise(resolve => setTimeout(resolve, 1500));
      // Khởi tạo xong, đưa người dùng vào thẳng không gian mới (Giả lập ID = 4)
      navigate('/workspace/4/projects');
    } catch (error) {
      console.error("Lỗi khi tạo workspace:", error);
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="min-h-screen bg-[#050A15] text-white font-sans flex items-center justify-center p-6 selection:bg-[#FF7575] selection:text-white relative overflow-hidden">
      
      {/* Background Grid Pattern */}
      <div 
        className="absolute inset-0 z-0 opacity-10 pointer-events-none"
        style={{ backgroundImage: 'radial-gradient(#ffffff 1px, transparent 1px)', backgroundSize: '32px 32px' }}
      ></div>

      <div className="w-full max-w-3xl bg-[#0A101D] border border-white/5 rounded-3xl p-8 md:p-12 relative z-10 shadow-2xl">
        
        {/* Nút Back */}
        <Link to="/workspaces" className="flex items-center gap-2 text-gray-400 hover:text-white transition-colors w-fit text-sm font-semibold mb-8">
          <ArrowLeft size={16} /> Quay lại danh sách
        </Link>

        {/* Header */}
        <div className="flex items-center gap-4 mb-10">
          <div className="w-16 h-16 rounded-2xl bg-[#FF7575]/10 flex items-center justify-center border border-[#FF7575]/20">
            <Building2 className="w-8 h-8 text-[#FF7575]" />
          </div>
          <div>
            <h1 className="text-3xl font-bold tracking-tight text-white mb-1">Khởi tạo Không gian làm việc</h1>
            <p className="text-gray-400">Thiết lập trung tâm điều hành cho tổ chức hoặc doanh nghiệp của bạn.</p>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="space-y-8">
          
          <div className="space-y-6 bg-[#151B2B] p-8 rounded-2xl border border-white/5">
            {/* Tên Workspace */}
            <div>
              <label className="block text-sm font-bold text-gray-300 mb-2">Tên Tổ chức / Doanh nghiệp <span className="text-[#FF7575]">*</span></label>
              <input 
                type="text" 
                required
                autoFocus
                value={workspaceName}
                onChange={(e) => setWorkspaceName(e.target.value)}
                placeholder="VD: Vinamilk Marketing Team..." 
                className="w-full px-4 py-3.5 bg-[#0A101D] border border-white/10 rounded-xl focus:outline-none focus:border-[#FF7575] focus:ring-1 focus:ring-[#FF7575] text-white transition-all placeholder-gray-600"
              />
            </div>

            {/* Lĩnh vực hoạt động */}
            <div>
              <label className="block text-sm font-bold text-gray-300 mb-2">Lĩnh vực hoạt động (Ngành nghề)</label>
              <select 
                value={industry}
                onChange={(e) => setIndustry(e.target.value)}
                className="w-full px-4 py-3.5 bg-[#0A101D] border border-white/10 rounded-xl focus:outline-none focus:border-[#FF7575] focus:ring-1 focus:ring-[#FF7575] text-white transition-all appearance-none"
              >
                <option value="" disabled className="text-gray-500">Chọn lĩnh vực...</option>
                <option value="fnb">F&B (Nhà hàng / Đồ uống)</option>
                <option value="retail">Bán lẻ & Tiêu dùng</option>
                <option value="tech">Công nghệ & Phần mềm</option>
                <option value="finance">Tài chính & Ngân hàng</option>
                <option value="other">Khác</option>
              </select>
            </div>
          </div>

          {/* Vùng mô tả tiện ích */}
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="flex items-center gap-3 p-4 bg-[#0A101D] rounded-xl border border-white/5 text-sm text-gray-300">
              <CheckCircle2 className="w-5 h-5 text-emerald-500 shrink-0" />
              Môi trường dữ liệu biệt lập và an toàn tuyệt đối.
            </div>
            <div className="flex items-center gap-3 p-4 bg-[#0A101D] rounded-xl border border-white/5 text-sm text-gray-300">
              <Users className="w-5 h-5 text-blue-500 shrink-0" />
              Không giới hạn số lượng thành viên phân quyền.
            </div>
          </div>

          {/* Nút Submit */}
          <div className="pt-6 border-t border-white/5 flex justify-end gap-4">
            <Link to="/workspaces" className="px-6 py-3.5 rounded-xl font-bold text-gray-400 hover:text-white hover:bg-white/5 transition-colors">
              Hủy bỏ
            </Link>
            <button 
              type="submit" 
              disabled={isSubmitting || !workspaceName.trim()}
              className="bg-[#FF7575] hover:bg-[#ff6262] disabled:opacity-50 disabled:cursor-not-allowed text-white px-8 py-3.5 rounded-xl font-bold transition-all shadow-[0_4px_14px_0_rgba(255,117,117,0.39)] flex items-center gap-2"
            >
              {isSubmitting ? (
                <>
                  <svg className="animate-spin h-5 w-5 text-white" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                  Đang khởi tạo...
                </>
              ) : (
                'Khởi tạo Workspace'
              )}
            </button>
          </div>

        </form>
      </div>
    </div>
  );
};

export default CreateWorkspace;