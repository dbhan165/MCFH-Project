import { useCallback, useEffect, useMemo, useState } from 'react';
import { Plus, Globe, Pencil, Trash2, Loader2 } from 'lucide-react';
import AdminLayout from '../../components/admin/AdminLayout';
import { adminApi } from '../../api/adminApi';
import type { SystemProxy, UpsertSystemProxy } from '../../types/admin';

const emptyForm: UpsertSystemProxy = {
  ipAddress: '',
  port: 8080,
  authUser: '',
  authPass: '',
  enabled: true,
};

const statusLabel = (status?: string | null) => {
  const s = (status ?? 'active').toLowerCase();
  if (s === 'dead') return { text: 'Dead', className: 'text-red-600 bg-red-50' };
  if (s === 'disabled') return { text: 'Disabled', className: 'text-gray-600 bg-gray-100' };
  return { text: 'Active', className: 'text-green-600 bg-green-50' };
};

const ProxyManagement = () => {
  const [proxies, setProxies] = useState<SystemProxy[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);
  const [modalOpen, setModalOpen] = useState(false);
  const [editing, setEditing] = useState<SystemProxy | null>(null);
  const [form, setForm] = useState<UpsertSystemProxy>(emptyForm);

  const loadProxies = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const { data } = await adminApi.listProxies();
      setProxies(data);
    } catch {
      setError('Không tải được danh sách proxy. Đăng nhập bằng tài khoản Admin.');
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadProxies();
  }, [loadProxies]);

  const stats = useMemo(() => {
    const total = proxies.length;
    const active = proxies.filter((p) => p.status === 'active').length;
    const dead = proxies.filter((p) => p.status === 'dead').length;
    const disabled = proxies.filter((p) => p.status === 'disabled').length;
    return { total, active, dead, disabled };
  }, [proxies]);

  const openCreate = () => {
    setEditing(null);
    setForm(emptyForm);
    setModalOpen(true);
  };

  const openEdit = (proxy: SystemProxy) => {
    setEditing(proxy);
    setForm({
      ipAddress: proxy.ipAddress,
      port: proxy.port,
      authUser: proxy.authUser ?? '',
      authPass: '',
      enabled: proxy.enabled,
      status: proxy.status ?? 'active',
    });
    setModalOpen(true);
  };

  const closeModal = () => {
    setModalOpen(false);
    setEditing(null);
    setForm(emptyForm);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setSaving(true);
    setError(null);
    try {
      const payload: UpsertSystemProxy = {
        ipAddress: form.ipAddress.trim(),
        port: Number(form.port),
        authUser: form.authUser?.trim() || undefined,
        authPass: form.authPass?.trim() || undefined,
        enabled: form.enabled,
      };
      if (editing) {
        await adminApi.updateProxy(editing.proxyId, payload);
      } else {
        await adminApi.createProxy(payload);
      }
      closeModal();
      await loadProxies();
    } catch {
      setError('Lưu proxy thất bại. Kiểm tra IP/port và quyền Admin.');
    } finally {
      setSaving(false);
    }
  };

  const toggleEnabled = async (proxy: SystemProxy) => {
    try {
      await adminApi.updateProxy(proxy.proxyId, {
        ipAddress: proxy.ipAddress,
        port: proxy.port,
        authUser: proxy.authUser ?? undefined,
        enabled: !proxy.enabled,
      });
      await loadProxies();
    } catch {
      setError('Không cập nhật được trạng thái proxy.');
    }
  };

  const handleDelete = async (proxy: SystemProxy) => {
    if (!window.confirm(`Xóa proxy ${proxy.ipAddress}:${proxy.port}?`)) return;
    try {
      await adminApi.deleteProxy(proxy.proxyId);
      await loadProxies();
    } catch {
      setError('Xóa proxy thất bại.');
    }
  };

  return (
    <AdminLayout searchPlaceholder="Search proxies...">
      <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 mb-8">
        <div>
          <h2 className="text-2xl lg:text-3xl font-bold tracking-tight">Proxy Management</h2>
          <p className="text-[#6b7280] text-sm mt-1">
            UC-70–73: Quản lý pool proxy dùng cho xoay IP khi cào dữ liệu (UC-78).
          </p>
        </div>
        <button
          type="button"
          onClick={openCreate}
          className="flex items-center gap-2 px-5 py-2.5 bg-[#ef4444] hover:bg-red-600 text-white rounded-lg text-sm font-semibold transition-colors shadow-sm shrink-0"
        >
          <Plus className="w-4 h-4" />
          Thêm proxy
        </button>
      </div>

      {error && (
        <div className="mb-4 px-4 py-3 bg-red-50 border border-red-100 text-red-700 text-sm rounded-lg">
          {error}
        </div>
      )}

      <div className="grid grid-cols-1 sm:grid-cols-2 xl:grid-cols-4 gap-4 mb-8">
        <StatCard label="Tổng proxy" value={String(stats.total)} />
        <StatCard label="Active" value={String(stats.active)} accent="green" />
        <StatCard label="Disabled" value={String(stats.disabled)} accent="gray" />
        <StatCard label="Dead" value={String(stats.dead)} accent="red" />
      </div>

      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden shadow-sm">
        <div className="px-6 py-5 border-b border-gray-100 flex items-center justify-between">
          <h3 className="text-base font-semibold">System Proxies</h3>
          {loading && <Loader2 className="w-4 h-4 animate-spin text-gray-400" />}
        </div>

        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100 bg-blue-50/40">
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">IP</th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Port</th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Auth user</th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Fail count</th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Last used</th>
                <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Status</th>
                <th className="text-right px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-100">
              {!loading && proxies.length === 0 && (
                <tr>
                  <td colSpan={7} className="px-6 py-10 text-center text-[#6b7280]">
                    Chưa có proxy. Thêm proxy active để bật IP rotation khi cào.
                  </td>
                </tr>
              )}
              {proxies.map((proxy) => {
                const badge = statusLabel(proxy.status);
                return (
                  <tr key={proxy.proxyId} className="hover:bg-gray-50/50">
                    <td className="px-6 py-4 font-medium">
                      <span className="inline-flex items-center gap-2">
                        <Globe className="w-4 h-4 text-blue-500" />
                        {proxy.ipAddress}
                      </span>
                    </td>
                    <td className="px-6 py-4">{proxy.port}</td>
                    <td className="px-6 py-4 text-[#6b7280]">{proxy.authUser || '—'}</td>
                    <td className="px-6 py-4">{proxy.failCount}</td>
                    <td className="px-6 py-4 text-[#6b7280]">
                      {proxy.lastUsedAt
                        ? new Date(proxy.lastUsedAt).toLocaleString('vi-VN')
                        : '—'}
                    </td>
                    <td className="px-6 py-4">
                      <span className={`text-xs font-semibold px-2 py-1 rounded ${badge.className}`}>
                        {badge.text}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          type="button"
                          onClick={() => toggleEnabled(proxy)}
                          className={`px-3 py-1.5 rounded-lg text-xs font-semibold border ${
                            proxy.enabled
                              ? 'border-green-200 text-green-700 bg-green-50'
                              : 'border-gray-200 text-gray-600 bg-gray-50'
                          }`}
                        >
                          {proxy.enabled ? 'On' : 'Off'}
                        </button>
                        <button
                          type="button"
                          onClick={() => openEdit(proxy)}
                          className="p-2 text-gray-400 hover:text-[#111827] hover:bg-gray-100 rounded-lg"
                        >
                          <Pencil className="w-4 h-4" />
                        </button>
                        <button
                          type="button"
                          onClick={() => handleDelete(proxy)}
                          className="p-2 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg"
                        >
                          <Trash2 className="w-4 h-4" />
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      </div>

      {modalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <form
            onSubmit={handleSubmit}
            className="bg-white rounded-xl shadow-xl w-full max-w-md p-6 space-y-4"
          >
            <h3 className="text-lg font-semibold">
              {editing ? 'Sửa proxy' : 'Thêm proxy mới'}
            </h3>
            <Field label="IP address">
              <input
                required
                value={form.ipAddress}
                onChange={(e) => setForm((f) => ({ ...f, ipAddress: e.target.value }))}
                className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm"
                placeholder="203.0.113.10"
              />
            </Field>
            <Field label="Port">
              <input
                required
                type="number"
                min={1}
                max={65535}
                value={form.port}
                onChange={(e) => setForm((f) => ({ ...f, port: Number(e.target.value) }))}
                className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm"
              />
            </Field>
            <Field label="Auth user (tuỳ chọn)">
              <input
                value={form.authUser ?? ''}
                onChange={(e) => setForm((f) => ({ ...f, authUser: e.target.value }))}
                className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm"
              />
            </Field>
            <Field label={editing ? 'Auth pass (để trống nếu giữ nguyên)' : 'Auth pass (tuỳ chọn)'}>
              <input
                type="password"
                value={form.authPass ?? ''}
                onChange={(e) => setForm((f) => ({ ...f, authPass: e.target.value }))}
                className="w-full border border-gray-200 rounded-lg px-3 py-2 text-sm"
              />
            </Field>
            <label className="flex items-center gap-2 text-sm">
              <input
                type="checkbox"
                checked={form.enabled}
                onChange={(e) => setForm((f) => ({ ...f, enabled: e.target.checked }))}
              />
              Kích hoạt (active)
            </label>
            <div className="flex justify-end gap-3 pt-2">
              <button
                type="button"
                onClick={closeModal}
                className="px-4 py-2 text-sm border border-gray-200 rounded-lg hover:bg-gray-50"
              >
                Huỷ
              </button>
              <button
                type="submit"
                disabled={saving}
                className="px-4 py-2 text-sm bg-[#ef4444] text-white rounded-lg hover:bg-red-600 disabled:opacity-60"
              >
                {saving ? 'Đang lưu...' : 'Lưu'}
              </button>
            </div>
          </form>
        </div>
      )}
    </AdminLayout>
  );
};

const Field = ({ label, children }: { label: string; children: React.ReactNode }) => (
  <label className="block space-y-1">
    <span className="text-xs font-medium text-[#6b7280]">{label}</span>
    {children}
  </label>
);

const StatCard = ({
  label,
  value,
  accent,
}: {
  label: string;
  value: string;
  accent?: 'green' | 'red' | 'gray';
}) => {
  const border =
    accent === 'green'
      ? 'border-l-green-500'
      : accent === 'red'
        ? 'border-l-red-500'
        : accent === 'gray'
          ? 'border-l-gray-400'
          : 'border-l-blue-500';

  return (
    <div className={`bg-white border border-gray-200 border-l-4 ${border} rounded-xl p-5 shadow-sm`}>
      <p className="text-xs font-medium text-[#6b7280] uppercase tracking-wide mb-2">{label}</p>
      <span className="text-3xl font-bold text-[#111827]">{value}</span>
    </div>
  );
};

export default ProxyManagement;
