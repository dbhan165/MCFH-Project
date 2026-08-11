import { useEffect, useState } from 'react';
import {
  Eye,
  EyeOff,
  Mail,
  Plus,
  Pencil,
  Trash2,
  Star,
  PowerOff,
  Power,
  X,
} from 'lucide-react';
import { providerKeysApi, type BrevoKey } from '../../../api/providerKeysApi';

const BrevoKeyPanel = () => {
  const [items, setItems] = useState<BrevoKey[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [editingId, setEditingId] = useState<number | null>(null);
  const [showCreate, setShowCreate] = useState(false);
  const [reveal, setReveal] = useState<{ id: number; apiKey: string; smtpLogin?: string | null } | null>(null);

  const load = async () => {
    setLoading(true);
    setError('');
    try {
      setItems(await providerKeysApi.listBrevo());
    } catch (err: unknown) {
      const apiErr = err as { response?: { data?: { message?: string } } };
      setError(apiErr.response?.data?.message ?? 'Không thể tải danh sách Brevo key.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  return (
    <div className="space-y-6">
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <h3 className="text-lg font-semibold text-[#111827]">Khóa API Brevo</h3>
          <p className="text-sm text-[#6b7280] mt-1">
            Quản lý key Brevo dùng để gửi email (xkeysib-... API hoặc xsmtpsib-... SMTP).
            EmailService sẽ dùng key có <strong>Đang chọn mặc định</strong>.
          </p>
        </div>
        <button
          type="button"
          onClick={() => setShowCreate(true)}
          className="inline-flex items-center gap-2 px-4 py-2.5 bg-[#ef4444] hover:bg-red-600 text-white rounded-lg text-sm font-semibold transition-colors"
        >
          <Plus className="w-4 h-4" />
          Thêm
        </button>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 text-red-700 text-sm rounded-lg px-4 py-3">
          {error}
        </div>
      )}

      {loading && <p className="text-sm text-[#6b7280]">Đang tải...</p>}

      {!loading && items.length === 0 && (
        <div className="border border-dashed border-gray-300 rounded-xl p-8 text-center bg-gray-50">
          <p className="text-sm text-[#6b7280]">Chưa có Brevo key nào. Bấm "Thêm" để bắt đầu.</p>
        </div>
      )}

      <div className="space-y-3">
        {items.map((item, i) => (
          <BrevoKeyRow
            key={item.brevoKeyId}
            item={item}
            index={i + 1}
            onEdit={() => setEditingId(item.brevoKeyId)}
            onDelete={async () => {
              if (!confirm(`Xóa Brevo key #${item.brevoKeyId}?`)) return;
              try {
                await providerKeysApi.deleteBrevo(item.brevoKeyId);
                await load();
              } catch (err: unknown) {
                const apiErr = err as { response?: { data?: { message?: string } } };
                alert(apiErr.response?.data?.message ?? 'Không thể xóa.');
              }
            }}
            onSetDefault={async () => {
              try {
                await providerKeysApi.updateBrevo(item.brevoKeyId, { isDefault: true });
                await load();
              } catch (err: unknown) {
                const apiErr = err as { response?: { data?: { message?: string } } };
                alert(apiErr.response?.data?.message ?? 'Không thể đổi default.');
              }
            }}
            onToggleStatus={async () => {
              const newStatus = item.status === 'active' ? 'disabled' : 'active';
              try {
                await providerKeysApi.updateBrevo(item.brevoKeyId, { status: newStatus });
                await load();
              } catch (err: unknown) {
                const apiErr = err as { response?: { data?: { message?: string } } };
                alert(apiErr.response?.data?.message ?? 'Không thể đổi trạng thái.');
              }
            }}
            onReveal={async () => {
              try {
                const r = await providerKeysApi.revealBrevo(item.brevoKeyId);
                setReveal({ id: item.brevoKeyId, apiKey: r.apiKey, smtpLogin: r.smtpLogin });
              } catch (err: unknown) {
                const apiErr = err as { response?: { data?: { message?: string } } };
                alert(apiErr.response?.data?.message ?? 'Không thể reveal.');
              }
            }}
          />
        ))}
      </div>

      {showCreate && (
        <BrevoKeyModal
          onClose={() => setShowCreate(false)}
          onSaved={async () => {
            setShowCreate(false);
            await load();
          }}
        />
      )}
      {editingId !== null && (
        <BrevoKeyModal
          editId={editingId}
          onClose={() => setEditingId(null)}
          onSaved={async () => {
            setEditingId(null);
            await load();
          }}
        />
      )}

      {reveal && (
        <RevealModal
          title="Brevo key"
          onClose={() => setReveal(null)}
          fields={[
            { label: 'API key', value: reveal.apiKey },
            ...(reveal.smtpLogin
              ? [{ label: 'SMTP login', value: reveal.smtpLogin }]
              : []),
          ]}
        />
      )}
    </div>
  );
};

const BrevoKeyRow = ({
  item,
  onEdit,
  onDelete,
  onSetDefault,
  onToggleStatus,
  onReveal,
}: {
  item: BrevoKey;
  index: number;
  onEdit: () => void;
  onDelete: () => void;
  onSetDefault: () => void;
  onToggleStatus: () => void;
  onReveal: () => void;
}) => {
  return (
    <div className="border border-gray-200 rounded-xl bg-white p-5 hover:shadow-sm transition-shadow">
      <div className="flex flex-col lg:flex-row lg:items-start lg:justify-between gap-4">
        <div className="min-w-0 flex-1">
          <div className="flex items-center gap-2 flex-wrap">
            <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-bold bg-blue-50 text-blue-700 border border-blue-100">
              <Mail className="w-3.5 h-3.5" />
              {item.keyType === 'api' ? 'Brevo API' : 'Brevo SMTP'}
            </span>
            {item.isDefault && (
              <span className="inline-flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-bold bg-amber-50 text-amber-700 border border-amber-200">
                <Star className="w-3.5 h-3.5 fill-amber-500 text-amber-500" />
                Mặc định
              </span>
            )}
            <span
              className={`inline-flex items-center px-2.5 py-1 rounded-md text-xs font-bold border ${
                item.status === 'active'
                  ? 'bg-emerald-50 text-emerald-700 border-emerald-200'
                  : 'bg-gray-100 text-gray-600 border-gray-200'
              }`}
            >
              {item.status === 'active' ? 'Hoạt động' : 'Vô hiệu'}
            </span>
            <span className="inline-flex items-center px-2.5 py-1 rounded-md text-xs font-bold bg-slate-100 text-slate-700 border border-slate-200">
              #{item.brevoKeyId}
            </span>
          </div>
          <div className="mt-2 grid grid-cols-1 sm:grid-cols-2 gap-x-6 gap-y-1 text-sm">
            <div>
              <span className="text-[#6b7280]">From: </span>
              <span className="font-medium text-[#111827]">
                {item.fromName ? `${item.fromName} <${item.fromAddress}>` : (item.fromAddress ?? '—')}
              </span>
            </div>
            <div>
              <span className="text-[#6b7280]">SMTP login: </span>
              <span className="font-mono text-[#111827] text-xs">{item.smtpLogin ?? '—'}</span>
            </div>
          </div>
          <div className="mt-3 flex items-center gap-2">
            <span className="text-xs font-bold text-[#6b7280] uppercase tracking-wider">API Key:</span>
            <code className="px-3 py-1.5 bg-gray-100 border border-gray-200 rounded-md text-xs font-mono text-[#111827]">
              {item.apiKeyMasked}
            </code>
            <button
              type="button"
              onClick={onReveal}
              className="text-xs font-bold text-[#ef4444] hover:underline"
            >
              Hiện
            </button>
          </div>
          {item.note && (
            <p className="mt-2 text-xs text-[#6b7280] italic">{item.note}</p>
          )}
        </div>

        <div className="flex flex-wrap gap-2 lg:flex-col lg:items-end">
          {!item.isDefault && item.status === 'active' && (
            <button
              type="button"
              onClick={onSetDefault}
              className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-bold rounded-lg bg-amber-50 text-amber-700 hover:bg-amber-100 border border-amber-200"
            >
              <Star className="w-3.5 h-3.5" />
              Đặt mặc định
            </button>
          )}
          <button
            type="button"
            onClick={onToggleStatus}
            className={`inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-bold rounded-lg border ${
              item.status === 'active'
                ? 'bg-gray-50 text-gray-700 hover:bg-gray-100 border-gray-200'
                : 'bg-emerald-50 text-emerald-700 hover:bg-emerald-100 border-emerald-200'
            }`}
          >
            {item.status === 'active' ? <PowerOff className="w-3.5 h-3.5" /> : <Power className="w-3.5 h-3.5" />}
            {item.status === 'active' ? 'Vô hiệu' : 'Kích hoạt'}
          </button>
          <button
            type="button"
            onClick={onEdit}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-bold rounded-lg bg-white border border-gray-200 hover:bg-gray-50 text-[#111827]"
          >
            <Pencil className="w-3.5 h-3.5" />
            Sửa
          </button>
          <button
            type="button"
            onClick={onDelete}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-xs font-bold rounded-lg bg-white border border-red-200 text-red-600 hover:bg-red-50"
          >
            <Trash2 className="w-3.5 h-3.5" />
            Xóa
          </button>
        </div>
      </div>
    </div>
  );
};

const BrevoKeyModal = ({
  editId,
  onClose,
  onSaved,
}: {
  editId?: number;
  onClose: () => void;
  onSaved: () => void;
}) => {
  const isEdit = editId !== undefined;
  const [keyType, setKeyType] = useState<'api' | 'smtp'>('api');
  const [apiKey, setApiKey] = useState('');
  const [smtpLogin, setSmtpLogin] = useState('');
  const [fromAddress, setFromAddress] = useState('');
  const [fromName, setFromName] = useState('');
  const [isDefault, setIsDefault] = useState(false);
  const [note, setNote] = useState('');
  const [showKey, setShowKey] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (editId === undefined) return;
    void (async () => {
      try {
        const list = await providerKeysApi.listBrevo();
        const k = list.find((it) => it.brevoKeyId === editId);
        if (k) {
          setKeyType(k.keyType);
          setSmtpLogin(k.smtpLogin ?? '');
          setFromAddress(k.fromAddress ?? '');
          setFromName(k.fromName ?? '');
          setIsDefault(k.isDefault);
          setNote(k.note ?? '');
        }
      } catch {
        // no-op
      }
    })();
  }, [editId]);

  const handleSave = async () => {
    setError('');
    if (!isEdit && !apiKey.trim()) {
      setError('API key là bắt buộc khi tạo mới.');
      return;
    }
    setSaving(true);
    try {
      if (isEdit) {
        const payload: Record<string, unknown> = {
          smtpLogin: smtpLogin || null,
          fromAddress: fromAddress || null,
          fromName: fromName || null,
          isDefault,
          note: note || null,
        };
        if (apiKey.trim()) payload.apiKey = apiKey.trim();
        await providerKeysApi.updateBrevo(editId!, payload as never);
      } else {
        await providerKeysApi.createBrevo({
          keyType,
          apiKey: apiKey.trim(),
          smtpLogin: smtpLogin || undefined,
          fromAddress: fromAddress || undefined,
          fromName: fromName || undefined,
          isDefault,
          note: note || undefined,
        });
      }
      onSaved();
    } catch (err: unknown) {
      const apiErr = err as { response?: { data?: { message?: string } } };
      setError(apiErr.response?.data?.message ?? 'Không thể lưu Brevo key.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal onClose={onClose} title={isEdit ? `Sửa Brevo key #${editId}` : 'Thêm key mới'}>
      {error && (
        <div className="mb-4 bg-red-50 border border-red-200 text-red-700 text-sm rounded-lg px-4 py-3">
          {error}
        </div>
      )}

      {!isEdit && (
        <Field label="Loại key">
          <select
            value={keyType}
            onChange={(e) => setKeyType(e.target.value as 'api' | 'smtp')}
            className="w-full bg-gray-50 border border-gray-200 rounded-lg px-3 py-2 text-sm"
          >
            <option value="api">Brevo REST API (xkeysib-...)</option>
            <option value="smtp">SMTP login (xsmtpsib-...)</option>
          </select>
        </Field>
      )}

      <Field label={isEdit ? 'API key mới (để trống nếu giữ)' : 'API key'}>
        <div className="relative">
          <input
            type={showKey ? 'text' : 'password'}
            value={apiKey}
            onChange={(e) => setApiKey(e.target.value)}
            placeholder={isEdit ? '******** (giữ nguyên key cũ)' : 'xkeysib-... hoặc xsmtpsib-...'}
            className="w-full bg-gray-50 border border-gray-200 rounded-lg px-3 py-2 pr-10 text-sm font-mono"
          />
          <button
            type="button"
            onClick={() => setShowKey((v) => !v)}
            className="absolute right-2 top-1/2 -translate-y-1/2 p-1 text-gray-400 hover:text-[#111827]"
          >
            {showKey ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
          </button>
        </div>
      </Field>

      <Field label="SMTP login (chỉ Brevo SMTP)">
        <input
          value={smtpLogin}
          onChange={(e) => setSmtpLogin(e.target.value)}
          placeholder="your-email@example.com"
          className="w-full bg-gray-50 border border-gray-200 rounded-lg px-3 py-2 text-sm font-mono"
        />
      </Field>

      <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
        <Field label="From — Địa chỉ">
          <input
            value={fromAddress}
            onChange={(e) => setFromAddress(e.target.value)}
            placeholder="no-reply@mcfh.io.vn"
            className="w-full bg-gray-50 border border-gray-200 rounded-lg px-3 py-2 text-sm font-mono"
          />
        </Field>
        <Field label="From — Tên hiển thị">
          <input
            value={fromName}
            onChange={(e) => setFromName(e.target.value)}
            placeholder="MCFH System Hub"
            className="w-full bg-gray-50 border border-gray-200 rounded-lg px-3 py-2 text-sm"
          />
        </Field>
      </div>

      <Field label="Ghi chú (tùy chọn)">
        <textarea
          rows={2}
          value={note}
          onChange={(e) => setNote(e.target.value)}
          className="w-full bg-gray-50 border border-gray-200 rounded-lg px-3 py-2 text-sm"
        />
      </Field>

      <label className="flex items-center gap-2 text-sm text-[#111827] cursor-pointer">
        <input
          type="checkbox"
          checked={isDefault}
          onChange={(e) => setIsDefault(e.target.checked)}
          className="w-4 h-4 accent-[#ef4444]"
        />
        Đặt làm key mặc định (EmailService sẽ dùng key này)
      </label>

      <ModalActions
        saving={saving}
        onCancel={onClose}
        onSave={() => void handleSave()}
      />
    </Modal>
  );
};

// ========= Shared modal primitives =========

interface ModalProps {
  onClose: () => void;
  title: string;
  children: React.ReactNode;
}

const Modal = ({ onClose, title, children }: ModalProps) => (
  <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 overflow-y-auto">
    <div className="bg-white rounded-2xl shadow-2xl max-w-2xl w-full max-h-[90vh] overflow-y-auto">
      <div className="sticky top-0 bg-white border-b border-gray-100 px-6 py-4 flex items-center justify-between">
        <h3 className="text-lg font-semibold text-[#111827]">{title}</h3>
        <button
          type="button"
          onClick={onClose}
          className="p-1.5 hover:bg-gray-100 rounded-lg"
          aria-label="Đóng"
        >
          <X className="w-5 h-5 text-gray-500" />
        </button>
      </div>
      <div className="px-6 py-5 space-y-4">{children}</div>
    </div>
  </div>
);

const Field = ({ label, children }: { label: string; children: React.ReactNode }) => (
  <div>
    <label className="block text-xs font-semibold text-[#6b7280] uppercase tracking-wider mb-1.5">
      {label}
    </label>
    {children}
  </div>
);

const ModalActions = ({
  saving,
  onCancel,
  onSave,
}: {
  saving: boolean;
  onCancel: () => void;
  onSave: () => void;
}) => (
  <div className="flex justify-end gap-3 pt-4 border-t border-gray-100">
    <button
      type="button"
      onClick={onCancel}
      disabled={saving}
      className="px-4 py-2 bg-white border border-gray-200 hover:bg-gray-50 text-[#111827] rounded-lg text-sm font-semibold"
    >
      Hủy
    </button>
    <button
      type="button"
      onClick={onSave}
      disabled={saving}
      className="px-4 py-2 bg-[#ef4444] hover:bg-red-600 disabled:opacity-50 text-white rounded-lg text-sm font-semibold"
    >
      {saving ? 'Đang lưu...' : 'Lưu'}
    </button>
  </div>
);

const RevealModal = ({
  title,
  onClose,
  fields,
}: {
  title: string;
  onClose: () => void;
  fields: { label: string; value: string }[];
}) => (
  <Modal onClose={onClose} title={`${title} — Giá trị đầy đủ`}>
    <p className="text-sm text-amber-700 bg-amber-50 border border-amber-200 rounded-lg px-4 py-3">
      ⚠️ Giá trị đầy đủ chỉ hiển thị ở đây. Hãy sao chép rồi đóng — không chia sẻ ra ngoài.
    </p>
    {fields.map((f, i) => (
      <Field key={i} label={f.label}>
        <div className="flex gap-2">
          <input
            readOnly
            value={f.value}
            className="flex-1 bg-gray-100 border border-gray-200 rounded-lg px-3 py-2 text-xs font-mono"
          />
          <button
            type="button"
            onClick={() => {
              void navigator.clipboard.writeText(f.value);
            }}
            className="px-3 py-2 bg-gray-50 border border-gray-200 hover:bg-gray-100 text-xs font-semibold rounded-lg"
          >
            Copy
          </button>
        </div>
      </Field>
    ))}
  </Modal>
);

// Re-export so PayOsKeyPanel can use the same primitives.
export { Modal, Field, ModalActions, RevealModal };
export default BrevoKeyPanel;
