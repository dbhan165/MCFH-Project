import { useCallback, useEffect, useState } from 'react';
import { ChevronLeft, ChevronRight, Loader2 } from 'lucide-react';
import AdminLayout from '../../components/admin/AdminLayout';
import { adminApi, type AdminUser } from '../../api/portalApi';
import { extractApiError, getAvatarFallback } from '../../utils/authStorage';

const roleStyles: Record<string, string> = {
  Admin: 'bg-slate-800 text-white',
  Client: 'bg-blue-50 text-blue-600',
  Reporter: 'bg-purple-50 text-purple-600',
};

const UserManagement = () => {
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [total, setTotal] = useState(0);
  const [page, setPage] = useState(1);
  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');
  const pageSize = 10;

  const loadUsers = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage('');
    try {
      const data = await adminApi.getUsers({
        search: search || undefined,
        role: roleFilter || undefined,
        page,
        pageSize,
      });
      setUsers(data.items);
      setTotal(data.total);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải danh sách user.'));
    } finally {
      setIsLoading(false);
    }
  }, [search, roleFilter, page]);

  useEffect(() => {
    loadUsers();
  }, [loadUsers]);

  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  const handleRoleChange = async (userId: number, systemRole: string) => {
    try {
      await adminApi.updateUser(userId, { systemRole });
      await loadUsers();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể cập nhật role.'));
    }
  };

  const handleBanToggle = async (user: AdminUser) => {
    try {
      await adminApi.updateUser(user.userId, { isBanned: !user.isBanned });
      await loadUsers();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể cập nhật trạng thái.'));
    }
  };

  return (
    <AdminLayout searchPlaceholder="Search by email or name...">
      <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 mb-8">
        <div>
          <h2 className="text-2xl lg:text-3xl font-bold tracking-tight">User Management</h2>
          <p className="text-[#6b7280] text-sm mt-1">
            Quản lý tài khoản hệ thống — {total} users
          </p>
        </div>

        <div className="flex items-center gap-3 shrink-0 flex-wrap">
          <input
            type="text"
            value={search}
            onChange={(e) => {
              setSearch(e.target.value);
              setPage(1);
            }}
            placeholder="Tìm email, tên..."
            className="px-3 py-2 border border-gray-200 rounded-lg text-sm"
          />
          <select
            value={roleFilter}
            onChange={(e) => {
              setRoleFilter(e.target.value);
              setPage(1);
            }}
            className="px-3 py-2 border border-gray-200 rounded-lg text-sm"
          >
            <option value="">Tất cả role</option>
            <option value="Admin">Admin</option>
            <option value="Reporter">Reporter</option>
            <option value="Client">Client</option>
          </select>
        </div>
      </div>

      {errorMessage && (
        <div className="mb-4 text-sm text-red-600 bg-red-50 border border-red-100 rounded-lg px-4 py-3">
          {errorMessage}
        </div>
      )}

      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden">
        {isLoading ? (
          <div className="flex items-center justify-center py-20 text-gray-500 gap-2">
            <Loader2 className="w-6 h-6 animate-spin" />
            Đang tải...
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-100 bg-gray-50/50">
                  <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">User</th>
                  <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Email</th>
                  <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Role</th>
                  <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Status</th>
                  <th className="text-right px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {users.map((user) => (
                  <tr key={user.userId} className="hover:bg-gray-50/50">
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-3">
                        <img
                          src={user.avatarUrl || getAvatarFallback(user.fullName)}
                          alt=""
                          className="w-9 h-9 rounded-full object-cover bg-gray-100"
                        />
                        <span className="font-medium text-[#111827]">{user.fullName}</span>
                      </div>
                    </td>
                    <td className="px-6 py-4 text-[#6b7280]">{user.email}</td>
                    <td className="px-6 py-4">
                      <select
                        value={user.systemRole}
                        onChange={(e) => handleRoleChange(user.userId, e.target.value)}
                        className={`text-xs font-semibold px-2 py-1 rounded border-0 ${roleStyles[user.systemRole] ?? 'bg-gray-100'}`}
                      >
                        <option value="Admin">Admin</option>
                        <option value="Reporter">Reporter</option>
                        <option value="Client">Client</option>
                      </select>
                    </td>
                    <td className="px-6 py-4">
                      <span className="inline-flex items-center gap-1.5 text-xs font-medium">
                        <span className={`w-2 h-2 rounded-full ${user.isBanned ? 'bg-gray-400' : 'bg-green-500'}`} />
                        {user.isBanned ? 'Banned' : 'Active'}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-right">
                      <button
                        type="button"
                        onClick={() => handleBanToggle(user)}
                        className="text-xs font-semibold text-red-500 hover:text-red-700"
                      >
                        {user.isBanned ? 'Unban' : 'Ban'}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <div className="flex items-center justify-between px-6 py-4 border-t border-gray-100">
          <p className="text-xs text-[#6b7280]">
            Trang {page} / {totalPages}
          </p>
          <div className="flex items-center gap-2">
            <button
              type="button"
              disabled={page <= 1}
              onClick={() => setPage((p) => p - 1)}
              className="p-2 rounded-lg border border-gray-200 disabled:opacity-40"
            >
              <ChevronLeft className="w-4 h-4" />
            </button>
            <button
              type="button"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => p + 1)}
              className="p-2 rounded-lg border border-gray-200 disabled:opacity-40"
            >
              <ChevronRight className="w-4 h-4" />
            </button>
          </div>
        </div>
      </div>
    </AdminLayout>
  );
};

export default UserManagement;
