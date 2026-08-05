import type { ReactNode } from 'react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import {
  ClipboardList,
  LineChart,
  Settings,
  User,
  LogOut,
  Search,
  HelpCircle,
  LayoutDashboard,
  Archive,
} from 'lucide-react';
import NotificationBell from '../notifications/NotificationBell';
import { clearAuthSession, getAvatarFallback, loadProfileFromStorage } from '../../utils/authStorage';

const navItems = [
  { label: 'Dashboard', icon: LayoutDashboard, href: '/reporter/dashboard' },
  { label: 'Tasks', icon: ClipboardList, href: '/reporter/tasks' },
  { label: 'My Performance', icon: LineChart, href: '/reporter/performance' },
  { label: 'Archive', icon: Archive, href: '/reporter/archive' },
  { label: 'Settings', icon: Settings, href: '/reporter/settings' },
];

interface ReporterLayoutProps {
  children: ReactNode;
  searchPlaceholder?: string;
  /** Giữ prop cũ để không phá các page đang truyền activeTopNav */
  activeTopNav?: 'dashboard' | 'reports' | 'archive' | 'settings' | 'performance';
}

const ReporterLayout = ({
  children,
  searchPlaceholder = 'Tìm kiếm mã đơn hoặc tên...',
}: ReporterLayoutProps) => {
  const location = useLocation();
  const navigate = useNavigate();
  const profile = loadProfileFromStorage();
  const displayName = profile?.fullName?.trim() || 'Reporter';
  const displayRole = profile?.role || 'Reporter';
  const avatarSrc = profile?.avatarUrl || getAvatarFallback(displayName);

  const handleSignOut = () => {
    clearAuthSession();
    navigate('/login');
  };

  return (
    <div className="min-h-screen flex bg-[#faf7f5] text-[#111827] font-sans">
      <aside className="w-64 bg-white border-r border-stone-200 flex flex-col shrink-0">
        <div className="px-6 py-6 border-b border-stone-100">
          <h1 className="text-xl font-bold tracking-tight">MCFH</h1>
          <p className="text-sm text-[#78716c] mt-0.5">Reporter Portal</p>
        </div>

        <nav className="flex-1 py-4 px-3 space-y-1">
          {navItems.map((item) => {
            const isActive =
              location.pathname === item.href ||
              (item.href === '/reporter/tasks' && location.pathname.startsWith('/reporter/requests'));
            return (
              <Link
                key={item.label}
                to={item.href}
                className={`flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors border-l-4 ${
                  isActive
                    ? 'bg-rose-50 text-[#e11d48] border-[#e11d48]'
                    : 'text-[#78716c] border-transparent hover:bg-stone-50 hover:text-[#111827]'
                }`}
              >
                <item.icon className="w-5 h-5 shrink-0" />
                {item.label}
              </Link>
            );
          })}
        </nav>

        <div className="px-3 py-4 border-t border-stone-100 space-y-1">
          <Link
            to="/reporter/profile"
            className={`flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors ${
              location.pathname === '/reporter/profile'
                ? 'bg-rose-50 text-[#e11d48]'
                : 'text-[#78716c] hover:bg-stone-50 hover:text-[#111827]'
            }`}
          >
            <User className="w-5 h-5" />
            Profile
          </Link>
          <button
            type="button"
            onClick={handleSignOut}
            className="w-full flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium text-[#78716c] hover:bg-stone-50 hover:text-[#111827] transition-colors"
          >
            <LogOut className="w-5 h-5" />
            Sign Out
          </button>
        </div>
      </aside>

      <div className="flex-1 flex flex-col min-w-0">
        <header className="h-16 bg-white border-b border-stone-200 flex items-center justify-between px-6 shrink-0">
          <div className="flex-1 max-w-xl mx-auto">
            <div className="relative">
              <Search className="absolute left-4 top-1/2 -translate-y-1/2 w-4 h-4 text-stone-400" />
              <input
                type="text"
                placeholder={searchPlaceholder}
                className="w-full bg-stone-50 border border-stone-200 rounded-full pl-11 pr-4 py-2.5 text-sm text-[#111827] placeholder-stone-400 focus:outline-none focus:ring-2 focus:ring-rose-500/20 focus:border-rose-400 transition-all"
              />
            </div>
          </div>

          <div className="flex items-center gap-4 ml-6">
            <NotificationBell theme="light" />
            <button
              type="button"
              className="p-2 text-stone-500 hover:text-[#111827] hover:bg-stone-50 rounded-lg transition-colors"
            >
              <HelpCircle className="w-5 h-5" />
            </button>
            <div className="hidden md:block text-right mr-1">
              <p className="text-sm font-semibold text-[#111827] leading-tight">{displayName}</p>
              <p className="text-[10px] font-bold text-[#78716c] tracking-wider uppercase">{displayRole}</p>
            </div>
            <Link
              to="/reporter/profile"
              className="w-9 h-9 rounded-full overflow-hidden border-2 border-stone-200 ml-1 shrink-0"
            >
              <img src={avatarSrc} alt="Reporter avatar" className="w-full h-full object-cover" />
            </Link>
          </div>
        </header>

        <main className="flex-1 overflow-y-auto p-6 lg:p-8">{children}</main>
      </div>
    </div>
  );
};

export default ReporterLayout;
