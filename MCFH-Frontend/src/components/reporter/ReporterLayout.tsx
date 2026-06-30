import type { ReactNode } from 'react';
import { Link, useLocation } from 'react-router-dom';
import {
  ClipboardList,
  LineChart,
  Settings,
  Plus,
  Search,
  Calendar,
} from 'lucide-react';

const sidebarItems = [
  { label: 'Tasks', icon: ClipboardList, href: '/reporter/tasks' },
  { label: 'My Performance', icon: LineChart, href: '/reporter/performance' },
  { label: 'Settings', icon: Settings, href: '/reporter/settings' },
];

const topNavItems = [
  { label: 'Dashboard', href: '/reporter/dashboard' },
  { label: 'Reports', href: '/reporter/tasks' },
  { label: 'Archive', href: '/reporter/archive' },
];

interface ReporterLayoutProps {
  children: ReactNode;
  activeTopNav?: 'dashboard' | 'reports' | 'archive' | 'settings' | 'performance';
}

const ReporterLayout = ({ children, activeTopNav = 'reports' }: ReporterLayoutProps) => {
  const location = useLocation();

  const isTopNavActive = (href: string, key: string) => {
    if (activeTopNav) {
      if (key === 'dashboard') return activeTopNav === 'dashboard';
      if (key === 'reports') return activeTopNav === 'reports';
      if (key === 'archive') return activeTopNav === 'archive';
    }
    return location.pathname === href;
  };

  return (
    <div className="min-h-screen flex bg-[#f8fafc] text-[#0f172a] font-sans">
      <aside className="w-64 bg-white border-r border-gray-200 flex flex-col shrink-0">
        <div className="px-5 py-6 border-b border-gray-100">
          <div className="flex items-center gap-3">
            <img
              src="https://i.pravatar.cc/150?img=33"
              alt="Analyst avatar"
              className="w-11 h-11 rounded-full object-cover border-2 border-gray-100"
            />
            <div>
              <p className="text-sm font-semibold text-[#0f172a] leading-tight">Trần Chuyên Viên</p>
              <p className="text-xs text-[#64748b] mt-0.5">Enterprise Analyst</p>
            </div>
          </div>
        </div>

        <nav className="flex-1 py-4 px-3 space-y-1">
          {sidebarItems.map((item) => {
            const isActive = location.pathname === item.href;
            return (
              <Link
                key={item.label}
                to={item.href}
                className={`flex items-center gap-3 px-3 py-2.5 rounded-lg text-sm font-medium transition-colors border-l-4 ${
                  isActive
                    ? 'bg-teal-50 text-teal-700 border-teal-500'
                    : 'text-[#64748b] border-transparent hover:bg-gray-50 hover:text-[#0f172a]'
                }`}
              >
                <item.icon className="w-5 h-5 shrink-0" />
                {item.label}
              </Link>
            );
          })}
        </nav>

        <div className="p-4 border-t border-gray-100">
          <button className="w-full flex items-center justify-center gap-2 py-3 bg-teal-600 hover:bg-teal-700 text-white rounded-lg text-sm font-semibold transition-colors shadow-sm">
            <Plus className="w-4 h-4" />
            New Report
          </button>
        </div>
      </aside>

      <div className="flex-1 flex flex-col min-w-0">
        <header className="h-16 bg-white border-b border-gray-200 flex items-center justify-between px-6 shrink-0">
          <h1 className="text-lg font-bold text-teal-800 tracking-tight shrink-0">Analyst Workspace</h1>

          <nav className="hidden md:flex items-center gap-8 absolute left-1/2 -translate-x-1/2">
            {topNavItems.map((item, index) => {
              const keys = ['dashboard', 'reports', 'archive'] as const;
              const isActive = isTopNavActive(item.href, keys[index]);
              return (
                <Link
                  key={item.label}
                  to={item.href}
                  className={`text-sm font-medium pb-0.5 transition-colors ${
                    isActive
                      ? 'text-teal-700 border-b-2 border-teal-600'
                      : 'text-[#64748b] hover:text-[#0f172a]'
                  }`}
                >
                  {item.label}
                </Link>
              );
            })}
          </nav>

          <div className="flex items-center gap-3 shrink-0">
            <div className="relative hidden sm:block">
              <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
              <input
                type="text"
                placeholder="Tìm kiếm mã đơn hoặc tên..."
                className="w-56 lg:w-72 bg-gray-50 border border-gray-200 rounded-lg pl-10 pr-4 py-2 text-sm text-[#0f172a] placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-teal-500/20 focus:border-teal-400 transition-all"
              />
            </div>
            <button className="p-2.5 text-gray-500 hover:text-teal-700 hover:bg-teal-50 border border-gray-200 rounded-lg transition-colors">
              <Calendar className="w-4 h-4" />
            </button>
          </div>
        </header>

        <main className="flex-1 overflow-y-auto p-6 lg:p-8">{children}</main>
      </div>
    </div>
  );
};

export default ReporterLayout;
