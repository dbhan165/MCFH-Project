import { useEffect, useState, type ReactNode } from 'react';
import {
  Plus,
  Globe,
  TrendingUp,
  Pencil,
  Download,
  Layers,
} from 'lucide-react';
import AdminLayout from '../../components/admin/AdminLayout';
import { adminApi } from '../../api/portalApi';

type LatencyType = 'good' | 'warning' | 'error';
type HistoryColor = 'green' | 'orange' | 'red';

interface ProxyNode {
  id: number;
  ip: string;
  region: string;
  provider: string;
  successRate: number;
  latency: string;
  latencyType: LatencyType;
  history: number[];
  historyColor: HistoryColor;
  enabled: boolean;
}

const successBarColor = (rate: number) => {
  if (rate >= 90) return 'bg-green-500';
  if (rate >= 50) return 'bg-amber-500';
  return 'bg-red-500';
};

const latencyStyles: Record<LatencyType, string> = {
  good: 'text-[#111827]',
  warning: 'text-amber-600',
  error: 'text-red-500 font-semibold',
};

const sparklineColors: Record<HistoryColor, string> = {
  green: '#22c55e',
  orange: '#f59e0b',
  red: '#ef4444',
};

const Sparkline = ({ data, color }: { data: number[]; color: HistoryColor }) => {
  const width = 80;
  const height = 28;
  const min = Math.min(...data);
  const max = Math.max(...data);
  const range = max - min || 1;

  const points = data
    .map((value, index) => {
      const x = (index / (data.length - 1)) * width;
      const y = height - ((value - min) / range) * (height - 4) - 2;
      return `${x},${y}`;
    })
    .join(' ');

  return (
    <svg width={width} height={height} className="overflow-visible">
      <polyline
        fill="none"
        stroke={sparklineColors[color]}
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
        points={points}
      />
    </svg>
  );
};

const Toggle = ({
  enabled,
  onChange,
}: {
  enabled: boolean;
  onChange: () => void;
}) => (
  <button
    type="button"
    role="switch"
    aria-checked={enabled}
    onClick={onChange}
    className={`relative w-11 h-6 rounded-full transition-colors ${
      enabled ? 'bg-[#ef4444]' : 'bg-gray-300'
    }`}
  >
    <span
      className={`absolute top-0.5 left-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform ${
        enabled ? 'translate-x-5' : 'translate-x-0'
      }`}
    />
  </button>
);

const ProxyManagement = () => {
  const [proxies, setProxies] = useState<ProxyNode[]>([]);

  const loadProxies = async () => {
    try {
      const data = await adminApi.getProxies();
      setProxies(
        data.map((p) => ({
          id: p.proxyId,
          ip: `${p.ipAddress}:${p.port}`,
          region: p.authUser ?? 'N/A',
          provider: p.status ?? 'active',
          successRate: Math.max(0, 100 - p.failCount * 5),
          latency: p.enabled ? '—' : 'OFF',
          latencyType: p.enabled ? 'good' : 'error',
          history: [90, 90, 90, 90, 90, 90, 90],
          historyColor: 'green' as HistoryColor,
          enabled: p.enabled,
        }))
      );
    } catch {
      setProxies([]);
    }
  };

  useEffect(() => {
    loadProxies();
  }, []);

  const toggleProxy = (id: number) => {
    setProxies((prev) =>
      prev.map((proxy) => (proxy.id === id ? { ...proxy, enabled: !proxy.enabled } : proxy))
    );
  };

  return (
    <AdminLayout searchPlaceholder="Search proxies or regions...">
      <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 mb-8">
        <div>
          <h2 className="text-2xl lg:text-3xl font-bold tracking-tight">
            Proxy Infrastructure Monitoring
          </h2>
          <p className="text-[#6b7280] text-sm mt-1">
            Real-time status and performance metrics for global proxy nodes.
          </p>
        </div>
        <button className="flex items-center gap-2 px-5 py-2.5 bg-[#ef4444] hover:bg-red-600 text-white rounded-lg text-sm font-semibold transition-colors shadow-sm shrink-0">
          <Plus className="w-4 h-4" />
          Add New Proxy
        </button>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4 mb-8">
        <StatCard
          borderColor="border-l-blue-500"
          label="Total Proxies"
          value="1,284"
          tag={
            <span className="flex items-center gap-1 text-xs font-semibold text-blue-600">
              <TrendingUp className="w-3.5 h-3.5" />
              +12%
            </span>
          }
        />
        <StatCard
          borderColor="border-l-green-500"
          label="Active"
          value="1,242"
          tag={<span className="text-xs font-semibold text-green-600 bg-green-50 px-2 py-0.5 rounded">Healthy</span>}
        />
        <StatCard
          borderColor="border-l-amber-500"
          label="Throttled"
          value="38"
          tag={<span className="text-xs font-semibold text-amber-600 bg-amber-50 px-2 py-0.5 rounded">Degraded</span>}
        />
        <StatCard
          borderColor="border-l-red-500"
          label="Failed"
          value="4"
          tag={<span className="text-xs font-semibold text-red-600 bg-red-50 px-2 py-0.5 rounded">Urgent</span>}
        />
      </div>

      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden shadow-sm">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 px-6 py-5 border-b border-gray-100">
          <h3 className="text-base font-semibold">Global Node Registry</h3>
          <div className="flex items-center gap-3">
            <button className="flex items-center gap-2 px-4 py-2 bg-white border border-gray-200 rounded-lg text-sm font-medium text-[#111827] hover:bg-gray-50 transition-colors">
              <Download className="w-4 h-4 text-gray-500" />
              Export CSV
            </button>
            <button className="flex items-center gap-2 px-4 py-2 bg-white border border-gray-200 rounded-lg text-sm font-medium text-[#111827] hover:bg-gray-50 transition-colors">
              <Layers className="w-4 h-4 text-gray-500" />
              Bulk Action
            </button>
          </div>
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100 bg-blue-50/40">
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  IP Address
                </th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  Region
                </th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  Provider
                </th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  Success Rate
                </th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  Latency
                </th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  History (24H)
                </th>
                <th className="text-right px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  Status
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {proxies.map((proxy) => (
                <tr key={proxy.id} className="hover:bg-gray-50/50 transition-colors">
                  <td className="px-6 py-4 font-medium text-[#111827]">{proxy.ip}</td>
                  <td className="px-6 py-4">
                    <span className="inline-flex items-center gap-2 text-[#6b7280]">
                      <Globe className="w-4 h-4 text-blue-500 shrink-0" />
                      {proxy.region}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-[#111827]">{proxy.provider}</td>
                  <td className="px-6 py-4">
                    <div className="flex items-center gap-3 min-w-[120px]">
                      <div className="flex-1 h-1.5 bg-gray-100 rounded-full overflow-hidden max-w-[60px]">
                        <div
                          className={`h-full rounded-full ${successBarColor(proxy.successRate)}`}
                          style={{ width: `${proxy.successRate}%` }}
                        />
                      </div>
                      <span className="text-sm font-medium text-[#111827]">{proxy.successRate}%</span>
                    </div>
                  </td>
                  <td className={`px-6 py-4 text-sm ${latencyStyles[proxy.latencyType]}`}>
                    {proxy.latency}
                  </td>
                  <td className="px-6 py-4">
                    <Sparkline data={proxy.history} color={proxy.historyColor} />
                  </td>
                  <td className="px-6 py-4">
                    <div className="flex items-center justify-end gap-3">
                      <Toggle enabled={proxy.enabled} onChange={() => toggleProxy(proxy.id)} />
                      <button className="p-2 text-gray-400 hover:text-[#111827] hover:bg-gray-100 rounded-lg transition-colors">
                        <Pencil className="w-4 h-4" />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </AdminLayout>
  );
};

interface StatCardProps {
  borderColor: string;
  label: string;
  value: string;
  tag: ReactNode;
}

const StatCard = ({ borderColor, label, value, tag }: StatCardProps) => (
  <div className={`bg-white border border-gray-200 border-l-4 ${borderColor} rounded-xl p-5 shadow-sm`}>
    <div className="flex items-start justify-between mb-3">
      <p className="text-xs font-medium text-[#6b7280] uppercase tracking-wide">{label}</p>
      {tag}
    </div>
    <span className="text-3xl font-bold text-[#111827]">{value}</span>
  </div>
);

export default ProxyManagement;
