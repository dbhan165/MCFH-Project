import { ArrowRight, PlayCircle } from 'lucide-react';
import wellcome from '../assets/wellcome.png';
import { Link } from 'react-router-dom';
import McfhLogo from '../components/brand/McfhLogo';

const Welcome = () => {
  return (
    // Wrapper chính với màu nền Navy tối và pattern lưới (grid) chìm
    <div className="min-h-screen bg-[#0A101D] text-white font-sans flex flex-col relative overflow-hidden selection:bg-[#FF7575] selection:text-white">
      
      {/* Background Grid Pattern (Tạo họa tiết chấm bi mờ) */}
      <div 
        className="absolute inset-0 z-0 opacity-20 pointer-events-none"
        style={{ 
          backgroundImage: 'radial-gradient(#ffffff 1px, transparent 1px)', 
          backgroundSize: '32px 32px' 
        }}
      ></div>

      {/* Header / Navbar */}
      <header className="relative z-10 border-b border-white/5">
        <div className="container mx-auto px-8 h-20 flex items-center justify-between">
          <McfhLogo
            linkTo="/"
            size={38}
            textClassName="text-white text-xl"
            subtitle="System Hub"
            subtitleClassName="text-[10px] text-gray-400 font-semibold tracking-[0.28em] uppercase"
          />
          
          {/* Main Navigation */}
          <nav className="hidden md:flex items-center gap-8 text-sm font-medium text-gray-300">
            <a href="#product" className="text-white border-b-2 border-[#FF7575] pb-1">Sản phẩm</a>
            <a href="#ai-solutions" className="hover:text-white transition-colors pb-1">Giải pháp AI</a>
            <Link to="/pricing" className="hover:text-white transition-colors pb-1">Báo cáo Bespoke</Link>
            <Link to="/pricing" className="hover:text-white transition-colors pb-1">Bảng giá</Link>
          </nav>

          {/* Auth Actions */}
          <div className="flex items-center gap-6">
            <Link to="/login" className="text-sm font-medium text-gray-300 hover:text-white transition-colors">
              Login
            </Link>
            <Link to="/login" className="bg-[#FF7575] hover:bg-[#ff6262] text-white px-5 py-2.5 rounded text-sm font-semibold tracking-wide transition-colors">
              Start Free Trial
            </Link>
          </div>
        </div>
      </header>

      {/* Hero Section */}
      <main id="product" className="relative z-10 grow flex items-center container mx-auto px-8 py-16 lg:py-0 scroll-mt-24">
        <div className="w-full grid grid-cols-1 lg:grid-cols-2 gap-16 items-center">
          
          {/* Cột trái: Nội dung Text */}
          <div className="space-y-8">
            {/* Badge */}
            <div className="inline-flex items-center gap-2 border border-[#FF7575]/30 bg-[#FF7575]/5 rounded-sm px-3 py-1 text-xs tracking-wider text-gray-200">
              <span className="w-1.5 h-1.5 rounded-full bg-[#FF7575]"></span>
              New Feature: Real-Time NSR Tracking
            </div>

            {/* Headline */}
            <h1 className="text-5xl lg:text-[4rem] font-bold leading-[1.1] tracking-tight">
              Làm chủ tiếng <br />
              nói khách hàng <br />
              đa kênh với AI.
            </h1>

            {/* Description */}
            <p className="text-[#9BA1B0] text-lg max-w-lg leading-relaxed">
              Nền tảng Social Listening kết hợp chấm điểm NSR và Báo cáo 
              Bespoke chuyên sâu dành cho doanh nghiệp lớn. Khai phá sức mạnh 
              dữ liệu để dẫn đầu thị trường.
            </p>

            {/* Call to Actions (CTA) */}
            <div className="flex items-center gap-8 pt-2">
              <Link to="/create-workspace" className="bg-[#FF7575] hover:bg-[#ff6262] text-white px-7 py-3.5 rounded text-sm font-medium flex items-center gap-2 transition-colors">
                Khởi tạo Workspace ngay <ArrowRight size={18} />
              </Link>
              <Link to="/pricing" className="text-gray-300 hover:text-white flex items-center gap-2 text-sm font-medium transition-colors">
                <PlayCircle size={20} /> Xem demo sản phẩm
              </Link>
            </div>

            {/* Statistics */}
            <div id="ai-solutions" className="pt-10 border-t border-white/10 flex gap-20 mt-4 scroll-mt-24">
              <div>
                <div className="text-[1.35rem] font-semibold mb-1">500+</div>
                <div className="text-sm text-[#9BA1B0]">Doanh nghiệp tin dùng</div>
              </div>
              <div>
                <div className="text-[1.35rem] font-semibold mb-1">98%</div>
                <div className="text-sm text-[#9BA1B0]">Độ chính xác AI</div>
              </div>
            </div>
          </div>

          {/* Cột phải: Hình ảnh Graphic */}
          <div className="relative w-full h-112.5 flex items-center justify-center">
            {/* Khối kim cương đỏ chìm phía sau ảnh */}
            <div className="absolute -left-5 top-[40%] w-16 h-16 bg-[#FF7575]/20 rotate-45 z-0"></div>
            
            {/* Box chứa ảnh với border kính (glassmorphism) */}
            <div className="relative z-10 w-full h-full rounded-xl border border-white/10 bg-linear-to-br from-[#1A2235]/80 to-[#0A101D]/80 backdrop-blur-sm overflow-hidden shadow-2xl p-1">
              
              {/* Nút Live Processing nổi trên ảnh */}
              <div className="absolute top-4 right-4 z-20 border border-white/20 px-3 py-1.5 rounded text-xs tracking-wider text-gray-200 bg-black/50 backdrop-blur-md">
                Live Processing
              </div>
              
              {/* Hình ảnh Demo */}
              <img 
                src={wellcome} 
                alt="AI Dashboard Visualization" 
                className="w-full h-full object-cover rounded-lg opacity-70 mix-blend-lighten"
              />
            </div>
          </div>
          
        </div>
      </main>

      {/* Footer */}
      <footer className="relative z-10 border-t border-white/5 bg-[#080D18]">
        <div className="container mx-auto px-8 py-10 flex flex-col md:flex-row justify-between items-end gap-6 text-sm text-[#9BA1B0]">
          <div className="space-y-4">
            <McfhLogo
              linkTo="/"
              size={34}
              textClassName="text-white text-2xl"
              subtitle="System Hub"
              subtitleClassName="text-[10px] text-gray-400 font-semibold tracking-[0.28em] uppercase"
            />
            <div>© {new Date().getFullYear()} MCFH System Hub. All rights reserved.</div>
          </div>
          <div className="flex gap-8">
            <Link to="/pricing" className="hover:text-white transition-colors">Bảng giá</Link>
            <Link to="/login" className="hover:text-white transition-colors">Đăng nhập</Link>
            <Link to="/create-workspace" className="hover:text-white transition-colors">Dùng thử</Link>
          </div>
        </div>
      </footer>

    </div>
  );
};

export default Welcome;