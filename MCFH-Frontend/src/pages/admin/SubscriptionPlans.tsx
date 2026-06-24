import type { ElementType } from 'react';
import {
  Play,
  Wallet,
  Users,
  Box,
  Zap,
  Crown,
  Building2,
  Calendar,
  Download,
} from 'lucide-react';
import AdminLayout from '../../components/admin/AdminLayout';

type PlanStatus = 'Active' | 'Popular';

interface SubscriptionPlan {
  id: number;
  name: string;
  icon: ElementType;
  iconBg: string;
  iconColor: string;
  monthlyPrice: string;
  creditLimit: string;
  activeSubscribers: number;
  status: PlanStatus;
  lastUpdated: string;
}

interface WorkspaceUsage {
  id: number;
  name: string;
  used: number;
  total: number;
  totalLabel: string;
}

const plans: SubscriptionPlan[] = [
  {
    id: 1,
    name: 'Basic',
    icon: Zap,
    iconBg: 'bg-blue-50',
    iconColor: 'text-blue-500',
    monthlyPrice: '$49.00',
    creditLimit: '1,000',
    activeSubscribers: 412,
    status: 'Active',
    lastUpdated: 'Oct 12, 2023',
  },
  {
    id: 2,
    name: 'Premium',
    icon: Crown,
    iconBg: 'bg-red-50',
    iconColor: 'text-[#ef4444]',
    monthlyPrice: '$199.00',
    creditLimit: '10,000',
    activeSubscribers: 725,
    status: 'Popular',
    lastUpdated: 'Nov 05, 2023',
  },
  {
    id: 3,
    name: 'Enterprise',
    icon: Building2,
    iconBg: 'bg-slate-100',
    iconColor: 'text-slate-700',
    monthlyPrice: '$599.00',
    creditLimit: '50,000',
    activeSubscribers: 147,
    status: 'Active',
    lastUpdated: 'Aug 22, 2023',
  },
];

const workspaces: WorkspaceUsage[] = [
  { id: 1, name: 'Global Insights Group', used: 42890, total: 50000, totalLabel: '50k' },
  { id: 2, name: 'DataSphere LLC', used: 38120, total: 50000, totalLabel: '50k' },
  { id: 3, name: 'TrendWatchers Agency', used: 9450, total: 10000, totalLabel: '10k' },
  { id: 4, name: 'NextGen Retail', used: 7200, total: 10000, totalLabel: '10k' },
];

const statusStyles: Record<PlanStatus, string> = {
  Active: 'bg-blue-50 text-blue-600',
  Popular: 'bg-red-50 text-[#ef4444]',
};

const SubscriptionPlans = () => {
  return (
    <AdminLayout searchPlaceholder="Search systems or users...">
      <div className="mb-8">
        <h2 className="text-2xl lg:text-3xl font-bold tracking-tight">Subscription Management</h2>
        <p className="text-[#6b7280] text-sm mt-1">
          Oversee tier configurations, revenue metrics, and resource allocation for the engine.
        </p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4 mb-8">
        <SummaryCard
          icon={Play}
          iconBg="bg-blue-50"
          iconColor="text-blue-500"
          label="Total Active Subscriptions"
          value="1,284"
          badge="Monthly Growth: +12%"
        />
        <SummaryCard
          icon={Wallet}
          iconBg="bg-emerald-50"
          iconColor="text-emerald-600"
          label="Monthly Revenue"
          value="$142,500"
          badge="This Month"
        />
        <SummaryCard
          icon={Users}
          iconBg="bg-violet-50"
          iconColor="text-violet-600"
          label="Active Workspaces"
          value="842"
          badge="98% Retention"
        />
        <SummaryCard
          icon={Box}
          iconBg="bg-amber-50"
          iconColor="text-amber-600"
          label="Total AI Credits Allocated"
          value="12.5M"
          badge="85% Utilization"
        />
      </div>

      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden mb-8">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 px-6 py-5 border-b border-gray-100">
          <h3 className="text-base font-semibold">Available Plans</h3>
          <button className="px-5 py-2.5 bg-[#111827] hover:bg-slate-800 text-white rounded-lg text-sm font-semibold transition-colors">
            Add New Plan
          </button>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100 bg-gray-50/50">
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  Plan Name
                </th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  Monthly Price
                </th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  AI Credit Limit
                </th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  Active Subscribers
                </th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  Status
                </th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  Last Updated
                </th>
                <th className="text-right px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  Actions
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {plans.map((plan) => (
                <tr key={plan.id} className="hover:bg-gray-50/50 transition-colors">
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-3">
                      <div
                        className={`w-9 h-9 ${plan.iconBg} rounded-lg flex items-center justify-center shrink-0`}
                      >
                        <plan.icon className={`w-4 h-4 ${plan.iconColor}`} />
                      </div>
                      <span className="font-medium text-[#111827]">{plan.name}</span>
                    </div>
                  </td>
                  <td className="px-6 py-4 font-medium text-[#111827]">{plan.monthlyPrice}</td>
                  <td className="px-6 py-4 text-[#6b7280]">{plan.creditLimit}</td>
                  <td className="px-6 py-4 text-[#111827]">{plan.activeSubscribers}</td>
                  <td className="px-6 py-4">
                    <span
                      className={`inline-block text-xs font-semibold px-2.5 py-1 rounded ${statusStyles[plan.status]}`}
                    >
                      {plan.status}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-[#6b7280]">{plan.lastUpdated}</td>
                  <td className="px-6 py-4 text-right">
                    <button className="text-sm font-semibold text-[#ef4444] hover:text-red-600 transition-colors">
                      Edit Plan
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>

      <div className="bg-white border border-gray-200 rounded-xl p-6">
        <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 mb-6">
          <div>
            <h3 className="text-base font-semibold">Top Workspaces by AI Credit Consumption</h3>
            <p className="text-sm text-[#6b7280] mt-1">
              Real-time resource utilization across active accounts.
            </p>
          </div>
          <div className="flex items-center gap-3 shrink-0">
            <button className="flex items-center gap-2 px-4 py-2.5 bg-white border border-gray-200 rounded-lg text-sm font-medium text-[#111827] hover:bg-gray-50 transition-colors">
              <Calendar className="w-4 h-4 text-gray-500" />
              Current Month
            </button>
            <button className="flex items-center gap-2 px-4 py-2.5 bg-white border border-gray-200 rounded-lg text-sm font-medium text-[#111827] hover:bg-gray-50 transition-colors">
              <Download className="w-4 h-4 text-gray-500" />
              Export
            </button>
          </div>
        </div>

        <div className="space-y-6">
          {workspaces.map((workspace) => {
            const percent = Math.round((workspace.used / workspace.total) * 100);
            return (
              <div key={workspace.id}>
                <div className="flex items-center justify-between mb-2">
                  <span className="text-sm font-medium text-[#111827]">{workspace.name}</span>
                  <span className="text-sm text-[#6b7280]">
                    {workspace.used.toLocaleString()} / {workspace.totalLabel} credits
                  </span>
                </div>
                <div className="h-3 bg-blue-50 rounded-full overflow-hidden">
                  <div
                    className="h-full bg-[#991b1b] rounded-full transition-all"
                    style={{ width: `${percent}%` }}
                  />
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </AdminLayout>
  );
};

interface SummaryCardProps {
  icon: ElementType;
  iconBg: string;
  iconColor: string;
  label: string;
  value: string;
  badge: string;
}

const SummaryCard = ({ icon: Icon, iconBg, iconColor, label, value, badge }: SummaryCardProps) => (
  <div className="bg-white border border-gray-200 rounded-xl p-5">
    <div className="flex items-start justify-between mb-4">
      <div className={`w-10 h-10 ${iconBg} rounded-lg flex items-center justify-center`}>
        <Icon className={`w-5 h-5 ${iconColor}`} />
      </div>
      <span className="text-[10px] font-medium text-[#6b7280] text-right leading-tight max-w-[100px]">
        {badge}
      </span>
    </div>
    <p className="text-xs font-medium text-[#6b7280] uppercase tracking-wide mb-1">{label}</p>
    <span className="text-2xl font-bold text-[#111827]">{value}</span>
  </div>
);

export default SubscriptionPlans;
