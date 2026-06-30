import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import {
  Settings,
  Users,
  Trash2,
  UserPlus,
  Save,
  AlertTriangle,
  ArrowLeft,
  Shield,
  X,
  Loader2,
  Clock,
  CheckCircle2,
  History,
  AlertCircle,
} from 'lucide-react';
import { workspaceApi } from '../api/workspaceApi';
import type { ActivityLog, Workspace, WorkspaceInvitation, WorkspaceMember } from '../types/workspace';
import { extractApiError } from '../utils/authStorage';
import {
  formatWorkspaceDateTime,
  getActivityLabel,
  getMemberAvatar,
  getRoleBadgeClass,
  getRoleLabel,
  isWorkspaceOwner,
} from '../utils/workspaceHelpers';
import ConfirmModal from '../components/common/ConfirmModal';
import { useAppModal } from '../contexts/AppModalContext';

type SettingsTab = 'general' | 'members' | 'activity';

const WorkspaceSettings = () => {
  const { workspaceId } = useParams();
  const navigate = useNavigate();
  const id = Number(workspaceId);
  const { confirm } = useAppModal();

  const [activeTab, setActiveTab] = useState<SettingsTab>('general');
  const [workspace, setWorkspace] = useState<Workspace | null>(null);
  const [workspaceName, setWorkspaceName] = useState('');
  const [members, setMembers] = useState<WorkspaceMember[]>([]);
  const [invitations, setInvitations] = useState<WorkspaceInvitation[]>([]);
  const [activityLogs, setActivityLogs] = useState<ActivityLog[]>([]);

  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState('');

  const [isInviteModalOpen, setIsInviteModalOpen] = useState(false);
  const [inviteEmail, setInviteEmail] = useState('');
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);

  const isOwner = isWorkspaceOwner(workspace?.myRole);

  const loadData = useCallback(async () => {
    if (!id || Number.isNaN(id)) return;

    setIsLoading(true);
    setErrorMessage('');
    try {
      const [ws, memberList, logs] = await Promise.all([
        workspaceApi.getById(id),
        workspaceApi.getMembers(id),
        workspaceApi.getActivityLogs(id),
      ]);

      setWorkspace(ws);
      setWorkspaceName(ws.name);
      setMembers(memberList);
      setActivityLogs(logs);

      if (isWorkspaceOwner(ws.myRole)) {
        const pending = await workspaceApi.getPendingInvitations(id);
        setInvitations(pending);
      } else {
        setInvitations([]);
      }
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải thông tin workspace.'));
    } finally {
      setIsLoading(false);
    }
  }, [id]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const showSuccess = (message: string) => {
    setSuccessMessage(message);
    setTimeout(() => setSuccessMessage(''), 3000);
  };

  const handleUpdateWorkspace = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!workspace || !isOwner) return;

    setIsSaving(true);
    setErrorMessage('');
    try {
      const updated = await workspaceApi.update(workspace.workspaceId, workspaceName.trim());
      setWorkspace(updated);
      showSuccess('Đã cập nhật tên workspace.');
      await loadData();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể cập nhật workspace.'));
    } finally {
      setIsSaving(false);
    }
  };

  const handleDeleteWorkspace = async () => {
    if (!workspace || !isOwner) return;

    setIsSaving(true);
    try {
      await workspaceApi.delete(workspace.workspaceId);
      navigate('/workspaces');
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể xóa workspace.'));
      setIsDeleteModalOpen(false);
    } finally {
      setIsSaving(false);
    }
  };

  const handleRoleChange = async (member: WorkspaceMember, newRole: string) => {
    if (!workspace || !isOwner || member.roleName === 'Owner') return;

    try {
      await workspaceApi.updateMemberRole(workspace.workspaceId, member.userId, newRole);
      showSuccess(`Đã đổi vai trò của ${member.fullName}.`);
      await loadData();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể đổi vai trò.'));
    }
  };

  const handleRemoveMember = async (member: WorkspaceMember) => {
    if (!workspace || !isOwner || member.roleName === 'Owner') return;

    const confirmed = await confirm({
      title: 'Xóa thành viên',
      message: `Bạn có chắc muốn xóa ${member.fullName} khỏi workspace này?`,
      confirmText: 'Xóa thành viên',
      cancelText: 'Hủy bỏ',
      type: 'danger',
    });
    if (!confirmed) return;

    try {
      await workspaceApi.kickMember(workspace.workspaceId, member.userId);
      showSuccess(`Đã xóa ${member.fullName} khỏi workspace.`);
      await loadData();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể xóa thành viên.'));
    }
  };

  const handleInvite = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!workspace) return;

    setIsSaving(true);
    setErrorMessage('');
    try {
      const message = await workspaceApi.inviteMember(workspace.workspaceId, inviteEmail.trim());
      setIsInviteModalOpen(false);
      setInviteEmail('');
      showSuccess(message);
      await loadData();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể gửi lời mời.'));
    } finally {
      setIsSaving(false);
    }
  };

  const handleApproveInvitation = async (invitationId: number) => {
    if (!workspace || !isOwner) return;

    try {
      await workspaceApi.approveInvitation(workspace.workspaceId, invitationId);
      showSuccess('Đã duyệt lời mời.');
      await loadData();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể duyệt lời mời.'));
    }
  };

  const handleRejectInvitation = async (invitationId: number) => {
    if (!workspace || !isOwner) return;

    try {
      await workspaceApi.rejectInvitation(workspace.workspaceId, invitationId);
      showSuccess('Đã từ chối lời mời.');
      await loadData();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể từ chối lời mời.'));
    }
  };

  if (!id || Number.isNaN(id)) {
    return (
      <div className="min-h-screen bg-[#050A15] text-white flex items-center justify-center">
        <p>Workspace không hợp lệ.</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-[#050A15] text-white font-sans selection:bg-[#FF7575] selection:text-white">
      <header className="h-16 bg-[#0A101D] border-b border-white/5 flex items-center px-8">
        <Link to="/workspaces" className="flex items-center gap-2 text-gray-400 hover:text-white transition-colors text-sm font-medium">
          <ArrowLeft size={18} />
          Quay lại danh sách Workspace
        </Link>
      </header>

      <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
        <div className="mb-8">
          <h1 className="text-3xl font-extrabold tracking-tight mb-2">Cài đặt Workspace</h1>
          <p className="text-[#9BA1B0]">
            {workspace ? workspace.name : 'Đang tải...'} — quản lý thông tin và phân quyền thành viên.
          </p>
        </div>

        {(errorMessage || successMessage) && (
          <div className="mb-6 space-y-3">
            {errorMessage && (
              <div className="bg-red-500/10 border border-red-500/20 text-red-300 p-4 rounded-xl flex items-center gap-3 text-sm">
                <AlertCircle className="w-5 h-5 shrink-0" />
                {errorMessage}
              </div>
            )}
            {successMessage && (
              <div className="bg-emerald-500/10 border border-emerald-500/20 text-emerald-300 p-4 rounded-xl flex items-center gap-3 text-sm">
                <CheckCircle2 className="w-5 h-5 shrink-0" />
                {successMessage}
              </div>
            )}
          </div>
        )}

        {isLoading ? (
          <div className="flex flex-col items-center justify-center py-24 text-gray-400 gap-4">
            <Loader2 className="w-10 h-10 animate-spin text-[#FF7575]" />
            <p>Đang tải cài đặt workspace...</p>
          </div>
        ) : !workspace ? (
          <div className="text-center py-24 text-gray-400">Không tìm thấy workspace hoặc bạn không có quyền truy cập.</div>
        ) : (
          <div className="flex flex-col md:flex-row gap-8">
            <aside className="w-full md:w-64 shrink-0">
              <nav className="flex flex-col gap-2">
                {([
                  { key: 'general', icon: Settings, label: 'Cài đặt chung' },
                  { key: 'members', icon: Users, label: 'Thành viên' },
                  { key: 'activity', icon: History, label: 'Nhật ký hoạt động' },
                ] as const).map((tab) => (
                  <button
                    key={tab.key}
                    onClick={() => setActiveTab(tab.key)}
                    className={`flex items-center gap-3 px-4 py-3 rounded-xl font-medium transition-all ${
                      activeTab === tab.key
                        ? 'bg-[#FF7575]/10 text-[#FF7575] border border-[#FF7575]/20'
                        : 'text-gray-400 hover:text-white hover:bg-white/5 border border-transparent'
                    }`}
                  >
                    <tab.icon size={20} />
                    {tab.label}
                  </button>
                ))}
              </nav>
            </aside>

            <main className="grow">
              {activeTab === 'general' && (
                <div className="space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-500">
                  <div className="bg-[#151B2B] border border-white/5 rounded-2xl p-6 md:p-8">
                    <h2 className="text-xl font-bold mb-6 flex items-center gap-2">
                      <Settings className="text-[#FF7575]" /> Thông tin cơ bản
                    </h2>
                    <form onSubmit={handleUpdateWorkspace}>
                      <div className="mb-6">
                        <label className="block text-sm font-medium text-gray-400 mb-2">Tên Workspace</label>
                        <input
                          type="text"
                          value={workspaceName}
                          onChange={(e) => setWorkspaceName(e.target.value)}
                          disabled={!isOwner || isSaving}
                          className="w-full md:w-1/2 bg-[#0A101D] border border-white/10 rounded-lg px-4 py-3 text-white focus:outline-none focus:border-[#FF7575] transition-colors disabled:opacity-60"
                        />
                        {!isOwner && (
                          <p className="text-xs text-gray-500 mt-2">Chỉ Owner mới có thể đổi tên workspace.</p>
                        )}
                      </div>
                      {isOwner && (
                        <button
                          type="submit"
                          disabled={isSaving || !workspaceName.trim()}
                          className="bg-white/10 hover:bg-white/20 disabled:opacity-50 text-white px-6 py-2.5 rounded-lg text-sm font-semibold flex items-center gap-2 transition-colors"
                        >
                          <Save size={18} /> {isSaving ? 'Đang lưu...' : 'Lưu thay đổi'}
                        </button>
                      )}
                    </form>
                  </div>

                  {isOwner && (
                    <div className="border border-red-500/20 bg-red-500/5 rounded-2xl p-6 md:p-8 relative overflow-hidden">
                      <div className="absolute top-0 left-0 w-1 h-full bg-red-500" />
                      <h2 className="text-xl font-bold text-red-500 mb-2 flex items-center gap-2">
                        <AlertTriangle /> Vùng nguy hiểm
                      </h2>
                      <p className="text-gray-400 text-sm mb-6 max-w-2xl">
                        Xóa workspace sẽ ẩn toàn bộ dữ liệu liên quan. Hành động này chỉ Owner mới thực hiện được.
                      </p>
                      <button
                        onClick={() => setIsDeleteModalOpen(true)}
                        className="bg-red-500/10 hover:bg-red-500 text-red-500 hover:text-white border border-red-500/20 px-6 py-2.5 rounded-lg text-sm font-bold flex items-center gap-2 transition-all"
                      >
                        <Trash2 size={18} /> Xóa Workspace này
                      </button>
                    </div>
                  )}
                </div>
              )}

              {activeTab === 'members' && (
                <div className="space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-500">
                  {isOwner && invitations.length > 0 && (
                    <div className="bg-[#151B2B] border border-yellow-500/20 rounded-2xl p-6">
                      <h3 className="text-lg font-bold mb-4 flex items-center gap-2 text-yellow-400">
                        <Clock size={18} /> Lời mời chờ duyệt ({invitations.length})
                      </h3>
                      <div className="space-y-3">
                        {invitations.map((inv) => (
                          <div key={inv.invitationId} className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 p-4 bg-[#0A101D] rounded-xl border border-white/5">
                            <div>
                              <p className="font-semibold">{inv.invitedEmail}</p>
                              <p className="text-xs text-gray-500">
                                Mời bởi {inv.invitedByName} · {formatWorkspaceDateTime(inv.createdAt)}
                              </p>
                            </div>
                            <div className="flex gap-2">
                              <button
                                onClick={() => handleApproveInvitation(inv.invitationId)}
                                className="px-4 py-2 rounded-lg bg-emerald-500/10 text-emerald-400 hover:bg-emerald-500/20 text-sm font-bold"
                              >
                                Duyệt
                              </button>
                              <button
                                onClick={() => handleRejectInvitation(inv.invitationId)}
                                className="px-4 py-2 rounded-lg bg-red-500/10 text-red-400 hover:bg-red-500/20 text-sm font-bold"
                              >
                                Từ chối
                              </button>
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}

                  <div className="bg-[#151B2B] border border-white/5 rounded-2xl p-6 md:p-8">
                    <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-8">
                      <div>
                        <h2 className="text-xl font-bold mb-1 flex items-center gap-2">
                          <Shield className="text-[#FF7575]" /> Danh sách thành viên
                        </h2>
                        <p className="text-sm text-gray-400">{members.length} thành viên trong workspace.</p>
                      </div>
                      <button
                        onClick={() => setIsInviteModalOpen(true)}
                        className="bg-[#FF7575] hover:bg-[#ff6262] text-white px-5 py-2.5 rounded-lg text-sm font-semibold flex items-center gap-2 transition-colors shadow-lg shadow-[#FF7575]/20"
                      >
                        <UserPlus size={18} /> Mời thành viên
                      </button>
                    </div>

                    <div className="overflow-x-auto">
                      <table className="w-full text-left border-collapse">
                        <thead>
                          <tr className="border-b border-white/10 text-gray-400 text-xs uppercase tracking-wider">
                            <th className="pb-4 font-semibold">Thành viên</th>
                            <th className="pb-4 font-semibold">Vai trò</th>
                            {isOwner && <th className="pb-4 font-semibold text-right">Hành động</th>}
                          </tr>
                        </thead>
                        <tbody>
                          {members.map((member) => (
                            <tr key={member.userId} className="border-b border-white/5 hover:bg-white/5 transition-colors">
                              <td className="py-4">
                                <div className="flex items-center gap-3">
                                  <img
                                    src={getMemberAvatar(member.fullName, member.avatarUrl)}
                                    alt={member.fullName}
                                    className="w-10 h-10 rounded-full border border-white/10 object-cover"
                                  />
                                  <div>
                                    <div className="font-semibold text-white">{member.fullName}</div>
                                    <div className="text-xs text-gray-400">{member.email}</div>
                                  </div>
                                </div>
                              </td>
                              <td className="py-4">
                                {isOwner && member.roleName !== 'Owner' ? (
                                  <select
                                    value={member.roleName}
                                    onChange={(e) => handleRoleChange(member, e.target.value)}
                                    className="bg-[#0A101D] border border-white/10 text-sm rounded-lg px-3 py-1.5 focus:outline-none focus:border-[#FF7575]"
                                  >
                                    <option value="Editor">Editor</option>
                                    <option value="Viewer">Viewer</option>
                                  </select>
                                ) : (
                                  <span className={`px-3 py-1 rounded-full text-xs font-bold border ${getRoleBadgeClass(member.roleName)}`}>
                                    {getRoleLabel(member.roleName)}
                                  </span>
                                )}
                              </td>
                              {isOwner && (
                                <td className="py-4 text-right">
                                  <button
                                    onClick={() => handleRemoveMember(member)}
                                    disabled={member.roleName === 'Owner'}
                                    className="text-gray-500 hover:text-red-500 p-2 transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
                                  >
                                    <Trash2 size={20} />
                                  </button>
                                </td>
                              )}
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  </div>
                </div>
              )}

              {activeTab === 'activity' && (
                <div className="bg-[#151B2B] border border-white/5 rounded-2xl p-6 md:p-8 animate-in fade-in slide-in-from-bottom-4 duration-500">
                  <h2 className="text-xl font-bold mb-6 flex items-center gap-2">
                    <History className="text-[#FF7575]" /> Nhật ký hoạt động
                  </h2>
                  {activityLogs.length === 0 ? (
                    <p className="text-gray-400 text-sm">Chưa có hoạt động nào được ghi nhận.</p>
                  ) : (
                    <div className="space-y-4">
                      {activityLogs.map((log) => (
                        <div key={log.logId} className="flex gap-4 p-4 bg-[#0A101D] rounded-xl border border-white/5">
                          <img
                            src={getMemberAvatar(log.userFullName, log.userAvatarUrl)}
                            alt={log.userFullName}
                            className="w-10 h-10 rounded-full border border-white/10 object-cover shrink-0"
                          />
                          <div className="flex-1 min-w-0">
                            <div className="flex flex-wrap items-center gap-2 mb-1">
                              <span className="font-semibold text-white">{log.userFullName}</span>
                              <span className="text-xs px-2 py-0.5 rounded-full bg-white/5 text-gray-400 border border-white/10">
                                {getActivityLabel(log.actionType)}
                              </span>
                              <span className="text-xs text-gray-500">{formatWorkspaceDateTime(log.createdAt)}</span>
                            </div>
                            <p className="text-sm text-gray-400">{log.description || '—'}</p>
                          </div>
                        </div>
                      ))}
                    </div>
                  )}
                </div>
              )}
            </main>
          </div>
        )}
      </div>

      {isInviteModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
          <div className="bg-[#0A101D] border border-white/10 rounded-2xl w-full max-w-md shadow-2xl p-6 relative">
            <button onClick={() => setIsInviteModalOpen(false)} className="absolute top-4 right-4 text-gray-400 hover:text-white">
              <X size={24} />
            </button>
            <h2 className="text-2xl font-bold mb-2">Mời thành viên mới</h2>
            <p className="text-[#9BA1B0] text-sm mb-6">
              Nhập email người dùng đã có tài khoản MCFH. Owner sẽ duyệt lời mời trước khi họ tham gia (vai trò Viewer).
            </p>
            <form onSubmit={handleInvite}>
              <div className="mb-6">
                <label className="block text-sm font-bold text-gray-300 mb-2">Email</label>
                <input
                  type="email"
                  required
                  value={inviteEmail}
                  onChange={(e) => setInviteEmail(e.target.value)}
                  placeholder="name@company.com"
                  className="w-full px-4 py-3 bg-[#151B2B] border border-white/10 rounded-xl focus:outline-none focus:border-[#FF7575] text-white"
                />
              </div>
              <div className="flex gap-4">
                <button type="button" onClick={() => setIsInviteModalOpen(false)} className="flex-1 py-3 rounded-lg font-bold text-gray-400 hover:bg-white/5">
                  Hủy
                </button>
                <button type="submit" disabled={isSaving} className="flex-1 bg-[#FF7575] hover:bg-[#ff6262] disabled:opacity-50 text-white py-3 rounded-lg font-bold">
                  {isSaving ? 'Đang gửi...' : 'Gửi lời mời'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

      <ConfirmModal
        isOpen={isDeleteModalOpen}
        onClose={() => setIsDeleteModalOpen(false)}
        onConfirm={handleDeleteWorkspace}
        title="Xóa Workspace?"
        message={`Bạn có chắc muốn xóa "${workspace?.name}"? Toàn bộ dữ liệu liên quan sẽ bị ẩn khỏi hệ thống.`}
        confirmText={isSaving ? 'Đang xóa...' : 'Xóa vĩnh viễn'}
        cancelText="Hủy"
        type="danger"
      />
    </div>
  );
};

export default WorkspaceSettings;
