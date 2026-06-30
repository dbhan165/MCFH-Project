import type { ElementType } from 'react';
import { useCallback, useEffect, useState } from 'react';
import {
  Calendar,
  Download,
  DollarSign,
  UserPlus,
  ShieldCheck,
  Server,
  AlertTriangle,
  TrendingUp,
} from 'lucide-react';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  PieChart,
  Pie,
  Cell,
} from 'recharts';
import AdminLayout from '../../components/admin/AdminLayout';
import { adminApi, type AdminDashboard as AdminDashboardData } from '../../api/portalApi';

const revenueGrowthData = [
  { month: 'Jan', revenue: 82000, users: 980 },
  { month: 'Feb', revenue: 88000, users: 1050 },
  { month: 'Mar', revenue: 91000, users: 1120 },
  { month: 'Apr', revenue: 98000, users: 1180 },
  { month: 'May', revenue: 105000, users: 1280 },
  { month: 'Jun', revenue: 112000, users: 1350 },
  { month: 'Jul', revenue: 118000, users: 1420 },
  { month: 'Aug', revenue: 124000, users: 1450 },
];

const subscriptionData = [
  { name: 'Enterprise', value: 192, color: '#111827' },
  { name: 'Premium', value: 96, color: '#ef4444' },
  { name: 'Basic', value: 32, color: '#3b82f6' },
];

const recentJobs = [
  { id: 'JOB-9402', status: 'RUNNING' as const, progress: 75 },
  { id: 'JOB-9401', status: 'COMPLETED' as const, progress: 100 },
  { id: 'JOB-9398', status: 'FAILED' as const, progress: 40 },
  { id: 'JOB-9395', status: 'COMPLETED' as const, progress: 100 },
  { id: 'JOB-9392', status: 'RUNNING' as const, progress: 62 },
];

const proxyRegions = [
  { name: 'US-EAST Node Cluster', health: 99.8 },
  { name: 'EU-WEST Edge Proxies', health: 98.2 },
  { name: 'APAC Residential Pool', health: 97.5 },
];

const statusStyles = {
  RUNNING: 'bg-blue-50 text-blue-600 border-blue-100',
  COMPLETED: 'bg-slate-800 text-white border-slate-800',
  FAILED: 'bg-red-50 text-red-500 border-red-100',
};

const progressBarColors = {
  RUNNING: 'bg-blue-500',
  COMPLETED: 'bg-slate-800',
  FAILED: 'bg-red-400',
};

const AdminDashboard = () => {
  const totalSubs = subscriptionData.reduce((sum, item) => sum + item.value, 0);
  const [stats, setStats] = useState<AdminDashboardData | null>(null);

  const load = useCallback(async () => {
    try {
      setStats(await adminApi.getDashboard());
    } catch {
      setStats(null);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  return (
    <AdminLayout>
      <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 mb-8">
        <div>
          <h2 className="text-2xl lg:text-3xl font-bold tracking-tight">Dashboard Overview</h2>
          <p className="text-[#6b7280] text-sm mt-1">
            Real-time performance metrics and system health monitoring.
          </p>
        </div>
        <div className="flex items-center gap-3 shrink-0">
          <button className="flex items-center gap-2 px-4 py-2.5 bg-white border border-gray-200 rounded-lg text-sm font-medium text-[#111827] hover:bg-gray-50 transition-colors">
            <Calendar className="w-4 h-4 text-gray-500" />
            Last 30 Days
          </button>
          <button className="flex items-center gap-2 px-4 py-2.5 bg-[#ef4444] hover:bg-red-600 text-white rounded-lg text-sm font-semibold transition-colors shadow-sm">
            <Download className="w-4 h-4" />
            Export Report
          </button>
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-5 gap-4 mb-6">
        <MetricCard
          icon={UserPlus}
          iconBg="bg-blue-50"
          iconColor="text-blue-500"
          label="Tổng Users"
          value={stats ? String(stats.totalUsers) : '—'}
        />
        <MetricCard
          icon={ShieldCheck}
          iconBg="bg-gray-50"
          iconColor="text-gray-600"
          label="Reporters"
          value={stats ? String(stats.totalReporters) : '—'}
        />
        <MetricCard
          icon={Server}
          iconBg="bg-gray-50"
          iconColor="text-gray-600"
          label="Dự án"
          value={stats ? String(stats.totalProjects) : '—'}
          valueColor="text-[#3b82f6]"
        />
        <MetricCard
          icon={DollarSign}
          iconBg="bg-blue-50"
          iconColor="text-blue-500"
          label="Mentions"
          value={stats ? stats.totalMentions.toLocaleString('vi-VN') : '—'}
        />
        <MetricCard
          icon={AlertTriangle}
          iconBg="bg-red-50"
          iconColor="text-[#ef4444]"
          label="Bespoke chờ"
          value={stats ? String(stats.pendingBespoke) : '—'}
          valueColor="text-[#ef4444]"
        />
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6 mb-6">
        <div className="bg-white border border-gray-200 rounded-xl p-6">
          <div className="flex items-center justify-between mb-6">
            <h3 className="text-base font-semibold">Revenue & User Growth</h3>
            <div className="flex items-center gap-4 text-xs text-[#6b7280]">
              <span className="flex items-center gap-1.5">
                <span className="w-2.5 h-2.5 rounded-full bg-[#111827]" />
                Revenue
              </span>
              <span className="flex items-center gap-1.5">
                <span className="w-2.5 h-2.5 rounded-full bg-[#ef4444]" />
                Users
              </span>
            </div>
          </div>
          <div className="h-64">
            <ResponsiveContainer width="100%" height="100%">
              <LineChart data={revenueGrowthData} margin={{ top: 5, right: 10, left: -10, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#f3f4f6" vertical={false} />
                <XAxis
                  dataKey="month"
                  axisLine={false}
                  tickLine={false}
                  tick={{ fontSize: 12, fill: '#9ca3af' }}
                />
                <YAxis
                  yAxisId="left"
                  axisLine={false}
                  tickLine={false}
                  tick={{ fontSize: 12, fill: '#9ca3af' }}
                  tickFormatter={(v) => `$${v / 1000}k`}
                />
                <YAxis
                  yAxisId="right"
                  orientation="right"
                  axisLine={false}
                  tickLine={false}
                  tick={{ fontSize: 12, fill: '#9ca3af' }}
                />
                <Tooltip
                  contentStyle={{
                    borderRadius: '8px',
                    border: '1px solid #e5e7eb',
                    fontSize: '12px',
                  }}
                />
                <Line
                  yAxisId="left"
                  type="monotone"
                  dataKey="revenue"
                  stroke="#111827"
                  strokeWidth={2}
                  dot={false}
                  activeDot={{ r: 4 }}
                />
                <Line
                  yAxisId="right"
                  type="monotone"
                  dataKey="users"
                  stroke="#ef4444"
                  strokeWidth={2}
                  dot={false}
                  activeDot={{ r: 4 }}
                />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </div>

        <div className="bg-white border border-gray-200 rounded-xl p-6">
          <div className="flex items-center justify-between mb-6">
            <h3 className="text-base font-semibold">Recent Jobs</h3>
            <button className="text-sm font-medium text-[#3b82f6] hover:text-blue-700 transition-colors">
              View All
            </button>
          </div>
          <div className="space-y-5">
            {recentJobs.map((job) => (
              <div key={job.id}>
                <div className="flex items-center justify-between mb-2">
                  <span className="text-sm font-semibold text-[#111827]">#{job.id}</span>
                  <span
                    className={`text-[10px] font-bold uppercase tracking-wider px-2.5 py-1 rounded border ${statusStyles[job.status]}`}
                  >
                    {job.status}
                  </span>
                </div>
                <div className="flex items-center gap-3">
                  <div className="flex-1 h-2 bg-gray-100 rounded-full overflow-hidden">
                    <div
                      className={`h-full rounded-full transition-all ${progressBarColors[job.status]}`}
                      style={{ width: `${job.progress}%` }}
                    />
                  </div>
                  <span className="text-xs font-medium text-[#6b7280] w-8 text-right">
                    {job.progress}%
                  </span>
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6">
        <div className="bg-white border border-gray-200 rounded-xl p-6">
          <h3 className="text-base font-semibold mb-6">Subscription Breakdown</h3>
          <div className="flex flex-col sm:flex-row items-center gap-8">
            <div className="relative w-48 h-48 shrink-0">
              <ResponsiveContainer width="100%" height="100%">
                <PieChart>
                  <Pie
                    data={subscriptionData}
                    cx="50%"
                    cy="50%"
                    innerRadius={55}
                    outerRadius={80}
                    paddingAngle={2}
                    dataKey="value"
                  >
                    {subscriptionData.map((entry) => (
                      <Cell key={entry.name} fill={entry.color} />
                    ))}
                  </Pie>
                </PieChart>
              </ResponsiveContainer>
              <div className="absolute inset-0 flex flex-col items-center justify-center pointer-events-none">
                <span className="text-2xl font-bold">{totalSubs}</span>
                <span className="text-[10px] font-semibold text-[#6b7280] tracking-wider">TOTAL</span>
              </div>
            </div>
            <div className="flex-1 w-full space-y-4">
              {subscriptionData.map((item) => (
                <div key={item.name} className="flex items-center justify-between">
                  <div className="flex items-center gap-3">
                    <span
                      className="w-3 h-3 rounded-full shrink-0"
                      style={{ backgroundColor: item.color }}
                    />
                    <span className="text-sm font-medium">{item.name}</span>
                  </div>
                  <div className="text-right">
                    <span className="text-sm font-semibold">{item.value}</span>
                    <span className="text-xs text-[#6b7280] ml-2">
                      {Math.round((item.value / totalSubs) * 100)}% of total
                    </span>
                  </div>
                </div>
              ))}
            </div>
          </div>
        </div>

        <div className="bg-white border border-gray-200 rounded-xl p-6">
          <div className="flex items-center justify-between mb-6">
            <h3 className="text-base font-semibold">Proxy Health Overview</h3>
            <span className="flex items-center gap-1.5 text-xs font-medium text-[#3b82f6]">
              <span className="w-2 h-2 rounded-full bg-[#3b82f6] animate-pulse" />
              Live Update
            </span>
          </div>
          <div className="space-y-6">
            {proxyRegions.map((region) => (
              <div key={region.name}>
                <div className="flex items-center justify-between mb-2">
                  <span className="text-sm font-medium text-[#111827]">{region.name}</span>
                  <span className="text-sm font-semibold text-[#3b82f6]">{region.health}%</span>
                </div>
                <div className="h-3 bg-gray-100 rounded-full overflow-hidden">
                  <div
                    className="h-full bg-[#3b82f6] rounded-full transition-all"
                    style={{ width: `${region.health}%` }}
                  />
                </div>
              </div>
            ))}
          </div>
        </div>
      </div>
    </AdminLayout>
  );
};

interface MetricCardProps {
  icon: ElementType;
  iconBg: string;
  iconColor: string;
  label: string;
  value: string;
  trend?: string;
  valueColor?: string;
}

const MetricCard = ({
  icon: Icon,
  iconBg,
  iconColor,
  label,
  value,
  trend,
  valueColor = 'text-[#111827]',
}: MetricCardProps) => (
  <div className="bg-white border border-gray-200 rounded-xl p-5">
    <div className={`w-10 h-10 ${iconBg} rounded-lg flex items-center justify-center mb-4`}>
      <Icon className={`w-5 h-5 ${iconColor}`} />
    </div>
    <p className="text-xs font-medium text-[#6b7280] uppercase tracking-wide mb-1">{label}</p>
    <div className="flex items-end justify-between gap-2">
      <span className={`text-2xl font-bold ${valueColor}`}>{value}</span>
      {trend && (
        <span className="flex items-center gap-0.5 text-xs font-semibold text-[#3b82f6] mb-1">
          <TrendingUp className="w-3.5 h-3.5" />
          {trend}
        </span>
      )}
    </div>
  </div>
);

export default AdminDashboard;
