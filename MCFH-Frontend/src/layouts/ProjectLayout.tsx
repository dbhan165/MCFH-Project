import { useState } from 'react';
import { Link, Outlet, useParams, useLocation } from 'react-router-dom';
import { 
  BarChart2, PieChart, LayoutDashboard, Share2, MessageCircle, 
  Bell, Download, FileText, FileSpreadsheet, 
  ArrowLeft, Calendar, GitCompareArrows, Users
} from 'lucide-react';

const ProjectLayout = () => {
  // CẬP NHẬT: Lấy cả id của dự án (id) VÀ id của tổ chức (workspaceId) từ URL
  const { workspaceId, id } = useParams();
  const location = useLocation();
  const [isExportOpen, setIsExportOpen] = useState(false);
  const [isNotifOpen, setIsNotifOpen] = useState(false);

  // Khai báo menu và nối đường dẫn chính xác (Khớp 100% với App.tsx)
  const menuItems = [
    { path: '', icon: <LayoutDashboard size={20} />, label: 'Overview Dashboard', exact: true },
    { path: 'mentions', icon: <MessageCircle size={20} />, label: 'Data Stream (Mentions)' },
    { path: 'sentiment', icon: <PieChart size={20} />, label: 'Sentiment Chart' },
    { path: 'influencers', icon: <Users size={20} />, label: 'KOLs & Influencers' },
    { path: 'channel', icon: <Share2 size={20} />, label: 'Channel Comparison' },
    { path: 'aspect', icon: <BarChart2 size={20} />, label: 'Aspect Analysis' },
    { path: 'reports', icon: <FileText size={20} />, label: 'Report Center' },
  ];

  return (
    <div className="h-screen w-full bg-[#050A15] text-white font-sans flex flex-col md:flex-row selection:bg-[#FF7575] selection:text-white overflow-hidden">
      
      {/* ================= SIDEBAR ================= */}
      <aside className="w-full md:w-72 bg-[#0A101D] border-r border-white/5 flex flex-col shrink-0 h-full">
        <div className="h-20 flex items-center px-6 border-b border-white/5 shrink-0">
          {/* CẬP NHẬT: Nút Back giờ trỏ về đúng danh sách dự án của Workspace hiện tại */}
          <Link to={`/workspace/${workspaceId}/projects`} className="flex items-center gap-2 text-gray-400 hover:text-white transition-colors text-sm font-semibold">
            <ArrowLeft size={18} /> Quay lại Hệ thống
          </Link>
        </div>
        
        <div className="p-6 flex-1 overflow-y-auto">
          <div className="mb-6">
            <div className="text-xs font-bold text-[#00B4D8] uppercase tracking-wider mb-2">Đang phân tích</div>
            <h2 className="text-xl font-bold leading-tight">Dự án #{id || 'PetCareHub'}</h2>
          </div>

          <p className="text-xs font-bold text-gray-500 mb-4 tracking-wider uppercase">Phân tích chuyên sâu</p>
          <nav className="space-y-2">
            {menuItems.map((item) => {
              // CẬP NHẬT: Sinh ra đường dẫn đầy đủ chứa cả workspaceId
              const itemPath = `/workspace/${workspaceId}/project/${id}${item.path ? `/${item.path}` : ''}`;
              
              // CẬP NHẬT: Logic xác định tab đang active cũng phải check khớp với đường dẫn mới
              const isActive = item.exact 
                ? location.pathname === `/workspace/${workspaceId}/project/${id}` || location.pathname === `/workspace/${workspaceId}/project/${id}/`
                : location.pathname.includes(item.path);

              return (
                <Link
                  key={item.path}
                  to={itemPath}
                  className={`w-full flex items-center gap-3 px-4 py-3 rounded-xl font-medium transition-all ${
                    isActive 
                    ? 'bg-[#FF7575]/10 text-[#FF7575] border border-[#FF7575]/20' 
                    : 'text-gray-400 hover:text-white hover:bg-white/5 border border-transparent'
                  }`}
                >
                  {item.icon}
                  {item.label}
                </Link>
              );
            })}
          </nav>
        </div>
      </aside>

      {/* ================= MAIN AREA (HEADER + OUTLET) ================= */}
      <main className="flex-1 flex flex-col h-full overflow-hidden relative">
        <div className="absolute inset-0 z-0 opacity-[0.03] pointer-events-none" style={{ backgroundImage: 'radial-gradient(#ffffff 1px, transparent 1px)', backgroundSize: '32px 32px' }}></div>
        
        <header className="h-20 bg-[#0A101D]/80 backdrop-blur-md border-b border-white/5 px-8 flex items-center justify-between shrink-0 z-20">
          <div>
            <h1 className="text-xl font-bold text-white hidden sm:block">Dashboard Tổng quan</h1>
          </div>
          <div className="flex items-center gap-4">
            <button className="hidden lg:flex items-center gap-2 px-4 py-2 bg-[#151B2B] border border-white/10 rounded-lg text-sm text-gray-300 hover:text-white transition-colors">
              <Calendar size={16} /> 7 ngày qua
            </button>
            <button className="hidden xl:flex items-center gap-2 px-4 py-2 bg-[#151B2B] border border-white/10 rounded-lg text-sm text-gray-300 hover:text-white transition-colors">
              <GitCompareArrows size={16} /> So sánh
            </button>
            <div className="hidden md:block w-px h-8 bg-white/10 mx-2"></div>
            
            {/* Export Menu */}
            <div className="relative">
              <button 
                onClick={() => setIsExportOpen(!isExportOpen)} 
                onBlur={() => setTimeout(() => setIsExportOpen(false), 200)}
                className="flex items-center gap-2 px-4 py-2 bg-blue-600/10 text-blue-400 border border-blue-600/20 rounded-lg text-sm font-semibold hover:bg-blue-600 hover:text-white transition-all"
              >
                <Download size={16} /> <span className="hidden sm:inline">Xuất báo cáo</span>
              </button>
              {isExportOpen && (
                <div className="absolute top-full right-0 mt-2 w-56 bg-[#151B2B] border border-white/10 rounded-xl shadow-2xl py-2 z-50">
                  <button className="w-full flex items-center gap-3 px-4 py-2.5 text-sm text-gray-300 hover:text-white hover:bg-white/5"><FileSpreadsheet size={16} className="text-green-400" /> Export Raw CSV</button>
                  <button className="w-full flex items-center gap-3 px-4 py-2.5 text-sm text-gray-300 hover:text-white hover:bg-white/5"><FileText size={16} className="text-red-400" /> Export PDF</button>
                </div>
              )}
            </div>

            {/* Noti */}
            <div className="relative">
              <button 
                onClick={() => setIsNotifOpen(!isNotifOpen)} 
                onBlur={() => setTimeout(() => setIsNotifOpen(false), 200)}
                className="p-2 text-gray-400 hover:text-white relative"
              >
                <Bell size={22} /><span className="absolute top-1.5 right-1.5 w-2 h-2 bg-[#FF7575] rounded-full animate-pulse"></span>
              </button>
              {isNotifOpen && (
                <div className="absolute top-full right-0 mt-2 w-80 bg-[#151B2B] border border-white/10 rounded-xl shadow-2xl p-4 z-50">
                  <p className="text-sm text-gray-400">Không có thông báo mới.</p>
                </div>
              )}
            </div>
          </div>
        </header>

        {/* Nơi hiển thị các trang con (Overview, Mentions, Sentiment...) */}
        <div className="flex-1 overflow-y-auto p-8 relative z-10">
          <Outlet />
        </div>
      </main>
    </div>
  );
};

export default ProjectLayout;