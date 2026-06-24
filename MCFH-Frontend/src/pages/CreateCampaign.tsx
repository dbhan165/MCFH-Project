import { useState } from 'react';
import { Users, Hash, Map, FileUp, Link as LinkIcon, Check } from 'lucide-react';
import { Link } from 'react-router-dom';

const CreateCampaign = () => {
  // State quản lý các nguồn dữ liệu được chọn
  const [selectedSources, setSelectedSources] = useState<string[]>(['facebook', 'tiktok']);

  // Hàm xử lý khi click vào Card chọn nguồn
  const toggleSource = (sourceId: string) => {
    if (selectedSources.includes(sourceId)) {
      setSelectedSources(selectedSources.filter(id => id !== sourceId));
    } else {
      setSelectedSources([...selectedSources, sourceId]);
    }
  };

  return (
    <div className="min-h-screen bg-[#050A15] text-white font-sans flex items-center justify-center p-6 selection:bg-[#FF7575] selection:text-white relative overflow-hidden">
      
      {/* Background Grid Pattern */}
      <div 
        className="absolute inset-0 z-0 opacity-10 pointer-events-none"
        style={{ backgroundImage: 'radial-gradient(#ffffff 1px, transparent 1px)', backgroundSize: '32px 32px' }}
      ></div>

      {/* Main Form Container */}
      <div className="w-full max-w-4xl bg-[#0A101D] border border-white/5 rounded-2xl p-8 md:p-12 relative z-10 shadow-2xl">
        
        {/* Header */}
        <h1 className="text-3xl md:text-4xl font-bold tracking-tight mb-10">Thiết lập Chiến dịch Giám sát mới</h1>

        {/* Stepper (Thanh tiến trình 3 bước) */}
        <div className="flex items-center justify-between mb-12 relative">
          {/* Đường kẻ ngang (Background Line) - Đã sửa h-[1px] thành h-px */}
          <div className="absolute top-1/2 left-0 w-full h-px bg-white/10 -z-10 -translate-y-1/2"></div>
          
          {/* Step 1: Active */}
          <div className="flex items-center gap-3 bg-[#0A101D] pr-4">
            <div className="w-10 h-10 rounded-full bg-[#FF7575]/10 border-2 border-[#FF7575] flex items-center justify-center text-[#FF7575] font-bold">
              1
            </div>
            <span className="font-bold text-[#FF7575] tracking-wide text-sm">NGUỒN DỮ LIỆU</span>
          </div>

          {/* Step 2: Inactive */}
          <div className="flex items-center gap-3 bg-[#0A101D] px-4">
            <div className="w-10 h-10 rounded-full bg-white/5 border-2 border-white/10 flex items-center justify-center text-gray-500 font-bold">
              2
            </div>
            <span className="font-semibold text-gray-500 tracking-wide text-sm">BỘ TỪ KHÓA</span>
          </div>

          {/* Step 3: Inactive */}
          <div className="flex items-center gap-3 bg-[#0A101D] pl-4">
            <div className="w-10 h-10 rounded-full bg-white/5 border-2 border-white/10 flex items-center justify-center text-gray-500 font-bold">
              3
            </div>
            <span className="font-semibold text-gray-500 tracking-wide text-sm">PHÂN TÍCH AI</span>
          </div>
        </div>

        {/* Form Content */}
        <div className="space-y-8">
          
          {/* Cấu hình Nguồn thu thập */}
          <div>
            <h2 className="text-xl font-bold mb-2">Cấu hình Nguồn thu thập (Data Sources)</h2>
            <p className="text-gray-400 text-sm mb-6">Chọn các nền tảng bạn muốn Bot Playwright của MCFH thu thập dữ liệu.</p>
            
            {/* Grid Selectable Cards */}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              
              {/* Card: Facebook */}
              <button 
                onClick={() => toggleSource('facebook')}
                className={`flex items-center gap-4 p-5 rounded-xl border text-left transition-all ${
                  selectedSources.includes('facebook') 
                    ? 'bg-[#FF7575]/5 border-[#FF7575] shadow-[0_0_15px_rgba(255,117,117,0.1)]' 
                    : 'bg-[#151B2B] border-white/5 hover:border-white/20'
                }`}
              >
                <div className={`w-5 h-5 rounded flex items-center justify-center border transition-colors ${
                  selectedSources.includes('facebook') ? 'bg-[#FF7575] border-[#FF7575]' : 'border-gray-500'
                }`}>
                  {selectedSources.includes('facebook') && <Check className="w-3.5 h-3.5 text-white" strokeWidth={3} />}
                </div>
                <Users className={`w-5 h-5 ${selectedSources.includes('facebook') ? 'text-[#FF7575]' : 'text-gray-400'}`} />
                <span className={`font-semibold text-sm tracking-wider ${selectedSources.includes('facebook') ? 'text-[#FF7575]' : 'text-gray-300'}`}>
                  FACEBOOK FANPAGES
                </span>
              </button>

              {/* Card: TikTok */}
              <button 
                onClick={() => toggleSource('tiktok')}
                className={`flex items-center gap-4 p-5 rounded-xl border text-left transition-all ${
                  selectedSources.includes('tiktok') 
                    ? 'bg-[#FF7575]/5 border-[#FF7575] shadow-[0_0_15px_rgba(255,117,117,0.1)]' 
                    : 'bg-[#151B2B] border-white/5 hover:border-white/20'
                }`}
              >
                <div className={`w-5 h-5 rounded flex items-center justify-center border transition-colors ${
                  selectedSources.includes('tiktok') ? 'bg-[#FF7575] border-[#FF7575]' : 'border-gray-500'
                }`}>
                  {selectedSources.includes('tiktok') && <Check className="w-3.5 h-3.5 text-white" strokeWidth={3} />}
                </div>
                <Hash className={`w-5 h-5 ${selectedSources.includes('tiktok') ? 'text-[#FF7575]' : 'text-gray-400'}`} />
                <span className={`font-semibold text-sm tracking-wider ${selectedSources.includes('tiktok') ? 'text-[#FF7575]' : 'text-gray-300'}`}>
                  TIKTOK HASHTAGS
                </span>
              </button>

              {/* Card: Google Maps */}
              <button 
                onClick={() => toggleSource('maps')}
                className={`flex items-center gap-4 p-5 rounded-xl border text-left transition-all ${
                  selectedSources.includes('maps') 
                    ? 'bg-[#FF7575]/5 border-[#FF7575] shadow-[0_0_15px_rgba(255,117,117,0.1)]' 
                    : 'bg-[#151B2B] border-white/5 hover:border-white/20'
                }`}
              >
                <div className={`w-5 h-5 rounded flex items-center justify-center border transition-colors ${
                  selectedSources.includes('maps') ? 'bg-[#FF7575] border-[#FF7575]' : 'border-gray-500'
                }`}>
                  {selectedSources.includes('maps') && <Check className="w-3.5 h-3.5 text-white" strokeWidth={3} />}
                </div>
                <Map className={`w-5 h-5 ${selectedSources.includes('maps') ? 'text-[#FF7575]' : 'text-gray-400'}`} />
                <span className={`font-semibold text-sm tracking-wider ${selectedSources.includes('maps') ? 'text-[#FF7575]' : 'text-gray-300'}`}>
                  GOOGLE MAPS REVIEWS
                </span>
              </button>

              {/* Card: Custom File */}
              <button 
                onClick={() => toggleSource('file')}
                className={`flex items-center gap-4 p-5 rounded-xl border text-left transition-all ${
                  selectedSources.includes('file') 
                    ? 'bg-[#FF7575]/5 border-[#FF7575] shadow-[0_0_15px_rgba(255,117,117,0.1)]' 
                    : 'bg-[#151B2B] border-white/5 hover:border-white/20'
                }`}
              >
                <div className={`w-5 h-5 rounded flex items-center justify-center border transition-colors ${
                  selectedSources.includes('file') ? 'bg-[#FF7575] border-[#FF7575]' : 'border-gray-500'
                }`}>
                  {selectedSources.includes('file') && <Check className="w-3.5 h-3.5 text-white" strokeWidth={3} />}
                </div>
                <FileUp className={`w-5 h-5 ${selectedSources.includes('file') ? 'text-[#FF7575]' : 'text-gray-400'}`} />
                <span className={`font-semibold text-sm tracking-wider ${selectedSources.includes('file') ? 'text-[#FF7575]' : 'text-gray-300'}`}>
                  IMPORT CUSTOM FILE (CSV/EXCEL)
                </span>
              </button>
              
            </div>
          </div>

          {/* Cấu hình chi tiết (Input URL/Hashtag) */}
          <div className="pt-4">
            <label className="block text-xs font-bold text-gray-500 tracking-wider uppercase mb-3">
              Cấu hình chi tiết
            </label>
            <div className="relative">
              <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none">
                <LinkIcon className="h-5 w-5 text-gray-400" />
              </div>
              <input 
                type="text" 
                placeholder="Nhập URL hoặc Hashtag cụ thể (Ví dụ: #PetCareHub, #MCFH)..." 
                className="w-full pl-12 pr-4 py-4 bg-[#151B2B] border border-white/5 text-white placeholder-gray-500 rounded-xl focus:outline-none focus:border-[#FF7575] focus:ring-1 focus:ring-[#FF7575] transition-all"
              />
            </div>
          </div>

          {/* Action Buttons */}
          <div className="pt-8 flex justify-end gap-4 border-t border-white/5 mt-8">
             <Link 
              to="/campaigns" 
              className="px-6 py-3 rounded-lg text-sm font-semibold text-gray-400 hover:text-white transition-colors"
            >
              Hủy bỏ
            </Link>
            <button className="bg-[#FF7575] hover:bg-[#ff6262] text-white px-8 py-3 rounded-lg text-sm font-semibold transition-colors shadow-lg shadow-[#FF7575]/20">
              Tiếp tục: Bộ từ khóa
            </button>
          </div>

        </div>
      </div>
    </div>
  );
};

export default CreateCampaign;