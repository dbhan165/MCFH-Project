import { Loader2, Maximize2 } from 'lucide-react';
import type { ScrapeJobStatus } from '../../types/project';

type ScrapeProgressFloatingProps = {
  projectName: string;
  job: ScrapeJobStatus | null;
  isRunning: boolean;
  onExpand: () => void;
};

function calcPercent(job: ScrapeJobStatus | null) {
  const platforms = job?.platforms ?? [];
  if (platforms.length === 0) return 15;
  return Math.round(
    platforms
      .filter((p) => p.status !== 'skipped')
      .reduce((sum, p) => sum + (p.status === 'done' ? 100 : p.status === 'running' ? 50 : 0), 0) /
      Math.max(1, platforms.filter((p) => p.status !== 'skipped').length)
  );
}

export default function ScrapeProgressFloating({
  projectName,
  job,
  isRunning,
  onExpand,
}: ScrapeProgressFloatingProps) {
  const percent = calcPercent(job);
  const phaseMessage =
    job?.phaseMessage ||
    (job?.phase === 'analyzing'
      ? 'Đang phân tích AI...'
      : job?.phase === 'starting'
        ? 'Đang khởi động...'
        : 'Đang thu thập dữ liệu...');

  return (
    <button
      type="button"
      onClick={onExpand}
      className="fixed bottom-6 right-6 z-[55] max-w-sm w-[min(100vw-2rem,22rem)] text-left rounded-2xl border border-[#FF7575]/30 bg-[#151B2B]/95 backdrop-blur-md shadow-2xl shadow-black/40 p-4 hover:border-[#FF7575]/50 transition-all group"
      aria-label="Mở tiến trình cào dữ liệu"
    >
      <div className="flex items-start gap-3">
        {isRunning ? (
          <Loader2 className="w-5 h-5 animate-spin text-[#FF7575] shrink-0 mt-0.5" />
        ) : (
          <Maximize2 className="w-5 h-5 text-[#00B4D8] shrink-0 mt-0.5" />
        )}
        <div className="flex-1 min-w-0">
          <p className="text-sm font-bold text-white truncate">{projectName}</p>
          <p className="text-xs text-gray-400 mt-0.5 truncate">{phaseMessage}</p>
          <div className="mt-3 flex items-center gap-2">
            <div className="flex-1 h-1.5 rounded-full bg-white/10 overflow-hidden">
              <div
                className="h-full bg-gradient-to-r from-[#FF7575] to-[#00B4D8] rounded-full transition-all duration-500"
                style={{ width: `${Math.min(100, percent)}%` }}
              />
            </div>
            <span className="text-[11px] font-bold text-[#00B4D8] tabular-nums">{percent}%</span>
          </div>
          <p className="text-[11px] text-gray-500 mt-2 group-hover:text-gray-300 transition-colors">
            Bấm để xem chi tiết hoặc dừng tiến trình
          </p>
        </div>
      </div>
    </button>
  );
}
