import { useCallback, useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import {
  Search,
  UserPlus,
  Shield,
  Mail,
  Trash2,
  CheckCircle2,
  Clock,
  Loader2,
  AlertCircle,
  Settings,
} from 'lucide-react';
import { workspaceApi } from '../api/workspaceApi';
import type { Workspace, WorkspaceInvitation, WorkspaceMember } from '../types/workspace';
import { extractApiError } from '../utils/authStorage';
import {
  formatWorkspaceDateTime,
  getMemberAvatar,
  getRoleBadgeClass,
  getRoleLabel,
  isWorkspaceOwner,
} from '../utils/workspaceHelpers';
import { useAppModal } from '../contexts/AppModalContext';

const Members = () => {
  const { workspaceId } = useParams();
  const id = Number(workspaceId);
  const { confirm, alert } = useAppModal();

  const [workspace, setWorkspace] = useState<Workspace | null>(null);
  const [members, setMembers] = useState<WorkspaceMember[]>([]);
  const [invitations, setInvitations] = useState<WorkspaceInvitation[]>([]);
  const [searchQuery, setSearchQuery] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState('');

  const [isInviteModalOpen, setIsInviteModalOpen] = useState(false);
  const [inviteEmail, setInviteEmail] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);

  const isOwner = isWorkspaceOwner(workspace?.myRole);

  const loadData = useCallback(async () => {
    if (!id || Number.isNaN(id)) return;

    setIsLoading(true);
    setErrorMessage('');
    try {
      const [ws, memberList] = await Promise.all([
        workspaceApi.getById(id),
        workspaceApi.getMembers(id),
      ]);
      setWorkspace(ws);
      setMembers(memberList);

      if (isWorkspaceOwner(ws.myRole)) {
        const pending = await workspaceApi.getPendingInvitations(id);
        setInvitations(pending);
      }
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải danh sách thành viên.'));
    } finally {
      setIsLoading(false);
    }
  }, [id]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  const filteredMembers = members.filter((member) => {
    const q = searchQuery.trim().toLowerCase();
    if (!q) return true;
    return member.fullName.toLowerCase().includes(q) || member.email.toLowerCase().includes(q);
  });

  const handleInvite = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!workspace) return;

    setIsSubmitting(true);
    try {
      const message = await workspaceApi.inviteMember(workspace.workspaceId, inviteEmail.trim());
      setSuccessMessage(message);
      setIsInviteModalOpen(false);
      setInviteEmail('');
      await loadData();
      setTimeout(() => setSuccessMessage(''), 3000);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể gửi lời mời.'));
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleRemove = async (member: WorkspaceMember) => {
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
      setSuccessMessage(`Đã xóa ${member.fullName}.`);
      await loadData();
      setTimeout(() => setSuccessMessage(''), 3000);
    } catch (error) {
      await alert({
        title: 'Không thể xóa',
        message: extractApiError(error, 'Không thể xóa thành viên.'),
        type: 'error',
      });
    }
  };

  const handleRejectSent = async (invitationId: number) => {
    if (!workspace || !isOwner) return;
    const confirmed = await confirm({
      title: 'Hủy lời mời',
      message: 'Bạn có chắc muốn hủy lời mời này?',
      confirmText: 'Hủy lời mời',
      cancelText: 'Đóng',
      type: 'warning',
    });
    if (!confirmed) return;
    try {
      await workspaceApi.rejectInvitation(workspace.workspaceId, invitationId);
      setSuccessMessage('Đã hủy lời mời.');
      await loadData();
      setTimeout(() => setSuccessMessage(''), 3000);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể hủy lời mời.'));
    }
  };

  return (
    <div className="flex flex-col h-full text-white bg-[#050A15]">
      <header className="h-20 bg-[#0A101D]/80 backdrop-blur-md border-b border-white/5 flex items-center justify-between px-8 sticky top-0 z-20 shrink-0">
        <div className="text-gray-300 font-medium tracking-wide">Quản lý Thành viên</div>
        {workspace && isOwner && (
          <Link
            to={`/workspace/${workspace.workspaceId}/settings`}
            className="text-sm text-gray-400 hover:text-white flex items-center gap-2"
          >
            <Settings size={16} /> Cài đặt workspace
          </Link>
        )}
      </header>

      <div className="flex-1 p-8 md:p-10 relative z-10 overflow-y-auto">
        <div className="max-w-6xl mx-auto">
          <div className="flex flex-col md:flex-row md:items-end justify-between gap-6 mb-8">
            <div>
              <h1 className="text-4xl font-bold tracking-tight mb-2">Thành viên Workspace</h1>
              <p className="text-gray-400">
                {workspace ? `Quản lý quyền truy cập vào "${workspace.name}"` : 'Đang tải...'}
              </p>
            </div>

            <div className="flex items-center gap-4">
              <div className="relative hidden sm:block">
                <Search className="absolute left-4 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-500" />
                <input
                  type="text"
                  value={searchQuery}
                  onChange={(e) => setSearchQuery(e.target.value)}
                  placeholder="Tìm email hoặc tên..."
                  className="w-64 bg-[#151B2B] border border-white/10 rounded-lg pl-11 pr-4 py-2.5 text-sm text-white placeholder-gray-500 focus:outline-none focus:border-[#FF7575]/50"
                />
              </div>
              <button
                onClick={() => setIsInviteModalOpen(true)}
                className="bg-[#FF7575] hover:bg-[#ff6262] text-white px-5 py-2.5 rounded-lg text-sm font-semibold flex items-center gap-2 shadow-[0_4px_14px_0_rgba(255,117,117,0.39)]"
              >
                <UserPlus className="w-4 h-4" />
                Mời thành viên
              </button>
            </div>
          </div>

          {(errorMessage || successMessage) && (
            <div className="mb-6 space-y-3">
              {errorMessage && (
                <div className="bg-red-500/10 border border-red-500/20 text-red-300 p-4 rounded-xl flex items-center gap-3 text-sm">
                  <AlertCircle className="w-5 h-5" />
                  {errorMessage}
                </div>
              )}
              {successMessage && (
                <div className="bg-emerald-500/10 border border-emerald-500/20 text-emerald-300 p-4 rounded-xl flex items-center gap-3 text-sm">
                  <CheckCircle2 className="w-5 h-5" />
                  {successMessage}
                </div>
              )}
            </div>
          )}

          {isLoading ? (
            <div className="flex flex-col items-center justify-center py-24 text-gray-400 gap-4">
              <Loader2 className="w-10 h-10 animate-spin text-[#FF7575]" />
              <p>Đang tải thành viên...</p>
            </div>
          ) : (
            <>
              {isOwner && invitations.length > 0 && (
                <div className="mb-6 bg-[#151B2B] border border-yellow-500/20 rounded-2xl p-6">
                  <h3 className="font-bold mb-4 text-yellow-400 flex items-center gap-2">
                    <Clock size={18} /> Lời mời đang chờ phản hồi
                  </h3>
                  <p className="text-xs text-gray-500 mb-4">
                    Người được mời sẽ nhận email và thông báo trên hệ thống để chấp nhận hoặc từ chối.
                  </p>
                  <div className="space-y-3">
                    {invitations.map((inv) => (
                      <div key={inv.invitationId} className="flex items-center justify-between gap-4 p-4 bg-[#0A101D] rounded-xl border border-white/5">
                        <div>
                          <p className="font-semibold">{inv.invitedEmail}</p>
                          <p className="text-xs text-gray-500">
                            Mời bởi {inv.invitedByName} · {formatWorkspaceDateTime(inv.createdAt)}
                          </p>
                        </div>
                        <button
                          onClick={() => handleRejectSent(inv.invitationId)}
                          className="px-4 py-2 rounded-lg bg-white/5 text-gray-300 text-sm font-bold hover:bg-red-500/10 hover:text-red-300"
                        >
                          Hủy lời mời
                        </button>
                      </div>
                    ))}
                  </div>
                </div>
              )}

              <div className="bg-[#0A101D] border border-white/5 rounded-2xl overflow-hidden">
                <table className="w-full text-left border-collapse">
                  <thead>
                    <tr className="border-b border-white/5 bg-white/[0.02]">
                      <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase">Người dùng</th>
                      <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase">Vai trò</th>
                      <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase">Trạng thái</th>
                      {isOwner && <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase text-right">Thao tác</th>}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-white/5">
                    {filteredMembers.map((member) => (
                      <tr key={member.userId} className="hover:bg-white/[0.02]">
                        <td className="px-6 py-4">
                          <div className="flex items-center gap-4">
                            <img
                              src={getMemberAvatar(member.fullName, member.avatarUrl)}
                              alt={member.fullName}
                              className="w-10 h-10 rounded-full border border-white/10 object-cover"
                            />
                            <div>
                              <p className="font-bold">{member.fullName}</p>
                              <p className="text-sm text-gray-400">{member.email}</p>
                            </div>
                          </div>
                        </td>
                        <td className="px-6 py-4">
                          <div className="flex items-center gap-2 text-sm">
                            {member.roleName === 'Owner' && <Shield className="w-4 h-4 text-[#FF7575]" />}
                            <span className={`px-2.5 py-1 rounded-full text-xs font-bold border ${getRoleBadgeClass(member.roleName)}`}>
                              {getRoleLabel(member.roleName)}
                            </span>
                          </div>
                        </td>
                        <td className="px-6 py-4">
                          <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-bold bg-emerald-500/10 text-emerald-500 border border-emerald-500/20">
                            <CheckCircle2 className="w-3.5 h-3.5" /> Đã tham gia
                          </span>
                        </td>
                        {isOwner && (
                          <td className="px-6 py-4 text-right">
                            {member.roleName !== 'Owner' && (
                              <button
                                onClick={() => handleRemove(member)}
                                className="p-2 text-gray-400 hover:text-[#FF7575] hover:bg-[#FF7575]/10 rounded-lg"
                              >
                                <Trash2 className="w-4 h-4" />
                              </button>
                            )}
                          </td>
                        )}
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </>
          )}
        </div>
      </div>

      {isInviteModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
          <div className="bg-[#0A101D] border border-white/10 rounded-2xl w-full max-w-md shadow-2xl overflow-hidden">
            <div className="p-6 border-b border-white/5 flex justify-between items-center">
              <h3 className="text-xl font-bold">Mời thành viên mới</h3>
              <button onClick={() => setIsInviteModalOpen(false)} className="text-gray-400 hover:text-white">✕</button>
            </div>
            <form onSubmit={handleInvite} className="p-6 space-y-5">
              <div>
                <label className="text-xs font-bold text-gray-400 uppercase">Email thành viên</label>
                <div className="relative mt-2">
                  <Mail className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-500" />
                  <input
                    type="email"
                    value={inviteEmail}
                    onChange={(e) => setInviteEmail(e.target.value)}
                    placeholder="name@company.com"
                    required
                    className="w-full bg-[#151B2B] border border-white/10 rounded-lg pl-10 pr-4 py-3 text-white focus:outline-none focus:border-[#FF7575]"
                  />
                </div>
                <p className="text-xs text-gray-500 mt-2">
                  Người được mời nhận email và thông báo trên MCFH để chấp nhận hoặc từ chối.
                  Nếu chưa có tài khoản, họ cần đăng ký bằng đúng email này.
                </p>
              </div>
              <div className="flex gap-3">
                <button type="button" onClick={() => setIsInviteModalOpen(false)} className="flex-1 px-4 py-3 rounded-lg font-bold text-gray-300 bg-white/5">
                  Hủy
                </button>
                <button type="submit" disabled={isSubmitting} className="flex-1 px-4 py-3 rounded-lg font-bold text-white bg-[#FF7575] disabled:opacity-50">
                  {isSubmitting ? 'Đang gửi...' : 'Gửi lời mời'}
                </button>
              </div>
            </form>
          </div>
        </div>
      )}
    </div>
  );
};

export default Members;
