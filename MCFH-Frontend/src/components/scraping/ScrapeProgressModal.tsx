import { Loader2, CheckCircle2, AlertCircle, Circle, MinusCircle, Square } from 'lucide-react';
import type { ScrapeJobStatus } from '../../types/project';

const PLATFORM_STYLE: Record<string, { dot: string; label: string }> = {
  facebook: { dot: 'bg-blue-400', label: 'Facebook' },
  youtube: { dot: 'bg-red-400', label: 'YouTube' },
  tiktok: { dot: 'bg-cyan-300', label: 'TikTok' },
};

function statusIcon(status: string) {
  switch (status) {
    case 'running':
      return <Loader2 className="w-4 h-4 animate-spin text-[#FF7575]" />;
    case 'done':
      return <CheckCircle2 className="w-4 h-4 text-emerald-400" />;
    case 'error':
      return <AlertCircle className="w-4 h-4 text-red-400" />;
    case 'skipped':
      return <MinusCircle className="w-4 h-4 text-gray-500" />;
    default:
      return <Circle className="w-4 h-4 text-gray-600" />;
  }
}

type ScrapeProgressModalProps = {
  open: boolean;
  projectName: string;
  job: ScrapeJobStatus | null;
  progressPercent?: number;
  onCancel?: () => void;
  isCancelling?: boolean;
};

export default function ScrapeProgressModal({
  open,
  projectName,
  job,
  progressPercent,
  onCancel,
  isCancelling,
}: ScrapeProgressModalProps) {
  if (!open) return null;

  const platforms = job?.platforms ?? [];
  const totalCount = platforms.reduce((sum, p) => sum + (p.status === 'skipped' ? 0 : p.count), 0);
  const isRunning = job?.status === 'running' || job?.status === 'pending' || !job?.status;
  const isCancelled = job?.status === 'cancelled';
  const isDone = job?.status === 'completed' || job?.status === 'failed' || isCancelled;
  const phaseMessage =
    job?.phaseMessage ||
    (job?.phase === 'analyzing'
      ? 'Đang phân tích AI...'
      : job?.phase === 'starting'
        ? 'Đang khởi động...'
        : 'Bot Playwright đang thu thập dữ liệu...');

  const title = isCancelled
    ? 'Đã dừng cào dữ liệu'
    : isDone
      ? job?.status === 'completed'
        ? 'Cào dữ liệu hoàn tất'
        : 'Cào dữ liệu kết thúc'
      : 'Đang cào dữ liệu...';

  const barPercent =
    progressPercent ??
    (platforms.length > 0
      ? Math.round(
          platforms
            .filter((p) => p.status !== 'skipped')
            .reduce((sum, p) => sum + (p.status === 'done' ? 100 : p.status === 'running' ? 50 : 0), 0) /
            Math.max(1, platforms.filter((p) => p.status !== 'skipped').length)
        )
      : 15);

  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center bg-black/70 backdrop-blur-sm p-4">
      <div className="w-full max-w-md bg-[#151B2B] border border-white/10 rounded-2xl shadow-2xl overflow-hidden">
        <div className="px-6 py-5 border-b border-white/5">
          <div className="flex items-center gap-3">
            {!isDone ? (
              <Loader2 className="w-8 h-8 animate-spin text-[#FF7575] shrink-0" />
            ) : isCancelled ? (
              <Square className="w-8 h-8 text-amber-400 shrink-0" />
            ) : job?.status === 'completed' ? (
              <CheckCircle2 className="w-8 h-8 text-emerald-400 shrink-0" />
            ) : (
              <AlertCircle className="w-8 h-8 text-amber-400 shrink-0" />
            )}
            <div className="min-w-0">
              <h3 className="text-lg font-bold text-white truncate">{title}</h3>
              <p className="text-sm text-gray-400 truncate">{projectName}</p>
            </div>
          </div>
          <p className="text-sm text-gray-300 mt-3">{phaseMessage}</p>
          {job && platforms.length > 0 && (
            <p className="text-xs text-gray-500 mt-1">
              Tổng tạm thời: <span className="text-white font-semibold">{totalCount}</span> bài
            </p>
          )}
        </div>

        <div className="px-6 py-4 space-y-3">
          {(platforms.length > 0
            ? platforms
            : [
                { platform: 'facebook', label: 'Facebook', status: 'pending', count: 0, message: 'Chờ...' },
                { platform: 'youtube', label: 'YouTube', status: 'pending', count: 0, message: 'Chờ...' },
                { platform: 'tiktok', label: 'TikTok', status: 'pending', count: 0, message: 'Chờ...' },
              ]
          ).map((item) => {
            const style = PLATFORM_STYLE[item.platform] ?? { dot: 'bg-gray-400', label: item.label };
            return (
              <div
                key={item.platform}
                className="flex items-start gap-3 rounded-xl bg-[#0A101D]/80 border border-white/5 px-4 py-3"
              >
                <div className="mt-0.5">{statusIcon(item.status)}</div>
                <div className="flex-1 min-w-0">
                  <div className="flex items-center justify-between gap-2">
                    <div className="flex items-center gap-2 min-w-0">
                      <span className={`w-2 h-2 rounded-full shrink-0 ${style.dot}`} />
                      <span className="text-sm font-semibold text-white">{item.label || style.label}</span>
                    </div>
                    {item.status !== 'skipped' && item.status !== 'pending' && (
                      <span className="text-sm font-bold text-[#00B4D8] shrink-0">{item.count} bài</span>
                    )}
                  </div>
                  {item.message ? (
                    <p className="text-xs text-gray-500 mt-1 leading-relaxed">{item.message}</p>
                  ) : null}
                </div>
              </div>
            );
          })}
        </div>

        {(isRunning || progressPercent != null) && (
          <div className="px-6 pb-5 flex flex-col gap-3">
            <div className="flex justify-between text-xs text-gray-500">
              <span>Tiến độ</span>
              <span className="text-[#00B4D8] font-bold">{barPercent}%</span>
            </div>
            <div className="h-2 rounded-full bg-white/5 overflow-hidden">
              <div
                className="h-full bg-gradient-to-r from-[#FF7575] to-[#00B4D8] rounded-full transition-all duration-500"
                style={{ width: `${Math.min(100, barPercent)}%` }}
              />
            </div>
            {onCancel && (
              <button
                type="button"
                onClick={onCancel}
                disabled={isCancelling || job?.phase === 'cancelling'}
                className="w-full py-2.5 rounded-xl border border-amber-500/40 text-amber-300 text-sm font-semibold hover:bg-amber-500/10 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
              >
                {isCancelling || job?.phase === 'cancelling'
                  ? 'Đang dừng — giữ dữ liệu đã cào...'
                  : 'Dừng (giữ dữ liệu đã cào)'}
              </button>
            )}
          </div>
        )}
      </div>
    </div>
  );
}
