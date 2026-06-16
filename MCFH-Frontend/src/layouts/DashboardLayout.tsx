import { useState, useRef, useEffect } from 'react';
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom';
import { 
  Building2, 
  UserCircle, 
  CreditCard, 
  LogOut,
  ChevronDown,
  ChevronRight,
  Plus
} from 'lucide-react';

const DashboardLayout = () => {
  const location = useLocation();
  const navigate = useNavigate();
  
  // State quản lý Dropdown
  const [isWorkspaceMenuOpen, setIsWorkspaceMenuOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  // Mock data danh sách Workspace của User
  const userWorkspaces = [
    { id: 1, name: 'Acma Agency', role: 'Owner' },
    { id: 2, name: 'MCFH Internal', role: 'Admin' },
    { id: 3, name: 'Startup XYZ', role: 'Member' },
  ];

  const bottomMenuItems = [
    { path: '/profile', icon: <UserCircle size={20} />, label: 'Hồ sơ cá nhân' },
    { path: '/subscription', icon: <CreditCard size={20} />, label: 'Gói cước (Billing)' },
  ];

  const handleLogout = () => {
    localStorage.removeItem('accessToken');
    navigate('/login');
  };

  // Click ra ngoài để đóng dropdown
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsWorkspaceMenuOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  return (
    <div className="flex h-screen w-full bg-[#050A15] text-white font-sans selection:bg-[#FF7575] selection:text-white overflow-hidden relative">
      
      {/* =========================================================
          SIDEBAR CẤP ĐỘ 1 (ĐIỀU HƯỚNG TỔ CHỨC)
          ========================================================= */}
      <aside className="w-64 bg-[#0A101D] border-r border-white/5 flex flex-col shrink-0 h-full relative z-30">
        
        {/* Logo & Brand */}
        <div className="h-20 shrink-0 flex items-center px-8 border-b border-white/5">
          <Link to="/workspaces" className="flex items-center gap-3">
            <div className="w-8 h-8 bg-gradient-to-br from-[#FF7575] to-orange-500 rounded-lg flex items-center justify-center font-bold text-white shadow-[0_0_15px_rgba(255,117,117,0.3)]">
              M
            </div>
            <span className="font-extrabold text-xl tracking-wider text-white">MCFH</span>
          </Link>
        </div>

        {/* Main Menu (Quản lý Workspace) */}
        <div className="p-4 flex-1 flex flex-col gap-4 overflow-y-auto">
          
          <div className="mt-4" ref={dropdownRef}>
            <p className="px-4 text-xs font-bold text-gray-500 mb-3 tracking-wider uppercase">Tổ chức của bạn</p>
            
            {/* Nút Xổ Workspace */}
            <button 
              onClick={() => setIsWorkspaceMenuOpen(!isWorkspaceMenuOpen)}
              className={`w-full flex items-center justify-between p-4 rounded-xl border transition-all ${
                isWorkspaceMenuOpen || location.pathname.includes('/workspaces')
                ? 'bg-[#FF7575]/10 border-[#FF7575]/30 shadow-[0_4px_20px_rgba(255,117,117,0.1)]' 
                : 'bg-[#151B2B] border-white/10 hover:border-white/20 hover:bg-[#1A2235]'
              }`}
            >
              <div className="flex items-center gap-3">
                <div className={`w-10 h-10 rounded-lg flex items-center justify-center ${
                  isWorkspaceMenuOpen || location.pathname.includes('/workspaces')
                  ? 'bg-[#FF7575] text-white shadow-lg' 
                  : 'bg-white/5 text-gray-400'
                }`}>
                  <Building2 size={20} />
                </div>
                <div className="text-left">
                  <div className="font-bold text-sm text-white">Không gian làm việc</div>
                  <div className="text-xs text-gray-500">Quản lý tổ chức</div>
                </div>
              </div>
              <ChevronDown 
                size={18} 
                className={`text-gray-400 transition-transform duration-300 ${isWorkspaceMenuOpen ? 'rotate-180 text-[#FF7575]' : ''}`} 
              />
            </button>

            {/* Khung Xổ xuống (Dropdown Menu) */}
            <div className={`grid transition-all duration-300 ease-in-out ${isWorkspaceMenuOpen ? 'grid-rows-[1fr] opacity-100 mt-2' : 'grid-rows-[0fr] opacity-0'}`}>
              <div className="overflow-hidden">
                <div className="bg-[#151B2B] border border-white/5 rounded-xl p-2 flex flex-col gap-1 shadow-2xl">
                  
                  {/* Nút Xem tất cả */}
                  <Link 
                    to="/workspaces"
                    onClick={() => setIsWorkspaceMenuOpen(false)}
                    className="flex items-center justify-between px-3 py-2.5 rounded-lg text-sm text-gray-400 hover:text-white hover:bg-white/5 transition-colors"
                  >
                    <span className="font-semibold">Xem tất cả tổ chức</span>
                    <ChevronRight size={16} />
                  </Link>

                  <div className="h-px w-full bg-white/5 my-1"></div>

                  {/* Danh sách các Workspace */}
                  <div className="max-h-48 overflow-y-auto custom-scrollbar">
                    {userWorkspaces.map(ws => (
                      <Link
                        key={ws.id}
                        to={`/workspace/${ws.id}/projects`}
                        onClick={() => setIsWorkspaceMenuOpen(false)}
                        className="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm group transition-all hover:bg-white/5"
                      >
                        <div className="w-6 h-6 rounded bg-[#0A101D] border border-white/10 flex items-center justify-center text-xs font-bold text-gray-400 group-hover:text-[#FF7575] group-hover:border-[#FF7575]/30 shrink-0">
                          {ws.name.charAt(0)}
                        </div>
                        <span className="font-semibold text-gray-300 group-hover:text-white truncate">{ws.name}</span>
                      </Link>
                    ))}
                  </div>

                  <div className="h-px w-full bg-white/5 my-1"></div>

                  {/* Nút Tạo mới trỏ sang trang CreateWorkspace */}
                  <Link 
                    to="/create-workspace"
                    onClick={() => setIsWorkspaceMenuOpen(false)} // Đóng dropdown
                    className="flex items-center gap-2 px-3 py-2.5 rounded-lg text-sm text-[#00B4D8] hover:bg-[#00B4D8]/10 transition-colors font-semibold"
                  >
                    <Plus size={16} /> Tạo Workspace mới
                  </Link>

                </div>
              </div>
            </div>

          </div>
        </div>

        {/* Bottom Menu (Cá nhân) */}
        <div className="p-4 border-t border-white/5 flex flex-col gap-1 shrink-0 bg-[#0A101D]">
          <p className="px-4 text-xs font-bold text-gray-500 mb-2 tracking-wider uppercase">Cá nhân</p>
          
          {bottomMenuItems.map((item) => {
            const isActive = location.pathname.includes(item.path);
            return (
              <Link
                key={item.path}
                to={item.path}
                className={`flex items-center gap-3 px-4 py-3 rounded-xl font-medium transition-all ${
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

          <button 
            onClick={handleLogout}
            className="flex items-center gap-3 px-4 py-3 rounded-xl font-medium text-gray-400 hover:text-red-400 hover:bg-red-400/10 transition-colors mt-2"
          >
            <LogOut size={20} />
            Đăng xuất
          </button>
        </div>
      </aside>

      {/* =========================================================
          KHU VỰC HIỂN THỊ NỘI DUNG CHÍNH
          ========================================================= */}
      <main className="flex-1 flex flex-col h-full overflow-hidden relative z-10">
        <div className="absolute inset-0 z-0 opacity-[0.02] pointer-events-none" style={{ backgroundImage: 'radial-gradient(#ffffff 1px, transparent 1px)', backgroundSize: '32px 32px' }}></div>
        
        <div className="relative z-10 w-full h-full overflow-y-auto bg-[#050A15]">
          <Outlet />
        </div>
      </main>

    </div>
  );
};

export default DashboardLayout;