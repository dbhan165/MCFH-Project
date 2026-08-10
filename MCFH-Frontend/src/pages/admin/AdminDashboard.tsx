import type { ElementType } from 'react';
import { useCallback, useEffect, useState } from 'react';
import {
  UserPlus,
  ShieldCheck,
  Server,
  AlertTriangle,
  TrendingUp,
  Wallet,
  CheckCircle2,
  Layers,
  Trophy,
  Sparkles,
} from 'lucide-react';
import {
  LineChart,
  Line,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
} from 'recharts';
import AdminLayout from '../../components/admin/AdminLayout';
import { adminApi, type AdminDashboard as AdminDashboardData } from '../../api/portalApi';

const featureBadgeStyles: Record<string, string> = {
  subscription: 'bg-emerald-50 text-emerald-700 border-emerald-200',
  scrape_order: 'bg-blue-50 text-blue-700 border-blue-200',
  bespoke: 'bg-purple-50 text-purple-700 border-purple-200',
};

const formatVND = (amount: number) => {
  return amount.toLocaleString('vi-VN') + ' ₫';
};

const formatDate = (dateStr: string | null) => {
  if (!dateStr) return '—';
  try {
    const d = new Date(dateStr);
    return d.toLocaleDateString('vi-VN', {
      day: '2-digit',
      month: '2-digit',
      year: 'numeric',
      hour: '2-digit',
      minute: '2-digit',
    });
  } catch {
    return dateStr;
  }
};

const AdminDashboard = () => {
  const [stats, setStats] = useState<AdminDashboardData | null>(null);
  const topFeature = stats?.revenueByType?.find((t) => t.isTopFeature) || stats?.revenueByType?.[0];

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
      <div className="mb-8">
        <h2 className="text-2xl lg:text-3xl font-bold tracking-tight">Tổng quan Dashboard</h2>
        <p className="text-[#6b7280] text-sm mt-1">
          Thống kê hiệu năng hệ thống & Doanh thu thực tế theo từng tính năng riêng biệt.
        </p>
      </div>

      {/* Top Metric Cards */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-5 gap-4 mb-6">
        <MetricCard
          icon={Wallet}
          iconBg="bg-emerald-50"
          iconColor="text-emerald-600"
          label="Tổng Doanh Thu"
          value={stats ? formatVND(stats.totalRevenue) : '—'}
          valueColor="text-emerald-600"
          trend={stats?.revenueGrowthRate ? `${stats.revenueGrowthRate > 0 ? '+' : ''}${stats.revenueGrowthRate}%` : undefined}
        />
        <MetricCard
          icon={UserPlus}
          iconBg="bg-blue-50"
          iconColor="text-blue-500"
          label="Tổng Người dùng"
          value={stats ? String(stats.totalUsers) : '—'}
        />
        <MetricCard
          icon={ShieldCheck}
          iconBg="bg-gray-50"
          iconColor="text-gray-600"
          label="Nhân viên báo cáo"
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
          icon={AlertTriangle}
          iconBg="bg-red-50"
          iconColor="text-[#ef4444]"
          label="Bespoke chờ"
          value={stats ? String(stats.pendingBespoke) : '—'}
          valueColor="text-[#ef4444]"
        />
      </div>

      {/* Feature Revenue Management & Comparison Section */}
      <div className="bg-white border border-gray-200 rounded-xl p-6 mb-6 shadow-sm">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6 pb-4 border-b border-gray-100">
          <div>
            <div className="flex items-center gap-2">
              <h3 className="text-lg font-bold text-gray-900">Quản Lý & So Sánh Doanh Thu Theo Tính Năng</h3>
              <span className="px-2.5 py-0.5 bg-indigo-50 text-indigo-700 text-xs font-semibold rounded-full border border-indigo-100 flex items-center gap-1">
                <Sparkles className="w-3 h-3" /> Feature Analytics
              </span>
            </div>
            <p className="text-xs text-gray-500 mt-1">
              Phân tích trực quan nguồn doanh thu từ <strong>Tạo Dự Án Mới (Scrape Order)</strong> vs <strong>Tạo Báo Cáo Chuyên Sâu (Bespoke Report)</strong> vs <strong>Gói Subscriptions</strong>.
            </p>
          </div>
          {topFeature && topFeature.totalAmount > 0 && (
            <div className="flex items-center gap-3 px-4 py-2.5 bg-gradient-to-r from-amber-50 to-orange-50 border border-amber-200/80 rounded-xl shrink-0">
              <div className="w-8 h-8 rounded-lg bg-amber-500 text-white flex items-center justify-center font-bold shrink-0 shadow-sm">
                <Trophy className="w-4 h-4" />
              </div>
              <div>
                <div className="text-[11px] font-semibold text-amber-800 uppercase tracking-wider">Tính Năng Thu Nhập Cao Nhất</div>
                <div className="text-xs font-bold text-gray-900">
                  {topFeature.typeName} <span className="text-emerald-600 ml-1">({formatVND(topFeature.totalAmount)})</span>
                </div>
              </div>
            </div>
          )}
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
          {/* Feature Revenue Cards */}
          {stats?.revenueByType && stats.revenueByType.length > 0 ? (
            stats.revenueByType.map((item) => (
              <div
                key={item.type}
                className={`p-5 rounded-xl border transition-all ${item.isTopFeature
                  ? 'bg-gradient-to-b from-indigo-50/30 to-white border-indigo-200 shadow-sm ring-1 ring-indigo-100'
                  : 'bg-white border-gray-200 hover:border-gray-300'
                  }`}
              >
                <div className="flex items-center justify-between mb-3">
                  <span
                    className={`inline-flex items-center gap-1 px-2.5 py-1 rounded text-xs font-semibold border ${featureBadgeStyles[item.type] || 'bg-gray-50 text-gray-700 border-gray-200'
                      }`}
                  >
                    {item.type === 'bespoke' && <Sparkles className="w-3 h-3" />}
                    {item.type === 'scrape_order' && <Layers className="w-3 h-3" />}
                    {item.typeName}
                  </span>
                  {item.isTopFeature && (
                    <span className="text-[10px] font-bold uppercase tracking-wider text-amber-600 bg-amber-50 border border-amber-200 px-2 py-0.5 rounded">
                      #1 Top Revenue
                    </span>
                  )}
                </div>

                <div className="mb-4">
                  <div className="text-xs font-medium text-gray-500 mb-1">Tổng doanh thu thực nhận</div>
                  <div className="text-2xl font-bold text-gray-900">{formatVND(item.totalAmount)}</div>
                </div>

                <div className="grid grid-cols-2 gap-2 pt-3 border-t border-gray-100 text-xs mb-4">
                  <div>
                    <span className="text-gray-400 block text-[11px]">Số giao dịch</span>
                    <span className="font-semibold text-gray-800">{item.transactionCount} lượt</span>
                  </div>
                  <div>
                    <span className="text-gray-400 block text-[11px]">TB / Đơn hàng (AOV)</span>
                    <span className="font-semibold text-emerald-600">{formatVND(item.averageOrderValue)}</span>
                  </div>
                </div>

                <div>
                  <div className="flex justify-between text-[11px] text-gray-500 mb-1">
                    <span>Tỷ trọng đóng góp</span>
                    <span className="font-bold text-gray-800">{item.percentage}%</span>
                  </div>
                  <div className="w-full bg-gray-100 h-2.5 rounded-full overflow-hidden">
                    <div
                      className={`h-full rounded-full transition-all ${item.type === 'bespoke'
                        ? 'bg-purple-500'
                        : item.type === 'scrape_order'
                          ? 'bg-blue-500'
                          : 'bg-emerald-500'
                        }`}
                      style={{ width: `${Math.min(100, Math.max(4, item.percentage))}%` }}
                    />
                  </div>
                </div>
              </div>
            ))
          ) : (
            <div className="col-span-3 text-center text-xs text-gray-400 py-10">
              Chưa phát sinh giao dịch thanh toán tính năng nào
            </div>
          )}
        </div>
      </div>

      {/* Revenue Growth Line Chart */}
      <div className="bg-white border border-gray-200 rounded-xl p-6 mb-6">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2 mb-6">
          <div>
            <h3 className="text-base font-semibold">Tăng Trưởng Doanh Thu & Người Dùng</h3>
            <p className="text-xs text-[#6b7280] mt-0.5">
              Trục trái: Doanh thu thực nhận (Triệu ₫) | Trục phải: Số lượng Users đăng ký mới
            </p>
          </div>
          <div className="flex items-center gap-4 text-xs font-medium text-[#6b7280]">
            <span className="flex items-center gap-1.5">
              <span className="w-2.5 h-2.5 rounded-full bg-[#111827]" />
              Doanh Thu (Tr ₫)
            </span>
            <span className="flex items-center gap-1.5">
              <span className="w-2.5 h-2.5 rounded-full bg-[#ef4444]" />
              Users Mới (Người)
            </span>
          </div>
        </div>
        <div className="h-80">
          <ResponsiveContainer width="100%" height="100%">
            <LineChart data={stats?.revenueGrowth || []}>
              <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f3f4f6" />
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
                tickFormatter={(v: number) => {
                  if (v >= 1000000) {
                    const millions = v / 1000000;
                    return Number.isInteger(millions) ? `${millions} Tr ₫` : `${millions.toFixed(1)} Tr ₫`;
                  }
                  if (v >= 1000) return `${(v / 1000).toFixed(0)}k ₫`;
                  return `${v.toLocaleString('vi-VN')} ₫`;
                }}
              />
              <YAxis
                yAxisId="right"
                orientation="right"
                axisLine={false}
                tickLine={false}
                allowDecimals={false}
                domain={[0, (dataMax: number) => Math.max(dataMax, 10)]}
                tick={{ fontSize: 12, fill: '#9ca3af' }}
                tickFormatter={(v: number) => `${v} users`}
              />
              <Tooltip
                formatter={(value: any, name: any) => {
                  const valNum = typeof value === 'number' ? value : Number(value || 0);
                  if (name === 'revenue') return [formatVND(valNum), 'Doanh Thu'];
                  return [`${valNum} người`, 'Users Mới'];
                }}
                contentStyle={{
                  borderRadius: '8px',
                  border: '1px solid #e5e7eb',
                  fontSize: '12px',
                  boxShadow: '0 4px 6px -1px rgba(0, 0, 0, 0.1)',
                }}
              />
              <Line
                yAxisId="left"
                type="monotone"
                dataKey="revenue"
                stroke="#111827"
                strokeWidth={2.5}
                dot={false}
                activeDot={{ r: 5 }}
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

      {/* Recent Feature Payments Table (Full Width) */}
      <div className="bg-white border border-gray-200 rounded-xl p-6 mb-6">
        <div className="flex items-center justify-between mb-4">
          <div>
            <h3 className="text-base font-semibold text-gray-900">Giao Dịch Thanh Toán Tính Năng Gần Đây</h3>
            <p className="text-xs text-[#6b7280] mt-0.5">Danh sách các giao dịch User đã trả tiền sử dụng từng tính năng hệ thống</p>
          </div>
          <span className="px-2.5 py-1 bg-emerald-50 text-emerald-700 border border-emerald-200 rounded text-xs font-semibold">
            Live Verified
          </span>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-left border-collapse">
            <thead>
              <tr className="border-b border-gray-100 text-xs font-semibold text-gray-500 uppercase tracking-wider">
                <th className="py-3 px-2">Mã GD / Trạng Thái</th>
                <th className="py-3 px-2">User Thanh Toán</th>
                <th className="py-3 px-2">Tính Năng Sử Dụng</th>
                <th className="py-3 px-2 text-right">Số Tiền (₫)</th>
                <th className="py-3 px-2 text-right">Ngày Giờ</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100 text-sm">
              {stats?.recentRevenueTransactions && stats.recentRevenueTransactions.length > 0 ? (
                stats.recentRevenueTransactions.map((tx) => (
                  <tr key={tx.paymentId} className="hover:bg-gray-50/50 transition-colors">
                    <td className="py-3 px-2">
                      <div className="font-mono text-xs font-semibold text-gray-900">{tx.transactionRef}</div>
                      <span className="inline-flex items-center gap-1 text-[11px] text-emerald-600 font-medium mt-0.5">
                        <CheckCircle2 className="w-3 h-3" />
                        {tx.status}
                      </span>
                    </td>
                    <td className="py-3 px-2">
                      <div className="font-medium text-gray-900">{tx.userName}</div>
                      <div className="text-xs text-gray-500">{tx.userEmail}</div>
                    </td>
                    <td className="py-3 px-2">
                      <span
                        className={`inline-block px-2.5 py-1 rounded text-xs font-medium border ${featureBadgeStyles[tx.type] || 'bg-gray-50 text-gray-700 border-gray-200'
                          }`}
                      >
                        {tx.featureName}
                      </span>
                    </td>
                    <td className="py-3 px-2 text-right font-bold text-gray-900">
                      {formatVND(tx.amount)}
                    </td>
                    <td className="py-3 px-2 text-right text-xs text-gray-500">
                      {formatDate(tx.paidAt)}
                    </td>
                  </tr>
                ))
              ) : (
                <tr>
                  <td colSpan={5} className="py-8 text-center text-xs text-gray-400">
                    Chưa có giao dịch thanh toán nào gần đây
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>

      {/* Proxy Health */}
      <div className="bg-white border border-gray-200 rounded-xl p-6">
        <div className="flex items-center justify-between mb-6">
          <h3 className="text-base font-semibold">Trạng thái hoạt động Proxy</h3>
          <span className="flex items-center gap-1.5 text-xs font-medium text-[#3b82f6]">
            <span className="w-2 h-2 rounded-full bg-[#3b82f6] animate-pulse" />
            Cập nhật trực tiếp
          </span>
        </div>
        <div className="space-y-6">
          {stats?.proxyHealthOverview?.map((region) => (
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
        <span className="flex items-center gap-0.5 text-xs font-semibold text-emerald-600 mb-1">
          <TrendingUp className="w-3.5 h-3.5" />
          {trend}
        </span>
      )}
    </div>
  </div>
);

export default AdminDashboard;
