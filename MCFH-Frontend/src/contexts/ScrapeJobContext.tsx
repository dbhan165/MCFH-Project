import {
  createContext,
  useCallback,
  useContext,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import { scrapeApi } from '../api/projectApi';
import type { ScrapeJobStatus } from '../types/project';
import { extractApiError } from '../utils/authStorage';
import { useAppModal } from './AppModalContext';
import ScrapeProgressModal from '../components/scraping/ScrapeProgressModal';
import ScrapeProgressFloating from '../components/scraping/ScrapeProgressFloating';

type ScrapeJobSession = {
  projectId: number;
  projectName: string;
  workspaceId: number;
  job: ScrapeJobStatus | null;
  jobId: string | null;
  isCancelling: boolean;
  isRunning: boolean;
};

export type StartKeywordScrapeOptions = {
  projectId: number;
  projectName: string;
  workspaceId: number;
  postedSinceDays?: number | null;
  onComplete?: () => void;
};

type ScrapeJobContextValue = {
  isRunning: boolean;
  startKeywordScrape: (options: StartKeywordScrapeOptions) => void;
  minimizeProgress: () => void;
  expandProgress: () => void;
  cancelActiveJob: () => Promise<void>;
};

const ScrapeJobContext = createContext<ScrapeJobContextValue | null>(null);

function countScrapeRecords(result: {
  facebook?: unknown[];
  youTube?: unknown[];
  tikTok?: unknown[];
}) {
  return (result.facebook?.length ?? 0) + (result.youTube?.length ?? 0) + (result.tikTok?.length ?? 0);
}

async function notifyScrapeResult(
  alert: ReturnType<typeof useAppModal>['alert'],
  projectName: string,
  result: Awaited<ReturnType<typeof scrapeApi.byKeyword>>
) {
  const count = countScrapeRecords(result);

  if (result.cancelled) {
    await alert({
      title: 'Đã dừng cào dữ liệu',
      message:
        count > 0
          ? result.message || `Đã giữ ${count} bản ghi cho "${projectName}".`
          : result.message || 'Không có bản ghi nào được lưu trước khi dừng.',
      type: count > 0 ? 'success' : 'warning',
    });
    return;
  }

  if (result.errorMessage && count === 0) {
    throw new Error(result.errorMessage);
  }

  if (count === 0 && result.message?.includes('đã cào trước')) {
    await alert({
      title: 'Cào lại hoàn tất',
      message: result.message,
      type: 'success',
    });
    return;
  }

  if (count === 0) {
    const detail = result.errors?.length
      ? result.errors.join('\n')
      : 'Không thu thập được dữ liệu. Kiểm tra Playwright, cookie Facebook và từ khóa.';
    await alert({
      title: 'Không có dữ liệu mới',
      message: `Đã thu thập 0 bản ghi cho "${projectName}".\n\n${detail}`,
      type: 'warning',
    });
    return;
  }

  await alert({
    title: 'Cào dữ liệu hoàn tất',
    message: result.message || `Đã thu thập ${count} bản ghi cho "${projectName}".`,
    type: 'success',
  });

  if (result.errors?.length) {
    await alert({
      title: 'Một số nguồn gặp lỗi',
      message: result.errors.join('\n'),
      type: 'warning',
    });
  }
}

export function ScrapeJobProvider({ children }: { children: ReactNode }) {
  const { alert } = useAppModal();
  const onCompleteRef = useRef<(() => void) | null>(null);
  const [session, setSession] = useState<ScrapeJobSession | null>(null);
  const [modalExpanded, setModalExpanded] = useState(false);

  const startKeywordScrape = useCallback(
    (options: StartKeywordScrapeOptions) => {
      if (session?.isRunning) {
        void alert({
          title: 'Đang cào dữ liệu',
          message: `Tiến trình "${session.projectName}" vẫn đang chạy. Mở lại tiến trình hoặc dừng trước khi cào dự án khác.`,
          type: 'warning',
        });
        setModalExpanded(true);
        return;
      }

      onCompleteRef.current = options.onComplete ?? null;
      setSession({
        projectId: options.projectId,
        projectName: options.projectName,
        workspaceId: options.workspaceId,
        job: null,
        jobId: null,
        isCancelling: false,
        isRunning: true,
      });
      setModalExpanded(true);

      void (async () => {
        try {
          const result = await scrapeApi.byKeyword(
            options.projectId,
            (job) => {
              setSession((prev) =>
                prev
                  ? {
                      ...prev,
                      job,
                      jobId: job.jobId || prev.jobId,
                      isCancelling: prev.isCancelling || job.phase === 'cancelling',
                    }
                  : prev
              );
            },
            options.postedSinceDays
          );

          setSession((prev) => (prev ? { ...prev, isRunning: false } : prev));
          await notifyScrapeResult(alert, options.projectName, result);
          onCompleteRef.current?.();
        } catch (error) {
          setSession((prev) => (prev ? { ...prev, isRunning: false } : prev));
          await alert({
            title: 'Cào dữ liệu thất bại',
            message: extractApiError(error, 'Không thể cào dữ liệu.'),
            type: 'error',
          });
        } finally {
          setSession(null);
          setModalExpanded(false);
          onCompleteRef.current = null;
        }
      })();
    },
    [alert, session?.isRunning, session?.projectName]
  );

  const minimizeProgress = useCallback(() => {
    if (session?.isRunning) setModalExpanded(false);
  }, [session?.isRunning]);

  const expandProgress = useCallback(() => {
    if (session) setModalExpanded(true);
  }, [session]);

  const cancelActiveJob = useCallback(async () => {
    if (!session?.jobId || session.isCancelling) return;
    setSession((prev) => (prev ? { ...prev, isCancelling: true } : prev));
    try {
      await scrapeApi.cancelJob(session.jobId);
    } catch {
      setSession((prev) => (prev ? { ...prev, isCancelling: false } : prev));
    }
  }, [session?.jobId, session?.isCancelling]);

  const value = useMemo(
    () => ({
      isRunning: Boolean(session?.isRunning),
      startKeywordScrape,
      minimizeProgress,
      expandProgress,
      cancelActiveJob,
    }),
    [session?.isRunning, startKeywordScrape, minimizeProgress, expandProgress, cancelActiveJob]
  );

  const showFloating = Boolean(session && !modalExpanded);
  const showModal = Boolean(session && modalExpanded);

  return (
    <ScrapeJobContext.Provider value={value}>
      {children}

      {showFloating ? (
        <ScrapeProgressFloating
          projectName={session!.projectName}
          job={session!.job}
          isRunning={session!.isRunning}
          onExpand={expandProgress}
        />
      ) : null}

      <ScrapeProgressModal
        open={showModal}
        projectName={session?.projectName ?? ''}
        job={session?.job ?? null}
        isCancelling={session?.isCancelling}
        onMinimize={session?.isRunning ? minimizeProgress : undefined}
        onCancel={
          session?.jobId && session.isRunning
            ? () => {
                void cancelActiveJob();
              }
            : undefined
        }
      />
    </ScrapeJobContext.Provider>
  );
}

export function useScrapeJob(): ScrapeJobContextValue {
  const ctx = useContext(ScrapeJobContext);
  if (!ctx) {
    throw new Error('useScrapeJob phải dùng bên trong ScrapeJobProvider');
  }
  return ctx;
}
