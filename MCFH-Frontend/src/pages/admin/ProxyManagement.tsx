import { useCallback, useEffect, useMemo, useState } from 'react';
import {
  Plus,
  Globe,
  Pencil,
  Trash2,
  Loader2,
  Server,
  Power,
  Search,
  Activity,
  X,
} from 'lucide-react';
import AdminLayout from '../../components/admin/AdminLayout';
import ConfirmModal from '../../components/common/ConfirmModal';
import { adminApi, type SystemProxy } from '../../api/portalApi';
import { extractApiError } from '../../utils/authStorage';

interface ProxyForm {
  ipAddress: string;
  port: string;
  authUser: string;
  authPass: string;
  status: 'active' | 'disabled' | 'dead';
  enabled: boolean;
}

const emptyForm: ProxyForm = {
  ipAddress: '',
  port: '',
  authUser: '',
  authPass: '',
  status: 'active',
  enabled: true,
};

const statusBadge: Record<string, string> = {
  active: 'bg-green-50 text-green-700',
  disabled: 'bg-gray-100 text-gray-600',
  dead: 'bg-red-50 text-red-600',
};

const ProxyManagement = () => {
  const [proxies, setProxies] = useState<SystemProxy[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');
  const [searchTerm, setSearchTerm] = useState('');

  const [modalOpen, setModalOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<ProxyForm>(emptyForm);
  const [isSaving, setIsSaving] = useState(false);
  const [formError, setFormError] = useState('');

  const [deleteId, setDeleteId] = useState<number | null>(null);
  const [isDeleting, setIsDeleting] = useState(false);

  const loadProxies = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage('');
    try {
      const data = await adminApi.getProxies();
      setProxies(data);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải danh sách proxy.'));
      setProxies([]);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadProxies();
  }, [loadProxies]);

  const stats = useMemo(() => {
    const total = proxies.length;
    const active = proxies.filter((p) => p.enabled).length;
    const failing = proxies.filter((p) => p.failCount > 0 && p.status !== 'dead').length;
    const dead = proxies.filter((p) => p.status === 'dead').length;
    return { total, active, failing, dead };
  }, [proxies]);

  const filteredProxies = useMemo(() => {
    const q = searchTerm.trim().toLowerCase();
    if (!q) return proxies;
    return proxies.filter(
      (p) =>
        p.ipAddress.toLowerCase().includes(q) ||
        String(p.port).includes(q) ||
        (p.authUser ?? '').toLowerCase().includes(q) ||
        (p.status ?? '').toLowerCase().includes(q)
    );
  }, [proxies, searchTerm]);

  const openCreate = () => {
    setEditingId(null);
    setForm(emptyForm);
    setFormError('');
    setModalOpen(true);
  };

  const openEdit = (proxy: SystemProxy) => {
    setEditingId(proxy.proxyId);
    setForm({
      ipAddress: proxy.ipAddress,
      port: String(proxy.port),
      authUser: proxy.authUser ?? '',
      authPass: '',
      status: (proxy.status as 'active' | 'disabled' | 'dead') ?? 'active',
      enabled: proxy.enabled,
    });
    setFormError('');
    setModalOpen(true);
  };

  const closeModal = () => {
    if (isSaving) return;
    setModalOpen(false);
    setFormError('');
  };

  const handleSave = async () => {
    setFormError('');

    const ip = form.ipAddress.trim();
    const portStr = form.port.trim();
    if (!ip) {
      setFormError('Vui lòng nhập địa chỉ IP.');
      return;
    }
    const port = Number(portStr);
    if (!portStr || !Number.isInteger(port) || port < 1 || port > 65535) {
      setFormError('Port phải là số nguyên từ 1 đến 65535.');
      return;
    }

    const payload = {
      ipAddress: ip,
      port,
      authUser: form.authUser.trim() || undefined,
      authPass: form.authPass || undefined,
      status: form.enabled ? form.status : 'disabled',
      enabled: form.enabled,
    };

    setIsSaving(true);
    try {
      if (editingId != null) {
        await adminApi.updateProxy(editingId, payload);
      } else {
        await adminApi.createProxy(payload);
      }
      setModalOpen(false);
      await loadProxies();
    } catch (error) {
      setFormError(extractApiError(error, 'Không thể lưu proxy.'));
    } finally {
      setIsSaving(false);
    }
  };

  const handleToggle = async (proxy: SystemProxy) => {
    setErrorMessage('');
    try {
      await adminApi.updateProxy(proxy.proxyId, {
        ipAddress: proxy.ipAddress,
        port: proxy.port,
        authUser: proxy.authUser ?? undefined,
        status: proxy.enabled ? 'disabled' : 'active',
        enabled: !proxy.enabled,
      });
      await loadProxies();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể thay đổi trạng thái proxy.'));
    }
  };

  const handleDelete = async () => {
    if (deleteId == null) return;
    setIsDeleting(true);
    setErrorMessage('');
    try {
      await adminApi.deleteProxy(deleteId);
      setDeleteId(null);
      await loadProxies();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể xóa proxy.'));
    } finally {
      setIsDeleting(false);
    }
  };

  const proxyToDelete = proxies.find((p) => p.proxyId === deleteId);

  return (
    <AdminLayout>
      <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 mb-8">
        <div>
          <h2 className="text-2xl lg:text-3xl font-bold tracking-tight">
            Giám sát Hạ tầng Proxy
          </h2>
          <p className="text-[#6b7280] text-sm mt-1">
            Quản lý danh sách nút Proxy toàn cầu (SYSTEM_PROXIES).
          </p>
        </div>
        <button
          type="button"
          onClick={openCreate}
          className="flex items-center gap-2 px-5 py-2.5 bg-[#ef4444] hover:bg-red-600 text-white rounded-lg text-sm font-semibold transition-colors shadow-sm shrink-0"
        >
          <Plus className="w-4 h-4" />
          Thêm Proxy mới
        </button>
      </div>

      {errorMessage && (
        <div className="mb-4 text-sm text-red-600 bg-red-50 border border-red-100 rounded-lg px-4 py-3">
          {errorMessage}
        </div>
      )}

      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4 mb-8">
        <StatCard
          borderColor="border-l-blue-500"
          icon={<Server className="w-4 h-4" />}
          label="Tổng Proxy"
          value={stats.total}
          tag={
            <span className="flex items-center gap-1 text-xs font-semibold text-blue-600">
              Pool toàn hệ thống
            </span>
          }
        />
        <StatCard
          borderColor="border-l-green-500"
          icon={<Power className="w-4 h-4" />}
          label="Đang hoạt động"
          value={stats.active}
          tag={
            <span className="text-xs font-semibold text-green-600 bg-green-50 px-2 py-0.5 rounded">
              Khỏe mạnh
            </span>
          }
        />
        <StatCard
          borderColor="border-l-amber-500"
          icon={<Activity className="w-4 h-4" />}
          label="Đang lỗi"
          value={stats.failing}
          tag={
            <span className="text-xs font-semibold text-amber-600 bg-amber-50 px-2 py-0.5 rounded">
              Bị suy giảm
            </span>
          }
        />
        <StatCard
          borderColor="border-l-red-500"
          icon={<X className="w-4 h-4" />}
          label="Lỗi / Dead"
          value={stats.dead}
          tag={
            <span className="text-xs font-semibold text-red-600 bg-red-50 px-2 py-0.5 rounded">
              Cần xử lý
            </span>
          }
        />
      </div>

      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden shadow-sm">
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4 px-6 py-5 border-b border-gray-100">
          <h3 className="text-base font-semibold">Danh sách Nút Proxy toàn cầu</h3>
          <div className="flex items-center gap-3">
            <div className="relative">
              <Search className="w-4 h-4 text-gray-400 absolute left-3 top-1/2 -translate-y-1/2 pointer-events-none" />
              <input
                type="text"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                placeholder="Tìm kiếm proxy hoặc khu vực..."
                className="pl-9 pr-3 py-2 border border-gray-200 rounded-lg text-sm w-64 focus:outline-none focus:border-[#ef4444]"
              />
            </div>
          </div>
        </div>

        {isLoading ? (
          <div className="flex items-center justify-center py-20 text-gray-500 gap-2">
            <Loader2 className="w-6 h-6 animate-spin" />
            Đang tải danh sách proxy...
          </div>
        ) : filteredProxies.length === 0 ? (
          <div className="py-16 text-center text-gray-500 text-sm">
            {searchTerm
              ? 'Không tìm thấy proxy phù hợp.'
              : 'Chưa có proxy nào trong hệ thống. Nhấn "Thêm Proxy mới" để bắt đầu.'}
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-100 bg-blue-50/40">
                  <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                    Địa chỉ IP : Port
                  </th>
                  <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                    Auth User
                  </th>
                  <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                    Trạng thái
                  </th>
                  <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                    Fail Count
                  </th>
                  <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                    Lần dùng cuối
                  </th>
                  <th className="text-right px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase tracking-wider">
                    Thao tác
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {filteredProxies.map((proxy) => (
                  <tr key={proxy.proxyId} className="hover:bg-gray-50/50 transition-colors">
                    <td className="px-6 py-4 font-medium text-[#111827]">
                      {proxy.ipAddress}:{proxy.port}
                    </td>
                    <td className="px-6 py-4">
                      <span className="inline-flex items-center gap-2 text-[#6b7280]">
                        <Globe className="w-4 h-4 text-blue-500 shrink-0" />
                        {proxy.authUser || <span className="text-gray-400">—</span>}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <span
                        className={`inline-flex px-2 py-0.5 rounded text-xs font-semibold ${
                          statusBadge[proxy.status ?? 'active'] ?? 'bg-gray-100 text-gray-600'
                        }`}
                      >
                        {proxy.status ?? 'active'}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-[#111827]">
                      <span
                        className={`font-semibold ${
                          proxy.failCount >= 5
                            ? 'text-red-600'
                            : proxy.failCount > 0
                              ? 'text-amber-600'
                              : 'text-[#111827]'
                        }`}
                      >
                        {proxy.failCount}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-[#6b7280]">
                      {proxy.lastUsedAt
                        ? new Date(proxy.lastUsedAt).toLocaleString('vi-VN')
                        : '—'}
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex items-center justify-end gap-2">
                        <Toggle
                          enabled={proxy.enabled}
                          onChange={() => handleToggle(proxy)}
                        />
                        <button
                          type="button"
                          onClick={() => openEdit(proxy)}
                          className="p-2 text-gray-400 hover:text-[#111827] hover:bg-gray-100 rounded-lg transition-colors"
                          title="Sửa proxy"
                        >
                          <Pencil className="w-4 h-4" />
                        </button>
                        <button
                          type="button"
                          onClick={() => setDeleteId(proxy.proxyId)}
                          className="p-2 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                          title="Xóa proxy"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {modalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-lg p-6">
            <h3 className="text-lg font-semibold mb-4">
              {editingId != null ? 'Sửa Proxy' : 'Thêm Proxy mới'}
            </h3>

            {formError && (
              <div className="mb-4 text-sm text-red-600 bg-red-50 border border-red-100 rounded-lg px-3 py-2">
                {formError}
              </div>
            )}

            <div className="space-y-4">
              <div className="grid grid-cols-3 gap-3">
                <div className="col-span-2">
                  <label className="block text-sm font-medium text-gray-700 mb-1">IP Address *</label>
                  <input
                    type="text"
                    value={form.ipAddress}
                    onChange={(e) => setForm((f) => ({ ...f, ipAddress: e.target.value }))}
                    placeholder="VD: 192.168.1.10"
                    className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-[#ef4444]"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Port *</label>
                  <input
                    type="number"
                    min={1}
                    max={65535}
                    value={form.port}
                    onChange={(e) => setForm((f) => ({ ...f, port: e.target.value }))}
                    placeholder="8080"
                    className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-[#ef4444]"
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Auth User <span className="text-gray-400 font-normal">(tùy chọn)</span>
                </label>
                <input
                  type="text"
                  value={form.authUser}
                  onChange={(e) => setForm((f) => ({ ...f, authUser: e.target.value }))}
                  placeholder="username"
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-[#ef4444]"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Auth Pass{' '}
                  <span className="text-gray-400 font-normal">
                    {editingId != null ? '(để trống nếu không đổi)' : '(tùy chọn)'}
                  </span>
                </label>
                <input
                  type="password"
                  value={form.authPass}
                  onChange={(e) => setForm((f) => ({ ...f, authPass: e.target.value }))}
                  placeholder="••••••••"
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-[#ef4444]"
                />
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Trạng thái</label>
                <select
                  value={form.status}
                  disabled={!form.enabled}
                  onChange={(e) =>
                    setForm((f) => ({ ...f, status: e.target.value as ProxyForm['status'] }))
                  }
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm focus:outline-none focus:border-[#ef4444] disabled:bg-gray-50 disabled:text-gray-400"
                >
                  <option value="active">active</option>
                  <option value="disabled">disabled</option>
                  <option value="dead">dead</option>
                </select>
              </div>

              <label className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={form.enabled}
                  onChange={(e) => setForm((f) => ({ ...f, enabled: e.target.checked }))}
                />
                Kích hoạt (proxy sẽ được dùng khi xoay vòng)
              </label>
            </div>

            <div className="flex justify-end gap-3 mt-6">
              <button
                type="button"
                onClick={closeModal}
                disabled={isSaving}
                className="px-4 py-2 text-sm border border-gray-200 rounded-lg hover:bg-gray-50 disabled:opacity-50"
              >
                Hủy
              </button>
              <button
                type="button"
                onClick={handleSave}
                disabled={isSaving}
                className="px-4 py-2 text-sm bg-[#ef4444] text-white rounded-lg hover:bg-red-600 disabled:opacity-60 inline-flex items-center gap-2"
              >
                {isSaving && <Loader2 className="w-4 h-4 animate-spin" />}
                {isSaving ? 'Đang lưu...' : 'Lưu'}
              </button>
            </div>
          </div>
        </div>
      )}

      <ConfirmModal
        isOpen={deleteId != null}
        onClose={() => !isDeleting && setDeleteId(null)}
        onConfirm={handleDelete}
        title="Xóa proxy"
        message={
          proxyToDelete
            ? `Bạn có chắc chắn muốn xóa proxy "${proxyToDelete.ipAddress}:${proxyToDelete.port}"? Hành động này không thể hoàn tác.`
            : 'Bạn có chắc chắn muốn xóa proxy này?'
        }
        confirmText="Xóa"
        type="danger"
        isLoading={isDeleting}
      />
    </AdminLayout>
  );
};

interface StatCardProps {
  borderColor: string;
  label: string;
  value: number;
  tag: React.ReactNode;
  icon?: React.ReactNode;
}

const StatCard = ({ borderColor, label, value, tag, icon }: StatCardProps) => (
  <div className={`bg-white border border-gray-200 border-l-4 ${borderColor} rounded-xl p-5 shadow-sm`}>
    <div className="flex items-start justify-between mb-3">
      <p className="text-xs font-medium text-[#6b7280] uppercase tracking-wide">{label}</p>
      {icon && <span className="text-[#6b7280]">{icon}</span>}
    </div>
    <div className="flex items-end justify-between">
      <span className="text-3xl font-bold text-[#111827]">{value}</span>
      {tag}
    </div>
  </div>
);

interface ToggleProps {
  enabled: boolean;
  onChange: () => void;
}

const Toggle = ({ enabled, onChange }: ToggleProps) => (
  <button
    type="button"
    role="switch"
    aria-checked={enabled}
    onClick={onChange}
    className={`relative w-11 h-6 rounded-full transition-colors ${
      enabled ? 'bg-[#ef4444]' : 'bg-gray-300'
    }`}
  >
    <span
      className={`absolute top-0.5 left-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform ${
        enabled ? 'translate-x-5' : 'translate-x-0'
      }`}
    />
  </button>
);

export default ProxyManagement;