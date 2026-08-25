import { useEffect, useRef, useState, type ReactNode } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import {
  LayoutDashboard,
  Users,
  Server,
  Globe,
  Cookie,
  Clock,
  Settings,
  User,
  LogOut,
  ChevronDown,
  ShieldCheck,
  Package,
} from 'lucide-react';
import NotificationBell from '../notifications/NotificationBell';
import { loadProfileFromStorage, clearAuthSession, getAvatarFallback } from '../../utils/authStorage';

const navItems = [
  { label: 'Dashboard', icon: LayoutDashboard, href: '/admin/dashboard' },
  { label: 'Quản lý người dùng', icon: Users, href: '/admin/users' },
  { label: 'Quản lý Proxy', icon: Server, href: '/admin/proxies' },
  { label: 'Nguồn Facebook', icon: Globe, href: '/admin/fb-sources' },
  { label: 'Gói Cào', icon: Package, href: '/admin/scrape-packages' },
  { label: 'Cookie Nền tảng', icon: Cookie, href: '/admin/cookies' },
  { label: 'Nhật ký hệ thống', icon: Clock, href: '/admin/audit-logs' },
  { label: 'Cài đặt hệ thống', icon: Settings, href: '/admin/settings' },
];

interface AdminLayoutProps {
  children: ReactNode;
  searchPlaceholder?: string;
  adminName?: string;
  adminRole?: string;
  /**
   * Nếu true (mặc định), <main> là scroll container cho page con (hành vi cũ).
   * Nếu false, <main> overflow-hidden — page con tự quản lý scroll bên trong
   * (dùng cho layout 2 cột có sticky sidebar như SystemSettings).
   */
  disableMainScroll?: boolean;
}

const AdminLayout = ({
  children,
  adminName,
  adminRole,
  disableMainScroll = false,
}: AdminLayoutProps) => {
  const location = useLocation();
  const navigate = useNavigate();
  const [isProfileOpen, setIsProfileOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  const cachedProfile = loadProfileFromStorage();
  const displayName = adminName || cachedProfile?.fullName || 'Trường Học';
  const displayEmail = cachedProfile?.email || 'admin@mcfh.com';
  const displayRole = adminRole || cachedProfile?.role || 'Quản trị viên hệ thống';
  const avatarSrc = cachedProfile?.avatarUrl || getAvatarFallback(displayName);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsProfileOpen(false);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const handleSignOut = () => {
    clearAuthSession();
    navigate('/login');
  };

  return (
    <div className="min-h-screen flex bg-[#f9fafb] text-[#111827] font-sans">
      <aside className="w-64 bg-white border-r border-gray-200 flex flex-col shrink-0">
        <div className="px-6 py-6 border-b border-gray-100">
          <h1 className="text-xl font-bold tracking-tight">MCFH</h1>
          <p className="text-sm text-[#6b7280] mt-0.5">Admin Portal</p>
        </div>

        <nav className="flex-1 py-4 px-3 space-y-1">
          {navItems.map((item) => {
            const isActive = location.pathname === item.href;
            return (
              <Link
                key={item.label}
                to={item.href}
                className={`flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors border-l-4 ${isActive
                  ? 'bg-red-50 text-[#ef4444] border-[#ef4444]'
                  : 'text-[#6b7280] border-transparent hover:bg-gray-50 hover:text-[#111827]'
                  }`}
              >
                <item.icon className="w-5 h-5 shrink-0" />
                {item.label}
              </Link>
            );
          })}
        </nav>

        <div className="px-3 py-4 border-t border-gray-100 space-y-1">
          <Link
            to="/admin/profile"
            className={`flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors ${location.pathname === '/admin/profile'
              ? 'bg-red-50 text-[#ef4444]'
              : 'text-[#6b7280] hover:bg-gray-50 hover:text-[#111827]'
              }`}
          >
            <User className="w-5 h-5" />
            Hồ sơ cá nhân
          </Link>
          <button
            onClick={handleSignOut}
            type="button"
            className="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium text-[#6b7280] hover:bg-gray-50 hover:text-[#111827] transition-colors text-left cursor-pointer"
          >
            <LogOut className="w-5 h-5" />
            Đăng xuất
          </button>
        </div>
      </aside>

      <div className="flex-1 flex flex-col min-w-0">
        <header className="h-16 bg-white border-b border-gray-200 flex items-center justify-between px-6 shrink-0">
          <div>
            <h2 className="text-sm font-semibold text-gray-700">Hệ Thống Quản Trị Admin</h2>
          </div>

          <div className="flex items-center gap-4">
            <NotificationBell theme="light" isAdmin={true} />

            <div className="relative" ref={dropdownRef}>
              <button
                type="button"
                onClick={() => setIsProfileOpen(!isProfileOpen)}
                className="flex items-center gap-2.5 p-1 rounded-xl hover:bg-gray-100 transition-colors focus:outline-none cursor-pointer group"
                aria-label="Admin Profile Menu"
              >
                <div className="w-9 h-9 rounded-full overflow-hidden border-2 border-red-400/80 shadow-xs shrink-0 group-hover:scale-105 transition-transform">
                  <img
                    src={avatarSrc}
                    alt={displayName}
                    className="w-full h-full object-cover"
                    onError={(e) => {
                      (e.target as HTMLImageElement).src = getAvatarFallback(displayName);
                    }}
                  />
                </div>
                <div className="hidden md:block text-left">
                  <p className="text-xs font-bold text-gray-900 leading-tight group-hover:text-red-600 transition-colors">{displayName}</p>
                  <p className="text-[10px] font-semibold text-gray-400 tracking-wide uppercase">{displayRole}</p>
                </div>
                <ChevronDown className={`w-3.5 h-3.5 text-gray-400 transition-transform ${isProfileOpen ? 'rotate-180' : ''}`} />
              </button>

              {isProfileOpen && (
                <div className="absolute right-0 top-full mt-2 w-72 bg-white border border-gray-200 rounded-2xl shadow-xl z-50 overflow-hidden animate-in fade-in slide-in-from-top-2 duration-150">
                  <div className="p-4 bg-gradient-to-br from-red-50/50 to-orange-50/30 border-b border-gray-100 flex items-center gap-3">
                    <div className="w-12 h-12 rounded-full overflow-hidden border-2 border-red-300 shrink-0 shadow-xs">
                      <img src={avatarSrc} alt={displayName} className="w-full h-full object-cover" />
                    </div>
                    <div className="min-w-0">
                      <h4 className="text-sm font-bold text-gray-900 truncate">{displayName}</h4>
                      <p className="text-xs text-gray-500 truncate">{displayEmail}</p>
                      <span className="inline-flex items-center gap-1 text-[10px] font-bold text-red-600 bg-red-100/80 px-2 py-0.5 rounded-full mt-1">
                        <ShieldCheck className="w-3 h-3" />
                        System Admin
                      </span>
                    </div>
                  </div>

                  <div className="p-2 space-y-1">
                    <Link
                      to="/admin/profile"
                      onClick={() => setIsProfileOpen(false)}
                      className="flex items-center gap-3 px-3 py-2.5 rounded-xl text-xs font-semibold text-gray-800 hover:bg-gray-50 hover:text-red-600 transition-colors"
                    >
                      <div className="w-7 h-7 rounded-lg bg-red-50 text-[#ef4444] flex items-center justify-center">
                        <User className="w-4 h-4" />
                      </div>
                      <div>
                        <p className="font-bold">Chi tiết hồ sơ Admin</p>
                        <p className="text-[10px] text-gray-400 font-normal">Cập nhật họ tên, SĐT, avatar</p>
                      </div>
                    </Link>

                    <Link
                      to="/admin/settings"
                      onClick={() => setIsProfileOpen(false)}
                      className="flex items-center gap-3 px-3 py-2.5 rounded-xl text-xs font-semibold text-gray-800 hover:bg-gray-50 transition-colors"
                    >
                      <div className="w-7 h-7 rounded-lg bg-gray-100 text-gray-600 flex items-center justify-center">
                        <Settings className="w-4 h-4" />
                      </div>
                      <div>
                        <p className="font-bold">Cài đặt hệ thống</p>
                        <p className="text-[10px] text-gray-400 font-normal">Thiết lập tham số Admin</p>
                      </div>
                    </Link>
                  </div>

                  <div className="p-2 border-t border-gray-100 bg-gray-50/50">
                    <button
                      type="button"
                      onClick={handleSignOut}
                      className="w-full flex items-center gap-2.5 px-3 py-2 rounded-xl text-xs font-bold text-red-600 hover:bg-red-50 transition-colors cursor-pointer"
                    >
                      <LogOut className="w-4 h-4" />
                      Đăng xuất tài khoản
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>
        </header>

        <main className={`flex-1 min-h-0 p-6 lg:p-8 ${disableMainScroll ? 'overflow-hidden' : 'overflow-y-auto'}`}>{children}</main>
      </div>
    </div>
  );
};

export default AdminLayout;
