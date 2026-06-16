import { useState } from 'react';
import { Users, Hash, Map, FileUp, Link as LinkIcon, Check } from 'lucide-react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
// LƯU Ý: Nếu bạn có KeywordStep.tsx, hãy giữ nguyên dòng import dưới đây.
// Nếu không có, bạn có thể thay thế nó bằng một div đơn giản.
import KeywordStep from './KeywordStep'; 

const CreateProject = () => {
  const navigate = useNavigate();
  
  // Lấy workspaceId từ query param trên URL (VD: /create-project?wid=1)
  // Đây là cách an toàn nhất để trang Create (vốn nằm ngoài Layout chính) biết nó đang phục vụ cho Workspace nào
  const [searchParams] = useSearchParams();
  const workspaceId = searchParams.get('wid') || '1'; // Mặc định là 1 nếu không truyền
  
  const [currentStep, setCurrentStep] = useState(1);
  const [isSubmitting, setIsSubmitting] = useState(false);

  // ==========================================
  // STATE LƯU TRỮ DỮ LIỆU FORM
  // ==========================================
  // Bước 1: Thông tin cơ bản (Đã bỏ Workspace Name vì giờ ta đã chọn Workspace trước rồi)
  const [campaignName, setCampaignName] = useState('');
  
  // Bước 2 & 3: Nguồn & Từ khóa
  const [selectedSources, setSelectedSources] = useState<string[]>(['facebook', 'tiktok']);
  const [keywords, setKeywords] = useState<string>(''); 

  // Hàm xử lý chọn nguồn
  const toggleSource = (sourceId: string) => {
    if (selectedSources.includes(sourceId)) {
      setSelectedSources(selectedSources.filter(id => id !== sourceId));
    } else {
      setSelectedSources([...selectedSources, sourceId]);
    }
  };

  const handleNext = () => {
    if (currentStep < 4) setCurrentStep(currentStep + 1);
  };

  const handleBack = () => {
    if (currentStep > 1) setCurrentStep(currentStep - 1);
  };

  // Hàm xử lý khi bấm hoàn thành ở Bước 4
  const handleSubmit = async () => {
    setIsSubmitting(true);
    try {
      // TODO: Gộp dữ liệu gọi API lên Backend
      console.log("Dữ liệu gửi đi:", { workspaceId, campaignName, selectedSources, keywords });
      
      // Giả lập API mất 1.5 giây để xử lý
      await new Promise(resolve => setTimeout(resolve, 1500));
      
      // Thành công -> Điều hướng về lại danh sách dự án của đúng Workspace đó
      navigate(`/workspace/${workspaceId}/projects`);
    } catch (error) {
      console.error("Lỗi khi tạo chiến dịch:", error);
    } finally {
      setIsSubmitting(false);
    }
  };

  // Validate Bước 1: Không cho bấm Tiếp tục nếu chưa nhập tên
  const isStep1Valid = campaignName.trim() !== '';

  return (
    <div className="min-h-screen bg-[#050A15] text-white font-sans flex items-center justify-center p-6 selection:bg-[#FF7575] selection:text-white relative overflow-hidden">
      
      {/* Background Grid Pattern */}
      <div 
        className="absolute inset-0 z-0 opacity-10 pointer-events-none"
        style={{ backgroundImage: 'radial-gradient(#ffffff 1px, transparent 1px)', backgroundSize: '32px 32px' }}
      ></div>

      <div className="w-full max-w-5xl bg-[#0A101D] border border-white/5 rounded-2xl p-8 md:p-12 relative z-10 shadow-2xl transition-all duration-500">
        
        {/* Header */}
        <h1 className="text-3xl md:text-4xl font-bold tracking-tight mb-10">Khởi tạo Dự án Giám sát</h1>

        {/* ========================================================
            STEPPER (THANH TIẾN TRÌNH 4 BƯỚC)
            ======================================================== */}
        <div className="flex items-center justify-between mb-12 relative">
          <div className="absolute top-1/2 left-0 w-full h-px bg-white/10 -z-10 -translate-y-1/2"></div>
          
          {/* Step 1: Thông tin */}
          <div className="flex items-center gap-3 bg-[#0A101D] pr-4">
            <div className={`w-10 h-10 rounded-full flex items-center justify-center font-bold transition-colors duration-300 ${currentStep >= 1 ? 'bg-[#FF7575]/10 border-2 border-[#FF7575] text-[#FF7575]' : 'bg-white/5 border-2 border-white/10 text-gray-500'}`}>
              {currentStep > 1 ? <Check className="w-5 h-5" /> : '1'}
            </div>
            <span className={`font-bold tracking-wide text-xs sm:text-sm hidden sm:block ${currentStep >= 1 ? 'text-[#FF7575]' : 'text-gray-500'}`}>THÔNG TIN</span>
          </div>

          {/* Step 2: Nguồn dữ liệu */}
          <div className="flex items-center gap-3 bg-[#0A101D] px-4">
            <div className={`w-10 h-10 rounded-full flex items-center justify-center font-bold transition-colors duration-300 ${currentStep >= 2 ? 'bg-[#FF7575]/10 border-2 border-[#FF7575] text-[#FF7575]' : 'bg-white/5 border-2 border-white/10 text-gray-500'}`}>
              {currentStep > 2 ? <Check className="w-5 h-5" /> : '2'}
            </div>
            <span className={`font-bold tracking-wide text-xs sm:text-sm hidden sm:block ${currentStep >= 2 ? 'text-[#FF7575]' : 'text-gray-500'}`}>NGUỒN DỮ LIỆU</span>
          </div>

          {/* Step 3: Từ khóa */}
          <div className="flex items-center gap-3 bg-[#0A101D] px-4">
            <div className={`w-10 h-10 rounded-full flex items-center justify-center font-bold transition-colors duration-300 ${currentStep >= 3 ? 'bg-[#FF7575]/10 border-2 border-[#FF7575] text-[#FF7575]' : 'bg-white/5 border-2 border-white/10 text-gray-500'}`}>
              {currentStep > 3 ? <Check className="w-5 h-5" /> : '3'}
            </div>
            <span className={`font-bold tracking-wide text-xs sm:text-sm hidden sm:block ${currentStep >= 3 ? 'text-[#FF7575]' : 'text-gray-500'}`}>BỘ TỪ KHÓA</span>
          </div>

          {/* Step 4: AI */}
          <div className="flex items-center gap-3 bg-[#0A101D] pl-4">
            <div className={`w-10 h-10 rounded-full flex items-center justify-center font-bold transition-colors duration-300 ${currentStep >= 4 ? 'bg-[#FF7575]/10 border-2 border-[#FF7575] text-[#FF7575]' : 'bg-white/5 border-2 border-white/10 text-gray-500'}`}>
              4
            </div>
            <span className={`font-bold tracking-wide text-xs sm:text-sm hidden sm:block ${currentStep >= 4 ? 'text-[#FF7575]' : 'text-gray-500'}`}>PHÂN TÍCH AI</span>
          </div>
        </div>

        {/* ========================================================
            NỘI DUNG FORM (4 BƯỚC)
            ======================================================== */}
        <div className="space-y-8 min-h-87.5">
          
          {/* STEP 1: THÔNG TIN CƠ BẢN */}
          {currentStep === 1 && (
            <div className="animate-in fade-in slide-in-from-right-4 duration-500">
              <h2 className="text-xl font-bold mb-2">Thông tin Dự án</h2>
              <p className="text-gray-400 text-sm mb-8">Đặt tên cho Dự án giám sát dữ liệu của bạn trên hệ thống.</p>
              
              <div className="space-y-6">
                {/* Input Tên Chiến dịch */}
                <div className="bg-[#151B2B] border border-white/5 rounded-xl p-6">
                  <label className="block text-sm font-bold text-white mb-2">Tên Dự án (Campaign)</label>
                  <p className="text-xs text-gray-400 mb-4">Tên gọi để bạn dễ dàng phân biệt dự án này với các dự án khác trong cùng Workspace.</p>
                  <input 
                    type="text" 
                    value={campaignName}
                    onChange={(e) => setCampaignName(e.target.value)}
                    placeholder="Ví dụ: Giám sát phản hồi PetCareHub Tháng 6..." 
                    className="w-full px-4 py-3 bg-[#0A101D] border border-white/10 text-white placeholder-gray-600 rounded-lg focus:outline-none focus:border-[#FF7575] focus:ring-1 focus:ring-[#FF7575] transition-all"
                    autoFocus
                  />
                </div>
              </div>
            </div>
          )}

          {/* STEP 2: CẤU HÌNH NGUỒN THU THẬP */}
          {currentStep === 2 && (
            <div className="animate-in fade-in slide-in-from-right-4 duration-500">
              <h2 className="text-xl font-bold mb-2">Cấu hình Nguồn thu thập (Data Sources)</h2>
              <p className="text-gray-400 text-sm mb-6">Chọn các nền tảng bạn muốn Bot Playwright của MCFH thu thập dữ liệu.</p>
              
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <button onClick={() => toggleSource('facebook')} className={`flex items-center gap-4 p-5 rounded-xl border text-left transition-all ${selectedSources.includes('facebook') ? 'bg-[#FF7575]/5 border-[#FF7575] shadow-[0_0_15px_rgba(255,117,117,0.1)]' : 'bg-[#151B2B] border-white/5 hover:border-white/20'}`}>
                  <div className={`w-5 h-5 rounded flex items-center justify-center border transition-colors ${selectedSources.includes('facebook') ? 'bg-[#FF7575] border-[#FF7575]' : 'border-gray-500'}`}>
                    {selectedSources.includes('facebook') && <Check className="w-3.5 h-3.5 text-white" strokeWidth={3} />}
                  </div>
                  <Users className={`w-5 h-5 ${selectedSources.includes('facebook') ? 'text-[#FF7575]' : 'text-gray-400'}`} />
                  <span className={`font-semibold text-sm tracking-wider ${selectedSources.includes('facebook') ? 'text-[#FF7575]' : 'text-gray-300'}`}>FACEBOOK FANPAGES</span>
                </button>

                <button onClick={() => toggleSource('tiktok')} className={`flex items-center gap-4 p-5 rounded-xl border text-left transition-all ${selectedSources.includes('tiktok') ? 'bg-[#FF7575]/5 border-[#FF7575] shadow-[0_0_15px_rgba(255,117,117,0.1)]' : 'bg-[#151B2B] border-white/5 hover:border-white/20'}`}>
                  <div className={`w-5 h-5 rounded flex items-center justify-center border transition-colors ${selectedSources.includes('tiktok') ? 'bg-[#FF7575] border-[#FF7575]' : 'border-gray-500'}`}>
                    {selectedSources.includes('tiktok') && <Check className="w-3.5 h-3.5 text-white" strokeWidth={3} />}
                  </div>
                  <Hash className={`w-5 h-5 ${selectedSources.includes('tiktok') ? 'text-[#FF7575]' : 'text-gray-400'}`} />
                  <span className={`font-semibold text-sm tracking-wider ${selectedSources.includes('tiktok') ? 'text-[#FF7575]' : 'text-gray-300'}`}>TIKTOK HASHTAGS</span>
                </button>

                <button onClick={() => toggleSource('maps')} className={`flex items-center gap-4 p-5 rounded-xl border text-left transition-all ${selectedSources.includes('maps') ? 'bg-[#FF7575]/5 border-[#FF7575] shadow-[0_0_15px_rgba(255,117,117,0.1)]' : 'bg-[#151B2B] border-white/5 hover:border-white/20'}`}>
                  <div className={`w-5 h-5 rounded flex items-center justify-center border transition-colors ${selectedSources.includes('maps') ? 'bg-[#FF7575] border-[#FF7575]' : 'border-gray-500'}`}>
                    {selectedSources.includes('maps') && <Check className="w-3.5 h-3.5 text-white" strokeWidth={3} />}
                  </div>
                  <Map className={`w-5 h-5 ${selectedSources.includes('maps') ? 'text-[#FF7575]' : 'text-gray-400'}`} />
                  <span className={`font-semibold text-sm tracking-wider ${selectedSources.includes('maps') ? 'text-[#FF7575]' : 'text-gray-300'}`}>GOOGLE MAPS REVIEWS</span>
                </button>

                <button onClick={() => toggleSource('file')} className={`flex items-center gap-4 p-5 rounded-xl border text-left transition-all ${selectedSources.includes('file') ? 'bg-[#FF7575]/5 border-[#FF7575] shadow-[0_0_15px_rgba(255,117,117,0.1)]' : 'bg-[#151B2B] border-white/5 hover:border-white/20'}`}>
                  <div className={`w-5 h-5 rounded flex items-center justify-center border transition-colors ${selectedSources.includes('file') ? 'bg-[#FF7575] border-[#FF7575]' : 'border-gray-500'}`}>
                    {selectedSources.includes('file') && <Check className="w-3.5 h-3.5 text-white" strokeWidth={3} />}
                  </div>
                  <FileUp className={`w-5 h-5 ${selectedSources.includes('file') ? 'text-[#FF7575]' : 'text-gray-400'}`} />
                  <span className={`font-semibold text-sm tracking-wider ${selectedSources.includes('file') ? 'text-[#FF7575]' : 'text-gray-300'}`}>IMPORT CUSTOM FILE</span>
                </button>
              </div>

              <div className="pt-6">
                <label className="block text-xs font-bold text-gray-500 tracking-wider uppercase mb-3">Cấu hình chi tiết</label>
                <div className="relative">
                  <div className="absolute inset-y-0 left-0 pl-4 flex items-center pointer-events-none">
                    <LinkIcon className="h-5 w-5 text-gray-400" />
                  </div>
                  <input 
                    type="text" 
                    placeholder="Nhập URL Fanpage hoặc Hashtag cụ thể..." 
                    className="w-full pl-12 pr-4 py-4 bg-[#151B2B] border border-white/5 text-white placeholder-gray-500 rounded-xl focus:outline-none focus:border-[#FF7575] focus:ring-1 focus:ring-[#FF7575] transition-all"
                  />
                </div>
              </div>
            </div>
          )}

          {/* STEP 3: TỪ KHÓA */}
          {currentStep === 3 && (
            <KeywordStep keywords={keywords} setKeywords={setKeywords} />
          )}

          {/* STEP 4: PHÂN TÍCH AI */}
          {currentStep === 4 && (
            <div className="animate-in fade-in slide-in-from-right-4 duration-500 flex flex-col items-center justify-center py-12">
              <div className="w-16 h-16 rounded-full bg-[#FF7575]/10 flex items-center justify-center mb-6">
                 <Check className="w-8 h-8 text-[#FF7575]" />
              </div>
              <h2 className="text-2xl font-bold mb-2 text-white">Sẵn sàng khởi tạo!</h2>
              <p className="text-gray-400 text-center max-w-md">
                Dự án <strong className="text-white">"{campaignName}"</strong> sẽ được đưa vào hàng đợi thu thập dữ liệu bằng Playwright và Phân tích Sentiment AI.
              </p>
            </div>
          )}

        </div>

        {/* ========================================================
            THANH ĐIỀU HƯỚNG
            ======================================================== */}
        <div className="pt-8 flex justify-between items-center border-t border-white/5 mt-8">
          <div>
            {currentStep === 1 ? (
               // ĐÃ FIX: Hủy bỏ sẽ đưa về đúng danh sách dự án của Workspace hiện hành
               <Link to={`/workspace/${workspaceId}/projects`} className="px-6 py-3 rounded-lg text-sm font-semibold text-gray-400 hover:bg-white/5 transition-colors">
                 Hủy bỏ
               </Link>
            ) : (
              <button onClick={handleBack} disabled={isSubmitting} className="px-6 py-3 rounded-lg text-sm font-semibold text-gray-400 hover:bg-white/5 transition-colors disabled:opacity-50">
                Quay lại
              </button>
            )}
          </div>
          
          <div>
            {currentStep < 4 ? (
              <button 
                onClick={handleNext} 
                disabled={currentStep === 1 && !isStep1Valid} 
                className="bg-[#FF7575] hover:bg-[#ff6262] text-white px-8 py-3 rounded-lg text-sm font-semibold transition-all shadow-[0_4px_14px_0_rgba(255,117,117,0.39)] flex items-center gap-2 disabled:opacity-50 disabled:cursor-not-allowed"
              >
                Tiếp tục: {currentStep === 1 ? 'Nguồn dữ liệu' : currentStep === 2 ? 'Bộ từ khóa' : 'Phân tích AI'}
              </button>
            ) : (
              <button 
                onClick={handleSubmit}
                disabled={isSubmitting}
                className="bg-blue-600 hover:bg-blue-500 text-white px-8 py-3 rounded-lg text-sm font-bold transition-all shadow-[0_4px_14px_0_rgba(37,99,235,0.39)] disabled:opacity-70 flex items-center gap-2"
              >
                {isSubmitting ? (
                  <>
                    <svg className="animate-spin h-4 w-4 text-white" fill="none" viewBox="0 0 24 24">
                      <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                      <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                    </svg>
                    Đang khởi tạo...
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