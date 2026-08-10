import { useCallback, useEffect, useMemo, useState } from 'react';
import { Package, Loader2, Pencil, Plus, Trash2, ToggleLeft, ToggleRight } from 'lucide-react';
import AdminLayout from '../../components/admin/AdminLayout';
import {
  adminApi,
  type ScrapePackage,
  type UpsertScrapePackage,
} from '../../api/portalApi';
import { extractApiError } from '../../utils/authStorage';

const emptyForm: UpsertScrapePackage = {
  code: '',
  name: '',
  description: '',
  price: 0,
  currency: 'VND',
  durationDays: 30,
  maxItems: 100,
  maxSources: null,
  isActive: true,
  sortOrder: 0,
};

/** Ranh giới cứng cho field maxSources (theo cấu hình hệ thống). */
const MAX_SOURCES_LIMITED = 6;          // Có 6 platform scrape hiện tại
const MAX_SOURCES_UNLIMITED = 99;       // Gói toàn diện — đặt trần cao để future-proof
const MAX_ITEMS = 100000;

const formatPrice = (price: number, currency: string) => {
  try {
    return new Intl.NumberFormat('vi-VN', { style: 'currency', currency: currency || 'VND' }).format(price);
  } catch {
    return `${price.toLocaleString('vi-VN')} ${currency || 'VND'}`;
  }
};

type FormErrors = Partial<Record<keyof UpsertScrapePackage, string>>;

const ScrapePackageManagement = () => {
  const [packages, setPackages] = useState<ScrapePackage[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState('');
  const [modalOpen, setModalOpen] = useState(false);
  const [editingId, setEditingId] = useState<number | null>(null);
  const [form, setForm] = useState<UpsertScrapePackage>(emptyForm);
  const [formErrors, setFormErrors] = useState<FormErrors>({});
  const [isSaving, setIsSaving] = useState(false);

  /** Đoán code đang nhập có phải gói toàn diện không (để chọn trần maxSources). */
  const isUnlimitedCode = useMemo(() => form.code.trim().toUpperCase().startsWith('FULL'), [form.code]);
  const maxSourcesCap = isUnlimitedCode ? MAX_SOURCES_UNLIMITED : MAX_SOURCES_LIMITED;

  const loadPackages = useCallback(async () => {
    setIsLoading(true);
    setErrorMessage('');
    try {
      setPackages(await adminApi.getScrapePackages());
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải danh sách gói scrape.'));
      setPackages([]);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    loadPackages();
  }, [loadPackages]);

  const resetModalState = () => {
    setForm(emptyForm);
    setFormErrors({});
    setEditingId(null);
  };

  const openCreate = () => {
    resetModalState();
    setModalOpen(true);
  };

  const openEdit = (pkg: ScrapePackage) => {
    setEditingId(pkg.packageId);
    setForm({
      code: pkg.code,
      name: pkg.name,
      description: pkg.description ?? '',
      price: pkg.price,
      currency: pkg.currency || 'VND',
      durationDays: pkg.durationDays,
      maxItems: pkg.maxItems,
      maxSources: pkg.maxSources ?? null,
      isActive: pkg.isActive,
      sortOrder: pkg.sortOrder,
    });
    setFormErrors({});
    setModalOpen(true);
  };

  const closeModal = () => {
    setModalOpen(false);
    setFormErrors({});
    setErrorMessage('');
  };

  /** Validate client-side. Trả về object errors — rỗng = OK. */
  const validate = (data: UpsertScrapePackage): FormErrors => {
    const errs: FormErrors = {};

    const code = data.code.trim().toUpperCase();
    if (!code) {
      errs.code = 'Vui lòng nhập mã gói (Code).';
    } else if (!/^[A-Z0-9_]{2,50}$/.test(code)) {
      errs.code = 'Mã chỉ gồm chữ HOA, số, gạch dưới (2–50 ký tự).';
    }

    if (!data.name.trim()) {
      errs.name = 'Vui lòng nhập tên gói.';
    } else if (data.name.trim().length > 255) {
      errs.name = 'Tên tối đa 255 ký tự.';
    }

    if (data.price < 0) {
      errs.price = 'Giá phải >= 0.';
    } else if (data.price > 1_000_000_000) {
      errs.price = 'Giá tối đa 1 tỷ.';
    }

    if (data.durationDays <= 0) {
      errs.durationDays = 'Số ngày phải > 0.';
    } else if (data.durationDays > 3650) {
      errs.durationDays = 'Tối đa 10 năm (3650 ngày).';
    }

    if (data.maxItems <= 0) {
      errs.maxItems = 'Số mentions tối đa phải > 0.';
    } else if (data.maxItems > MAX_ITEMS) {
      errs.maxItems = `Tối đa ${MAX_ITEMS.toLocaleString('vi-VN')} mentions.`;
    }

    // maxSources — optional; nếu nhập thì phải hợp lệ
    if (data.maxSources != null) {
      if (data.maxSources < 0) {
        errs.maxSources = 'Số nguồn phải >= 0.';
      } else if (data.maxSources > maxSourcesCap) {
        errs.maxSources = `Tối đa ${maxSourcesCap} nguồn${
          isUnlimitedCode
            ? ' (theo cấu hình hệ thống).'
            : ` (hiện có ${MAX_SOURCES_LIMITED} platform).`
        }`;
      }
    }

    if (data.sortOrder < 0) {
      errs.sortOrder = 'Thứ tự phải >= 0.';
    }

    return errs;
  };

  const handleSave = async () => {
    const code = form.code.trim().toUpperCase();
    const candidate: UpsertScrapePackage = {
      ...form,
      code,
      currency: form.currency.trim().toUpperCase() || 'VND',
    };

    const errs = validate(candidate);
    if (Object.keys(errs).length > 0) {
      setFormErrors(errs);
      return;
    }
    setFormErrors({});

    setIsSaving(true);
    setErrorMessage('');
    try {
      const payload: UpsertScrapePackage = {
        ...candidate,
        name: candidate.name.trim(),
        description: candidate.description?.trim() ?? '',
        maxSources: candidate.maxSources ?? null,
      };
      if (editingId != null) {
        await adminApi.updateScrapePackage(editingId, payload);
      } else {
        await adminApi.createScrapePackage(payload);
      }
      setModalOpen(false);
      resetModalState();
      await loadPackages();
    } catch (error) {
      const message = extractApiError(error, 'Không thể lưu gói scrape.');
      // Phân loại lỗi server: nếu message chứa tên field cụ thể thì gán vào fieldErrors
      const fieldMatch = message.match(/'(code|price|durationDays|maxItems|maxSources)'/i);
      if (fieldMatch) {
        const key = fieldMatch[1].toLowerCase() as keyof UpsertScrapePackage;
        setFormErrors({ [key]: message });
      } else {
        setErrorMessage(message);
      }
    } finally {
      setIsSaving(false);
    }
  };

  const handleDelete = async (pkg: ScrapePackage) => {
    if (!window.confirm(`Xóa gói "${pkg.name}" (${pkg.code})? Hành động không thể hoàn tác.`)) return;

    setErrorMessage('');
    try {
      await adminApi.deleteScrapePackage(pkg.packageId);
      await loadPackages();
    } catch (error) {
      const message = extractApiError(error, 'Không thể xóa gói.');
      setErrorMessage(
        message.includes('tham chiếu') || message.includes('reference')
          ? `${message} — Vô hiệu hóa (isActive=false) nếu muốn giữ lịch sử.`
          : message
      );
    }
  };

  /** Helper: khi user sửa 1 field thì xóa lỗi của field đó luôn (UX tốt hơn). */
  const updateField = <K extends keyof UpsertScrapePackage>(
    key: K,
    value: UpsertScrapePackage[K],
  ) => {
    setForm((f) => ({ ...f, [key]: value }));
    if (formErrors[key]) {
      setFormErrors((e) => {
        const { [key]: _omit, ...rest } = e;
        return rest;
      });
    }
  };

  const activeCount = packages.filter((p) => p.isActive).length;

  // Field class helper — thêm border đỏ khi có lỗi
  const inputCls = (key: keyof UpsertScrapePackage) =>
    `w-full px-3 py-2 border rounded-lg text-sm ${
      formErrors[key] ? 'border-red-400 bg-red-50/30' : 'border-gray-200'
    }`;

  return (
    <AdminLayout>
      <div className="flex flex-col sm:flex-row sm:items-start sm:justify-between gap-4 mb-8">
        <div>
        <h2 className="text-2xl lg:text-3xl font-bold tracking-tight">Quản lý Gói Cào</h2>
          <p className="text-[#6b7280] text-sm mt-1">
            Cấu hình các gói cào dữ liệu (SCRAPE_PACKAGES) — {activeCount} đang bán / {packages.length} tổng
          </p>
        </div>
        <button
          type="button"
          onClick={openCreate}
          className="flex items-center gap-2 px-5 py-2.5 bg-[#ef4444] hover:bg-red-600 text-white rounded-lg text-sm font-semibold transition-colors shadow-sm shrink-0"
        >
          <Plus className="w-4 h-4" />
          Thêm gói
        </button>
      </div>

      {errorMessage && !modalOpen && (
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
        ) : packages.length === 0 ? (
          <div className="py-16 text-center text-gray-500 text-sm">
            Chưa có gói scrape nào. Nhấn &quot;Thêm gói&quot; để tạo gói đầu tiên.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-100 bg-blue-50/40">
                  <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Gói</th>
                  <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Giá</th>
                  <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Mentions</th>
                  <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Thời hạn</th>
                  <th className="text-left px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Trạng thái</th>
                  <th className="text-right px-6 py-4 text-xs font-semibold text-[#6b7280] uppercase">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {packages.map((pkg) => (
                  <tr key={pkg.packageId} className="hover:bg-gray-50/50">
                    <td className="px-6 py-4">
                      <div className="flex items-start gap-2">
                        <Package className="w-4 h-4 text-blue-600 shrink-0 mt-0.5" />
                        <div>
                          <p className="font-medium text-[#111827]">{pkg.name}</p>
                          <code className="text-xs text-[#6b7280]">{pkg.code}</code>
                          {pkg.description && (
                            <p className="text-xs text-[#9ca3af] mt-1 line-clamp-2">{pkg.description}</p>
                          )}
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4">
                      <span className="font-semibold text-[#111827]">
                        {formatPrice(pkg.price, pkg.currency)}
                      </span>
                      {pkg.activeOrdersCount > 0 && (
                        <p className="text-xs text-[#6b7280] mt-0.5">{pkg.activeOrdersCount} đơn</p>
                      )}
                    </td>
                    <td className="px-6 py-4 text-[#6b7280]">
                      <span className="font-medium">{pkg.maxItems.toLocaleString('vi-VN')}</span>
                      {pkg.maxSources != null && (
                        <span className="text-xs text-[#9ca3af] ml-1">/ {pkg.maxSources} nguồn</span>
                      )}
                    </td>
                    <td className="px-6 py-4 text-[#6b7280]">{pkg.durationDays} ngày</td>
                    <td className="px-6 py-4">
                      <span
                        className={`inline-flex px-2 py-0.5 rounded text-xs font-semibold ${
                          pkg.isActive
                            ? 'bg-green-50 text-green-700'
                            : 'bg-gray-100 text-gray-600'
                        }`}
                      >
                        {pkg.isActive ? 'Đang bán' : 'Ngừng bán'}
                      </span>
                    </td>
                    <td className="px-6 py-4">
                      <div className="flex items-center justify-end gap-2">
                        <button
                          type="button"
                          onClick={() => openEdit(pkg)}
                          className="p-2 text-gray-400 hover:text-[#111827] hover:bg-gray-100 rounded-lg"
                          title="Sửa"
                        >
                          <Pencil className="w-4 h-4" />
                        </button>
                        <button
                          type="button"
                          onClick={() => handleDelete(pkg)}
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
          <div className="bg-white rounded-xl shadow-xl w-full max-w-2xl p-6 max-h-[90vh] overflow-y-auto">
            <h3 className="text-lg font-semibold mb-1">
              {editingId != null ? 'Sửa gói scrape' : 'Thêm gói scrape'}
            </h3>
            {isUnlimitedCode && (
              <p className="text-xs text-[#FF7575] mb-4">
                Phát hiện mã gói toàn diện (FULL_*) — cho phép tối đa {MAX_SOURCES_UNLIMITED} nguồn.
              </p>
            )}
            {!isUnlimitedCode && (
              <p className="text-xs text-gray-500 mb-4">
                Trần số nguồn cho gói thường: <strong>{MAX_SOURCES_LIMITED}</strong> (số platform hiện có).
              </p>
            )}
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Mã gói (Code) *
                </label>
                <input
                  type="text"
                  value={form.code}
                  onChange={(e) => updateField('code', e.target.value.toUpperCase())}
                  placeholder="VD: PACK_100 hoặc FULL_UNLIMITED"
                  className={`${inputCls('code')} font-mono`}
                />
                {formErrors.code ? (
                  <p className="text-xs text-red-600 mt-1">{formErrors.code}</p>
                ) : (
                  <p className="text-xs text-[#9ca3af] mt-1">Mã duy nhất, uppercase tự động. Mã bắt đầu bằng &quot;FULL&quot; cho phép max nguồn cao.</p>
                )}
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Tên hiển thị *</label>
                <input
                  type="text"
                  value={form.name}
                  onChange={(e) => updateField('name', e.target.value)}
                  placeholder="VD: Gói Cơ bản"
                  className={inputCls('name')}
                />
                {formErrors.name && (
                  <p className="text-xs text-red-600 mt-1">{formErrors.name}</p>
                )}
              </div>
              <div className="md:col-span-2">
                <label className="block text-sm font-medium text-gray-700 mb-1">Mô tả</label>
                <textarea
                  value={form.description ?? ''}
                  onChange={(e) => updateField('description', e.target.value)}
                  rows={2}
                  placeholder="Mô tả ngắn về gói..."
                  className={inputCls('description')}
                />
                {formErrors.description && (
                  <p className="text-xs text-red-600 mt-1">{formErrors.description}</p>
                )}
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Giá *</label>
                <input
                  type="number"
                  min="0"
                  step="1000"
                  value={form.price}
                  onChange={(e) => updateField('price', Number(e.target.value) || 0)}
                  className={inputCls('price')}
                />
                {formErrors.price && (
                  <p className="text-xs text-red-600 mt-1">{formErrors.price}</p>
                )}
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Đơn vị tiền</label>
                <input
                  type="text"
                  maxLength={10}
                  value={form.currency}
                  onChange={(e) => updateField('currency', e.target.value.toUpperCase())}
                  placeholder="VND"
                  className={`${inputCls('currency')} font-mono`}
                />
                {formErrors.currency && (
                  <p className="text-xs text-red-600 mt-1">{formErrors.currency}</p>
                )}
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Mentions tối đa *</label>
                <input
                  type="number"
                  min="1"
                  value={form.maxItems}
                  onChange={(e) => updateField('maxItems', Number(e.target.value) || 0)}
                  className={inputCls('maxItems')}
                />
                {formErrors.maxItems && (
                  <p className="text-xs text-red-600 mt-1">{formErrors.maxItems}</p>
                )}
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">
                  Số nguồn (tùy chọn)
                </label>
                <input
                  type="number"
                  min="0"
                  max={maxSourcesCap}
                  value={form.maxSources ?? ''}
                  onChange={(e) =>
                    updateField(
                      'maxSources',
                      e.target.value === '' ? null : Number(e.target.value) || 0,
                    )
                  }
                  placeholder="Để trống = không giới hạn"
                  className={inputCls('maxSources')}
                />
                {formErrors.maxSources ? (
                  <p className="text-xs text-red-600 mt-1">{formErrors.maxSources}</p>
                ) : (
                  <p className="text-xs text-[#9ca3af] mt-1">
                    Tối đa <strong>{maxSourcesCap}</strong> cho {isUnlimitedCode ? 'gói FULL' : 'gói thường'}.
                  </p>
                )}
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Thời hạn (ngày) *</label>
                <input
                  type="number"
                  min="1"
                  value={form.durationDays}
                  onChange={(e) => updateField('durationDays', Number(e.target.value) || 0)}
                  className={inputCls('durationDays')}
                />
                {formErrors.durationDays && (
                  <p className="text-xs text-red-600 mt-1">{formErrors.durationDays}</p>
                )}
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Thứ tự hiển thị</label>
                <input
                  type="number"
                  value={form.sortOrder}
                  onChange={(e) => updateField('sortOrder', Number(e.target.value) || 0)}
                  className={inputCls('sortOrder')}
                />
                {formErrors.sortOrder && (
                  <p className="text-xs text-red-600 mt-1">{formErrors.sortOrder}</p>
                )}
              </div>
              <div className="md:col-span-2">
                <label className="flex items-center gap-2 text-sm cursor-pointer">
                  <button
                    type="button"
                    onClick={() => updateField('isActive', !form.isActive)}
                    className="text-gray-600 hover:text-[#111827]"
                  >
                    {form.isActive ? (
                      <ToggleRight className="w-6 h-6 text-green-600" />
                    ) : (
                      <ToggleLeft className="w-6 h-6 text-gray-400" />
                    )}
                  </button>
                  <span>Kích hoạt gói (cho phép mua)</span>
                </label>
              </div>
            </div>
            <div className="flex justify-end gap-3 mt-6">
              <button
                type="button"
                onClick={closeModal}
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

export default ScrapePackageManagement;