import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import {
  Search,
  Plus,
  ArrowRight,
  Building2,
  UserPlus,
  Loader2,
  AlertCircle,
  MoreVertical,
  LayoutDashboard,
  RefreshCw,
  Sparkles,
  Trash2,
  Pencil,
  FolderKanban,
  Radio,
  Hash,
  ChevronLeft,
  GitCompare,
  Check,
} from 'lucide-react';
import { Link, useParams, useNavigate } from 'react-router-dom';
import { projectApi } from '../api/projectApi';
import { scrapeOrderApi, type ScrapeOrder } from '../api/scrapeOrderApi';
import { workspaceApi } from '../api/workspaceApi';
import type { Project, AiAnalysisProgress } from '../types/project';
import { extractApiError } from '../utils/authStorage';
import { formatWorkspaceDate } from '../utils/workspaceHelpers';
import { useAppModal } from '../contexts/AppModalContext';
import { useScrapeJob } from '../contexts/ScrapeJobContext';

const CARD_GRADIENTS = [
  'from-[#FF7575]/15 via-transparent to-transparent',
  'from-[#00B4D8]/15 via-transparent to-transparent',
  'from-violet-500/15 via-transparent to-transparent',
  'from-emerald-500/15 via-transparent to-transparent',
];

const PLATFORM_STYLES: Record<string, string> = {
  Facebook: 'bg-blue-500/10 text-blue-400 border-blue-500/20',
  YouTube: 'bg-red-500/10 text-red-400 border-red-500/20',
  TikTok: 'bg-cyan-500/10 text-cyan-300 border-cyan-500/20',
  'Tin tức': 'bg-amber-500/10 text-amber-400 border-amber-500/20',
  Maps: 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20',
};

function pickGradientIndex(name: string) {
  let hash = 0;
  for (let i = 0; i < name.length; i++) hash = name.charCodeAt(i) + ((hash << 5) - hash);
  return Math.abs(hash) % CARD_GRADIENTS.length;
}

function getPlatformTags(project: Project) {
  const tags: string[] = [];
  if (project.enableFacebook) tags.push('Facebook');
  if (project.enableYoutube) tags.push('YouTube');
  if (project.enableTiktok) tags.push('TikTok');
  if (project.enableNews) tags.push('Tin tức');
  if (project.enableMaps) tags.push('Maps');
  return tags;
}

const ACTIVE_ORDER_STATUSES = new Set(['paid', 'scraping', 'analyzing']);

function isActiveOrder(order: ScrapeOrder) {
  return ACTIVE_ORDER_STATUSES.has(order.status);
}

function isRecentlyCompleted(order: ScrapeOrder) {
  if (order.status !== 'completed') return false;
  const doneAt = order.completedAt || order.reportReadyAt;
  if (!doneAt) return true;
  const hours = (Date.now() - new Date(doneAt).getTime()) / (1000 * 60 * 60);
  return hours < 24;
}

function ProjectCard({
  project,
  activeOrder,
  workspaceId,
  isMenuOpen,
  isBusy,
  aiProgress,
  menuRef,
  compareMode = false,
  isCompareSelected = false,
  onToggleCompare,
  onToggleMenu,
  onEnter,
  onRescrape,
  onAnalyze,
  onEdit,
  onDelete,
}: {
  project: Project;
  activeOrder?: ScrapeOrder | null;
  workspaceId: string | undefined;
  isMenuOpen: boolean;
  isBusy: boolean;
  aiProgress?: AiAnalysisProgress;
  menuRef: React.RefObject<HTMLDivElement | null> | undefined;
  compareMode?: boolean;
  isCompareSelected?: boolean;
  onToggleCompare?: () => void;
  onToggleMenu: () => void;
  onEnter: () => void;
  onRescrape: () => void;
  onAnalyze: () => void;
  onEdit: () => void;
  onDelete: () => void;
}) {
  const gradient = CARD_GRADIENTS[pickGradientIndex(project.name)];
  const platforms = getPlatformTags(project);

  return (
    <article
      onClick={() => {
        if (isBusy) return;
        if (compareMode) {
          onToggleCompare?.();
          return;
        }
        onEnter();
      }}
      className={`group relative overflow-hidden rounded-3xl border bg-[#151B2B] cursor-pointer transition-all duration-300 hover:-translate-y-0.5 ${
        isBusy
          ? 'opacity-90 pointer-events-none border-[#FF7575]/30'
          : compareMode && isCompareSelected
          ? 'border-[#4FD1C5]/60 shadow-[0_16px_40px_rgba(79,209,197,0.15)]'
          : 'border-white/10 hover:border-[#FF7575]/35 hover:shadow-[0_16px_40px_rgba(255,117,117,0.1)]'
        }`}
    >
      {isBusy && !aiProgress?.isAnalyzing && (
        <div className="absolute top-0 left-0 right-0 h-1 bg-[#1a2133] z-30 overflow-hidden">
          <div className="h-full w-1/2 bg-gradient-to-r from-transparent via-[#FF7575] to-transparent shadow-[0_0_10px_#FF7575] rounded-full animate-indeterminate" />
        </div>
      )}
      {aiProgress?.isAnalyzing && (
        <div className="absolute top-0 left-0 right-0 h-1 bg-[#1a2133] z-30 overflow-hidden">
          <div 
            className="h-full bg-gradient-to-r from-transparent via-[#FF7575] to-[#FF7575] shadow-[0_0_10px_#FF7575] transition-all duration-300"
            style={{ width: `${Math.max(5, aiProgress.progressPercent)}%` }}
          />
        </div>
      )}

      <div className={`absolute inset-0 bg-gradient-to-br ${gradient} pointer-events-none`} />

      {compareMode && (
        <button
          type="button"
          onClick={(e) => {
            e.stopPropagation();
            onToggleCompare?.();
          }}
          className={`absolute top-4 left-4 z-20 w-7 h-7 rounded-lg border flex items-center justify-center transition-all ${isCompareSelected
              ? 'bg-[#4FD1C5] border-[#4FD1C5] text-[#0A101D]'
              : 'bg-[#0A101D]/80 border-white/20 text-transparent hover:border-[#4FD1C5]/50'
            }`}
          aria-label={isCompareSelected ? 'Bỏ chọn' : 'Chọn để so sánh'}
        >
          <Check size={14} className={isCompareSelected ? 'opacity-100' : 'opacity-0'} />
        </button>
      )}

      <div className="relative p-6 flex flex-col min-h-[260px]">
        <div className="flex items-start justify-between gap-3 mb-4">
          <div className="flex items-center gap-3 min-w-0 flex-1">
            <div className="w-11 h-11 rounded-xl bg-[#0A101D] border border-white/10 flex items-center justify-center text-[#FF7575] font-bold text-lg shrink-0 group-hover:border-[#FF7575]/30 transition-colors">
              {project.name.charAt(0).toUpperCase()}
            </div>
            <div className="min-w-0">
              <h3 className="text-lg font-bold text-white line-clamp-1 group-hover:text-[#FF7575] transition-colors">
                {project.name}
              </h3>
              {project.description ? (
                <p className="text-xs text-gray-500 mt-0.5 line-clamp-1">{project.description}</p>
              ) : (
                <p className="text-xs text-gray-600 mt-0.5">Dự án giám sát mạng xã hội</p>
              )}
            </div>
          </div>

          <div className="relative shrink-0" ref={isMenuOpen ? menuRef : undefined}>
            {!compareMode && (
              <button
                type="button"
                onClick={(e) => {
                  e.stopPropagation();
                  onToggleMenu();
                }}
                disabled={isBusy}
                className="p-2 rounded-xl text-gray-500 hover:text-white hover:bg-white/10 opacity-100 sm:opacity-0 sm:group-hover:opacity-100 focus:opacity-100 transition-all disabled:opacity-40"
                aria-label="Tùy chọn dự án"
              >
                {isBusy ? <Loader2 size={18} className="animate-spin" /> : <MoreVertical size={18} />}
              </button>
            )}

            {isMenuOpen && !compareMode && (
              <div className="absolute right-0 top-10 w-52 bg-[#1A2235] border border-white/10 rounded-xl shadow-2xl z-50 py-1.5 overflow-hidden">
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation();
                    onEnter();
                  }}
                  className="w-full flex items-center gap-2.5 px-4 py-2.5 text-sm text-gray-300 hover:text-white hover:bg-white/5"
                >
                  <LayoutDashboard size={16} className="text-[#00B4D8]" />
                  Vào Dashboard
                </button>
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation();
                    onRescrape();
                  }}
                  className="w-full flex items-center gap-2.5 px-4 py-2.5 text-sm text-gray-300 hover:text-white hover:bg-white/5"
                >
                  <RefreshCw size={16} className="text-emerald-400" />
                  Cào lại dữ liệu
                </button>
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation();
                    onAnalyze();
                  }}
                  className="w-full flex items-center gap-2.5 px-4 py-2.5 text-sm text-gray-300 hover:text-white hover:bg-white/5"
                >
                  <Sparkles size={16} className="text-amber-400" />
                  Phân tích AI
                </button>
                <div className="h-px bg-white/5 my-1" />
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation();
                    onEdit();
                  }}
                  className="w-full flex items-center gap-2.5 px-4 py-2.5 text-sm text-gray-300 hover:text-white hover:bg-white/5"
                >
                  <Pencil size={16} className="text-violet-400" />
                  Chỉnh sửa
                </button>
                <button
                  type="button"
                  onClick={(e) => {
                    e.stopPropagation();
                    onDelete();
                  }}
                  className="w-full flex items-center gap-2.5 px-4 py-2.5 text-sm text-red-400 hover:text-red-300 hover:bg-red-500/10"
                >
                  <Trash2 size={16} />
                  Xóa dự án
                </button>
              </div>
            )}
          </div>
        </div>

        {project.searchQuery && (
          <div className="flex items-center gap-2 text-xs text-gray-400 bg-white/[0.03] border border-white/5 rounded-xl px-3 py-2 mb-4">
            <Hash size={13} className="text-[#00B4D8] shrink-0" />
            <span className="truncate font-medium text-gray-300">{project.searchQuery}</span>
          </div>
        )}

        <div className="flex flex-wrap gap-1.5 mb-5">
          {platforms.length > 0 ? (
            platforms.map((tag) => (
              <span
                key={tag}
                className={`text-[10px] font-semibold uppercase tracking-wide px-2.5 py-1 rounded-lg border ${PLATFORM_STYLES[tag] ?? 'bg-white/5 text-gray-400 border-white/10'}`}
              >
                {tag}
              </span>
            ))
          ) : (
            <span className="text-xs text-gray-600">Chưa cấu hình nguồn</span>
          )}
        </div>

        <div className="grid grid-cols-2 gap-2 mb-5">
          <div className="rounded-xl bg-white/[0.03] border border-white/5 px-3 py-2">
            <p className="text-base font-bold text-white tabular-nums">{project.dataSourceCount}</p>
            <p className="text-[10px] text-gray-500 uppercase tracking-wide">Nguồn dữ liệu</p>
          </div>
          <div className="rounded-xl bg-white/[0.03] border border-white/5 px-3 py-2">
            <p className="text-sm font-semibold text-gray-300 leading-tight mt-0.5">
              {formatWorkspaceDate(project.createdAt)}
            </p>
            <p className="text-[10px] text-gray-500 uppercase tracking-wide mt-1">Ngày tạo</p>
          </div>
        </div>

        {activeOrder && isActiveOrder(activeOrder) && (
          <div
            className="mb-4 rounded-xl bg-[#0A101D]/80 border border-[#FF7575]/20 px-3 py-3"
            onClick={(e) => e.stopPropagation()}
          >
            <div className="flex items-center justify-between text-xs mb-2">
              <span className="text-[#FF7575] font-semibold">{activeOrder.statusLabel}</span>
              <span className="text-[#00B4D8] font-bold tabular-nums">{activeOrder.progressPercent}%</span>
            </div>
            <div className="h-2 rounded-full bg-white/5 overflow-hidden mb-2">
              <div
                className="h-full rounded-full bg-gradient-to-r from-[#FF7575] to-[#00B4D8] transition-all duration-700"
                style={{ width: `${Math.min(100, Math.max(0, activeOrder.progressPercent))}%` }}
              />
            </div>
            {activeOrder.statusMessage && (
              <p className="text-[11px] text-gray-500 line-clamp-2 mb-2">{activeOrder.statusMessage}</p>
            )}
            <Link
              to={`/workspace/${workspaceId}/orders/${activeOrder.orderId}`}
              className="text-[11px] font-semibold text-[#00B4D8] hover:text-white transition-colors"
            >
              Xem chi tiết tiến độ →
            </Link>
          </div>
        )}

        <div className="mt-auto flex items-center justify-between gap-3 pt-4 border-t border-white/10">
          {isBusy || aiProgress?.isAnalyzing ? (
            <span className="inline-flex items-center gap-2 text-[11px] font-semibold text-[#FF7575] tracking-wide uppercase">
              <span className="relative flex h-2 w-2">
                <span className="animate-ping absolute inline-flex h-full w-full rounded-full bg-[#FF7575] opacity-75"></span>
                <span className="relative inline-flex rounded-full h-2 w-2 bg-[#FF7575]"></span>
              </span>
              Đang phân tích AI... {aiProgress?.isAnalyzing ? `${aiProgress.progressPercent}%` : ''}
            </span>
          ) : activeOrder && isActiveOrder(activeOrder) ? (
            <span className="inline-flex items-center gap-1.5 text-[11px] font-medium text-[#FF7575]">
              <Loader2 size={12} className="animate-spin" />
              Đang xử lý đơn cào
            </span>
          ) : activeOrder && isRecentlyCompleted(activeOrder) ? (
            <span className="inline-flex items-center gap-1.5 text-[11px] font-medium text-emerald-400">
              <span className="w-1.5 h-1.5 rounded-full bg-emerald-400" />
              Báo cáo sẵn sàng
            </span>
          ) : (
            <span className="inline-flex items-center gap-1.5 text-[11px] font-medium text-emerald-400">
              <span className="w-1.5 h-1.5 rounded-full bg-emerald-400 animate-pulse" />
              Đang giám sát
            </span>
          )}
          <span className="inline-flex items-center gap-1.5 text-sm font-semibold text-[#FF7575] group-hover:gap-2.5 transition-all">
            Phân tích
            <ArrowRight size={15} />
          </span>
        </div>
      </div>
    </article>
  );
}

const Projects = () => {
  const { workspaceId } = useParams();
  const navigate = useNavigate();
  const wid = Number(workspaceId);
  const { confirm, alert } = useAppModal();
  const { startKeywordScrape } = useScrapeJob();

  const [workspaceName, setWorkspaceName] = useState('');
  const [projectList, setProjectList] = useState<Project[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');
  const [openMenuId, setOpenMenuId] = useState<number | null>(null);
  const [actionProjectId, setActionProjectId] = useState<number | null>(null);
  const [orderByProject, setOrderByProject] = useState<Record<number, ScrapeOrder>>({});
  const [compareMode, setCompareMode] = useState(false);
  const [selectedCompareIds, setSelectedCompareIds] = useState<number[]>([]);
  const [aiProgressByProject, setAiProgressByProject] = useState<Record<number, AiAnalysisProgress>>({});
  const menuRef = useRef<HTMLDivElement>(null);

  const loadProjects = useCallback(async () => {
    if (!wid || Number.isNaN(wid)) return;

    setIsLoading(true);
    setErrorMessage('');
    try {
      const [ws, projects] = await Promise.all([
        workspaceApi.getById(wid),
        projectApi.getProjects(wid),
      ]);
      setWorkspaceName(ws.name);
      setProjectList(projects);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải danh sách dự án.'));
    } finally {
      setIsLoading(false);
    }
  }, [wid]);

  const loadScrapeOrders = useCallback(async () => {
    if (!wid || Number.isNaN(wid)) return;
    try {
      const orders = await scrapeOrderApi.list(wid);
      const map: Record<number, ScrapeOrder> = {};
      for (const order of orders) {
        const existing = map[order.projectId];
        if (!existing || new Date(order.createdAt) > new Date(existing.createdAt)) {
          map[order.projectId] = order;
        }
      }
      setOrderByProject(map);
    } catch {
      /* không chặn trang nếu API đơn lỗi */
    }
  }, [wid]);

  const loadAiProgress = useCallback(async () => {
    if (!wid || Number.isNaN(wid)) return;
    try {
      const progressMap = await projectApi.getWorkspaceAnalyzeProgress(wid);
      setAiProgressByProject(progressMap);
    } catch {
      /* ignore */
    }
  }, [wid]);

  useEffect(() => {
    loadProjects();
    loadScrapeOrders();
    loadAiProgress();
  }, [loadProjects, loadScrapeOrders, loadAiProgress]);

  const hasActiveOrders = useMemo(
    () => Object.values(orderByProject).some(isActiveOrder),
    [orderByProject]
  );

  const hasAnalyzing = useMemo(
    () => Object.values(aiProgressByProject).some((p) => p.isAnalyzing),
    [aiProgressByProject]
  );

  useEffect(() => {
    if (!hasActiveOrders && !hasAnalyzing) return;

    const tick = () => {
      if (document.visibilityState === 'visible') {
        // Use refs or current state checks if necessary, but since the effect
        // restarts when these booleans change, the closure values are fresh enough
        // to handle the current polling state.
        if (hasActiveOrders) loadScrapeOrders();
        if (hasAnalyzing) loadAiProgress();
      }
    };
    tick();
    const timer = setInterval(tick, 3000);
    return () => clearInterval(timer);
  }, [hasActiveOrders, hasAnalyzing, loadScrapeOrders, loadAiProgress]);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setOpenMenuId(null);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const filteredProjects = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    if (!q) return projectList;
    return projectList.filter(
      (p) =>
        p.name.toLowerCase().includes(q) ||
        (p.searchQuery?.toLowerCase().includes(q) ?? false) ||
        (p.description?.toLowerCase().includes(q) ?? false)
    );
  }, [projectList, searchQuery]);

  const stats = useMemo(() => {
    const sources = projectList.reduce((sum, p) => sum + p.dataSourceCount, 0);
    const withKeyword = projectList.filter((p) => p.searchQuery).length;
    return { total: projectList.length, sources, withKeyword };
  }, [projectList]);

  const handleDeleteProject = async (project: Project) => {
    const confirmed = await confirm({
      title: 'Xóa dự án',
      message: `Bạn có chắc muốn xóa "${project.name}"?\nDữ liệu đã cào và phân tích AI sẽ không thể khôi phục.`,
      confirmText: 'Xóa dự án',
      cancelText: 'Hủy bỏ',
      type: 'danger',
    });
    if (!confirmed) return;

    setActionProjectId(project.projectId);
    setOpenMenuId(null);

    try {
      await projectApi.delete(wid, project.projectId);
      setProjectList((prev) => prev.filter((p) => p.projectId !== project.projectId));
      await alert({
        title: 'Đã xóa dự án',
        message: `"${project.name}" đã được xóa khỏi workspace.`,
        type: 'success',
      });
    } catch (error) {
      await alert({
        title: 'Không thể xóa',
        message: extractApiError(error, 'Không thể xóa dự án.'),
        type: 'error',
      });
    } finally {
      setActionProjectId(null);
    }
  };

  const handleRescrape = (project: Project) => {
    setOpenMenuId(null);
    setErrorMessage('');
    startKeywordScrape({
      projectId: project.projectId,
      projectName: project.name,
      workspaceId: wid,
      onComplete: loadProjects,
    });
  };

  const handleAnalyze = async (project: Project) => {
    setActionProjectId(project.projectId);
    setOpenMenuId(null);
    setErrorMessage('');

    try {
      const result = await projectApi.analyze(wid, project.projectId, true);
      // Set ngay trạng thái đang phân tích để effect polling khởi động;
      // các lần poll sau sẽ đồng bộ lại với backend.
      setAiProgressByProject((prev) => ({
        ...prev,
        [project.projectId]: { isAnalyzing: true, progressPercent: 0 },
      }));
      await alert({
        title: 'Phân tích AI',
        message: result.message,
        type: 'success',
      });
      loadAiProgress();
    } catch (error) {
      await alert({
        title: 'Phân tích thất bại',
        message: extractApiError(error, 'Không thể phân tích AI.'),
        type: 'error',
      });
    } finally {
      setActionProjectId(null);
    }
  };

  const enterProject = (projectId: number) => {
    navigate(`/workspace/${workspaceId}/project/${projectId}`);
  };

  const toggleCompareSelection = (projectId: number) => {
    setSelectedCompareIds((prev) => {
      if (prev.includes(projectId)) return prev.filter((id) => id !== projectId);
      if (prev.length >= 2) return [prev[1], projectId];
      return [...prev, projectId];
    });
  };

  const exitCompareMode = () => {
    setCompareMode(false);
    setSelectedCompareIds([]);
    setOpenMenuId(null);
  };

  const handleStartCompare = () => {
    if (selectedCompareIds.length !== 2) return;
    const [leftId, rightId] = selectedCompareIds;
    navigate(`/workspace/${wid}/project/${leftId}/comparison`, {
      state: { compareWith: rightId },
    });
  };

  const selectedCompareProjects = useMemo(
    () =>
      selectedCompareIds
        .map((id) => projectList.find((p) => p.projectId === id))
        .filter((p): p is Project => Boolean(p)),
    [selectedCompareIds, projectList]
  );

  return (
    <div className="animate-in fade-in duration-500 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 pb-16 space-y-8">
      {/* Hero */}
      <div className="relative overflow-hidden rounded-3xl border border-white/5 bg-gradient-to-br from-[#151B2B] via-[#141A2C] to-[#0A101D] p-8 sm:p-10">
        <div className="absolute -top-20 -right-16 w-64 h-64 bg-[#FF7575]/10 rounded-full blur-3xl pointer-events-none" />
        <div className="absolute -bottom-12 -left-12 w-48 h-48 bg-[#00B4D8]/10 rounded-full blur-3xl pointer-events-none" />

        <div className="relative flex flex-col lg:flex-row lg:items-center lg:justify-between gap-6">
          <div className="space-y-4">
            <Link
              to="/workspaces"
              className="inline-flex items-center gap-1.5 text-xs text-gray-500 hover:text-white transition-colors"
            >
              <ChevronLeft size={14} />
              Quay lại workspace
            </Link>

            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-white/5 border border-white/10 text-xs text-gray-400">
              <Building2 className="w-3.5 h-3.5 text-[#FF7575]" />
              {workspaceName || `Workspace #${workspaceId}`}
            </div>

            <div>
              <h1 className="text-3xl sm:text-4xl font-extrabold text-white tracking-tight mb-2">
                Dự án Giám sát
              </h1>
              <p className="text-gray-400 text-sm sm:text-base max-w-xl">
                Quản lý chiến dịch cào dữ liệu, phân tích sentiment và báo cáo trên mạng xã hội.
              </p>
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-3 shrink-0">
            <Link
              to={`/workspace/${workspaceId}/members`}
              className="inline-flex items-center gap-2 px-4 py-3 rounded-2xl text-sm font-semibold text-gray-300 bg-white/5 border border-white/10 hover:bg-white/10 hover:text-white transition-colors"
            >
              <UserPlus size={16} />
              Thành viên
            </Link>
            {projectList.length >= 2 && (
              <button
                type="button"
                onClick={() => {
                  if (compareMode) exitCompareMode();
                  else setCompareMode(true);
                }}
                className={`inline-flex items-center gap-2 px-4 py-3 rounded-2xl text-sm font-semibold border transition-colors ${compareMode
                    ? 'bg-[#4FD1C5]/15 text-[#4FD1C5] border-[#4FD1C5]/30'
                    : 'text-gray-300 bg-white/5 border-white/10 hover:bg-white/10 hover:text-white'
                  }`}
              >
                <GitCompare size={16} />
                {compareMode ? 'Hủy so sánh' : 'So sánh dự án'}
              </button>
            )}
            <Link
              to={`/create-project?wid=${workspaceId}`}
              className="inline-flex items-center gap-2 bg-[#FF7575] hover:bg-[#ff6262] text-white px-5 py-3 rounded-2xl text-sm font-bold shadow-[0_8px_30px_rgba(255,117,117,0.3)] transition-colors"
            >
              <Plus size={18} />
              Tạo dự án mới
            </Link>

            <Link
              to={`/workspace/${workspaceId}/project/bespoke-reports`}
              className="inline-flex items-center gap-2 bg-[#FF7575] hover:bg-[#ff6262] text-white px-5 py-3 rounded-2xl text-sm font-bold shadow-[0_8px_30px_rgba(255,117,117,0.3)] transition-colors"
            >
              <Plus size={18} />
              Tạo báo cáo chuyên sâu
            </Link>
          </div>
        </div>

        {!isLoading && projectList.length > 0 && (
          <div className="relative grid grid-cols-3 gap-3 mt-8 pt-8 border-t border-white/5">
            {[
              { label: 'Dự án', value: stats.total, icon: FolderKanban, color: 'text-[#FF7575]' },
              { label: 'Nguồn dữ liệu', value: stats.sources, icon: Radio, color: 'text-[#00B4D8]' },
              { label: 'Có từ khóa', value: stats.withKeyword, icon: Hash, color: 'text-emerald-400' },
            ].map(({ label, value, icon: Icon, color }) => (
              <div
                key={label}
                className="rounded-2xl bg-white/[0.03] border border-white/5 px-4 py-3 flex items-center gap-3"
              >
                <div className={`w-9 h-9 rounded-xl bg-white/5 flex items-center justify-center ${color}`}>
                  <Icon size={16} />
                </div>
                <div>
                  <p className="text-lg font-bold text-white tabular-nums">{value}</p>
                  <p className="text-[10px] text-gray-500 uppercase tracking-wide">{label}</p>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {compareMode && projectList.length >= 2 && (
        <div className="rounded-2xl border border-[#4FD1C5]/25 bg-[#4FD1C5]/10 px-4 py-3 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
          <div className="text-sm text-[#a7f3ec]">
            <span className="font-semibold text-white">Chế độ so sánh:</span> chọn đúng{' '}
            <span className="font-bold text-[#4FD1C5]">2 dự án</span> ({selectedCompareIds.length}/2)
            {selectedCompareProjects.length > 0 && (
              <span className="block sm:inline sm:ml-2 text-[#7dd3fc] mt-1 sm:mt-0">
                {selectedCompareProjects.map((p) => p.name).join(' vs ')}
              </span>
            )}
          </div>
          <div className="flex items-center gap-2 shrink-0">
            <button
              type="button"
              onClick={exitCompareMode}
              className="px-4 py-2 rounded-xl text-sm font-semibold text-gray-300 border border-white/10 hover:bg-white/5"
            >
              Hủy
            </button>
            <button
              type="button"
              onClick={handleStartCompare}
              disabled={selectedCompareIds.length !== 2}
              className="inline-flex items-center gap-2 px-4 py-2 rounded-xl text-sm font-bold bg-[#4FD1C5] hover:bg-[#3dbfb3] text-[#0A101D] disabled:opacity-40 disabled:cursor-not-allowed transition-colors"
            >
              <GitCompare size={16} />
              So sánh ngay
            </button>
          </div>
        </div>
      )}

      {errorMessage && (
        <div className="bg-red-500/10 border border-red-500/20 text-red-300 p-4 rounded-2xl flex items-center gap-3 text-sm">
          <AlertCircle className="w-5 h-5 shrink-0" />
          <span className="flex-1">{errorMessage}</span>
          <button
            type="button"
            onClick={async () => {
              await loadProjects();
              await loadScrapeOrders();
            }}
            className="inline-flex items-center gap-1.5 text-red-200 hover:text-white font-semibold text-xs"
          >
            <RefreshCw size={14} />
            Thử lại
          </button>
        </div>
      )}

      {!isLoading && projectList.length > 0 && (
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div className="relative flex-1 max-w-md">
            <Search className="absolute left-4 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-500" />
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Tìm theo tên dự án hoặc từ khóa..."
              className="w-full pl-11 pr-4 py-3 bg-[#151B2B] border border-white/10 rounded-2xl text-sm text-white placeholder:text-gray-600 focus:outline-none focus:border-[#FF7575]/40 focus:ring-1 focus:ring-[#FF7575]/20 transition-colors"
            />
          </div>
          <p className="text-sm text-gray-500">
            <span className="text-white font-semibold">{filteredProjects.length}</span> / {projectList.length} dự án
          </p>
        </div>
      )}

      {isLoading ? (
        <div className="flex flex-col items-center justify-center py-32 text-gray-400 gap-4">
          <div className="relative">
            <div className="absolute inset-0 rounded-full bg-[#FF7575]/20 blur-2xl animate-pulse" />
            <Loader2 className="relative w-12 h-12 animate-spin text-[#FF7575]" />
          </div>
          <p className="text-sm font-medium">Đang tải dự án...</p>
        </div>
      ) : projectList.length === 0 && !errorMessage ? (
        <div className="relative overflow-hidden rounded-3xl border border-white/10 bg-[#151B2B] p-12 sm:p-16 text-center">
          <div className="absolute inset-0 bg-gradient-to-br from-[#00B4D8]/5 to-transparent pointer-events-none" />
          <div className="relative">
            <div className="w-20 h-20 rounded-3xl bg-gradient-to-br from-[#00B4D8] to-blue-600 flex items-center justify-center mx-auto mb-6 shadow-[0_12px_40px_rgba(0,180,216,0.25)]">
              <FolderKanban className="w-10 h-10 text-white" />
            </div>
            <h3 className="text-2xl sm:text-3xl font-bold text-white mb-3">Chưa có dự án nào</h3>
            <p className="text-gray-400 mb-8 max-w-lg mx-auto leading-relaxed">
              Tạo dự án đầu tiên để cào mentions từ Facebook, YouTube, TikTok và chạy phân tích AI sentiment.
            </p>
            <Link
              to={`/create-project?wid=${workspaceId}&onboarding=1`}
              className="inline-flex items-center gap-2 bg-[#FF7575] hover:bg-[#ff6262] text-white px-8 py-3.5 rounded-2xl text-sm font-bold shadow-[0_8px_30px_rgba(255,117,117,0.3)] transition-colors"
            >
              <Plus size={18} />
              Khởi tạo dự án đầu tiên
            </Link>
          </div>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
          {filteredProjects.map((project) => {
            const isBusy = actionProjectId === project.projectId || aiProgressByProject[project.projectId]?.isAnalyzing;
            return (
              <ProjectCard
                key={project.projectId}
                project={project}
                activeOrder={orderByProject[project.projectId]}
                workspaceId={workspaceId}
                compareMode={compareMode}
                isCompareSelected={selectedCompareIds.includes(project.projectId)}
                onToggleCompare={() => toggleCompareSelection(project.projectId)}
                isMenuOpen={openMenuId === project.projectId}
                isBusy={isBusy}
                aiProgress={aiProgressByProject[project.projectId]}
                menuRef={openMenuId === project.projectId ? menuRef : undefined}
                onToggleMenu={() =>
                  setOpenMenuId(openMenuId === project.projectId ? null : project.projectId)
                }
                onEnter={() => {
                  setOpenMenuId(null);
                  enterProject(project.projectId);
                }}
                onRescrape={() => handleRescrape(project)}
                onAnalyze={() => handleAnalyze(project)}
                onEdit={() => {
                  setOpenMenuId(null);
                  navigate(`/workspace/${workspaceId}/project/${project.projectId}/edit`);
                }}
                onDelete={() => handleDeleteProject(project)}
              />
            );
          })}

          {filteredProjects.length === 0 && searchQuery && (
            <div className="md:col-span-2 xl:col-span-3 rounded-3xl border border-white/10 bg-[#151B2B] p-12 text-center">
              <Search className="w-10 h-10 text-gray-600 mx-auto mb-4" />
              <p className="text-white font-semibold mb-1">Không tìm thấy dự án</p>
              <p className="text-sm text-gray-500">Thử từ khóa khác hoặc xóa bộ lọc.</p>
            </div>
          )}

          {!searchQuery && (
            <Link
              to={`/create-project?wid=${workspaceId}`}
              className="group rounded-3xl border-2 border-dashed border-white/10 hover:border-[#FF7575]/40 bg-[#151B2B]/50 hover:bg-[#FF7575]/[0.03] p-6 flex flex-col items-center justify-center gap-4 min-h-[260px] transition-all"
            >
              <div className="w-14 h-14 rounded-2xl bg-white/5 border border-white/10 flex items-center justify-center group-hover:bg-[#FF7575]/15 group-hover:border-[#FF7575]/30 transition-all">
                <Plus size={26} className="text-gray-500 group-hover:text-[#FF7575]" />
              </div>
              <div className="text-center">
                <p className="font-bold text-gray-300 group-hover:text-white transition-colors">Tạo dự án mới</p>
                <p className="text-xs text-gray-600 mt-1">Thêm chiến dịch giám sát</p>
              </div>
            </Link>
          )}
        </div>
      )}

    </div>
  );
};

export default Projects;
