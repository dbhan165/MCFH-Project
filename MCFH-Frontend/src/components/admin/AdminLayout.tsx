import type { ReactNode } from 'react';
import { Link, useLocation } from 'react-router-dom';
import {
  LayoutDashboard,
  Users,
  CreditCard,
  Server,
  Activity,
  Settings,
  User,
  LogOut,
  Search,
  Bell,
  Radio,
  HelpCircle,
} from 'lucide-react';

const navItems = [
  { label: 'Dashboard', icon: LayoutDashboard, href: '/admin/dashboard' },
  { label: 'User Management', icon: Users, href: '/admin/users' },
  { label: 'Subscription Plans', icon: CreditCard, href: '/admin/subscriptions' },
  { label: 'Proxy Management', icon: Server, href: '/admin/proxies' },
  { label: 'Scraping Monitor', icon: Activity, href: '/admin/scraping' },
  { label: 'System Settings', icon: Settings, href: '/admin/settings' },
];

interface AdminLayoutProps {
  children: ReactNode;
  searchPlaceholder?: string;
  adminName?: string;
  adminRole?: string;
}

const AdminLayout = ({
  children,
  searchPlaceholder = 'Search analytics or users...',
  adminName,
  adminRole,
}: AdminLayoutProps) => {
  const location = useLocation();

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
                className={`flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors border-l-4 ${
                  isActive
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
            className="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium text-[#6b7280] hover:bg-gray-50 hover:text-[#111827] transition-colors"
          >
            <User className="w-5 h-5" />
            Profile
          </Link>
          <Link
            to="/login"
            className="flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium text-[#6b7280] hover:bg-gray-50 hover:text-[#111827] transition-colors"
          >
            <LogOut className="w-5 h-5" />
            Sign Out
          </Link>
        </div>
      </aside>

      <div className="flex-1 flex flex-col min-w-0">
        <header className="h-16 bg-white border-b border-gray-200 flex items-center justify-between px-6 shrink-0">
          <div className="flex-1 max-w-xl mx-auto">
            <div className="relative">
              <Search className="absolute left-4 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
              <input
                type="text"
                placeholder={searchPlaceholder}
                className="w-full bg-gray-50 border border-gray-200 rounded-full pl-11 pr-4 py-2.5 text-sm text-[#111827] placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500/20 focus:border-blue-400 transition-all"
              />
            </div>
          </div>

          <div className="flex items-center gap-4 ml-6">
            <button className="relative p-2 text-gray-500 hover:text-[#111827] hover:bg-gray-50 rounded-lg transition-colors">
              <Bell className="w-5 h-5" />
              <span className="absolute top-1.5 right-1.5 w-2 h-2 bg-[#ef4444] rounded-full" />
            </button>
            <button className="p-2 text-gray-500 hover:text-[#111827] hover:bg-gray-50 rounded-lg transition-colors">
              <Radio className="w-5 h-5" />
            </button>
            <button className="p-2 text-gray-500 hover:text-[#111827] hover:bg-gray-50 rounded-lg transition-colors">
              <HelpCircle className="w-5 h-5" />
            </button>
            {(adminName || adminRole) && (
              <div className="hidden md:block text-right mr-1">
                {adminName && (
                  <p className="text-sm font-semibold text-[#111827] leading-tight">{adminName}</p>
                )}
                {adminRole && (
                  <p className="text-[10px] font-bold text-[#6b7280] tracking-wider">{adminRole}</p>
                )}
              </div>
            )}
            <div className="w-9 h-9 rounded-full overflow-hidden border-2 border-gray-200 ml-1">
              <img
                src="https://i.pravatar.cc/150?img=32"
                alt="Admin avatar"
                className="w-full h-full object-cover"
              />
            </div>
          </div>
        </header>

        <main className="flex-1 overflow-y-auto p-6 lg:p-8">{children}</main>
      </div>
    </div>
  );
};

export default AdminLayout;
