import { useCallback, useEffect, useState } from 'react';
import { Users, Loader2, Pencil, Plus, Trash2 } from 'lucide-react';
import AdminLayout from '../../components/admin/AdminLayout';
import { adminApi, type FbSource, type UpsertFbSource } from '../../api/portalApi';
import { extractApiError } from '../../utils/authStorage';

const emptyForm: UpsertFbSource = {
  groupUrl: '',
  groupName: '',
  status: 'active',
  enabled: true,
};

const FbSourceManagement = () => {
  const [sources, setSources] = useState<FbSource[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');
  const [modalOpen, setModalOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<UpsertFbSource>(emptyForm);
  const [isSaving, setIsSaving] = useState(false);

  const loadSources = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage('');
    try {
      setSources(await adminApi.getFbSources());
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải danh sách nguồn Facebook.'));
      setSources([]);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadSources();
  }, [loadSources]);

  const openCreate = () => {
    setEditingId(null);
    setForm(emptyForm);
    setModalOpen(true);
  };

  const openEdit = (source: FbSource) => {
    setEditingId(source.fbSourceId);
    setForm({
      groupUrl: source.groupUrl,
      groupName: source.groupName ?? '',
      status: source.status ?? 'active',
      enabled: source.status !== 'disabled',
    });
    setModalOpen(true);
  };

  const handleSave = async () => {
    if (!form.groupUrl.trim()) {
      setErrorMessage('Vui lòng nhập URL group/page Facebook.');
      return;
    }

    setIsSaving(true);
    setErrorMessage('');
    try {
      if (editingId != null) {
        await adminApi.updateFbSource(editingId, form);
      } else {
        await adminApi.createFbSource(form);
      }
      setModalOpen(false);
      await loadSources();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể lưu nguồn Facebook.'));
    } finally {
      setIsSaving(false);
    }
  };

  const handleDelete = async (source: FbSource) => {
    if (!window.confirm(`Xóa nguồn "${source.groupName || source.groupUrl}"?`)) return;

    setErrorMessage('');
    try {
      await adminApi.deleteFbSource(source.fbSourceId);
      await loadSources();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể xóa nguồn.'));
    }
  };

  const activeCount = sources.filter((s) => s.status === 'active').length;

  return (
    <AdminLayout searchPlaceholder="Search Facebook group URL...">
      <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 mb-8">
        <div>
          <h2 className="text-2xl lg:text-3xl font-bold tracking-tight">Facebook Sources</h2>
          <p className="text-[#6b7280] text-sm mt-1">
            Quản lý URL group/page Facebook toàn hệ thống (FB_SOURCES) — {activeCount} active / {sources.length} tổng
          </p>
        </div>
        <button
          type="button"
          onClick={openCreate}
          className="flex items-center gap-2 px-5 py-2.5 bg-[#ef4444] hover:bg-red-600 text-white rounded-lg text-sm font-semibold transition-colors shadow-sm shrink-0"
        >
          <Plus className="w-4 h-4" />
          Thêm nguồn FB
        </button>
      </div>

      {errorMessage && (
        <div className="mb-4 text-sm text-red-600 bg-red-50 border border-red-100 rounded-lg px-4 py-3">
          {errorMessage}
        </div>
      )}

      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden shadow-sm">
        {isLoading ? (
          <div className="flex items-center justify-center py-20 text-gray-500 gap-2">
            <Loader2 className="w-6 h-6 animate-spin" />
            Đang tải...
          </div>
        ) : sources.length === 0 ? (
          <div className="py-16 text-center text-gray-500 text-sm">
            Chưa có nguồn Facebook. Nhấn &quot;Thêm nguồn FB&quot; để thêm URL group.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-100 bg-blue-50/40">
                  <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Tên / URL</th>
                  <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Trạng thái</th>
                  <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Thêm bởi</th>
                  <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Ngày tạo</th>
                  <th className="text-right px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {sources.map((source) => (
                  <tr key={source.fbSourceId} className="hover:bg-gray-50/50">
                    <td className="px-6 py-4">
                      <div className="flex items-start gap-2">
                        <Users className="w-4 h-4 text-blue-600 shrink-0 mt-0.5" />
                        <div>
                          {source.groupName && (
                            <p className="font-medium text-[#111827]">{source.groupName}</p>
                          )}
                          <a
                            href={source.groupUrl}
                            target="_blank"
                            rel="noreferrer"
                            className="text-blue-600 hover:underline break-all text-xs"
                          >
                            {source.groupUrl}
                          </a>
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <span
                        className={`inline-flex px-2 py-0.5 rounded text-xs font-semibold ${
                          source.status === 'active'
                            ? 'bg-green-50 text-green-700'
                            : 'bg-gray-100 text-gray-600'
                        }`}
                      >
                        {source.status ?? 'active'}
                      </span>
                    </td>
                    <td className="px-6 py-4 text-[#6b7280]">{source.addedByName ?? `User #${source.addedBy}`}</td>
                    <td className="px-6 py-4 text-[#6b7280]">
                      {source.createdAt ? new Date(source.createdAt).toLocaleString('vi-VN') : '—'}
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          type="button"
                          onClick={() => openEdit(source)}
                          className="p-2 text-gray-400 hover:text-[#111827] hover:bg-gray-100 rounded-lg"
                          title="Sửa"
                        >
                          <Pencil className="w-4 h-4" />
                        </button>
                        <button
                          type="button"
                          onClick={() => handleDelete(source)}
                          className="p-2 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg"
                          title="Xóa"
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
              {editingId != null ? 'Sửa nguồn Facebook' : 'Thêm nguồn Facebook'}
            </h3>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">URL group/page *</label>
                <input
                  type="url"
                  value={form.groupUrl}
                  onChange={(e) => setForm((f) => ({ ...f, groupUrl: e.target.value }))}
                  placeholder="https://www.facebook.com/groups/..."
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Tên hiển thị (tùy chọn)</label>
                <input
                  type="text"
                  value={form.groupName ?? ''}
                  onChange={(e) => setForm((f) => ({ ...f, groupName: e.target.value }))}
                  placeholder="VD: Group review sản phẩm"
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm"
                />
              </div>
              <label className="flex items-center gap-2 text-sm">
                <input
                  type="checkbox"
                  checked={form.enabled}
                  onChange={(e) => setForm((f) => ({ ...f, enabled: e.target.checked }))}
                />
                Kích hoạt (dùng khi cào Facebook)
              </label>
            </div>
            <div className="flex justify-end gap-3 mt-6">
              <button
                type="button"
                onClick={() => setModalOpen(false)}
                className="px-4 py-2 text-sm border border-gray-200 rounded-lg hover:bg-gray-50"
              >
                Hủy
              </button>
              <button
                type="button"
                onClick={handleSave}
                disabled={isSaving}
                className="px-4 py-2 text-sm bg-[#ef4444] text-white rounded-lg hover:bg-red-600 disabled:opacity-60"
              >
                {isSaving ? 'Đang lưu...' : 'Lưu'}
              </button>
            </div>
          </div>
        </div>
      )}
    </AdminLayout>
  );
};

export default FbSourceManagement;
