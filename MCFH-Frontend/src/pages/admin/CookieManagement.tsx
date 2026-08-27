import { useCallback, useEffect, useMemo, useState } from 'react';
import { Cookie, Loader2, Pencil, Plus, Trash2, Upload } from 'lucide-react';
import AdminLayout from '../../components/admin/AdminLayout';
import {
  adminApi,
  type PlatformCookie,
  type UpdatePlatformCookieMeta,
} from '../../api/portalApi';
import { extractApiError } from '../../utils/authStorage';

const LABELS_STORAGE_KEY = 'mcfh.platformLabels';

type LabelMap = Record<string, string>;

const readStoredLabels = (): LabelMap => {
  try {
    const raw = localStorage.getItem(LABELS_STORAGE_KEY);
    if (!raw) return {};
    const parsed = JSON.parse(raw);
    return typeof parsed === 'object' && parsed !== null ? (parsed as LabelMap) : {};
  } catch {
    return {};
  }
};

const writeStoredLabels = (labels: LabelMap) => {
  try {
    localStorage.setItem(LABELS_STORAGE_KEY, JSON.stringify(labels));
  } catch {
    /* bỏ qua nếu storage đầy / blocked */
  }
};

const capitalize = (value: string): string => {
  if (!value) return value;
  // Giữ nguyên chữ in hoa ở giữa (TikTok), chỉ capitalize chữ đầu.
  return value.charAt(0).toUpperCase() + value.slice(1);
};

const formatDate = (value?: string | null) =>
  value ? new Date(value).toLocaleString('vi-VN') : '—';

const statusClass = (status: string) => {
  if (status === 'active') return 'bg-green-50 text-green-700';
  if (status === 'expired') return 'bg-amber-50 text-amber-700';
  return 'bg-gray-100 text-gray-600';
};

const CookieManagement = () => {
  const [cookies, setCookies] = useState<PlatformCookie[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState('');

  const [contentModalOpen, setContentModalOpen] = useState(false);
  const [metaModalOpen, setMetaModalOpen] = useState(false);
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [selectedPlatform, setSelectedPlatform] = useState<string | null>(null);
  const [cookiesJson, setCookiesJson] = useState('');

  // Meta form: status + note + filePath (rename) + displayName (chỉ frontend)
  const [metaForm, setMetaForm] = useState<UpdatePlatformCookieMeta & { displayName?: string }>({
    status: 'active',
    note: '',
    filePath: '',
    displayName: '',
  });
  const [displayLabels, setDisplayLabels] = useState<LabelMap>(readStoredLabels);

  const [createForm, setCreateForm] = useState({
    platform: '',
    filePath: 'cookies/',
    status: 'disabled',
    note: '',
    cookiesJson: '',
    displayName: '',
  });
  const [isSaving, setIsSaving] = useState(false);

  const getDisplayName = useCallback(
    (platform: string) => displayLabels[platform] || capitalize(platform),
    [displayLabels]
  );

  const loadCookies = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage('');
    try {
      setCookies(await adminApi.getPlatformCookies());
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải danh sách cookie.'));
      setCookies([]);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadCookies();
  }, [loadCookies]);

  const openContentModal = (item: PlatformCookie) => {
    setSelectedPlatform(item.platform);
    setCookiesJson('');
    setSuccessMessage('');
    setErrorMessage('');
    setContentModalOpen(true);
  };

  const openCreateModal = () => {
    setCreateForm({
      platform: '',
      filePath: 'cookies/',
      status: 'disabled',
      note: '',
      cookiesJson: '',
      displayName: '',
    });
    setSuccessMessage('');
    setErrorMessage('');
    setCreateModalOpen(true);
  };

  const handleCreate = async () => {
    const platform = createForm.platform.trim().toLowerCase();
    if (!platform) {
      setErrorMessage('Vui lòng nhập mã platform (vd: facebook, tiktok).');
      return;
    }
    const filePath = createForm.filePath.trim();
    if (!filePath || !filePath.toLowerCase().endsWith('.json')) {
      setErrorMessage('File path phải có đuôi .json và nằm trong thư mục cookies/.');
      return;
    }

    setIsSaving(true);
    setErrorMessage('');
    setSuccessMessage('');
    try {
      await adminApi.createPlatformCookie({
        platform,
        filePath,
        status: createForm.status,
        note: createForm.note.trim() || undefined,
        cookiesJson: createForm.cookiesJson.trim() || undefined,
      });
      // Lưu displayName vào localStorage (chỉ frontend, không gửi backend)
      const trimmedLabel = createForm.displayName.trim();
      if (trimmedLabel) {
        const next = { ...displayLabels, [platform]: trimmedLabel };
        setDisplayLabels(next);
        writeStoredLabels(next);
      } else if (displayLabels[platform]) {
        // Nếu trước đó có label nhưng lần này để trống → giữ nguyên (không xóa).
      }
      setCreateModalOpen(false);
      setSuccessMessage(`Đã tạo platform ${platform}.`);
      await loadCookies();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tạo platform.'));
    } finally {
      setIsSaving(false);
    }
  };

  const openMetaModal = (item: PlatformCookie) => {
    setSelectedPlatform(item.platform);
    setMetaForm({
      status: item.status,
      note: item.note ?? '',
      filePath: item.filePath,
      displayName: getDisplayName(item.platform),
    });
    setSuccessMessage('');
    setErrorMessage('');
    setMetaModalOpen(true);
  };

  const handleSaveContent = async () => {
    if (!selectedPlatform || !cookiesJson.trim()) {
      setErrorMessage('Vui lòng dán JSON cookie từ Cookie Editor.');
      return;
    }

    setIsSaving(true);
    setErrorMessage('');
    setSuccessMessage('');
    try {
      const result = await adminApi.updatePlatformCookieContent(selectedPlatform, cookiesJson.trim());
      setSuccessMessage(result.message);
      setContentModalOpen(false);
      await loadCookies();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể cập nhật cookie.'));
    } finally {
      setIsSaving(false);
    }
  };

  const handleSaveMeta = async () => {
    if (!selectedPlatform) return;

    // Validate đường dẫn file JSON nếu có thay đổi.
    const trimmedPath = (metaForm.filePath ?? '').trim();
    const original = cookies.find((c) => c.platform === selectedPlatform)?.filePath ?? '';
    if (trimmedPath && !trimmedPath.toLowerCase().endsWith('.json')) {
      setErrorMessage('Đường dẫn file JSON phải có đuôi .json.');
      return;
    }

    setIsSaving(true);
    setErrorMessage('');
    setSuccessMessage('');
    try {
      const payload: UpdatePlatformCookieMeta = {
        status: metaForm.status,
        note: metaForm.note ?? '',
      };
      // Chỉ gửi filePath khi khác giá trị hiện tại.
      if (trimmedPath && trimmedPath !== original) {
        payload.filePath = trimmedPath;
      }

      await adminApi.updatePlatformCookieMeta(selectedPlatform, payload);

      // Cập nhật displayName vào localStorage (chỉ frontend)
      const trimmedLabel = (metaForm.displayName ?? '').trim();
      const currentLabel = displayLabels[selectedPlatform] ?? '';
      if (trimmedLabel !== currentLabel) {
        const next = trimmedLabel
          ? { ...displayLabels, [selectedPlatform]: trimmedLabel }
          : { ...displayLabels };
        if (!trimmedLabel) delete next[selectedPlatform];
        setDisplayLabels(next);
        writeStoredLabels(next);
      }

      setMetaModalOpen(false);
      setSuccessMessage(
        trimmedPath && trimmedPath !== original
          ? `Đã cập nhật metadata và đổi tên file sang ${trimmedPath}.`
          : 'Đã cập nhật metadata cookie.'
      );
      await loadCookies();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể cập nhật metadata.'));
    } finally {
      setIsSaving(false);
    }
  };

  const handleClear = async (item: PlatformCookie) => {
    const label = getDisplayName(item.platform);
    if (!window.confirm(`Xóa nội dung cookie ${label}? Scraper sẽ không dùng được cho đến khi upload lại.`)) {
      return;
    }

    setErrorMessage('');
    setSuccessMessage('');
    try {
      await adminApi.clearPlatformCookieContent(item.platform);
      setSuccessMessage(`Đã xóa cookie ${label}.`);
      await loadCookies();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể xóa cookie.'));
    }
  };

  const activeCount = cookies.filter((c) => c.status === 'active' && c.fileExists).length;

  // Danh sách file path hiện dùng (để cảnh báo khi admin sửa trùng).
  const takenFilePaths = useMemo(() => {
    const map = new Map<string, string>();
    cookies.forEach((c) => map.set(c.filePath.toLowerCase(), c.platform));
    return map;
  }, [cookies]);

  return (
    <AdminLayout searchPlaceholder="Tìm kiếm cookie...">
      <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 mb-8">
        <div>
          <h2 className="text-2xl lg:text-3xl font-bold tracking-tight">Quản lý Cookie Nền tảng</h2>
          <p className="text-[#6b7280] text-sm mt-1">
            Quản lý session cookie cho scraper — {activeCount} active / {cookies.length} platform
          </p>
        </div>
        <button
          type="button"
          onClick={openCreateModal}
          className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium bg-[#ef4444] text-white rounded-lg hover:bg-red-600"
        >
          <Plus className="w-4 h-4" />
          Thêm Platform
        </button>
      </div>

      {errorMessage && (
        <div className="mb-4 text-sm text-red-600 bg-red-50 border border-red-100 rounded-lg px-4 py-3">
          {errorMessage}
        </div>
      )}

      {successMessage && (
        <div className="mb-4 text-sm text-green-700 bg-green-50 border border-green-100 rounded-lg px-4 py-3">
          {successMessage}
        </div>
      )}

      <div className="bg-white border border-gray-200 rounded-xl overflow-hidden shadow-sm">
        {isLoading ? (
          <div className="flex items-center justify-center py-20 text-gray-500 gap-2">
            <Loader2 className="w-6 h-6 animate-spin" />
            Đang tải...
          </div>
        ) : cookies.length === 0 ? (
          <div className="py-16 text-center text-gray-500 text-sm">
            Chưa có bản ghi PLATFORM_COOKIES. Chạy script seed trong database.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-100 bg-amber-50/40">
                  <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Platform</th>
                  <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">File</th>
                  <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Trạng thái</th>
                  <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Cookies</th>
                  <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Hết hạn</th>
                  <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Upload / Dùng</th>
                  <th className="text-right px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {cookies.map((item) => (
                  <tr key={item.platformCookieId} className="hover:bg-gray-50/50">
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-2">
                        <Cookie className="w-4 h-4 text-amber-600 shrink-0" />
                        <div>
                          <p className="font-medium text-[#111827]">{getDisplayName(item.platform)}</p>
                          <p className="text-xs text-[#6b7280] mt-0.5 font-mono">{item.platform}</p>
                          {item.note && <p className="text-xs text-[#6b7280] mt-0.5">{item.note}</p>}
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <p className="font-mono text-xs text-[#6b7280] break-all">{item.filePath}</p>
                      <p className="text-xs mt-1">
                        {item.fileExists ? (
                          <span className="text-green-600">File tồn tại</span>
                        ) : (
                          <span className="text-red-500">File thiếu</span>
                        )}
                        {item.backupExists && (
                          <span className="text-[#6b7280] ml-2">• có backup</span>
                        )}
                      </p>
                    </td>
                    <td className="px-6 py-4">
                      <span className={`inline-flex px-2 py-0.5 rounded text-xs font-semibold ${statusClass(item.status)}`}>
                        {item.status}
                      </span>
                      {item.isExpired ? (
                        <p className="text-xs text-red-600 mt-1 font-medium">Hết hạn</p>
                      ) : item.isExpiringSoon && (
                        <p className="text-xs text-amber-600 mt-1">Sắp hết hạn</p>
                      )}
                    </td>
                    <td className="px-6 py-4 text-[#111827]">{item.cookieCount}</td>
                    <td className="px-6 py-4 text-[#6b7280]">{formatDate(item.expiresAt)}</td>
                    <td className="px-6 py-4 text-[#6b7280] text-xs">
                      <p>Upload: {formatDate(item.uploadedAt)}</p>
                      <p className="mt-1">Dùng: {formatDate(item.lastUsedAt)}</p>
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          type="button"
                          onClick={() => openContentModal(item)}
                          className="p-2 text-gray-400 hover:text-[#111827] hover:bg-gray-100 rounded-lg"
                          title="Cập nhật cookie JSON"
                        >
                          <Upload className="w-4 h-4" />
                        </button>
                        <button
                          type="button"
                          onClick={() => openMetaModal(item)}
                          className="p-2 text-gray-400 hover:text-[#111827] hover:bg-gray-100 rounded-lg"
                          title="Sửa metadata"
                        >
                          <Pencil className="w-4 h-4" />
                        </button>
                        <button
                          type="button"
                          onClick={() => handleClear(item)}
                          className="p-2 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg"
                          title="Xóa nội dung cookie"
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

      <div className="mt-6 p-4 bg-blue-50 border border-blue-100 rounded-xl text-sm text-blue-900">
        <p className="font-semibold mb-1">Hướng dẫn cập nhật cookie</p>
        <ol className="list-decimal list-inside space-y-1 text-blue-800">
          <li>Đăng nhập Facebook/TikTok trên Chrome/Edge (tài khoản test).</li>
          <li>Dùng extension <strong>Cookie Editor</strong> → Export as JSON.</li>
          <li>Nhấn biểu tượng Upload → dán JSON → Lưu. Không cần restart backend.</li>
        </ol>
      </div>

      {contentModalOpen && selectedPlatform && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-2xl p-6 max-h-[90vh] overflow-y-auto">
            <h3 className="text-lg font-semibold mb-1">
              Cập nhật cookie {getDisplayName(selectedPlatform)}
            </h3>
            <p className="text-sm text-[#6b7280] mb-4">
              Dán toàn bộ JSON export từ Cookie Editor. Hệ thống sẽ ghi file trên server và backup bản cũ.
            </p>
            <textarea
              value={cookiesJson}
              onChange={(e) => setCookiesJson(e.target.value)}
              placeholder='[{"domain":".facebook.com","name":"c_user",...}]'
              rows={14}
              className="w-full px-3 py-2 border border-gray-200 rounded-lg text-xs font-mono"
            />
            <div className="flex justify-end gap-3 mt-6">
              <button
                type="button"
                onClick={() => setContentModalOpen(false)}
                className="px-4 py-2 text-sm border border-gray-200 rounded-lg hover:bg-gray-50"
              >
                Hủy
              </button>
              <button
                type="button"
                onClick={handleSaveContent}
                disabled={isSaving}
                className="px-4 py-2 text-sm bg-[#ef4444] text-white rounded-lg hover:bg-red-600 disabled:opacity-60"
              >
                {isSaving ? 'Đang lưu...' : 'Lưu cookie'}
              </button>
            </div>
          </div>
        </div>
      )}

      {metaModalOpen && selectedPlatform && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-md p-6">
            <h3 className="text-lg font-semibold mb-1">
              Metadata — {getDisplayName(selectedPlatform)}
            </h3>
            <p className="text-xs text-[#6b7280] mb-4 font-mono">{selectedPlatform}</p>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Tên hiển thị</label>
                <input
                  type="text"
                  value={metaForm.displayName ?? ''}
                  onChange={(e) => setMetaForm((f) => ({ ...f, displayName: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm"
                  placeholder={capitalize(selectedPlatform)}
                />
                <p className="text-xs text-[#6b7280] mt-1">
                  Chỉ hiển thị trong UI (lưu trên trình duyệt). Để trống sẽ tự capitalize mã platform.
                </p>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Trạng thái</label>
                <select
                  value={metaForm.status ?? 'active'}
                  onChange={(e) => setMetaForm((f) => ({ ...f, status: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm"
                >
                  <option value="active">active</option>
                  <option value="disabled">disabled</option>
                  <option value="expired">expired</option>
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Đường dẫn file JSON</label>
                <input
                  type="text"
                  value={metaForm.filePath ?? ''}
                  onChange={(e) => setMetaForm((f) => ({ ...f, filePath: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm font-mono"
                  placeholder="cookies/facebook.json"
                />
                <p className="text-xs text-[#6b7280] mt-1">
                  Đổi tên file sẽ tự rename file vật lý trên server (kèm backup nếu có). File mới không được trùng với platform khác.
                </p>
                {(() => {
                  const newPath = (metaForm.filePath ?? '').trim().toLowerCase();
                  const original = cookies.find((c) => c.platform === selectedPlatform)?.filePath.toLowerCase() ?? '';
                  if (!newPath || newPath === original) return null;
                  const owner = takenFilePaths.get(newPath);
                  if (owner && owner !== selectedPlatform) {
                    return (
                      <p className="text-xs text-red-600 mt-1 font-medium">
                        Đã được dùng bởi platform "{owner}". Hãy chọn đường dẫn khác.
                      </p>
                    );
                  }
                  return null;
                })()}
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Ghi chú</label>
                <textarea
                  value={metaForm.note ?? ''}
                  onChange={(e) => setMetaForm((f) => ({ ...f, note: e.target.value }))}
                  rows={3}
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm"
                  placeholder="VD: Tài khoản test FB — đổi 08/07"
                />
              </div>
            </div>
            <div className="flex justify-end gap-3 mt-6">
              <button
                type="button"
                onClick={() => setMetaModalOpen(false)}
                className="px-4 py-2 text-sm border border-gray-200 rounded-lg hover:bg-gray-50"
              >
                Hủy
              </button>
              <button
                type="button"
                onClick={handleSaveMeta}
                disabled={isSaving}
                className="px-4 py-2 text-sm bg-[#ef4444] text-white rounded-lg hover:bg-red-600 disabled:opacity-60"
              >
                {isSaving ? 'Đang lưu...' : 'Lưu'}
              </button>
            </div>
          </div>
        </div>
      )}

      {createModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="bg-white rounded-xl shadow-xl w-full max-w-2xl p-6 max-h-[90vh] overflow-y-auto">
            <h3 className="text-lg font-semibold mb-1">Thêm Platform mới</h3>
            <p className="text-sm text-[#6b7280] mb-4">
              Tạo mới một platform trong PLATFORM_COOKIES. Có thể upload cookie luôn hoặc tạo record rỗng để dùng sau.
            </p>
            <div className="grid grid-cols-1 sm:grid-cols-2 gap-4 mb-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Mã platform <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  value={createForm.platform}
                  onChange={(e) => setCreateForm((f) => ({ ...f, platform: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm"
                  placeholder="vd: instagram"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Tên hiển thị
                </label>
                <input
                  type="text"
                  value={createForm.displayName}
                  onChange={(e) => setCreateForm((f) => ({ ...f, displayName: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm"
                  placeholder={capitalize(createForm.platform) || 'Instagram'}
                />
                <p className="text-xs text-[#6b7280] mt-1">
                  Để trống sẽ tự capitalize mã platform.
                </p>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  File path <span className="text-red-500">*</span>
                </label>
                <input
                  type="text"
                  value={createForm.filePath}
                  onChange={(e) => setCreateForm((f) => ({ ...f, filePath: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm font-mono"
                  placeholder="cookies/instagram.json"
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Trạng thái</label>
                <select
                  value={createForm.status}
                  onChange={(e) => setCreateForm((f) => ({ ...f, status: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm"
                >
                  <option value="disabled">disabled</option>
                  <option value="active">active</option>
                  <option value="expired">expired</option>
                </select>
              </div>
              <div className="sm:col-span-2">
                <label className="block text-sm font-medium text-gray-700 mb-1">Ghi chú</label>
                <input
                  type="text"
                  value={createForm.note}
                  onChange={(e) => setCreateForm((f) => ({ ...f, note: e.target.value }))}
                  className="w-full px-3 py-2 border border-gray-200 rounded-lg text-sm"
                  placeholder="VD: Tài khoản test Instagram"
                />
              </div>
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">
                Cookie JSON (tuỳ chọn)
              </label>
              <p className="text-xs text-[#6b7280] mb-2">
                Nếu dán JSON ngay, hệ thống sẽ ghi file và set status=active + tính expiresAt.
              </p>
              <textarea
                value={createForm.cookiesJson}
                onChange={(e) => setCreateForm((f) => ({ ...f, cookiesJson: e.target.value }))}
                placeholder='[{"domain":".instagram.com","name":"sessionid",...}]'
                rows={8}
                className="w-full px-3 py-2 border border-gray-200 rounded-lg text-xs font-mono"
              />
            </div>
            <div className="flex justify-end gap-3 mt-6">
              <button
                type="button"
                onClick={() => setCreateModalOpen(false)}
                className="px-4 py-2 text-sm border border-gray-200 rounded-lg hover:bg-gray-50"
              >
                Hủy
              </button>
              <button
                type="button"
                onClick={handleCreate}
                disabled={isSaving}
                className="px-4 py-2 text-sm bg-[#ef4444] text-white rounded-lg hover:bg-red-600 disabled:opacity-60"
              >
                {isSaving ? 'Đang tạo...' : 'Tạo platform'}
              </button>
            </div>
          </div>
        </div>
      )}
    </AdminLayout>
  );
};

export default CookieManagement;
