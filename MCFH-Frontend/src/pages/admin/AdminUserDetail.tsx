import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import {
  ArrowLeft,
  Ban,
  Building2,
  CreditCard,
  FileText,
  Loader2,
  Mail,
  Phone,
  Shield,
  UserCheck,
  Bell,
  FolderKanban,
} from 'lucide-react';
import AdminLayout from '../../components/admin/AdminLayout';
import { adminApi, type AdminUserDetail } from '../../api/portalApi';
import { extractApiError, getAvatarFallback } from '../../utils/authStorage';

const roleStyles: Record<string, string> = {
  Admin: 'bg-slate-800 text-white',
  Client: 'bg-blue-50 text-blue-600',
  Reporter: 'bg-purple-50 text-purple-600',
};

function formatDate(value: string | null) {
  if (!value) return '—';
  return new Date(value).toLocaleDateString('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

function formatCurrency(value: number) {
  return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: 'VND' }).format(value);
}

const AdminUserDetailPage = () => {
  const { userId } = useParams();
  const navigate = useNavigate();
  const uid = Number(userId);

  const [user, setUser] = useState<AdminUserDetail | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');
  const [isUpdating, setIsUpdating] = useState(false);

  const loadUser = useCallback(async () => {
    if (!uid || Number.isNaN(uid)) return;

    setIsLoading(true);
    setErrorMessage('');
    try {
      setUser(await adminApi.getUserDetail(uid));
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải chi tiết user.'));
    } finally {
      setIsLoading(false);
    }
  }, [uid]);

  useEffect(() => {
    loadUser();
  }, [loadUser]);

  const handleBanToggle = async () => {
    if (!user) return;
    setIsUpdating(true);
    try {
      await adminApi.updateUser(user.userId, { isBanned: !user.isBanned });
      await loadUser();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể cập nhật trạng thái.'));
    } finally {
      setIsUpdating(false);
    }
  };

  const handleRoleChange = async (systemRole: string) => {
    if (!user) return;
    setIsUpdating(true);
    try {
      await adminApi.updateUser(user.userId, { systemRole });
      await loadUser();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể cập nhật role.'));
    } finally {
      setIsUpdating(false);
    }
  };

  if (isLoading) {
    return (
      <AdminLayout>
        <div className="flex items-center justify-center py-32 text-gray-500 gap-2">
          <Loader2 className="w-6 h-6 animate-spin" />
          Đang tải chi tiết user...
        </div>
      </AdminLayout>
    );
  }

  if (errorMessage && !user) {
    return (
      <AdminLayout>
        <div className="max-w-lg mx-auto text-center py-20">
          <p className="text-red-600 mb-4">{errorMessage}</p>
          <Link to="/admin/users" className="text-sm font-semibold text-[#ef4444] hover:underline">
            Quay lại User Management
          </Link>
        </div>
      </AdminLayout>
    );
  }

  if (!user) return null;

  const statCards = [
    { label: 'Workspace sở hữu', value: user.stats.ownedWorkspaces, icon: Building2 },
    { label: 'Workspace tham gia', value: user.stats.memberWorkspaces, icon: Shield },
    { label: 'Dự án', value: user.stats.totalProjects, icon: FolderKanban },
    { label: 'Thông báo chưa đọc', value: user.stats.unreadNotifications, icon: Bell },
  ];

  return (
    <AdminLayout searchPlaceholder="Search by email or name...">
      <Link
        to="/admin/users"
        className="inline-flex items-center gap-2 text-sm text-[#6b7280] hover:text-[#111827] mb-6"
      >
        <ArrowLeft className="w-4 h-4" />
        Quay lại User Management
      </Link>

      {errorMessage && (
        <div className="mb-4 text-sm text-red-600 bg-red-50 border border-red-100 rounded-lg px-4 py-3">
          {errorMessage}
        </div>
      )}

      <div className="bg-white border border-gray-200 rounded-xl p-6 sm:p-8 mb-6">
        <div className="flex flex-col lg:flex-row lg:items-start lg:justify-between gap-6">
          <div className="flex items-start gap-4">
            <img
              src={user.avatarUrl || getAvatarFallback(user.fullName)}
              alt=""
              className="w-16 h-16 rounded-full object-cover bg-gray-100 shrink-0"
            />
            <div>
              <h2 className="text-2xl font-bold tracking-tight">{user.fullName}</h2>
              <p className="text-[#6b7280] text-sm mt-1 flex items-center gap-2">
                <Mail className="w-4 h-4" />
                {user.email}
              </p>
              {user.phone && (
                <p className="text-[#6b7280] text-sm mt-1 flex items-center gap-2">
                  <Phone className="w-4 h-4" />
                  {user.phone}
                </p>
              )}
              <div className="flex flex-wrap items-center gap-2 mt-3">
                <span
                  className={`text-xs font-semibold px-2.5 py-1 rounded ${roleStyles[user.systemRole] ?? 'bg-gray-100'}`}
                >
                  {user.systemRole}
                </span>
                <span className="inline-flex items-center gap-1.5 text-xs font-medium text-[#6b7280]">
                  <span className={`w-2 h-2 rounded-full ${user.isBanned ? 'bg-gray-400' : 'bg-green-500'}`} />
                  {user.isBanned ? 'Banned' : 'Active'}
                </span>
                <span className="inline-flex items-center gap-1 text-xs text-[#6b7280]">
                  <UserCheck className="w-3.5 h-3.5" />
                  {user.isVerified ? 'Đã xác thực email' : 'Chưa xác thực'}
                </span>
              </div>
            </div>
          </div>

          <div className="flex flex-wrap items-center gap-3 shrink-0">
            <select
              value={user.systemRole}
              disabled={isUpdating}
              onChange={(e) => handleRoleChange(e.target.value)}
              className={`text-xs font-semibold px-3 py-2 rounded-lg border border-gray-200 ${roleStyles[user.systemRole] ?? 'bg-gray-100'}`}
            >
              <option value="Admin">Admin</option>
              <option value="Reporter">Reporter</option>
              <option value="Client">Client</option>
            </select>
            <button
              type="button"
              disabled={isUpdating}
              onClick={handleBanToggle}
              className="inline-flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-semibold border border-red-200 text-red-600 hover:bg-red-50 disabled:opacity-50"
            >
              {isUpdating ? <Loader2 className="w-4 h-4 animate-spin" /> : <Ban className="w-4 h-4" />}
              {user.isBanned ? 'Unban user' : 'Ban user'}
            </button>
          </div>
        </div>

        <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mt-8 pt-8 border-t border-gray-100">
          {[
            { label: 'Auth provider', value: user.authProvider },
            { label: 'Ngày tạo', value: formatDate(user.createdAt) },
            { label: 'Xác thực lúc', value: formatDate(user.verifiedAt) },
            { label: 'Banned lúc', value: formatDate(user.bannedAt) },
          ].map((item) => (
            <div key={item.label} className="rounded-xl bg-gray-50 px-4 py-3">
              <p className="text-[10px] uppercase tracking-wide text-[#6b7280] font-semibold">{item.label}</p>
              <p className="text-sm font-medium text-[#111827] mt-1">{item.value}</p>
            </div>
          ))}
        </div>
      </div>

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        {statCards.map(({ label, value, icon: Icon }) => (
          <div key={label} className="bg-white border border-gray-200 rounded-xl p-4 flex items-center gap-3">
            <div className="w-10 h-10 rounded-lg bg-red-50 text-[#ef4444] flex items-center justify-center shrink-0">
              <Icon className="w-5 h-5" />
            </div>
            <div>
              <p className="text-xl font-bold tabular-nums">{value}</p>
              <p className="text-xs text-[#6b7280]">{label}</p>
            </div>
          </div>
        ))}
      </div>

      <div className="grid grid-cols-1 xl:grid-cols-2 gap-6 mb-6">
        <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
          <div className="px-6 py-4 border-b border-gray-100 flex items-center gap-2">
            <Building2 className="w-5 h-5 text-[#6b7280]" />
            <h3 className="font-bold">Workspaces ({user.workspaces.length})</h3>
          </div>
          {user.workspaces.length === 0 ? (
            <p className="px-6 py-10 text-sm text-[#6b7280] text-center">User chưa tham gia workspace nào.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-gray-50/80 text-left">
                    <th className="px-6 py-3 text-xs font-semibold text-[#6b7280] uppercase">Tên</th>
                    <th className="px-6 py-3 text-xs font-semibold text-[#6b7280] uppercase">Vai trò</th>
                    <th className="px-6 py-3 text-xs font-semibold text-[#6b7280] uppercase">Dự án</th>
                    <th className="px-6 py-3 text-xs font-semibold text-[#6b7280] uppercase">Gói cước</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {user.workspaces.map((ws) => (
                    <tr key={ws.workspaceId} className="hover:bg-gray-50/50">
                      <td className="px-6 py-3 font-medium">{ws.name}</td>
                      <td className="px-6 py-3 text-[#6b7280]">{ws.membershipRole}</td>
                      <td className="px-6 py-3 tabular-nums">{ws.projectCount}</td>
                      <td className="px-6 py-3 text-[#6b7280]">
                        {ws.subscriptionPlan ? (
                          <>
                            {ws.subscriptionPlan}
                            {ws.subscriptionStatus ? ` · ${ws.subscriptionStatus}` : ''}
                          </>
                        ) : (
                          '—'
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>

        <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
          <div className="px-6 py-4 border-b border-gray-100 flex items-center gap-2">
            <FileText className="w-5 h-5 text-[#6b7280]" />
            <h3 className="font-bold">
              Bespoke ({user.stats.bespokeAsClient} client · {user.stats.bespokeAsReporter} reporter)
            </h3>
          </div>
          {user.bespokeRequests.length === 0 ? (
            <p className="px-6 py-10 text-sm text-[#6b7280] text-center">Chưa có đơn bespoke liên quan.</p>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="bg-gray-50/80 text-left">
                    <th className="px-6 py-3 text-xs font-semibold text-[#6b7280] uppercase">Tiêu đề</th>
                    <th className="px-6 py-3 text-xs font-semibold text-[#6b7280] uppercase">Vai trò</th>
                    <th className="px-6 py-3 text-xs font-semibold text-[#6b7280] uppercase">Trạng thái</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-100">
                  {user.bespokeRequests.map((req) => (
                    <tr key={`${req.requestId}-${req.involvement}`} className="hover:bg-gray-50/50">
                      <td className="px-6 py-3 font-medium line-clamp-1">{req.title}</td>
                      <td className="px-6 py-3 text-[#6b7280]">{req.involvement}</td>
                      <td className="px-6 py-3">
                        <span className="text-xs font-semibold px-2 py-1 rounded bg-gray-100 capitalize">
                          {req.status}
                        </span>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
        <div className="px-6 py-4 border-b border-gray-100 flex items-center gap-2">
          <CreditCard className="w-5 h-5 text-[#6b7280]" />
          <h3 className="font-bold">Thanh toán gần đây</h3>
        </div>
        {user.recentPayments.length === 0 ? (
          <p className="px-6 py-10 text-sm text-[#6b7280] text-center">Chưa có giao dịch thanh toán.</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-gray-50/80 text-left">
                  <th className="px-6 py-3 text-xs font-semibold text-[#6b7280] uppercase">Mã</th>
                  <th className="px-6 py-3 text-xs font-semibold text-[#6b7280] uppercase">Số tiền</th>
                  <th className="px-6 py-3 text-xs font-semibold text-[#6b7280] uppercase">Loại</th>
                  <th className="px-6 py-3 text-xs font-semibold text-[#6b7280] uppercase">Trạng thái</th>
                  <th className="px-6 py-3 text-xs font-semibold text-[#6b7280] uppercase">Ngày</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {user.recentPayments.map((payment) => (
                  <tr key={payment.paymentId} className="hover:bg-gray-50/50">
                    <td className="px-6 py-3 tabular-nums">#{payment.paymentId}</td>
                    <td className="px-6 py-3 font-medium">{formatCurrency(payment.amount)}</td>
                    <td className="px-6 py-3 text-[#6b7280]">
                      {payment.planName ?? payment.type ?? '—'}
                    </td>
                    <td className="px-6 py-3 capitalize">{payment.status ?? '—'}</td>
                    <td className="px-6 py-3 text-[#6b7280]">{formatDate(payment.createdAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      <div className="mt-6 flex justify-end">
        <button
          type="button"
          onClick={() => navigate('/admin/users')}
          className="px-4 py-2 rounded-lg text-sm font-semibold border border-gray-200 hover:bg-gray-50"
        >
          Đóng
        </button>
      </div>
    </AdminLayout>
  );
};

export default AdminUserDetailPage;
