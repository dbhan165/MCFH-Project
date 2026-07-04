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
} from 'lucide-react';
import { projectApi } from '../api/projectApi';
import type { ProjectMention } from '../types/project';
import { extractApiError } from '../utils/authStorage';
import { formatWorkspaceDateTime } from '../utils/workspaceHelpers';
import { useAppModal } from '../contexts/AppModalContext';
import {
  getPlatformBadgeClass,
  getPlatformDisplayName,
  getPlatformLabel,
  getSentimentBadgeClass,
  getSentimentFilterBadgeClass,
  getSentimentFilterLabel,
  getSentimentLabel,
  MENTION_PLATFORMS,
  MENTION_SENTIMENTS,
  type MentionPlatformFilter,
  type MentionSentimentFilter,
} from '../utils/sentimentHelpers';

const ProjectMentions = () => {
  const { workspaceId, id } = useParams();
  const wid = Number(workspaceId);
  const projectId = Number(id);

  const [mentions, setMentions] = useState<ProjectMention[]>([]);
  const [activePlatform, setActivePlatform] = useState<MentionPlatformFilter>('all');
  const [activeSentiment, setActiveSentiment] = useState<MentionSentimentFilter>('all');
  const [searchText, setSearchText] = useState('');
  const [savedFilters, setSavedFilters] = useState<{ filterId: number; name: string }[]>([]);
  const [filterName, setFilterName] = useState('');
  const [expandedComments, setExpandedComments] = useState<Record<number, boolean>>({});
  const [isLoading, setIsLoading] = useState(true);
  const [isAnalyzing, setIsAnalyzing] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [analyzeMessage, setAnalyzeMessage] = useState('');
  const [openMenuId, setOpenMenuId] = useState<number | null>(null);
  const [actionMentionId, setActionMentionId] = useState<number | null>(null);
  const menuRef = useRef<HTMLDivElement>(null);
  const { confirm, alert } = useAppModal();

  const loadMentions = useCallback(async () => {
    if (!wid || !projectId || Number.isNaN(wid) || Number.isNaN(projectId)) return;

    setIsLoading(true);
    setErrorMessage('');
    try {
      const data = await projectApi.getMentions(wid, projectId, {
        platform: activePlatform !== 'all' ? activePlatform : undefined,
        sentiment: activeSentiment !== 'all' ? activeSentiment : undefined,
        search: searchText.trim() || undefined,
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
  }, [wid, projectId, activePlatform, activeSentiment, searchText]);

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
    loadMentions();
  }, [loadMentions]);

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
        },
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

    setIsAnalyzing(true);
    setAnalyzeMessage(force ? 'Đang phân tích lại toàn bộ dữ liệu...' : 'Đang phân tích các mention chưa có AI...');
    setErrorMessage('');
    try {
      const result = await projectApi.analyze(wid, projectId, force);
      setAnalyzeMessage(result.message);
      await loadMentions();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể chạy phân tích AI.'));
    } finally {
      setIsAnalyzing(false);
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

  const visiblePlatforms = useMemo(
    () =>
      MENTION_PLATFORMS.filter(
        (p) => p === 'all' || platformCounts[p] > 0 || mentions.length === 0
      ),
    [platformCounts, mentions.length]
  );

  const filteredMentions = mentions;

  const tabLabel = (platform: MentionPlatformFilter) => {
    if (platform === 'all') return `Tất cả (${platformCounts.all})`;
    return `${getPlatformDisplayName(platform)} (${platformCounts[platform]})`;
  };

  const sentimentTabLabel = (filter: MentionSentimentFilter) => {
    if (filter === 'all') return `Tất cả (${sentimentCounts.all})`;
    return `${getSentimentFilterLabel(filter)} (${sentimentCounts[filter]})`;
  };

  const toggleComments = (feedbackId: number) => {
    setExpandedComments((prev) => ({ ...prev, [feedbackId]: !prev[feedbackId] }));
  };

  const commentCountLabel = (item: ProjectMention) => {
    const n = item.comments.length > 0 ? item.comments.length : item.commentsCount;
    return n > 0 ? `${n} bình luận` : 'không có bình luận';
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
    setIsAnalyzing(true);
    setAnalyzeMessage('Đang phân tích mention này...');
    setErrorMessage('');

    try {
      const result = await projectApi.analyzeMention(wid, projectId, item.feedbackId);
      setAnalyzeMessage(result.message);
      await loadMentions();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể phân tích mention.'));
    } finally {
      setIsAnalyzing(false);
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

  return (
    <div className="animate-in fade-in duration-500 max-w-5xl mx-auto">
      <div className="mb-8 flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4">
        <div>
          <h2 className="text-2xl font-bold flex items-center gap-2 text-white">
            <MessageCircle className="text-[#FF7575]" /> Data Stream (Mentions)
          </h2>
          <p className="text-gray-400 text-sm mt-1">
            {mentions.length} bản ghi — AI phân tích caption + bình luận gom theo từng bài
          </p>
        </div>
        <div className="flex flex-wrap gap-2 shrink-0">
          <button
            type="button"
            onClick={() => runAnalyze(false)}
            disabled={isAnalyzing || isLoading || mentions.length === 0}
            className="flex items-center gap-2 px-4 py-2.5 rounded-xl bg-[#FF7575] text-white text-sm font-medium hover:bg-[#ff5c5c] disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isAnalyzing ? <Loader2 className="w-4 h-4 animate-spin" /> : <Sparkles className="w-4 h-4" />}
            Phân tích AI
          </button>
        </div>
      </div>

      {isAnalyzing && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-sm">
          <div className="bg-[#151B2B] border border-white/10 rounded-2xl px-8 py-6 flex flex-col items-center gap-3 shadow-2xl">
            <Loader2 className="w-10 h-10 animate-spin text-[#FF7575]" />
            <p className="text-sm text-gray-300">Đang phân tích AI — vui lòng đợi...</p>
          </div>
        </div>
      )}

      {analyzeMessage && !isAnalyzing && (
        <div className="mb-6 bg-emerald-500/10 border border-emerald-500/20 text-emerald-300 p-4 rounded-xl text-sm">
          {analyzeMessage}
        </div>
      )}

      <div className="mb-6 flex flex-col sm:flex-row gap-3">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-500" />
          <input
            value={searchText}
            onChange={(e) => setSearchText(e.target.value)}
            placeholder="Tìm theo nội dung hoặc tác giả..."
            className="w-full pl-10 pr-4 py-2.5 bg-[#151B2B] border border-white/10 rounded-xl text-white text-sm"
          />
        </div>
        <div className="flex gap-2">
          <input
            value={filterName}
            onChange={(e) => setFilterName(e.target.value)}
            placeholder="Tên bộ lọc"
            className="px-3 py-2.5 bg-[#151B2B] border border-white/10 rounded-xl text-white text-sm w-36"
          />
          <button
            type="button"
            onClick={saveCurrentFilter}
            className="flex items-center gap-1.5 px-3 py-2.5 rounded-xl bg-white/10 hover:bg-white/15 text-sm text-white"
          >
            <BookmarkPlus className="w-4 h-4" />
            Lưu
          </button>
        </div>
      </div>

      {savedFilters.length > 0 && (
        <div className="mb-4 flex flex-wrap gap-2">
          {savedFilters.map((f) => (
            <span key={f.filterId} className="px-3 py-1 rounded-full bg-[#151B2B] border border-white/10 text-xs text-gray-300">
              {f.name}
            </span>
          ))}
        </div>
      )}

      {/* Sentiment summary + filter */}
      <div className="mb-6 grid grid-cols-2 sm:grid-cols-4 gap-3">
        {(['positive', 'negative', 'neutral', 'pending'] as const).map((key) => (
          <button
            key={key}
            type="button"
            onClick={() => setActiveSentiment(activeSentiment === key ? 'all' : key)}
            className={`rounded-xl border p-4 text-left transition-all ${
              activeSentiment === key
                ? getSentimentFilterBadgeClass(key) + ' ring-1 ring-white/20'
                : 'bg-[#151B2B] border-white/5 hover:border-white/15'
            }`}
          >
            <p className="text-xs text-gray-500 mb-1">{getSentimentFilterLabel(key)}</p>
            <p className="text-2xl font-bold text-white">{sentimentCounts[key]}</p>
          </button>
        ))}
      </div>

      <div className="flex flex-wrap gap-2 mb-4 p-1.5 bg-[#0A101D] border border-white/5 rounded-xl">
        {MENTION_SENTIMENTS.map((filter) => (
          <button
            key={filter}
            type="button"
            onClick={() => setActiveSentiment(filter)}
            className={`px-4 py-2 rounded-lg text-sm font-medium transition-all ${
              activeSentiment === filter
                ? filter === 'all'
                  ? 'bg-white/10 text-white'
                  : getSentimentFilterBadgeClass(filter)
                : 'text-gray-400 hover:text-white hover:bg-white/5'
            }`}
          >
            {sentimentTabLabel(filter)}
          </button>
        ))}
      </div>

      <div className="flex flex-wrap gap-2 mb-8 p-1.5 bg-[#0A101D] border border-white/5 rounded-xl">
        {visiblePlatforms.map((platform) => (
          <button
            key={platform}
            type="button"
            onClick={() => setActivePlatform(platform)}
            className={`px-4 py-2.5 rounded-lg text-sm font-medium transition-all ${
              activePlatform === platform
                ? 'bg-[#FF7575] text-white shadow-lg shadow-[#FF7575]/20'
                : 'text-gray-400 hover:text-white hover:bg-white/5'
            }`}
          >
            {tabLabel(platform)}
          </button>
        ))}
      </div>

      {errorMessage && (
        <div className="mb-6 bg-red-500/10 border border-red-500/20 text-red-300 p-4 rounded-xl flex items-center gap-3 text-sm">
          <AlertCircle className="w-5 h-5 shrink-0" />
          {errorMessage}
        </div>
      )}

      {isLoading ? (
        <div className="flex flex-col items-center justify-center py-24 text-gray-400 gap-4">
          <Loader2 className="w-10 h-10 animate-spin text-[#FF7575]" />
          <p>Đang tải mentions...</p>
        </div>
      ) : filteredMentions.length === 0 ? (
        <div className="bg-[#151B2B] border border-white/5 rounded-2xl p-12 text-center text-gray-400">
          <p>
            {mentions.length === 0
              ? 'Chưa có mention nào. Chạy scrape từ trang tạo dự án hoặc gọi API scrape.'
              : 'Không có mention nào khớp bộ lọc hiện tại.'}
          </p>
        </div>
      ) : (
        <div className="space-y-5">
          {filteredMentions.map((item) => {
            const hasComments = item.comments.length > 0;
            const commentsExpanded = expandedComments[item.feedbackId] ?? hasComments;
            const hasSentiment = item.sentiment != null && item.sentiment !== '';
            const isMenuOpen = openMenuId === item.feedbackId;
            const isBusy = actionMentionId === item.feedbackId;

            return (
              <div key={item.feedbackId} className="bg-[#151B2B] border border-white/5 rounded-2xl p-6 group">
                <div className="flex justify-between items-start mb-4 gap-4">
                  <div className="flex items-center gap-3 min-w-0">
                    <div
                      className={`w-10 h-10 rounded-full flex items-center justify-center font-bold text-sm shrink-0 ${getPlatformBadgeClass(item.platform)}`}
                      title={getPlatformDisplayName(item.platform)}
                    >
                      {getPlatformLabel(item.platform)}
                    </div>
                    <div className="min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <h4 className="font-bold text-white truncate">{item.authorName || 'Không rõ tác giả'}</h4>
                        <span className="text-[10px] uppercase tracking-wide text-gray-500 bg-white/5 px-2 py-0.5 rounded">
                          {getPlatformDisplayName(item.platform)}
                        </span>
                      </div>
                      <p className="text-xs text-gray-500">
                        {item.postedAt
                          ? `Đăng: ${formatWorkspaceDateTime(item.postedAt)}`
                          : 'Chưa có ngày đăng'}
                        {item.scrapedAt && (
                          <span className="text-gray-600"> · Cào: {formatWorkspaceDateTime(item.scrapedAt)}</span>
                        )}
                      </p>
                    </div>
                  </div>
                  <div className="flex items-start gap-2 shrink-0">
                    <span
                      className={`px-3 py-1 border rounded-full text-xs font-semibold ${getSentimentBadgeClass(item.sentiment)}`}
                    >
                      {hasSentiment ? getSentimentLabel(item.sentiment) : 'Chưa phân tích'}
                      {item.confidenceScore != null && hasSentiment
                        ? ` · ${Math.round(item.confidenceScore * 100)}%`
                        : ''}
                    </span>
                    <div className="relative" ref={isMenuOpen ? menuRef : undefined}>
                      <button
                        type="button"
                        onClick={() => setOpenMenuId(isMenuOpen ? null : item.feedbackId)}
                        disabled={isBusy}
                        className="p-2 rounded-xl text-gray-500 hover:text-white hover:bg-white/10 opacity-100 sm:opacity-0 sm:group-hover:opacity-100 focus:opacity-100 transition-all disabled:opacity-40"
                        aria-label="Tùy chọn mention"
                      >
                        {isBusy ? (
                          <Loader2 size={18} className="animate-spin" />
                        ) : (
                          <MoreVertical size={18} />
                        )}
                      </button>

                      {isMenuOpen && (
                        <div className="absolute right-0 top-10 w-52 bg-[#1A2235] border border-white/10 rounded-xl shadow-2xl z-50 py-1.5 overflow-hidden">
                          {item.originalUrl ? (
                            <button
                              type="button"
                              onClick={() => handleOpenOriginal(item)}
                              className="w-full flex items-center gap-2.5 px-4 py-2.5 text-sm text-gray-300 hover:text-white hover:bg-white/5"
                            >
                              <ExternalLink size={16} className="text-[#00B4D8]" />
                              Xem bài gốc
                            </button>
                          ) : null}
                          <button
                            type="button"
                            onClick={() => handleAnalyzeMention(item)}
                            className="w-full flex items-center gap-2.5 px-4 py-2.5 text-sm text-gray-300 hover:text-white hover:bg-white/5"
                          >
                            <Sparkles size={16} className="text-amber-400" />
                            Phân tích lại
                          </button>
                          <button
                            type="button"
                            onClick={() => handleCopyContent(item)}
                            className="w-full flex items-center gap-2.5 px-4 py-2.5 text-sm text-gray-300 hover:text-white hover:bg-white/5"
                          >
                            <Copy size={16} className="text-violet-400" />
                            Sao chép nội dung
                          </button>
                          <div className="h-px bg-white/5 my-1" />
                          <button
                            type="button"
                            onClick={() => handleDeleteMention(item)}
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

                <p className="text-gray-300 text-sm mb-4">"{item.content}"</p>

                <div className="mb-4 rounded-xl border border-[#FF7575]/20 bg-gradient-to-br from-[#FF7575]/8 to-[#00B4D8]/5 overflow-hidden">
                  <div className="px-4 py-3 border-b border-white/5 flex items-center justify-between gap-3 flex-wrap">
                    <div className="flex items-center gap-2 flex-wrap">
                      <Sparkles className="w-4 h-4 text-[#FF7575]" />
                      <span className="text-sm font-semibold text-white">Phân tích AI từ bình luận</span>
                      <span className="text-xs text-gray-500">({commentCountLabel(item)})</span>
                      {hasSentiment && (
                        <span
                          className={`px-2 py-0.5 rounded-full text-[10px] font-semibold border ${getSentimentBadgeClass(item.sentiment)}`}
                        >
                          {getSentimentLabel(item.sentiment)}
                        </span>
                      )}
                    </div>
                    {item.analyzedAt && (
                      <span className="text-[10px] text-gray-500 shrink-0">
                        {formatWorkspaceDateTime(item.analyzedAt)}
                      </span>
                    )}
                  </div>

                  <div className="px-4 py-4">
                    {item.aiSummary ? (
                      <p className="text-sm text-gray-200 leading-relaxed whitespace-pre-wrap">{item.aiSummary}</p>
                    ) : (
                      <div className="flex flex-col sm:flex-row sm:items-center gap-3">
                        <p className="text-sm text-gray-400">
                          {hasSentiment
                            ? 'Đã có sentiment nhưng chưa có tóm tắt chi tiết — bấm «Phân tích lại».'
                            : 'Chưa phân tích. AI sẽ đọc toàn bộ comment đã gom và mô tả tình hình cộng đồng.'}
                        </p>
                        <button
                          type="button"
                          onClick={() => runAnalyze(true)}
                          disabled={isAnalyzing}
                          className="flex items-center gap-1.5 text-xs text-[#FF7575] hover:text-white shrink-0 disabled:opacity-50"
                        >
                          <RefreshCw className={`w-3.5 h-3.5 ${isAnalyzing ? 'animate-spin' : ''}`} />
                          {hasSentiment ? 'Phân tích lại' : 'Phân tích ngay'}
                        </button>
                      </div>
                    )}
                  </div>

                  {(hasComments || item.commentsCount > 0) && (
                    <div className="border-t border-white/5">
                      <button
                        type="button"
                        onClick={() => toggleComments(item.feedbackId)}
                        className="w-full flex items-center justify-between gap-2 px-4 py-3 text-sm text-gray-400 hover:text-white hover:bg-white/5 transition-colors"
                      >
                        <span>
                          {hasComments
                            ? `Danh sách ${item.comments.length} bình luận đã gom`
                            : `${item.commentsCount} bình luận (chưa tải chi tiết)`}
                        </span>
                        {commentsExpanded ? (
                          <ChevronUp className="w-4 h-4 shrink-0" />
                        ) : (
                          <ChevronDown className="w-4 h-4 shrink-0" />
                        )}
                      </button>

                      {commentsExpanded && hasComments && (
                        <ul className="px-4 pb-4 space-y-2 max-h-72 overflow-y-auto">
                          {item.comments.map((comment, idx) => (
                            <li
                              key={`${item.feedbackId}-c-${idx}`}
                              className="text-sm text-gray-300 bg-[#0A101D]/60 rounded-lg px-3 py-2.5 border border-white/5"
                            >
                              <span className="text-[#FF7575] font-medium mr-2">{idx + 1}.</span>
                              {comment}
                            </li>
                          ))}
                        </ul>
                      )}

                      {commentsExpanded && !hasComments && item.commentsCount > 0 && (
                        <p className="px-4 pb-4 text-xs text-amber-400/90">
                          Dữ liệu cũ bị lệch (DB ghi {item.commentsCount} comment nhưng file chi tiết trống).
                          Bấm «Cào lại dữ liệu» trên project để tải comment — phân tích AI chỉ đọc dữ liệu đã lưu.
                        </p>
                      )}
                    </div>
                  )}
                </div>

                <div className="flex justify-between items-center pt-2 gap-4">
                  <span className="text-xs text-gray-500">{commentCountLabel(item)}</span>
                  {item.originalUrl ? (
                    <a
                      href={item.originalUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="flex items-center gap-2 text-sm text-[#00B4D8] bg-[#00B4D8]/10 px-4 py-2 rounded-lg hover:bg-[#00B4D8]/20 transition-colors"
                    >
                      <ExternalLink size={16} /> Xem bài gốc
                    </a>
                  ) : null}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};

export default ProjectMentions;
