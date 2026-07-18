import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  MessageCircle,
  ExternalLink,
  Loader2,
  AlertCircle,
  Sparkles,
  ChevronDown,
  ChevronUp,
  RefreshCw,
  Search,
  BookmarkPlus,
  MoreVertical,
  Trash2,
  Copy,
  Filter,
  X,
  MessagesSquare,
  Tag,
  VolumeX,
  AlertTriangle,
  BrainCircuit,
} from 'lucide-react';
import { projectApi } from '../api/projectApi';
import type { AiAnalysisProgress, ProjectMention, MentionTag } from '../types/project';
import { extractApiError } from '../utils/authStorage';
import { formatWorkspaceDateTime } from '../utils/workspaceHelpers';
import { useAppModal } from '../contexts/AppModalContext';
import {
  formatNumber,
  getPlatformBadgeClass,
  getPlatformChartColor,
  getPlatformDisplayName,
  getPlatformLabel,
  getSentimentBadgeClass,
  getSentimentFilterBadgeClass,
  getSentimentFilterLabel,
  getSentimentLabel,
  MENTION_PLATFORMS,
  MENTION_SENTIMENTS,
  SENTIMENT_COLORS,
  type MentionPlatformFilter,
  type MentionSentimentFilter,
} from '../utils/sentimentHelpers';

const SENTIMENT_ACCENT: Record<string, string> = {
  positive: SENTIMENT_COLORS.positive,
  negative: SENTIMENT_COLORS.negative,
  neutral: SENTIMENT_COLORS.neutral,
  pending: SENTIMENT_COLORS.pending,
};

function getSentimentAccent(sentiment: string | null | undefined) {
  return SENTIMENT_ACCENT[sentiment?.toLowerCase() ?? 'pending'] ?? SENTIMENT_COLORS.pending;
}

function MetricCard({
  label,
  value,
  detail,
  accentClass,
  active,
  onClick,
}: {
  label: string;
  value: string;
  detail: string;
  accentClass: string;
  active?: boolean;
  onClick?: () => void;
}) {
  const Tag = onClick ? 'button' : 'div';
  return (
    <Tag
      type={onClick ? 'button' : undefined}
      onClick={onClick}
      className={`rounded-2xl border p-4 text-left transition-all duration-200 ${
        active
          ? 'border-white/20 bg-white/[0.06] ring-1 ring-white/10'
          : 'border-white/5 bg-[#151B2B]/80 hover:border-white/10'
      } ${onClick ? 'cursor-pointer' : ''}`}
    >
      <p className="text-[10px] text-gray-500 uppercase tracking-wider font-semibold">{label}</p>
      <p className={`text-2xl font-black tabular-nums mt-1 ${accentClass}`}>{value}</p>
      <p className="text-[11px] text-gray-500 mt-1">{detail}</p>
    </Tag>
  );
}

function MentionCard({
  item,
  isMenuOpen,
  isBusy,
  commentsExpanded,
  contentExpanded,
  menuRef,
  onToggleMenu,
  onToggleComments,
  onToggleContent,
  onAnalyze,
  onCopy,
  onDelete,
  onOpenOriginal,
  onEditSentiment,
  onManageTags,
  onMuteAuthor,
  onMutePlatform,
}: {
  item: ProjectMention;
  isMenuOpen: boolean;
  isBusy: boolean;
  commentsExpanded: boolean;
  contentExpanded: boolean;
  menuRef: React.RefObject<HTMLDivElement | null> | undefined;
  onToggleMenu: () => void;
  onToggleComments: () => void;
  onToggleContent: () => void;
  onAnalyze: () => void;
  onCopy: () => void;
  onDelete: () => void;
  onOpenOriginal: () => void;
  onEditSentiment: (sentiment: 'positive' | 'negative' | 'neutral') => void;
  onManageTags: () => void;
  onMuteAuthor: () => void;
  onMutePlatform: () => void;
}) {
  const hasComments = item.comments.length > 0;
  const hasSentiment = item.sentiment != null && item.sentiment !== '';
  const accent = getSentimentAccent(item.sentiment);
  const platformColor = getPlatformChartColor(item.platform);
  const isLongContent = item.content.length > 320;
  const displayContent =
    isLongContent && !contentExpanded ? `${item.content.slice(0, 320).trimEnd()}…` : item.content;

  const commentLabel =
    item.comments.length > 0
      ? `${formatNumber(item.comments.length)} bình luận`
      : item.commentsCount > 0
        ? `${formatNumber(item.commentsCount)} bình luận`
        : 'Không có bình luận';

  return (
    <article
      className="group relative overflow-hidden rounded-2xl border border-white/5 bg-gradient-to-br from-[#151B2B] via-[#141A28] to-[#101622] transition-all duration-200 hover:border-white/10 hover:shadow-lg hover:shadow-black/20"
      style={{ boxShadow: `inset 3px 0 0 ${accent}` }}
    >
      <div className="p-5 sm:p-6">
        <div className="flex items-start justify-between gap-4 mb-4">
          <div className="flex items-start gap-3 min-w-0 flex-1">
            <div
              className={`w-11 h-11 rounded-xl flex items-center justify-center font-bold text-sm shrink-0 border border-white/10 ${getPlatformBadgeClass(item.platform)}`}
              title={getPlatformDisplayName(item.platform)}
            >
              {getPlatformLabel(item.platform)}
            </div>
            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center gap-2">
                <h4 className="font-bold text-white truncate max-w-full">
                  {item.authorName || 'Không rõ tác giả'}
                </h4>
                {item.isCrisisAlert && (
                  <span className="inline-flex items-center gap-1 text-[10px] font-bold uppercase tracking-wide px-2 py-0.5 rounded-md border border-amber-500/30 bg-amber-500/10 text-amber-300">
                    <AlertTriangle className="w-3 h-3" />
                    Khủng hoảng
                  </span>
                )}
                <span
                  className="text-[10px] font-semibold uppercase tracking-wide px-2 py-0.5 rounded-md border border-white/10"
                  style={{ color: platformColor, borderColor: `${platformColor}33`, background: `${platformColor}14` }}
                >
                  {getPlatformDisplayName(item.platform)}
                </span>
              </div>
              <p className="text-xs text-gray-500 mt-1.5 flex flex-wrap gap-x-2 gap-y-0.5">
                {item.postedAt ? (
                  <span>Đăng {formatWorkspaceDateTime(item.postedAt)}</span>
                ) : (
                  <span>Chưa có ngày đăng</span>
                )}
                {item.scrapedAt && <span className="text-gray-600">· Cào {formatWorkspaceDateTime(item.scrapedAt)}</span>}
              </p>
            </div>
          </div>

          <div className="flex items-start gap-2 shrink-0">
            <span
              className={`inline-flex items-center gap-1.5 px-3 py-1.5 border rounded-full text-xs font-semibold ${getSentimentBadgeClass(item.sentiment)}`}
            >
              <span className="w-1.5 h-1.5 rounded-full" style={{ background: accent }} />
              {hasSentiment ? getSentimentLabel(item.sentiment) : 'Chờ AI'}
              {item.isSentimentOverridden ? ' · Sửa tay' : ''}
              {item.confidenceScore != null && hasSentiment
                ? ` · ${Math.round(item.confidenceScore * 100)}%`
                : ''}
            </span>

            <div className="relative" ref={isMenuOpen ? menuRef : undefined}>
              <button
                type="button"
                onClick={onToggleMenu}
                disabled={isBusy}
                className="p-2 rounded-xl text-gray-500 hover:text-white hover:bg-white/10 transition-all disabled:opacity-40"
                aria-label="Tùy chọn mention"
              >
                {isBusy ? <Loader2 size={18} className="animate-spin" /> : <MoreVertical size={18} />}
              </button>

              {isMenuOpen && (
                <div className="absolute right-0 top-10 w-56 bg-[#1A2235] border border-white/10 rounded-xl shadow-2xl z-50 py-1.5 overflow-hidden">
                  {item.originalUrl ? (
                    <button
                      type="button"
                      onClick={onOpenOriginal}
                      className="w-full flex items-center gap-2.5 px-4 py-2.5 text-sm text-gray-300 hover:text-white hover:bg-white/5"
                    >
                      <ExternalLink size={16} className="text-[#00B4D8]" />
                      Xem bài gốc
                    </button>
                  ) : null}
                  <button
                    type="button"
                    onClick={onManageTags}
                    className="w-full flex items-center gap-2.5 px-4 py-2.5 text-sm text-gray-300 hover:text-white hover:bg-white/5"
                  >
                    <Tag size={16} className="text-emerald-400" />
                    Gán tag
                  </button>
                  <div className="px-4 py-2">
                    <p className="text-[10px] uppercase tracking-wider text-gray-500 font-semibold mb-1.5">Sửa sentiment</p>
                    <div className="flex flex-wrap gap-1.5">
                      {(['positive', 'negative', 'neutral'] as const).map((s) => (
                        <button
                          key={s}
                          type="button"
                          onClick={() => onEditSentiment(s)}
                          className={`px-2 py-1 rounded-lg text-[11px] font-semibold border ${getSentimentBadgeClass(s)}`}
                        >
                          {getSentimentLabel(s)}
                        </button>
                      ))}
                    </div>
                  </div>
                  <button
                    type="button"
                    onClick={onAnalyze}
                    className="w-full flex items-center gap-2.5 px-4 py-2.5 text-sm text-gray-300 hover:text-white hover:bg-white/5"
                  >
                    <Sparkles size={16} className="text-amber-400" />
                    Phân tích lại
                  </button>
                  <button
                    type="button"
                    onClick={onCopy}
                    className="w-full flex items-center gap-2.5 px-4 py-2.5 text-sm text-gray-300 hover:text-white hover:bg-white/5"
                  >
                    <Copy size={16} className="text-violet-400" />
                    Sao chép nội dung
                  </button>
                  <div className="h-px bg-white/5 my-1" />
                  {item.authorName ? (
                    <button
                      type="button"
                      onClick={onMuteAuthor}
                      className="w-full flex items-center gap-2.5 px-4 py-2.5 text-sm text-gray-300 hover:text-white hover:bg-white/5"
                    >
                      <VolumeX size={16} className="text-gray-400" />
                      Mute tác giả
                    </button>
                  ) : null}
                  <button
                    type="button"
                    onClick={onMutePlatform}
                    className="w-full flex items-center gap-2.5 px-4 py-2.5 text-sm text-gray-300 hover:text-white hover:bg-white/5"
                  >
                    <VolumeX size={16} className="text-gray-400" />
                    Mute nền tảng
                  </button>
                  <div className="h-px bg-white/5 my-1" />
                  <button
                    type="button"
                    onClick={onDelete}
                    className="w-full flex items-center gap-2.5 px-4 py-2.5 text-sm text-red-400 hover:text-red-300 hover:bg-red-500/10"
                  >
                    <Trash2 size={16} />
                    Xóa mention
                  </button>
                </div>
              )}
            </div>
          </div>
        </div>

        {item.tags.length > 0 && (
          <div className="flex flex-wrap gap-2 mb-4">
            {item.tags.map((tag) => (
              <span
                key={tag.tagId}
                className="inline-flex items-center gap-1 px-2.5 py-1 rounded-full text-[11px] font-semibold border border-white/10 bg-white/5 text-gray-200"
                style={{ borderColor: `${tag.color ?? '#00B4D8'}44`, color: tag.color ?? '#00B4D8' }}
              >
                <Tag className="w-3 h-3" />
                {tag.name}
              </span>
            ))}
          </div>
        )}

        <div className="rounded-xl border border-white/5 bg-[#0A101D]/50 px-4 py-3.5 mb-4">
          <p className="text-sm text-gray-200 leading-relaxed whitespace-pre-wrap">{displayContent}</p>
          {isLongContent && (
            <button
              type="button"
              onClick={onToggleContent}
              className="mt-2 text-xs font-semibold text-[#00B4D8] hover:text-white transition-colors"
            >
              {contentExpanded ? 'Thu gọn' : 'Xem thêm'}
            </button>
          )}
        </div>

        <div className="rounded-xl border border-white/5 bg-white/[0.02] overflow-hidden">
          <div className="px-4 py-3 border-b border-white/5 flex items-center justify-between gap-3 flex-wrap">
            <div className="flex items-center gap-2">
              <div className="p-1.5 rounded-lg bg-[#FF7575]/10">
                <Sparkles className="w-3.5 h-3.5 text-[#FF7575]" />
              </div>
              <div>
                <p className="text-sm font-semibold text-white">Tóm tắt AI</p>
                <p className="text-[11px] text-gray-500">{commentLabel}</p>
              </div>
            </div>
            {item.analyzedAt && (
              <span className="text-[10px] text-gray-500 shrink-0">{formatWorkspaceDateTime(item.analyzedAt)}</span>
            )}
          </div>

          <div className="px-4 py-4">
            {item.aiSummary ? (
              <p className="text-sm text-gray-300 leading-relaxed whitespace-pre-wrap">{item.aiSummary}</p>
            ) : (
              <div className="flex flex-col sm:flex-row sm:items-center gap-3">
                <p className="text-sm text-gray-500 flex-1">
                  {hasSentiment
                    ? 'Đã có sentiment — bấm «Phân tích lại» để tạo tóm tắt chi tiết.'
                    : 'Chưa phân tích. AI sẽ đọc nội dung và bình luận để đánh giá sentiment.'}
                </p>
                <button
                  type="button"
                  onClick={onAnalyze}
                  disabled={isBusy}
                  className="inline-flex items-center gap-1.5 text-xs font-semibold text-[#FF7575] hover:text-white shrink-0 disabled:opacity-50"
                >
                  <Sparkles className="w-3.5 h-3.5" />
                  Phân tích ngay
                </button>
              </div>
            )}
          </div>

          {(hasComments || item.commentsCount > 0) && (
            <div className="border-t border-white/5">
              <button
                type="button"
                onClick={onToggleComments}
                className="w-full flex items-center justify-between gap-2 px-4 py-3 text-sm text-gray-400 hover:text-white hover:bg-white/[0.03] transition-colors"
              >
                <span className="inline-flex items-center gap-2">
                  <MessagesSquare className="w-4 h-4 text-gray-500" />
                  {hasComments
                    ? `Xem ${formatNumber(item.comments.length)} bình luận đã gom`
                    : `${formatNumber(item.commentsCount)} bình luận (chưa tải chi tiết)`}
                </span>
                {commentsExpanded ? <ChevronUp className="w-4 h-4 shrink-0" /> : <ChevronDown className="w-4 h-4 shrink-0" />}
              </button>

              {commentsExpanded && hasComments && (
                <ul className="px-4 pb-4 space-y-2 max-h-64 overflow-y-auto">
                  {item.comments.map((comment, idx) => (
                    <li
                      key={`${item.feedbackId}-c-${idx}`}
                      className="text-sm text-gray-300 bg-[#0A101D]/70 rounded-lg px-3 py-2.5 border border-white/5 leading-relaxed"
                    >
                      <span className="text-[#00B4D8] font-semibold mr-2 tabular-nums">{idx + 1}.</span>
                      {comment}
                    </li>
                  ))}
                </ul>
              )}

              {commentsExpanded && !hasComments && item.commentsCount > 0 && (
                <p className="px-4 pb-4 text-xs text-amber-300/90 leading-relaxed">
                  Dữ liệu cũ bị lệch (DB ghi {item.commentsCount} comment nhưng file chi tiết trống). Cào lại dự án để
                  tải comment.
                </p>
              )}
            </div>
          )}
        </div>

        {item.originalUrl && (
          <div className="mt-4 flex justify-end">
            <a
              href={item.originalUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="inline-flex items-center gap-2 text-xs font-semibold text-[#00B4D8] bg-[#00B4D8]/10 px-3.5 py-2 rounded-lg hover:bg-[#00B4D8]/20 transition-colors"
            >
              <ExternalLink size={14} />
              Xem bài gốc
            </a>
          </div>
        )}
      </div>
    </article>
  );
}

const ProjectMentions = () => {
  const { workspaceId, id } = useParams();
  const wid = Number(workspaceId);
  const projectId = Number(id);

  const [mentions, setMentions] = useState<ProjectMention[]>([]);
  const [activePlatform, setActivePlatform] = useState<MentionPlatformFilter>('all');
  const [activeSentiment, setActiveSentiment] = useState<MentionSentimentFilter>('all');
  const [searchText, setSearchText] = useState('');
  const [showCrisisOnly, setShowCrisisOnly] = useState(false);
  const [savedFilters, setSavedFilters] = useState<{ filterId: number; name: string }[]>([]);
  const [filterName, setFilterName] = useState('');
  const [expandedComments, setExpandedComments] = useState<Record<number, boolean>>({});
  const [expandedContent, setExpandedContent] = useState<Record<number, boolean>>({});
  const [isLoading, setIsLoading] = useState(true);
  const [aiProgress, setAiProgress] = useState<AiAnalysisProgress>({ isAnalyzing: false, progressPercent: 0 });
  const wasAnalyzing = useRef(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [analyzeMessage, setAnalyzeMessage] = useState('');
  const [openMenuId, setOpenMenuId] = useState<number | null>(null);
  const [actionMentionId, setActionMentionId] = useState<number | null>(null);
  const [projectTags, setProjectTags] = useState<MentionTag[]>([]);
  const [tagModalMention, setTagModalMention] = useState<ProjectMention | null>(null);
  const [selectedTagIds, setSelectedTagIds] = useState<number[]>([]);
  const [newTagName, setNewTagName] = useState('');
  const menuRef = useRef<HTMLDivElement>(null);
  const { confirm, alert } = useAppModal();

  const loadProjectTags = useCallback(async () => {
    if (!wid || !projectId || Number.isNaN(wid) || Number.isNaN(projectId)) return;
    try {
      const tags = await projectApi.listMentionTags(wid, projectId);
      setProjectTags(tags);
    } catch {
      setProjectTags([]);
    }
  }, [wid, projectId]);

  const loadMentions = useCallback(async () => {
    if (!wid || !projectId || Number.isNaN(wid) || Number.isNaN(projectId)) return;

    setIsLoading(true);
    setErrorMessage('');
    try {
      const data = await projectApi.getMentions(wid, projectId, {
        search: searchText.trim() || undefined,
        isCrisisAlert: showCrisisOnly || undefined,
      });
      setMentions(data);

      const defaultExpanded: Record<number, boolean> = {};
      for (const item of data) {
        if (item.comments.length > 0) defaultExpanded[item.feedbackId] = true;
      }
      setExpandedComments(defaultExpanded);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải danh sách mentions.'));
    } finally {
      setIsLoading(false);
    }
  }, [wid, projectId, searchText, showCrisisOnly]);

  const loadAiProgress = useCallback(async () => {
    if (!wid || !projectId || Number.isNaN(wid) || Number.isNaN(projectId)) return;
    try {
      const progress = await projectApi.getAnalyzeProgress(wid, projectId);
      setAiProgress(progress);
      if (wasAnalyzing.current && !progress.isAnalyzing) {
        loadMentions();
      }
      wasAnalyzing.current = progress.isAnalyzing;
    } catch {
      /* ignore */
    }
  }, [wid, projectId, loadMentions]);

  useEffect(() => {
    loadProjectTags();
    loadMentions();
  }, [loadProjectTags, loadMentions]);

  useEffect(() => {
    loadAiProgress();
    const timer = setInterval(() => {
      if (document.visibilityState === 'visible') loadAiProgress();
    }, 3000);
    return () => clearInterval(timer);
  }, [loadAiProgress]);

  const loadSavedFilters = useCallback(async () => {
    if (!wid || !projectId) return;
    try {
      const filters = await projectApi.getMentionFilters(wid, projectId);
      setSavedFilters(filters.map((f) => ({ filterId: f.filterId, name: f.name })));
    } catch {
      setSavedFilters([]);
    }
  }, [wid, projectId]);

  useEffect(() => {
    loadSavedFilters();
  }, [loadSavedFilters]);

  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (menuRef.current && !menuRef.current.contains(event.target as Node)) {
        setOpenMenuId(null);
      }
    };
    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const saveCurrentFilter = async () => {
    if (!wid || !projectId || !filterName.trim()) return;
    try {
      await projectApi.saveMentionFilter(wid, projectId, {
        name: filterName.trim(),
        config: {
          platform: activePlatform,
          sentiment: activeSentiment,
          search: searchText.trim() || null,
          isCrisisAlert: showCrisisOnly || undefined,
        } as any,
      });
      setFilterName('');
      await loadSavedFilters();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể lưu bộ lọc.'));
    }
  };

  const runAnalyze = async (force: boolean) => {
    if (!wid || !projectId) return;

    if (!force && sentimentCounts.pending === 0) {
      setAnalyzeMessage('Không có mention mới cần phân tích AI.');
      setErrorMessage('');
      return;
    }

    setAnalyzeMessage(force ? 'Đang phân tích lại toàn bộ dữ liệu...' : 'Đang phân tích các mention chưa có AI...');
    setErrorMessage('');
    try {
      const result = await projectApi.analyze(wid, projectId, force);
      setAnalyzeMessage(result.message);
      loadAiProgress();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể chạy phân tích AI.'));
    }
  };

  const platformCounts = useMemo(() => {
    const counts: Record<MentionPlatformFilter, number> = {
      all: mentions.length,
      youtube: 0,
      tiktok: 0,
      facebook: 0,
      news: 0,
    };
    for (const item of mentions) {
      const p = item.platform.toLowerCase() as MentionPlatformFilter;
      if (p in counts && p !== 'all') counts[p]++;
    }
    return counts;
  }, [mentions]);

  const sentimentCounts = useMemo(() => {
    const counts: Record<MentionSentimentFilter, number> = {
      all: mentions.length,
      positive: 0,
      negative: 0,
      neutral: 0,
      pending: 0,
    };
    for (const item of mentions) {
      const s = item.sentiment?.toLowerCase();
      if (s === 'positive') counts.positive++;
      else if (s === 'negative') counts.negative++;
      else if (s === 'neutral') counts.neutral++;
      else counts.pending++;
    }
    return counts;
  }, [mentions]);

  const displayedMentions = useMemo(() => {
    return mentions.filter(m => {
      if (activePlatform !== 'all' && m.platform.toLowerCase() !== activePlatform) return false;
      if (activeSentiment !== 'all') {
        const s = m.sentiment?.toLowerCase();
        if (activeSentiment === 'pending') {
          if (s === 'positive' || s === 'negative' || s === 'neutral') return false;
        } else {
          if (s !== activeSentiment) return false;
        }
      }
      return true;
    });
  }, [mentions, activePlatform, activeSentiment]);

  const totalComments = useMemo(
    () => displayedMentions.reduce((sum, m) => sum + (m.comments.length > 0 ? m.comments.length : m.commentsCount), 0),
    [displayedMentions]
  );

  const analyzedCount = useMemo(() => 
    displayedMentions.filter(m => {
      const s = m.sentiment?.toLowerCase();
      return s === 'positive' || s === 'negative' || s === 'neutral';
    }).length,
  [displayedMentions]);

  const coveragePercent = displayedMentions.length > 0 ? Math.round((analyzedCount / displayedMentions.length) * 100) : 0;

  const visiblePlatforms = useMemo(
    () =>
      MENTION_PLATFORMS.filter(
        (p) => p === 'all' || platformCounts[p] > 0 || mentions.length === 0
      ),
    [platformCounts, mentions.length]
  );

  const hasActiveFilters =
    activePlatform !== 'all' || activeSentiment !== 'all' || Boolean(searchText.trim()) || showCrisisOnly;

  const clearFilters = () => {
    setActivePlatform('all');
    setActiveSentiment('all');
    setSearchText('');
    setShowCrisisOnly(false);
  };

  const toggleSentimentFilter = (filter: MentionSentimentFilter) => {
    setActiveSentiment((prev) => (prev === filter ? 'all' : filter));
  };

  const sentimentTabLabel = (filter: MentionSentimentFilter) => {
    if (filter === 'all') return `Tất cả (${sentimentCounts.all})`;
    return `${getSentimentFilterLabel(filter)} (${sentimentCounts[filter]})`;
  };

  const platformTabLabel = (platform: MentionPlatformFilter) => {
    if (platform === 'all') return `Tất cả (${platformCounts.all})`;
    return `${getPlatformDisplayName(platform)} (${platformCounts[platform]})`;
  };

  const handleDeleteMention = async (item: ProjectMention) => {
    const confirmed = await confirm({
      title: 'Xóa mention',
      message: `Bạn có chắc muốn xóa bài của "${item.authorName || 'Không rõ tác giả'}"?\nDữ liệu và phân tích AI sẽ không thể khôi phục.`,
      confirmText: 'Xóa mention',
      cancelText: 'Hủy bỏ',
      type: 'danger',
    });
    if (!confirmed || !wid || !projectId) return;

    setActionMentionId(item.feedbackId);
    setOpenMenuId(null);

    try {
      await projectApi.deleteMention(wid, projectId, item.feedbackId);
      setMentions((prev) => prev.filter((m) => m.feedbackId !== item.feedbackId));
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể xóa mention.'));
    } finally {
      setActionMentionId(null);
    }
  };

  const handleAnalyzeMention = async (item: ProjectMention) => {
    if (!wid || !projectId) return;
    setActionMentionId(item.feedbackId);
    setOpenMenuId(null);
    setAnalyzeMessage('Đang phân tích mention này...');
    setErrorMessage('');

    try {
      const result = await projectApi.analyzeMention(wid, projectId, item.feedbackId);
      setAnalyzeMessage(result.message);
      await loadMentions();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể phân tích mention.'));
    } finally {
      setActionMentionId(null);
    }
  };

  const handleCopyContent = async (item: ProjectMention) => {
    setOpenMenuId(null);
    try {
      await navigator.clipboard.writeText(item.content);
      await alert({
        title: 'Đã sao chép',
        message: 'Nội dung mention đã được copy vào clipboard.',
        type: 'success',
      });
    } catch {
      setErrorMessage('Không thể sao chép nội dung.');
    }
  };

  const handleOpenOriginal = (item: ProjectMention) => {
    setOpenMenuId(null);
    if (item.originalUrl) window.open(item.originalUrl, '_blank', 'noopener,noreferrer');
  };

  const handleEditSentiment = async (item: ProjectMention, sentiment: 'positive' | 'negative' | 'neutral') => {
    if (!wid || !projectId) return;
    setOpenMenuId(null);
    setActionMentionId(item.feedbackId);
    try {
      await projectApi.updateMentionSentiment(wid, projectId, item.feedbackId, sentiment);
      await loadMentions();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể cập nhật sentiment.'));
    } finally {
      setActionMentionId(null);
    }
  };

  const openTagModal = (item: ProjectMention) => {
    setOpenMenuId(null);
    setTagModalMention(item);
    setSelectedTagIds(item.tags.map((t) => t.tagId));
    setNewTagName('');
  };

  const handleSaveTags = async () => {
    if (!wid || !projectId || !tagModalMention) return;
    setActionMentionId(tagModalMention.feedbackId);
    try {
      await projectApi.assignMentionTags(wid, projectId, tagModalMention.feedbackId, selectedTagIds);
      setTagModalMention(null);
      await loadMentions();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể gán tag.'));
    } finally {
      setActionMentionId(null);
    }
  };

  const handleCreateTag = async () => {
    if (!wid || !projectId || !newTagName.trim()) return;
    try {
      const tag = await projectApi.createMentionTag(wid, projectId, { name: newTagName.trim() });
      setProjectTags((prev) => [...prev, tag]);
      setSelectedTagIds((prev) => [...prev, tag.tagId]);
      setNewTagName('');
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tạo tag.'));
    }
  };

  const handleMuteAuthor = async (item: ProjectMention) => {
    if (!wid || !projectId || !item.authorName) return;
    const confirmed = await confirm({
      title: 'Mute tác giả',
      message: `Ẩn tất cả mention từ «${item.authorName}» khỏi danh sách?`,
      confirmText: 'Mute',
      cancelText: 'Hủy',
      type: 'warning',
    });
    if (!confirmed) return;
    setOpenMenuId(null);
    setActionMentionId(item.feedbackId);
    try {
      await projectApi.muteMentionSource(wid, projectId, {
        entityType: 'author',
        entityValue: item.authorName,
      });
      await loadMentions();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể mute tác giả.'));
    } finally {
      setActionMentionId(null);
    }
  };

  const handleMutePlatform = async (item: ProjectMention) => {
    if (!wid || !projectId) return;
    const confirmed = await confirm({
      title: 'Mute nền tảng',
      message: `Ẩn tất cả mention từ ${getPlatformDisplayName(item.platform)}?`,
      confirmText: 'Mute',
      cancelText: 'Hủy',
      type: 'warning',
    });
    if (!confirmed) return;
    setOpenMenuId(null);
    setActionMentionId(item.feedbackId);
    try {
      await projectApi.muteMentionSource(wid, projectId, {
        entityType: 'platform',
        entityValue: item.platform,
      });
      await loadMentions();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể mute nền tảng.'));
    } finally {
      setActionMentionId(null);
    }
  };

  return (
    <div className="animate-in fade-in duration-500 max-w-7xl mx-auto space-y-8 pb-10">
      <div className="relative overflow-hidden rounded-3xl border border-white/5 bg-gradient-to-br from-[#151B2B] via-[#12182A] to-[#0A101D] p-8">
        <div className="absolute -top-24 -right-24 w-72 h-72 bg-[#FF7575]/10 rounded-full blur-3xl pointer-events-none" />
        <div className="absolute -bottom-16 -left-16 w-56 h-56 bg-[#00B4D8]/10 rounded-full blur-3xl pointer-events-none" />

        <div className="relative flex flex-col lg:flex-row lg:items-start lg:justify-between gap-6">
          <div className="max-w-3xl">
            <h2 className="text-3xl font-bold text-white flex items-center gap-3">
              <MessageCircle className="text-[#FF7575] w-8 h-8" />
              Lượt nhắc
            </h2>
            <p className="text-gray-400 text-sm mt-3 leading-relaxed">
              Toàn bộ bài đăng và bài báo đã thu thập — lọc theo nền tảng, sentiment hoặc từ khóa.
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-2 shrink-0">
            <button
              type="button"
              onClick={() => loadMentions()}
              disabled={isLoading}
              className="p-3 rounded-xl border border-white/10 bg-white/5 text-gray-400 hover:text-white hover:bg-white/10 transition-colors disabled:opacity-50"
              title="Làm mới"
            >
              <RefreshCw className={`w-5 h-5 ${isLoading ? 'animate-spin' : ''}`} />
            </button>
            <button
              type="button"
              onClick={(e) => {
                e.stopPropagation();
                runAnalyze(true);
              }}
              disabled={aiProgress.isAnalyzing || isLoading || mentions.length === 0}
              className={`inline-flex items-center gap-2 px-3 lg:px-4 py-2 rounded-xl text-sm font-semibold transition-all shadow-sm
                ${
                  aiProgress.isAnalyzing || isLoading || mentions.length === 0
                    ? 'bg-white/5 text-gray-500 cursor-not-allowed border border-white/5'
                    : 'bg-gradient-to-r from-[#FF7575] to-[#ff5252] hover:from-[#ff6262] hover:to-[#ff4242] text-white hover:shadow-[#FF7575]/25 hover:-translate-y-0.5'
                }`}
            >
              {aiProgress.isAnalyzing ? <Loader2 className="w-4 h-4 animate-spin" /> : <Sparkles className="w-4 h-4" />}
              <span className="hidden sm:inline">Phân tích lại toàn bộ</span>
              <span className="sm:hidden">Phân tích</span>
            </button>
          </div>
        </div>
      </div>

      {aiProgress.isAnalyzing && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-[#0a0f1c]/80 backdrop-blur-md">
          <div className="relative flex flex-col items-center p-8 rounded-3xl bg-[#151B2B]/80 border border-white/10 shadow-[0_0_80px_-20px_rgba(255,117,117,0.3)] overflow-hidden min-w-[320px]">
            {/* Animated glowing background */}
            <div className="absolute inset-0 bg-gradient-to-br from-[#FF7575]/20 via-transparent to-transparent opacity-50 animate-pulse" />
            
            <div className="relative mb-6 mt-2">
              <div className="absolute inset-0 bg-[#FF7575] blur-xl opacity-40 animate-pulse" />
              <div className="relative bg-[#1a2133] p-5 rounded-2xl border border-white/10 shadow-inner">
                <BrainCircuit className="w-12 h-12 text-[#FF7575] animate-pulse" />
              </div>
            </div>
            
            <h3 className="text-xl font-bold text-white mb-2 tracking-wide flex items-center gap-2">
              <Sparkles className="w-5 h-5 text-[#FF7575]" />
              AI Đang Làm Việc... {aiProgress.progressPercent}%
            </h3>
            
            {/* Progress bar */}
            <div className="w-full max-w-[280px] h-2 bg-white/10 rounded-full mt-2 mb-4 overflow-hidden relative">
              <div 
                className="h-full bg-gradient-to-r from-[#FF7575] to-[#00B4D8] transition-all duration-300"
                style={{ width: `${Math.max(5, aiProgress.progressPercent)}%` }}
              />
            </div>
            
            <p className="text-sm text-gray-400 text-center max-w-[280px] leading-relaxed">
              Hệ thống đang đọc và tổng hợp cảm xúc từ các lượt nhắc. Vui lòng chờ trong giây lát.
            </p>
            
            <div className="mt-8 mb-2 flex justify-center gap-2">
              <div className="w-2.5 h-2.5 rounded-full bg-[#FF7575] animate-bounce" style={{ animationDelay: '0ms' }} />
              <div className="w-2.5 h-2.5 rounded-full bg-[#FF7575] animate-bounce" style={{ animationDelay: '150ms' }} />
              <div className="w-2.5 h-2.5 rounded-full bg-[#FF7575] animate-bounce" style={{ animationDelay: '300ms' }} />
            </div>
          </div>
        </div>
      )}

      {analyzeMessage && !aiProgress.isAnalyzing && (
        <div className="bg-emerald-500/10 border border-emerald-500/20 text-emerald-300 px-4 py-3 rounded-xl text-sm flex items-center gap-2">
          <Sparkles className="w-4 h-4 shrink-0" />
          {analyzeMessage}
        </div>
      )}

      {errorMessage && (
        <div className="bg-red-500/10 border border-red-500/20 text-red-300 p-4 rounded-xl flex items-center gap-3 text-sm">
          <AlertCircle className="w-5 h-5 shrink-0" />
          {errorMessage}
        </div>
      )}

      <div className="grid grid-cols-2 xl:grid-cols-4 gap-4">
        <MetricCard
          label="Tổng mentions"
          value={formatNumber(displayedMentions.length)}
          detail="Trong bộ lọc hiện tại"
          accentClass="text-white"
        />
        <MetricCard
          label="Đã phân tích"
          value={`${coveragePercent}%`}
          detail={`${formatNumber(analyzedCount)} / ${formatNumber(displayedMentions.length)} có sentiment`}
          accentClass="text-[#00B4D8]"
        />
        <MetricCard
          label="Bình luận"
          value={formatNumber(totalComments)}
          detail="Tổng comment đã gom"
          accentClass="text-emerald-400"
        />
        <MetricCard
          label="Chờ AI"
          value={formatNumber(sentimentCounts.pending)}
          detail="Chưa có phân tích sentiment"
          accentClass="text-gray-400"
          active={activeSentiment === 'pending'}
          onClick={() => toggleSentimentFilter('pending')}
        />
      </div>

      <div className="rounded-3xl border border-white/5 bg-[#151B2B]/80 p-5 sm:p-6 space-y-5">
        <div className="flex flex-col lg:flex-row lg:items-center gap-3">
          <div className="relative flex-1">
            <Search className="absolute left-3.5 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-500" />
            <input
              value={searchText}
              onChange={(e) => setSearchText(e.target.value)}
              placeholder="Tìm theo nội dung hoặc tác giả..."
              className="w-full pl-10 pr-4 py-3 bg-[#0A101D] border border-white/10 rounded-xl text-white text-sm focus:border-[#00B4D8]/50 focus:outline-none transition-colors"
            />
          </div>
          <div className="flex gap-2 shrink-0">
            <input
              value={filterName}
              onChange={(e) => setFilterName(e.target.value)}
              placeholder="Tên bộ lọc"
              className="px-3 py-3 bg-[#0A101D] border border-white/10 rounded-xl text-white text-sm w-36 focus:outline-none focus:border-white/20"
            />
            <button
              type="button"
              onClick={saveCurrentFilter}
              className="inline-flex items-center gap-1.5 px-4 py-3 rounded-xl bg-white/10 hover:bg-white/15 text-sm font-medium text-white transition-colors"
            >
              <BookmarkPlus className="w-4 h-4" />
              Lưu
            </button>
          </div>
        </div>

        {savedFilters.length > 0 && (
          <div className="flex flex-wrap items-center gap-2">
            <span className="text-[10px] text-gray-500 uppercase tracking-wider font-semibold mr-1">Đã lưu</span>
            {savedFilters.map((f) => (
              <span
                key={f.filterId}
                className="px-3 py-1 rounded-full bg-white/[0.04] border border-white/10 text-xs text-gray-300"
              >
                {f.name}
              </span>
            ))}
          </div>
        )}

        <div className="space-y-3">
          <div className="flex items-center justify-between gap-3">
            <div className="flex items-center gap-2 text-xs text-gray-500 font-semibold uppercase tracking-wider">
              <Filter className="w-3.5 h-3.5" />
              Sentiment
            </div>
            {hasActiveFilters && (
              <button
                type="button"
                onClick={clearFilters}
                className="inline-flex items-center gap-1 text-xs font-semibold text-gray-400 hover:text-white transition-colors"
              >
                <X className="w-3.5 h-3.5" />
                Xóa bộ lọc
              </button>
            )}
          </div>
          <div className="flex flex-wrap gap-2">
            {MENTION_SENTIMENTS.map((filter) => (
              <button
                key={filter}
                type="button"
                onClick={() => setActiveSentiment(filter)}
                className={`px-3.5 py-2 rounded-xl text-xs font-semibold transition-all ${
                  activeSentiment === filter
                    ? filter === 'all'
                      ? 'bg-white/10 text-white border border-white/15'
                      : `${getSentimentFilterBadgeClass(filter)} border`
                    : 'text-gray-500 hover:text-gray-300 bg-white/[0.03] border border-white/5 hover:border-white/10'
                }`}
              >
                {sentimentTabLabel(filter)}
              </button>
            ))}

            <button
              onClick={() => setShowCrisisOnly(prev => !prev)}
              className={`inline-flex items-center gap-1.5 px-3.5 py-2 rounded-xl text-xs font-semibold transition-all border ml-auto ${
                showCrisisOnly
                  ? 'border-amber-500/30 bg-amber-500/10 text-amber-400'
                  : 'border-white/5 bg-white/[0.03] text-gray-500 hover:text-amber-400/80 hover:border-amber-500/20'
              }`}
              title="Chỉ hiển thị các bài viết có cảnh báo khủng hoảng truyền thông"
            >
              <AlertTriangle className="w-3.5 h-3.5 shrink-0" />
              Chỉ hiện Khủng hoảng
            </button>
          </div>
        </div>

        <div className="space-y-3">
          <p className="text-xs text-gray-500 font-semibold uppercase tracking-wider">Nền tảng</p>
          <div className="flex flex-wrap gap-2">
            {visiblePlatforms.map((platform) => (
              <button
                key={platform}
                type="button"
                onClick={() => setActivePlatform(platform)}
                className={`px-3.5 py-2 rounded-xl text-xs font-semibold transition-all ${
                  activePlatform === platform
                    ? platform === 'all'
                      ? 'bg-[#FF7575]/20 text-[#FF7575] border border-[#FF7575]/30'
                      : `${getPlatformBadgeClass(platform)} border`
                    : 'text-gray-500 hover:text-gray-300 bg-white/[0.03] border border-white/5 hover:border-white/10'
                }`}
              >
                {platformTabLabel(platform)}
              </button>
            ))}
          </div>
        </div>
      </div>

      {isLoading ? (
        <div className="flex flex-col items-center justify-center py-32 text-gray-400 gap-4">
          <div className="relative">
            <div className="absolute inset-0 rounded-full bg-[#FF7575]/20 blur-2xl animate-pulse" />
            <Loader2 className="relative w-12 h-12 animate-spin text-[#FF7575]" />
          </div>
          <p className="text-sm">Đang tải mentions...</p>
        </div>
      ) : displayedMentions.length === 0 ? (
        <div className="rounded-3xl border border-white/5 bg-[#151B2B] p-16 text-center">
          <MessageCircle className="w-14 h-14 text-gray-600 mx-auto mb-4" />
          <p className="text-gray-300 font-medium">Chưa có mention nào</p>
          <p className="text-gray-500 text-sm mt-2 max-w-md mx-auto">
            Chạy cào dữ liệu từ trang Dự án để thu thập bài đăng từ Facebook, YouTube, TikTok và Tin tức.
          </p>
        </div>
      ) : (
        <div className="space-y-4">
          <p className="text-xs text-gray-500 px-1">
            Hiển thị <span className="text-white font-semibold tabular-nums">{formatNumber(displayedMentions.length)}</span> kết
            quả
          </p>
          {displayedMentions.map((item) => (
            <MentionCard
              key={item.feedbackId}
              item={item}
              isMenuOpen={openMenuId === item.feedbackId}
              isBusy={actionMentionId === item.feedbackId}
              commentsExpanded={expandedComments[item.feedbackId] ?? item.comments.length > 0}
              contentExpanded={expandedContent[item.feedbackId] ?? false}
              menuRef={openMenuId === item.feedbackId ? menuRef : undefined}
              onToggleMenu={() => setOpenMenuId(openMenuId === item.feedbackId ? null : item.feedbackId)}
              onToggleComments={() =>
                setExpandedComments((prev) => ({ ...prev, [item.feedbackId]: !prev[item.feedbackId] }))
              }
              onToggleContent={() =>
                setExpandedContent((prev) => ({ ...prev, [item.feedbackId]: !prev[item.feedbackId] }))
              }
              onAnalyze={() => handleAnalyzeMention(item)}
              onCopy={() => handleCopyContent(item)}
              onDelete={() => handleDeleteMention(item)}
              onOpenOriginal={() => handleOpenOriginal(item)}
              onEditSentiment={(sentiment) => handleEditSentiment(item, sentiment)}
              onManageTags={() => openTagModal(item)}
              onMuteAuthor={() => handleMuteAuthor(item)}
              onMutePlatform={() => handleMutePlatform(item)}
            />
          ))}
        </div>
      )}

      {tagModalMention && (
        <div className="fixed inset-0 z-[100] flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
          <div className="w-full max-w-md rounded-2xl border border-white/10 bg-[#151B2B] p-6 shadow-2xl">
            <div className="flex items-center justify-between mb-4">
              <h3 className="text-lg font-bold text-white">Gán tag</h3>
              <button
                type="button"
                onClick={() => setTagModalMention(null)}
                className="p-2 rounded-lg text-gray-400 hover:text-white hover:bg-white/5"
              >
                <X className="w-4 h-4" />
              </button>
            </div>
            <p className="text-sm text-gray-400 mb-4 line-clamp-2">{tagModalMention.content}</p>
            <div className="space-y-2 max-h-48 overflow-y-auto mb-4">
              {projectTags.length === 0 ? (
                <p className="text-sm text-gray-500">Chưa có tag — tạo tag mới bên dưới.</p>
              ) : (
                projectTags.map((tag) => {
                  const checked = selectedTagIds.includes(tag.tagId);
                  return (
                    <label
                      key={tag.tagId}
                      className="flex items-center gap-3 px-3 py-2 rounded-xl border border-white/5 hover:bg-white/5 cursor-pointer"
                    >
                      <input
                        type="checkbox"
                        checked={checked}
                        onChange={() =>
                          setSelectedTagIds((prev) =>
                            checked ? prev.filter((id) => id !== tag.tagId) : [...prev, tag.tagId]
                          )
                        }
                        className="rounded border-white/20"
                      />
                      <span className="text-sm text-white">{tag.name}</span>
                    </label>
                  );
                })
              )}
            </div>
            <div className="flex gap-2 mb-5">
              <input
                type="text"
                value={newTagName}
                onChange={(e) => setNewTagName(e.target.value)}
                placeholder="Tên tag mới"
                className="flex-1 px-3 py-2 rounded-xl bg-[#0A101D] border border-white/10 text-sm text-white"
              />
              <button
                type="button"
                onClick={handleCreateTag}
                disabled={!newTagName.trim()}
                className="px-4 py-2 rounded-xl bg-white/10 text-white text-sm font-semibold disabled:opacity-40"
              >
                Tạo
              </button>
            </div>
            <div className="flex justify-end gap-2">
              <button
                type="button"
                onClick={() => setTagModalMention(null)}
                className="px-4 py-2 rounded-xl text-sm text-gray-400 hover:text-white"
              >
                Hủy
              </button>
              <button
                type="button"
                onClick={handleSaveTags}
                className="px-4 py-2 rounded-xl bg-[#FF7575] text-white text-sm font-semibold hover:bg-[#ff9090]"
              >
                Lưu tag
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default ProjectMentions;
