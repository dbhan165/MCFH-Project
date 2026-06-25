import { useCallback, useEffect, useMemo, useState } from 'react';
import { useNavigate, Link } from 'react-router-dom';
import {
  Building2,
  Plus,
  Users,
  Settings,
  ArrowRight,
  Loader2,
  AlertCircle,
  FolderKanban,
  Search,
  Sparkles,
  RefreshCw,
  Crown,
  LayoutGrid,
  CalendarDays,
} from 'lucide-react';
import { workspaceApi } from '../api/workspaceApi';
import type { Workspace } from '../types/workspace';
import { extractApiError, loadProfileFromStorage } from '../utils/authStorage';
import { formatWorkspaceDate, getRoleBadgeClass, getRoleLabel, isWorkspaceOwner } from '../utils/workspaceHelpers';

const CARD_GRADIENTS = [
  'from-[#FF7575]/20 via-[#FF7575]/5 to-transparent',
  'from-[#00B4D8]/20 via-[#00B4D8]/5 to-transparent',
  'from-violet-500/20 via-violet-500/5 to-transparent',
  'from-emerald-500/20 via-emerald-500/5 to-transparent',
  'from-amber-500/20 via-amber-500/5 to-transparent',
];

const AVATAR_GRADIENTS = [
  'from-[#FF7575] to-orange-500',
  'from-[#00B4D8] to-blue-600',
  'from-violet-500 to-purple-700',
  'from-emerald-500 to-teal-600',
  'from-amber-500 to-orange-600',
];

function pickGradientIndex(name: string, mod: number) {
  let hash = 0;
  for (let i = 0; i < name.length; i++) hash = name.charCodeAt(i) + ((hash << 5) - hash);
  return Math.abs(hash) % mod;
}

function WorkspaceCard({
  ws,
  onEnter,
}: {
  ws: Workspace;
  onEnter: (id: number) => void;
}) {
  const idx = pickGradientIndex(ws.name, CARD_GRADIENTS.length);
  const cardGradient = CARD_GRADIENTS[idx];
  const avatarGradient = AVATAR_GRADIENTS[idx];
  const initial = ws.name.charAt(0).toUpperCase() || '?';

  return (
    <article
      className="group relative overflow-hidden rounded-3xl border border-white/10 bg-[#151B2B] transition-all duration-300 hover:border-[#FF7575]/40 hover:shadow-[0_20px_50px_rgba(255,117,117,0.12)] hover:-translate-y-1"
    >
      <div className={`absolute inset-0 bg-gradient-to-br ${cardGradient} opacity-80 pointer-events-none`} />
      <div className="absolute -top-16 -right-16 w-40 h-40 bg-[#FF7575]/10 rounded-full blur-3xl opacity-0 group-hover:opacity-100 transition-opacity pointer-events-none" />

      <div className="relative p-6 flex flex-col h-full min-h-[280px]">
        <div className="flex items-start justify-between gap-3 mb-5">
          <div
            className={`w-14 h-14 rounded-2xl bg-gradient-to-br ${avatarGradient} flex items-center justify-center text-white font-bold text-xl shadow-lg ring-2 ring-white/10`}
          >
            {initial}
          </div>
          <span className={`px-3 py-1 rounded-full text-[11px] font-bold tracking-wide border shrink-0 ${getRoleBadgeClass(ws.myRole)}`}>
            {getRoleLabel(ws.myRole)}
          </span>
        </div>

        <h3 className="text-xl font-bold text-white mb-1 line-clamp-1 group-hover:text-[#FF7575] transition-colors">
          {ws.name}
        </h3>
        <p className="text-sm text-gray-500 mb-5 flex items-center gap-1.5">
          <Crown className="w-3.5 h-3.5 text-amber-500/80 shrink-0" />
          <span className="truncate">{ws.ownerName}</span>
        </p>

        <div className="grid grid-cols-3 gap-2 mb-6">
          <div className="rounded-xl bg-white/[0.04] border border-white/5 px-3 py-2.5 text-center">
            <p className="text-lg font-bold text-white tabular-nums">{ws.memberCount}</p>
            <p className="text-[10px] text-gray-500 uppercase tracking-wide mt-0.5">Thành viên</p>
          </div>
          <div className="rounded-xl bg-white/[0.04] border border-white/5 px-3 py-2.5 text-center">
            <p className="text-lg font-bold text-white tabular-nums">{ws.projectCount}</p>
            <p className="text-[10px] text-gray-500 uppercase tracking-wide mt-0.5">Dự án</p>
          </div>
          <div className="rounded-xl bg-white/[0.04] border border-white/5 px-3 py-2.5 text-center col-span-1">
            <p className="text-[11px] font-semibold text-gray-300 leading-tight mt-1">
              {formatWorkspaceDate(ws.createdAt)}
            </p>
            <p className="text-[10px] text-gray-500 uppercase tracking-wide mt-1">Tạo ngày</p>
          </div>
        </div>

        <div className="mt-auto pt-4 border-t border-white/10 flex items-center justify-between gap-3">
          {isWorkspaceOwner(ws.myRole) ? (
            <Link
              to={`/workspace/${ws.workspaceId}/settings`}
              onClick={(e) => e.stopPropagation()}
              className="inline-flex items-center gap-2 text-sm text-gray-400 hover:text-white px-3 py-2 rounded-xl hover:bg-white/5 transition-colors"
            >
              <Settings size={16} />
              Cài đặt
            </Link>
          ) : (
            <span className="text-xs text-gray-600">ID #{ws.workspaceId}</span>
          )}

          <button
            type="button"
            onClick={() => onEnter(ws.workspaceId)}
            className="inline-flex items-center gap-2 text-sm font-bold text-white bg-[#FF7575]/90 hover:bg-[#FF7575] px-4 py-2.5 rounded-xl transition-all group-hover:shadow-[0_4px_20px_rgba(255,117,117,0.35)]"
          >
            Vào workspace
            <ArrowRight size={16} className="group-hover:translate-x-0.5 transition-transform" />
          </button>
        </div>
      </div>
    </article>
  );
}

const Workspaces = () => {
  const navigate = useNavigate();
  const profile = loadProfileFromStorage();
  const [workspaces, setWorkspaces] = useState<Workspace[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');
  const [searchQuery, setSearchQuery] = useState('');

  const loadWorkspaces = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage('');
    try {
      const data = await workspaceApi.getMyWorkspaces();
      setWorkspaces(data);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải danh sách workspace.'));
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadWorkspaces();
  }, [loadWorkspaces]);

  const handleEnterWorkspace = (workspaceId: number) => {
    navigate(`/workspace/${workspaceId}/projects`);
  };

  const filteredWorkspaces = useMemo(() => {
    const q = searchQuery.trim().toLowerCase();
    if (!q) return workspaces;
    return workspaces.filter(
      (ws) =>
        ws.name.toLowerCase().includes(q) ||
        ws.ownerName.toLowerCase().includes(q) ||
        getRoleLabel(ws.myRole).toLowerCase().includes(q)
    );
  }, [workspaces, searchQuery]);

  const stats = useMemo(
    () => ({
      total: workspaces.length,
      members: workspaces.reduce((sum, ws) => sum + ws.memberCount, 0),
      projects: workspaces.reduce((sum, ws) => sum + ws.projectCount, 0),
      owned: workspaces.filter((ws) => isWorkspaceOwner(ws.myRole)).length,
    }),
    [workspaces]
  );

  const greeting = profile?.fullName?.split(' ').pop() || 'bạn';

  return (
    <div className="animate-in fade-in duration-500 max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 pb-16 space-y-8">
      {/* Hero */}
      <div className="relative overflow-hidden rounded-3xl border border-white/5 bg-gradient-to-br from-[#151B2B] via-[#141A2C] to-[#0A101D] p-8 sm:p-10">
        <div className="absolute -top-24 -right-20 w-72 h-72 bg-[#FF7575]/15 rounded-full blur-3xl pointer-events-none" />
        <div className="absolute -bottom-16 -left-16 w-56 h-56 bg-[#00B4D8]/10 rounded-full blur-3xl pointer-events-none" />

        <div className="relative flex flex-col lg:flex-row lg:items-center lg:justify-between gap-8">
          <div className="max-w-2xl">
            <div className="inline-flex items-center gap-2 px-3 py-1 rounded-full bg-white/5 border border-white/10 text-xs text-gray-400 mb-4">
              <Sparkles className="w-3.5 h-3.5 text-[#FF7575]" />
              Social Listening Platform
            </div>
            <h1 className="text-3xl sm:text-4xl font-extrabold text-white tracking-tight mb-3">
              Xin chào, {greeting}
            </h1>
            <p className="text-gray-400 text-sm sm:text-base leading-relaxed">
              Chọn workspace để quản lý dự án giám sát mạng xã hội, hoặc tạo không gian làm việc mới cho tổ chức của bạn.
            </p>
          </div>

          <Link
            to="/create-workspace"
            className="inline-flex items-center justify-center gap-2 bg-[#FF7575] hover:bg-[#ff6262] text-white px-6 py-3.5 rounded-2xl text-sm font-bold transition-all shadow-[0_8px_30px_rgba(255,117,117,0.35)] shrink-0"
          >
            <Plus size={20} />
            Tạo Workspace mới
          </Link>
        </div>

        {!isLoading && workspaces.length > 0 && (
          <div className="relative grid grid-cols-2 sm:grid-cols-4 gap-3 mt-8 pt-8 border-t border-white/5">
            {[
              { label: 'Workspace', value: stats.total, icon: LayoutGrid, color: 'text-[#FF7575]' },
              { label: 'Bạn sở hữu', value: stats.owned, icon: Crown, color: 'text-amber-400' },
              { label: 'Thành viên', value: stats.members, icon: Users, color: 'text-[#00B4D8]' },
              { label: 'Dự án', value: stats.projects, icon: FolderKanban, color: 'text-emerald-400' },
            ].map(({ label, value, icon: Icon, color }) => (
              <div
                key={label}
                className="rounded-2xl bg-white/[0.03] border border-white/5 px-4 py-3 flex items-center gap-3"
              >
                <div className={`w-10 h-10 rounded-xl bg-white/5 flex items-center justify-center ${color}`}>
                  <Icon size={18} />
                </div>
                <div>
                  <p className="text-xl font-bold text-white tabular-nums">{value}</p>
                  <p className="text-[11px] text-gray-500 uppercase tracking-wide">{label}</p>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>

      {errorMessage && (
        <div className="bg-red-500/10 border border-red-500/20 text-red-300 p-4 rounded-2xl flex items-center gap-3 text-sm">
          <AlertCircle className="w-5 h-5 shrink-0" />
          <span className="flex-1">{errorMessage}</span>
          <button
            type="button"
            onClick={loadWorkspaces}
            className="inline-flex items-center gap-1.5 text-red-200 hover:text-white font-semibold text-xs"
          >
            <RefreshCw size={14} />
            Thử lại
          </button>
        </div>
      )}

      {/* Toolbar */}
      {!isLoading && workspaces.length > 0 && (
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
          <div className="relative flex-1 max-w-md">
            <Search className="absolute left-4 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-500" />
            <input
              type="text"
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="Tìm workspace, owner hoặc vai trò..."
              className="w-full pl-11 pr-4 py-3 bg-[#151B2B] border border-white/10 rounded-2xl text-sm text-white placeholder:text-gray-600 focus:outline-none focus:border-[#FF7575]/40 focus:ring-1 focus:ring-[#FF7575]/20 transition-colors"
            />
          </div>
          <p className="text-sm text-gray-500">
            Hiển thị <span className="text-white font-semibold">{filteredWorkspaces.length}</span> / {workspaces.length} workspace
          </p>
        </div>
      )}

      {isLoading ? (
        <div className="flex flex-col items-center justify-center py-32 text-gray-400 gap-4">
          <div className="relative">
            <div className="absolute inset-0 rounded-full bg-[#FF7575]/20 blur-2xl animate-pulse" />
            <Loader2 className="relative w-12 h-12 animate-spin text-[#FF7575]" />
          </div>
          <p className="text-sm font-medium">Đang tải workspace của bạn...</p>
        </div>
      ) : workspaces.length === 0 && !errorMessage ? (
        <div className="relative overflow-hidden rounded-3xl border border-white/10 bg-[#151B2B] p-12 sm:p-16 text-center">
          <div className="absolute inset-0 bg-gradient-to-br from-[#FF7575]/5 to-transparent pointer-events-none" />
          <div className="relative">
            <div className="w-20 h-20 rounded-3xl bg-gradient-to-br from-[#FF7575] to-orange-500 flex items-center justify-center mx-auto mb-6 shadow-[0_12px_40px_rgba(255,117,117,0.3)]">
              <Building2 className="w-10 h-10 text-white" />
            </div>
            <h3 className="text-2xl sm:text-3xl font-bold text-white mb-3">Bắt đầu hành trình MCFH</h3>
            <p className="text-gray-400 mb-8 max-w-lg mx-auto leading-relaxed">
              Tạo workspace đầu tiên để theo dõi thương hiệu trên Facebook, YouTube, TikTok và biên soạn báo cáo chuyên sâu cùng team.
            </p>
            <Link
              to="/create-workspace"
              className="inline-flex items-center gap-2 bg-[#FF7575] hover:bg-[#ff6262] text-white px-8 py-3.5 rounded-2xl text-sm font-bold transition-all shadow-[0_8px_30px_rgba(255,117,117,0.35)]"
            >
              <Plus size={18} />
              Khởi tạo Workspace đầu tiên
            </Link>
          </div>
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-6">
          {filteredWorkspaces.map((ws) => (
            <WorkspaceCard key={ws.workspaceId} ws={ws} onEnter={handleEnterWorkspace} />
          ))}

          {filteredWorkspaces.length === 0 && searchQuery && (
            <div className="md:col-span-2 xl:col-span-3 rounded-3xl border border-white/10 bg-[#151B2B] p-12 text-center">
              <Search className="w-10 h-10 text-gray-600 mx-auto mb-4" />
              <p className="text-white font-semibold mb-1">Không tìm thấy workspace</p>
              <p className="text-sm text-gray-500">Thử từ khóa khác hoặc xóa bộ lọc tìm kiếm.</p>
            </div>
          )}

          {!searchQuery && (
            <Link
              to="/create-workspace"
              className="group relative overflow-hidden rounded-3xl border-2 border-dashed border-white/10 hover:border-[#FF7575]/40 bg-[#151B2B]/50 hover:bg-[#FF7575]/[0.03] p-6 flex flex-col items-center justify-center gap-4 text-gray-500 hover:text-[#FF7575] transition-all duration-300 min-h-[280px]"
            >
              <div className="w-16 h-16 rounded-2xl bg-white/5 border border-white/10 flex items-center justify-center group-hover:bg-[#FF7575]/15 group-hover:border-[#FF7575]/30 group-hover:scale-105 transition-all">
                <Plus size={28} className="group-hover:text-[#FF7575]" />
              </div>
              <div className="text-center">
                <p className="font-bold text-lg text-gray-300 group-hover:text-white transition-colors">Tạo Workspace mới</p>
                <p className="text-xs text-gray-600 mt-1">Thêm tổ chức hoặc team mới</p>
              </div>
            </Link>
          )}
        </div>
      )}

      {!isLoading && workspaces.length > 0 && (
        <p className="text-center text-xs text-gray-600 flex items-center justify-center gap-2">
          <CalendarDays className="w-3.5 h-3.5" />
          Workspace giúp phân tách dữ liệu theo từng tổ chức — mỗi team có dự án và thành viên riêng.
        </p>
      )}
    </div>
  );
};

export default Workspaces;
