import { ArrowRight, PlayCircle, BarChart3, BrainCircuit, FileText, Zap, Globe, Shield } from 'lucide-react';
import wellcome from '../assets/wellcome.png';
import { Link } from 'react-router-dom';
import McfhLogo from '../components/brand/McfhLogo';

const Welcome = () => {
  return (
    // Wrapper chính với màu nền Navy tối và pattern lưới (grid) chìm
    <div className="bg-[#0A101D] text-white font-sans flex flex-col relative overflow-x-hidden selection:bg-[#00B4D8] selection:text-white">
      
      {/* Background Grid Pattern (Tạo họa tiết chấm bi mờ) */}
      <div 
        className="fixed inset-0 z-0 opacity-20 pointer-events-none"
        style={{ 
          backgroundImage: 'radial-gradient(#ffffff 1px, transparent 1px)', 
          backgroundSize: '32px 32px' 
        }}
      ></div>

      {/* Header / Navbar */}
      <header className="fixed top-0 left-0 right-0 z-50 border-b border-white/5 bg-[#0A101D]/80 backdrop-blur-lg">
        <div className="container mx-auto px-8 h-20 flex items-center justify-between">
          <McfhLogo
            linkTo="/"
            size={38}
            textClassName="text-white text-xl"
            subtitle="System Hub"
            subtitleClassName="text-[10px] text-gray-400 font-semibold tracking-[0.28em] uppercase"
          />
          
          <nav className="hidden md:flex items-center gap-1 bg-white/5 border border-white/10 p-1.5 rounded-full backdrop-blur-md">
            <a href="#product" className="bg-[#00B4D8]/15 text-[#00B4D8] px-5 py-2 rounded-full text-sm font-semibold transition-all">
              Sản phẩm
            </a>
            <a href="#ai-solutions" className="text-gray-300 hover:text-white hover:bg-white/10 px-5 py-2 rounded-full text-sm font-medium transition-all">
              Giải pháp AI
            </a>
            <a href="#bespoke" className="text-gray-300 hover:text-white hover:bg-white/10 px-5 py-2 rounded-full text-sm font-medium transition-all">
              Báo cáo Bespoke
            </a>
            <Link to="/pricing" className="text-gray-300 hover:text-white hover:bg-white/10 px-5 py-2 rounded-full text-sm font-medium transition-all">
              Bảng giá
            </Link>
          </nav>

          {/* Auth Actions */}
          <div className="flex items-center gap-6">
            <Link to="/login" className="text-sm font-medium text-gray-300 hover:text-white transition-colors">
              Login
            </Link>
            <Link to="/login" className="bg-gradient-to-r from-[#00B4D8] to-[#3B82F6] hover:from-[#0693B0] hover:to-[#2563EB] shadow-[0_4px_15px_rgba(0,180,216,0.3)] text-white px-6 py-2.5 rounded-full text-sm font-semibold tracking-wide transition-all hover:shadow-[0_6px_20px_rgba(0,180,216,0.4)] hover:-translate-y-0.5">
              Start Free Trial
            </Link>
          </div>
        </div>
      </header>

      {/* Hero Section */}
      <main className="relative z-10 flex flex-col justify-center min-h-[90vh] container mx-auto px-8 pt-32 pb-16">
        <div className="w-full grid grid-cols-1 lg:grid-cols-2 gap-16 items-center">
          
          {/* Cột trái: Nội dung Text */}
          <div className="space-y-8">
            {/* Badge */}
            <div className="inline-flex items-center gap-2 border border-[#00B4D8]/30 bg-[#00B4D8]/5 rounded-full px-4 py-1.5 text-xs font-semibold tracking-wider text-[#00B4D8] shadow-[0_0_15px_rgba(0,180,216,0.1)]">
              <span className="w-2 h-2 rounded-full bg-[#00B4D8] animate-pulse"></span>
              New Feature: Real-Time NSR Tracking
            </div>

            {/* Headline */}
            <h1 className="text-5xl lg:text-[4rem] font-extrabold leading-[1.1] tracking-tight text-transparent bg-clip-text bg-gradient-to-r from-white via-gray-200 to-gray-400">
              Làm chủ tiếng <br />
              nói khách hàng <br />
              <span className="text-transparent bg-clip-text bg-gradient-to-r from-[#00B4D8] to-[#3B82F6]">đa kênh với AI.</span>
            </h1>

            {/* Description */}
            <p className="text-[#9BA1B0] text-lg max-w-lg leading-relaxed">
              Nền tảng Social Listening kết hợp chấm điểm NSR và Báo cáo 
              Bespoke chuyên sâu dành cho doanh nghiệp lớn. Khai phá sức mạnh 
              dữ liệu để dẫn đầu thị trường.
            </p>

            {/* Call to Actions (CTA) */}
            <div className="flex items-center gap-6 pt-2">
              <Link to="/create-workspace" className="bg-gradient-to-r from-[#00B4D8] to-[#3B82F6] hover:from-[#0693B0] hover:to-[#2563EB] text-white px-8 py-4 rounded-full text-sm font-bold flex items-center gap-2 transition-all shadow-[0_8px_25px_rgba(0,180,216,0.3)] hover:shadow-[0_12px_30px_rgba(0,180,216,0.4)] hover:-translate-y-1">
                Khởi tạo Workspace ngay <ArrowRight size={18} />
              </Link>
              <a href="#product" className="text-gray-300 hover:text-white flex items-center gap-2 text-sm font-medium transition-colors group">
                <PlayCircle size={24} className="text-[#00B4D8] group-hover:text-white transition-colors" /> Khám phá tính năng
              </a>
            </div>

            {/* Statistics */}
            <div className="pt-10 border-t border-white/10 flex gap-20 mt-4">
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
            {/* Khối sáng (Glow) phía sau ảnh thay cho khối đỏ */}
            <div className="absolute -left-10 top-[30%] w-64 h-64 bg-[#00B4D8]/20 rounded-full mix-blend-screen filter blur-[80px] z-0 animate-pulse-slow"></div>
            <div className="absolute -right-10 bottom-[20%] w-64 h-64 bg-[#3B82F6]/20 rounded-full mix-blend-screen filter blur-[80px] z-0"></div>
            
            {/* Box chứa ảnh với border kính (glassmorphism) */}
            <div className="relative z-10 w-full h-full rounded-2xl border border-white/10 bg-linear-to-br from-[#151B2B]/60 to-[#0A101D]/80 backdrop-blur-md overflow-hidden shadow-[0_20px_50px_rgba(0,0,0,0.5)] p-2">
              
              {/* Nút Live Processing nổi trên ảnh */}
              <div className="absolute top-6 right-6 z-20 border border-[#00B4D8]/30 px-3 py-1.5 rounded-full text-xs font-semibold tracking-wider text-[#00B4D8] bg-[#0A101D]/80 backdrop-blur-md shadow-[0_0_15px_rgba(0,180,216,0.2)] flex items-center gap-2">
                <span className="w-1.5 h-1.5 rounded-full bg-[#00B4D8] animate-ping"></span>
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

      {/* Product Section */}
      <section id="product" className="relative z-10 border-t border-white/5 bg-[#050A15]/50 py-24 scroll-mt-20">
        <div className="container mx-auto px-8">
          <div className="text-center mb-16">
            <h2 className="text-3xl font-extrabold mb-4 tracking-tight text-transparent bg-clip-text bg-gradient-to-r from-white to-gray-400">Hệ sinh thái Sản phẩm</h2>
            <p className="text-[#9BA1B0] max-w-2xl mx-auto leading-relaxed">Nền tảng lắng nghe mạng xã hội toàn diện, thu thập và phân tích hàng triệu thảo luận theo thời gian thực để mang lại insights kinh doanh giá trị.</p>
          </div>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-8">
            {[
              { icon: Globe, title: 'Brand Monitoring', desc: 'Theo dõi sức khỏe thương hiệu liên tục 24/7 trên đa nền tảng mạng xã hội.' },
              { icon: Shield, title: 'Crisis Management', desc: 'Cảnh báo khủng hoảng truyền thông tức thì, giúp doanh nghiệp xử lý kịp thời.' },
              { icon: BarChart3, title: 'Competitor Analysis', desc: 'Nắm bắt chiến lược và phản ứng của khách hàng đối với đối thủ cạnh tranh.' }
            ].map((item, idx) => (
              <div key={idx} className="bg-[#0A101D] p-8 rounded-2xl border border-white/5 hover:border-[#00B4D8]/30 transition-all hover:-translate-y-1 hover:shadow-[0_10px_30px_rgba(0,180,216,0.1)] group">
                <div className="w-14 h-14 bg-[#00B4D8]/10 rounded-2xl flex items-center justify-center text-[#00B4D8] mb-6 group-hover:scale-110 transition-transform"><item.icon size={28} strokeWidth={1.5} /></div>
                <h3 className="text-xl font-bold mb-3">{item.title}</h3>
                <p className="text-[#9BA1B0] text-sm leading-relaxed">{item.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      {/* AI Solutions Section */}
      <section id="ai-solutions" className="relative z-10 border-t border-white/5 py-24 scroll-mt-20">
        <div className="container mx-auto px-8">
          <div className="grid grid-cols-1 lg:grid-cols-2 gap-16 items-center">
            <div className="space-y-6">
              <div className="text-[#00B4D8] font-bold tracking-wider text-sm uppercase">Công nghệ cốt lõi</div>
              <h2 className="text-4xl font-extrabold tracking-tight">Giải pháp AI tạo ra sự khác biệt</h2>
              <p className="text-[#9BA1B0] text-lg leading-relaxed">Không chỉ đếm số lượng Mention, AI của chúng tôi đi sâu vào sắc thái cảm xúc (Sentiment) và tự động phân loại chủ đề (Aspect Extraction).</p>
              
              <ul className="space-y-4 pt-4">
                {[
                  'Phân tích sắc thái (Tích cực, Tiêu cực, Trung lập) với độ chính xác 98%',
                  'Tự động gom cụm và phát hiện xu hướng đang lên',
                  'Xử lý ngôn ngữ tự nhiên (NLP) tối ưu cho tiếng Việt'
                ].map((text, idx) => (
                  <li key={idx} className="flex items-center gap-4 text-sm font-medium">
                    <div className="w-6 h-6 rounded-full bg-[#3B82F6]/20 flex items-center justify-center text-[#3B82F6] shrink-0"><Zap size={14} /></div>
                    {text}
                  </li>
                ))}
              </ul>
            </div>
            <div className="relative h-96 bg-[#050A15] rounded-3xl border border-white/10 overflow-hidden flex items-center justify-center">
              {/* Fake AI Visual */}
              <div className="absolute inset-0 bg-[radial-gradient(ellipse_at_center,_var(--tw-gradient-stops))] from-[#00B4D8]/10 via-transparent to-transparent"></div>
              <BrainCircuit size={120} className="text-[#00B4D8]/20 absolute" strokeWidth={1} />
              <div className="relative z-10 bg-[#0A101D]/80 backdrop-blur p-6 rounded-2xl border border-white/10 text-center">
                <div className="text-3xl font-bold text-[#00B4D8] mb-1">98.5%</div>
                <div className="text-xs text-gray-400 uppercase tracking-widest">Độ tin cậy NLP</div>
              </div>
            </div>
          </div>
        </div>
      </section>

      {/* Bespoke Reports Section */}
      <section id="bespoke" className="relative z-10 border-t border-white/5 bg-gradient-to-b from-[#0A101D] to-[#050A15] py-24 scroll-mt-20">
        <div className="container mx-auto px-8 text-center">
          <div className="w-16 h-16 bg-[#3B82F6]/10 rounded-2xl flex items-center justify-center text-[#3B82F6] mx-auto mb-6"><FileText size={32} strokeWidth={1.5} /></div>
          <h2 className="text-4xl font-extrabold mb-6 tracking-tight">Báo cáo Bespoke chuyên sâu</h2>
          <p className="text-[#9BA1B0] max-w-2xl mx-auto leading-relaxed mb-10 text-lg">Vượt xa những biểu đồ tự động, đội ngũ chuyên gia dữ liệu của MCFH sẽ thiết kế riêng các báo cáo độc quyền, mang tính chiến lược cao dành riêng cho mô hình kinh doanh của bạn.</p>
          <Link to="/pricing" className="inline-flex items-center gap-2 bg-white text-[#0A101D] hover:bg-gray-200 px-8 py-4 rounded-full text-sm font-bold transition-colors">
            Khám phá gói dịch vụ <ArrowRight size={18} />
          </Link>
        </div>
      </section>

      {/* Footer */}
      <footer className="relative z-10 border-t border-white/5 bg-[#080D18]">
        <div className="container mx-auto px-8 py-10 flex flex-col md:flex-row justify-between items-center gap-6 text-sm text-[#9BA1B0]">
          <div className="flex items-center gap-4">
            <McfhLogo
              linkTo="/"
              size={28}
              textClassName="text-white text-xl"
            />
            <div className="border-l border-white/10 pl-4">© {new Date().getFullYear()} MCFH. All rights reserved.</div>
          </div>
          <div className="flex gap-8 font-medium">
            <Link to="/pricing" className="hover:text-white transition-colors">Bảng giá</Link>
            <Link to="/login" className="hover:text-white transition-colors">Đăng nhập</Link>
            <Link to="/create-workspace" className="hover:text-white transition-colors">Dùng thử miễn phí</Link>
          </div>
        </div>
      </footer>

    </div>
  );
};

export default Welcome;