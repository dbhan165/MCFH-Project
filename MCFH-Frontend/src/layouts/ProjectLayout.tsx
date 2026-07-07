import { useEffect, useState } from 'react';
import { Link, Outlet, useParams, useLocation } from 'react-router-dom';
import { 
  BarChart2, PieChart, LayoutDashboard, Share2, MessageCircle, 
  Download, FileText,
  ArrowLeft, Users, Loader2
} from 'lucide-react';
import McfhLogo from '../components/brand/McfhLogo';
import NotificationBell from '../components/notifications/NotificationBell';
import { projectApi } from '../api/projectApi';

const ProjectLayout = () => {
  const { workspaceId, id } = useParams();
  const wid = Number(workspaceId);
  const projectId = Number(id);
  const location = useLocation();
  const [projectName, setProjectName] = useState('');
  const [projectKeyword, setProjectKeyword] = useState<string | null>(null);
  const [isLoadingProject, setIsLoadingProject] = useState(true);

  useEffect(() => {
    if (!wid || !projectId || Number.isNaN(wid) || Number.isNaN(projectId)) {
      setIsLoadingProject(false);
      return;
    }

    let cancelled = false;
    setIsLoadingProject(true);

    projectApi
      .getById(wid, projectId)
      .then((project) => {
        if (cancelled) return;
        setProjectName(project.name);
        setProjectKeyword(project.searchQuery);
      })
      .catch(() => {
        if (!cancelled) {
          setProjectName('');
          setProjectKeyword(null);
        }
      })
      .finally(() => {
        if (!cancelled) setIsLoadingProject(false);
      });

    return () => {
      cancelled = true;
    };
  }, [wid, projectId]);

  const menuItems = [
    { path: '', icon: <LayoutDashboard size={20} />, label: 'Tổng quan', exact: true },
    { path: 'mentions', icon: <MessageCircle size={20} />, label: 'Lượt nhắc' },
    { path: 'sentiment', icon: <PieChart size={20} />, label: 'Cảm xúc' },
    { path: 'influencers', icon: <Users size={20} />, label: 'KOL & Người ảnh hưởng' },
    { path: 'channel', icon: <Share2 size={20} />, label: 'So sánh kênh' },
    { path: 'aspect', icon: <BarChart2 size={20} />, label: 'Khía cạnh' },
    { path: 'reports', icon: <FileText size={20} />, label: 'Báo cáo' },
  ];

  const activePage = menuItems.find((item) =>
    item.exact
      ? location.pathname === `/workspace/${workspaceId}/project/${id}` ||
        location.pathname === `/workspace/${workspaceId}/project/${id}/`
      : location.pathname.includes(item.path)
  );

  return (
    <div className="h-screen w-full bg-[#050A15] text-white font-sans flex flex-col md:flex-row selection:bg-[#FF7575] selection:text-white overflow-hidden">
      
      {/* ================= SIDEBAR ================= */}
      <aside className="w-full md:w-72 bg-[#0A101D] border-r border-white/5 flex flex-col shrink-0 h-full">
        <div className="h-20 flex items-center justify-between px-6 border-b border-white/5 shrink-0 gap-3">
          <Link to={`/workspace/${workspaceId}/projects`} className="flex items-center gap-2 text-gray-400 hover:text-white transition-colors text-sm font-semibold min-w-0">
            <ArrowLeft size={18} className="shrink-0" />
            <span className="truncate">Quay lại Hệ thống</span>
          </Link>
          <McfhLogo linkTo="/workspaces" size={28} showText={false} className="shrink-0 opacity-80 hover:opacity-100 transition-opacity" />
        </div>
        
        <div className="p-6 flex-1 overflow-y-auto">
          <div className="mb-6">
            <div className="text-xs font-bold text-[#00B4D8] uppercase tracking-wider mb-2">Đang phân tích</div>
            {isLoadingProject ? (
              <div className="flex items-center gap-2 text-gray-400">
                <Loader2 size={18} className="animate-spin" />
                <span className="text-sm">Đang tải dự án...</span>
              </div>
            ) : (
              <>
                <h2 className="text-xl font-bold leading-tight line-clamp-2">
                  {projectName || `Dự án #${id}`}
                </h2>
                {projectKeyword ? (
                  <p className="text-xs text-gray-500 mt-1.5 truncate">Từ khóa: {projectKeyword}</p>
                ) : null}
              </>
            )}
          </div>

          <p className="text-xs font-bold text-gray-500 mb-4 tracking-wider uppercase">Bảng điều khiển</p>
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
            <h1 className="text-xl font-bold text-white hidden sm:block">{activePage?.label ?? 'Bảng điều khiển'}</h1>
          </div>
          <div className="flex items-center gap-4">
            <Link
              to={`/workspace/${workspaceId}/project/${id}/reports`}
              className="flex items-center gap-2 px-4 py-2 bg-blue-600/10 text-blue-400 border border-blue-600/20 rounded-lg text-sm font-semibold hover:bg-blue-600 hover:text-white transition-all"
            >
              <Download size={16} /> <span className="hidden sm:inline">Báo cáo</span>
            </Link>

            <NotificationBell />
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