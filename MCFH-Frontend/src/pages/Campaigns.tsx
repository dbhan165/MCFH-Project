import { 
    LayoutGrid, 
    Database, 
    BarChart3, 
    ArrowRightLeft, 
    FileText,
    Search,
    Plus,
    Bell,
    ChevronDown,
    ArrowRight
  } from 'lucide-react';
  import { Link } from 'react-router-dom';
  import McfhLogo from '../components/brand/McfhLogo';
  
  const Campaigns = () => {
    return (
      <div className="min-h-screen flex bg-[#050A15] text-white font-sans selection:bg-[#FF7575] selection:text-white">
        
        {/* SIDEBAR (Trái) */}
        <aside className="w-64 bg-[#0A101D] border-r border-white/5 flex flex-col shrink-0 z-20">
          {/* Logo Area */}
          <div className="h-20 flex items-center px-6 border-b border-white/5">
            <McfhLogo linkTo="/workspaces" size={32} textClassName="text-white text-xl" />
          </div>
  
          {/* Menu Navigation */}
          <nav className="grow py-8 space-y-2">
            {/* Active Item - Cập nhật bg-linear-to-r */}
            <div className="relative flex items-center px-6 py-3 text-[#FF7575] bg-linear-to-r from-[#FF7575]/10 to-transparent border-l-2 border-[#FF7575] cursor-default">
              <LayoutGrid className="w-5 h-5 mr-4" />
              <span className="font-semibold text-sm">Overview</span>
            </div>
            
            {/* Các mục khác gắn Link để chuyển trang */}
            <Link to="/dashboard" className="flex items-center px-6 py-3 text-gray-400 hover:text-white hover:bg-white/5 cursor-pointer transition-colors border-l-2 border-transparent">
              <Database className="w-5 h-5 mr-4" />
              <span className="font-medium text-sm">Data Stream</span>
            </Link>
            
            <Link to="#" className="flex items-center px-6 py-3 text-gray-400 hover:text-white hover:bg-white/5 cursor-pointer transition-colors border-l-2 border-transparent">
              <BarChart3 className="w-5 h-5 mr-4" />
              <span className="font-medium text-sm">Aspect Analysis</span>
            </Link>
            
            <Link to="#" className="flex items-center px-6 py-3 text-gray-400 hover:text-white hover:bg-white/5 cursor-pointer transition-colors border-l-2 border-transparent">
              <ArrowRightLeft className="w-5 h-5 mr-4" />
              <span className="font-medium text-sm">Comparison</span>
            </Link>
            
            <Link to="#" className="flex items-center px-6 py-3 text-gray-400 hover:text-white hover:bg-white/5 cursor-pointer transition-colors border-l-2 border-transparent">
              <FileText className="w-5 h-5 mr-4" />
              <span className="font-medium text-sm">B2B Reports</span>
            </Link>
          </nav>
        </aside>
  
        {/* MAIN CONTENT (Phải) */}
        <main className="grow flex flex-col relative overflow-hidden">
          {/* Background Grid Mờ */}
          <div 
            className="absolute inset-0 z-0 opacity-[0.03] pointer-events-none"
            style={{ backgroundImage: 'radial-gradient(#ffffff 1px, transparent 1px)', backgroundSize: '32px 32px' }}
          ></div>
  
          {/* TOP BAR */}
          <header className="h-20 bg-[#0A101D] border-b border-white/5 flex items-center justify-between px-8 relative z-10 shrink-0">
            <div className="text-gray-300 font-medium">Campaigns</div>
            
            <div className="flex items-center gap-6">
              {/* Workspace Selector */}
              <button className="flex items-center gap-2 bg-[#151B2B] hover:bg-[#1A2235] border border-white/10 px-4 py-2 rounded-lg text-sm text-gray-300 transition-colors">
                Workspace: <span className="font-semibold text-white">Acma Agency</span>
                <ChevronDown className="w-4 h-4 text-gray-500 ml-1" />
              </button>
  
              {/* Notification */}
              <button className="relative text-gray-400 hover:text-white transition-colors">
                <Bell className="w-5 h-5" />
                <span className="absolute -top-1 -right-1 w-2 h-2 bg-[#FF7575] rounded-full"></span>
              </button>
  
              {/* Avatar */}
              <div className="w-8 h-8 rounded-full overflow-hidden border border-white/20">
                <img src="https://i.pravatar.cc/150?img=11" alt="Avatar" className="w-full h-full object-cover" />
              </div>
  
              {/* Create Report Button */}
              <button className="bg-[#FF7575]/10 hover:bg-[#FF7575]/20 text-[#FF7575] border border-[#FF7575]/20 px-4 py-2 rounded-lg text-sm font-semibold transition-colors">
                Create Report
              </button>
            </div>
          </header>
  
          {/* DASHBOARD CONTENT */}
          <div className="p-8 md:p-10 relative z-10 overflow-y-auto">
            
            {/* Header Section */}
            <div className="flex flex-col md:flex-row md:items-end justify-between gap-6 mb-10">
              <h1 className="text-4xl font-bold tracking-tight leading-tight">
                Quản lý Chiến dịch <br /> (Campaigns)
              </h1>
              
              <div className="flex items-center gap-4">
                <div className="relative">
                  <Search className="absolute left-4 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-500" />
                  <input 
                    type="text" 
                    placeholder="Tìm kiếm chiến dịch..." 
                    className="w-64 bg-[#151B2B] border border-white/10 rounded-lg pl-11 pr-4 py-3 text-sm text-white placeholder-gray-500 focus:outline-none focus:border-[#FF7575]/50 transition-colors"
                  />
                </div>
                <button className="bg-[#FF7575] hover:bg-[#ff6262] text-white px-5 py-3 rounded-lg text-sm font-semibold flex items-center gap-2 transition-colors shadow-lg shadow-[#FF7575]/20">
                  <Plus className="w-4 h-4" />
                  Khởi tạo Chiến dịch mới
                </button>
              </div>
            </div>
  
            {/* Cards Grid */}
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
              
              {/* Card 1: PetCareHub (Green/Teal) */}
              <div className="bg-[#151B2B] border border-white/5 rounded-2xl p-6 flex flex-col hover:border-[#00B4D8]/30 transition-colors">
                <h3 className="text-2xl font-bold mb-4 leading-snug">Giám sát phản hồi PetCareHub</h3>
                <div className="inline-flex items-center gap-2 bg-[#00B4D8]/10 text-[#00B4D8] border border-[#00B4D8]/20 px-3 py-1.5 rounded-md text-xs font-semibold w-fit mb-8">
                  <span className="w-2 h-2 rounded-full bg-[#00B4D8] animate-pulse"></span>
                  Đang quét (Real-time)
                </div>
                
                <div className="grid grid-cols-2 gap-4 mb-6">
                  <div>
                    <div className="text-gray-400 text-xs font-semibold uppercase tracking-wider mb-1">Tổng thảo luận</div>
                    <div className="text-2xl font-bold">12,450</div>
                  </div>
                  <div>
                    <div className="text-gray-400 text-xs font-semibold uppercase tracking-wider mb-1">Điểm NSR</div>
                    <div className="text-2xl font-bold text-[#00B4D8]">68</div>
                  </div>
                </div>
  
                <div className="mb-8">
                  <div className="flex justify-between text-xs font-medium mb-2">
                    <span className="text-gray-400 uppercase tracking-wider">Sentiment Ratio</span>
                    <span className="text-[#00B4D8]">80% Positive</span>
                  </div>
                  <div className="h-1.5 w-full bg-[#0A101D] rounded-full overflow-hidden">
                    <div className="h-full bg-[#00B4D8] w-[80%] rounded-full"></div>
                  </div>
                </div>
  
                <button className="mt-auto w-full bg-[#0A101D] hover:bg-[#FF7575]/10 text-gray-300 hover:text-[#FF7575] border border-white/5 hover:border-[#FF7575]/20 py-3 rounded-lg text-sm font-semibold flex items-center justify-center gap-2 transition-all">
                  Vào Dashboard <ArrowRight className="w-4 h-4" />
                </button>
              </div>
  
              {/* Card 2: Lô hàng lỗi (Red) */}
              <div className="bg-[#151B2B] border border-white/5 rounded-2xl p-6 flex flex-col hover:border-[#FF7575]/30 transition-colors">
                <h3 className="text-2xl font-bold mb-4 leading-snug">Khủng hoảng truyền thông Lô hàng lỗi</h3>
                <div className="inline-flex items-center gap-2 bg-[#FF7575]/10 text-[#FF7575] border border-[#FF7575]/20 px-3 py-1.5 rounded-md text-xs font-semibold w-fit mb-8">
                  <span className="w-2 h-2 rounded-full bg-[#FF7575]"></span>
                  Mức độ nghiêm trọng cao
                </div>
                
                <div className="grid grid-cols-2 gap-4 mb-6">
                  <div>
                    <div className="text-gray-400 text-xs font-semibold uppercase tracking-wider mb-1">Tổng thảo luận</div>
                    <div className="text-2xl font-bold">45,000</div>
                  </div>
                  <div>
                    <div className="text-gray-400 text-xs font-semibold uppercase tracking-wider mb-1">Điểm NSR</div>
                    <div className="text-2xl font-bold text-[#FF7575]">-45</div>
                  </div>
                </div>
  
                <div className="mb-8">
                  <div className="flex justify-between text-xs font-medium mb-2">
                    <span className="text-gray-400 uppercase tracking-wider">Sentiment Ratio</span>
                    <span className="text-[#FF7575]">70% Negative</span>
                  </div>
                  <div className="h-1.5 w-full bg-[#0A101D] rounded-full overflow-hidden flex">
                    <div className="h-full bg-[#FF7575] w-[70%] rounded-l-full"></div>
                    <div className="h-full bg-gray-600 w-[10%]"></div>
                    <div className="h-full bg-[#00B4D8] w-[20%] rounded-r-full"></div>
                  </div>
                </div>
  
                <button className="mt-auto w-full bg-[#0A101D] hover:bg-[#FF7575]/10 text-gray-300 hover:text-[#FF7575] border border-white/5 hover:border-[#FF7575]/20 py-3 rounded-lg text-sm font-semibold flex items-center justify-center gap-2 transition-all">
                  Vào Dashboard <ArrowRight className="w-4 h-4" />
                </button>
              </div>
  
              {/* Card 3: Đối thủ (Yellow) */}
              <div className="bg-[#151B2B] border border-white/5 rounded-2xl p-6 flex flex-col hover:border-yellow-500/30 transition-colors">
                <h3 className="text-2xl font-bold mb-4 leading-snug">Theo dõi Đối thủ cạnh tranh Q2</h3>
                <div className="inline-flex items-center gap-2 bg-yellow-500/10 text-yellow-500 border border-yellow-500/20 px-3 py-1.5 rounded-md text-xs font-semibold w-fit mb-8">
                  <span className="w-2 h-2 rounded-full bg-yellow-500"></span>
                  Đang tạm dừng
                </div>
                
                <div className="grid grid-cols-2 gap-4 mb-6">
                  <div>
                    <div className="text-gray-400 text-xs font-semibold uppercase tracking-wider mb-1">Tổng thảo luận</div>
                    <div className="text-2xl font-bold">5,200</div>
                  </div>
                  <div>
                    <div className="text-gray-400 text-xs font-semibold uppercase tracking-wider mb-1">Điểm NSR</div>
                    <div className="text-2xl font-bold text-yellow-500">12</div>
                  </div>
                </div>
  
                <div className="mb-8">
                  <div className="flex justify-between text-xs font-medium mb-2">
                    <span className="text-gray-400 uppercase tracking-wider">Sentiment Ratio</span>
                    <span className="text-yellow-500">Neutral</span>
                  </div>
                  <div className="h-1.5 w-full bg-[#0A101D] rounded-full overflow-hidden flex">
                    <div className="h-full bg-[#00B4D8] w-[30%] rounded-l-full"></div>
                    <div className="h-full bg-yellow-500 w-[50%]"></div>
                    <div className="h-full bg-[#FF7575] w-[20%] rounded-r-full"></div>
                  </div>
                </div>
  
                <button className="mt-auto w-full bg-[#0A101D] hover:bg-[#FF7575]/10 text-gray-300 hover:text-[#FF7575] border border-white/5 hover:border-[#FF7575]/20 py-3 rounded-lg text-sm font-semibold flex items-center justify-center gap-2 transition-all">
                  Vào Dashboard <ArrowRight className="w-4 h-4" />
                </button>
              </div>
  
            </div>
          </div>
        </main>
      </div>
    );
  };
  
  export default Campaigns;