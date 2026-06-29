import { useCallback, useEffect, useState } from 'react';
import { RefreshCw, Loader2 } from 'lucide-react';
import {
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  ResponsiveContainer,
  Cell,
} from 'recharts';
import AdminLayout from '../../components/admin/AdminLayout';
import { adminApi } from '../../api/adminApi';
import type { ScrapingJob } from '../../types/admin';

type JobStatus = 'Running' | 'COMPLETED' | 'FAILED';
type BarType = 'normal' | 'peak' | 'api';

interface ActiveEngine {
  id: number;
  name: string;
  progress: number;
  barColor: string;
}

const activeEngines: ActiveEngine[] = [
  { id: 1, name: 'Facebook Crawler v2.4', progress: 84, barColor: 'bg-blue-500' },
  { id: 2, name: 'TikTok Scraper (FastMode)', progress: 42, barColor: 'bg-[#ef4444]' },
  { id: 3, name: 'Web Gen-Core Engine', progress: 67, barColor: 'bg-slate-800' },
];

const throughputData: { time: string; value: number; type: BarType }[] = [
  { time: '00:00', value: 42, type: 'normal' },
  { time: '02:00', value: 38, type: 'normal' },
  { time: '04:00', value: 35, type: 'normal' },
  { time: '06:00', value: 48, type: 'normal' },
  { time: '08:00', value: 72, type: 'peak' },
  { time: '10:00', value: 55, type: 'normal' },
  { time: '12:00', value: 68, type: 'normal' },
  { time: '14:00', value: 85, type: 'peak' },
  { time: '16:00', value: 58, type: 'normal' },
  { time: '18:00', value: 62, type: 'api' },
  { time: '20:00', value: 50, type: 'normal' },
  { time: '23:59', value: 44, type: 'normal' },
];

const barColors: Record<BarType, string> = {
  normal: '#bfdbfe',
  peak: '#3b82f6',
  api: '#ef4444',
};

const jobStatusStyles: Record<JobStatus, string> = {
  Running: 'bg-blue-50 text-blue-600 border-blue-100',
  COMPLETED: 'bg-green-50 text-green-600 border-green-100',
  FAILED: 'bg-red-50 text-red-500 border-red-100',
};

const formatDuration = (startedAt?: string | null, finishedAt?: string | null) => {
  if (!startedAt) return '—';
  const start = new Date(startedAt).getTime();
  const end = finishedAt ? new Date(finishedAt).getTime() : Date.now();
  const mins = Math.max(0, Math.floor((end - start) / 60000));
  const h = Math.floor(mins / 60);
  const m = mins % 60;
  return h > 0 ? `${h}h ${m}m` : `${m}m`;
};

const normalizeStatus = (status?: string | null): JobStatus => {
  const s = (status ?? '').toLowerCase();
  if (s === 'running') return 'Running';
  if (s === 'failed') return 'FAILED';
  return 'COMPLETED';
};

const ScrapingMonitor = () => {
  const [jobs, setJobs] = useState<ScrapingJob[]>([]);
  const [jobsLoading, setJobsLoading] = useState(true);
  const [jobsError, setJobsError] = useState<string | null>(null);

  const loadJobs = useCallback(async () => {
    setJobsLoading(true);
    setJobsError(null);
    try {
      const { data } = await adminApi.listScrapingJobs();
      setJobs(data);
    } catch {
      setJobsError('Không tải được scraping jobs. Cần đăng nhập Admin.');
    } finally {
      setJobsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadJobs();
  }, [loadJobs]);

  return (
    <AdminLayout searchPlaceholder="Search crawlers or job IDs...">
      <div className="flex flex-col lg:flex-row lg:items-start lg:justify-between gap-4 mb-8">
        <div>
          <h2 className="text-2xl lg:text-3xl font-bold tracking-tight">
            Crawler Engine Monitor (Python Jobs)
          </h2>
          <p className="text-[#6b7280] text-sm mt-1">
            Real-time supervision of distributed web scrapers and data ingestion nodes.
          </p>
        </div>
        <div className="flex items-center gap-2 px-4 py-2 bg-white border border-gray-200 rounded-full text-xs font-semibold text-[#111827] shrink-0">
          <span className="w-2 h-2 rounded-full bg-[#ef4444] animate-pulse" />
          LIVE CLUSTER STATUS: ACTIVE
        </div>
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6 mb-8">
        <div className="bg-white border border-gray-200 rounded-xl p-6 shadow-sm">
          <div className="flex items-center justify-between mb-6">
            <h3 className="text-base font-semibold">Active Engines</h3>
            <button className="p-2 text-gray-400 hover:text-[#111827] hover:bg-gray-100 rounded-lg transition-colors">
              <RefreshCw className="w-4 h-4" />
            </button>
          </div>

          <div className="space-y-5 mb-6">
            {activeEngines.map((engine) => (
              <div key={engine.id}>
                <div className="flex items-center justify-between mb-2">
                  <span className="text-sm font-medium text-[#111827]">{engine.name}</span>
                  <span className="text-sm font-semibold text-[#6b7280]">{engine.progress}%</span>
                </div>
                <div className="h-2.5 bg-gray-100 rounded-full overflow-hidden">
                  <div
                    className={`h-full rounded-full transition-all ${engine.barColor}`}
                    style={{ width: `${engine.progress}%` }}
                  />
                </div>
              </div>
            ))}
          </div>

          <button className="w-full py-3 border-2 border-gray-200 rounded-lg text-xs font-bold tracking-wider text-[#6b7280] hover:bg-gray-50 hover:text-[#111827] hover:border-gray-300 transition-colors">
            MANAGE CLUSTER NODES
          </button>
        </div>

        <div className="bg-white border border-gray-200 rounded-xl p-6 shadow-sm">
          <div className="flex items-center justify-between mb-6">
            <h3 className="text-base font-semibold">Daily Scraping Throughput</h3>
            <div className="flex items-center gap-4 text-xs text-[#6b7280]">
              <span className="flex items-center gap-1.5">
                <span className="w-2.5 h-2.5 rounded-full bg-blue-500" />
                Pages/Sec
              </span>
              <span className="flex items-center gap-1.5">
                <span className="w-2.5 h-2.5 rounded-full bg-[#ef4444]" />
                API Calls
              </span>
            </div>
          </div>

          <div className="h-56">
            <ResponsiveContainer width="100%" height="100%">
              <BarChart data={throughputData} margin={{ top: 5, right: 5, left: -20, bottom: 0 }}>
                <CartesianGrid strokeDasharray="3 3" stroke="#f3f4f6" vertical={false} />
                <XAxis
                  dataKey="time"
                  axisLine={false}
                  tickLine={false}
                  tick={{ fontSize: 10, fill: '#9ca3af' }}
                />
                <YAxis
                  axisLine={false}
                  tickLine={false}
                  tick={{ fontSize: 10, fill: '#9ca3af' }}
                />
                <Tooltip
                  contentStyle={{
                    borderRadius: '8px',
                    border: '1px solid #e5e7eb',
                    fontSize: '12px',
                  }}
                  formatter={(value) => [`${value}`, 'Throughput']}
                />
                <Bar dataKey="value" radius={[4, 4, 0, 0]}>
                  {throughputData.map((entry, index) => (
                    <Cell key={index} fill={barColors[entry.type]} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </div>
        </div>
      </div>

      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden shadow-sm">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 px-6 py-5 border-b border-gray-100">
          <h3 className="text-base font-semibold">Current Job Registry (UC-74)</h3>
          <button
            type="button"
            onClick={loadJobs}
            className="flex items-center gap-2 px-4 py-2 bg-white border border-gray-200 rounded-lg text-sm font-medium hover:bg-gray-50"
          >
            {jobsLoading ? <Loader2 className="w-4 h-4 animate-spin" /> : <RefreshCw className="w-4 h-4" />}
            Refresh
          </button>
        </div>

        {jobsError && (
          <div className="mx-6 mt-4 px-4 py-3 bg-red-50 border border-red-100 text-red-700 text-sm rounded-lg">
            {jobsError}
          </div>
        )}

        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100 bg-gray-50/50">
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  Job ID
                </th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  Project
                </th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  Proxy IP
                </th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  Start Time
                </th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  Duration
                </th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  Scraped
                </th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  Status
                </th>
                <th className="text-right px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                  Error
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {!jobsLoading && jobs.length === 0 && (
                <tr>
                  <td colSpan={8} className="px-6 py-10 text-center text-[#6b7280]">
                    Chưa có scraping job nào.
                  </td>
                </tr>
              )}
              {jobs.map((job) => {
                const status = normalizeStatus(job.status);
                return (
                  <tr key={job.jobId} className="hover:bg-gray-50/50 transition-colors">
                    <td className="px-6 py-4 font-semibold text-[#111827] font-mono text-xs">
                      #{job.jobId.slice(0, 8)}
                    </td>
                    <td className="px-6 py-4 text-[#111827]">
                      {job.projectName ?? `Project #${job.projectId}`}
                    </td>
                    <td className="px-6 py-4 text-[#6b7280]">{job.proxyIp ?? 'direct'}</td>
                    <td className="px-6 py-4 text-[#6b7280]">
                      {job.startedAt
                        ? new Date(job.startedAt).toLocaleString('vi-VN')
                        : '—'}
                    </td>
                    <td className="px-6 py-4 text-[#111827] font-medium">
                      {formatDuration(job.startedAt, job.finishedAt)}
                    </td>
                    <td className="px-6 py-4 text-[#111827]">{job.totalScraped.toLocaleString()}</td>
                    <td className="px-6 py-4">
                      <span
                        className={`inline-block text-[10px] font-bold uppercase tracking-wider px-2.5 py-1 rounded border ${jobStatusStyles[status]}`}
                      >
                        {status}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-right text-xs text-red-600 max-w-[200px] truncate">
                      {job.errorLog ?? '—'}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>
    </AdminLayout>
  );
};

export default ScrapingMonitor;
