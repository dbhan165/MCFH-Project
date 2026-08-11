import { useEffect, useState } from 'react';
import {
  Eye,
  EyeOff,
  CreditCard,
  Plus,
  Pencil,
  Trash2,
  Star,
  PowerOff,
  Power,
} from 'lucide-react';
import { providerKeysApi, type PayOsKey } from '../../../api/providerKeysApi';
import { Modal, Field, ModalActions, RevealModal } from './BrevoKeyPanel';

const PayOsKeyPanel = () => {
  const [items, setItems] = useState<PayOsKey[]>([]);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [editingId, setEditingId] = useState<number | null>(null);
  const [showCreate, setShowCreate] = useState(false);
  const [reveal, setReveal] = useState<
    | { id: number; clientId: string; apiKey: string; checksumKey: string }
    | null
  >(null);

  const load = async () => {
    setLoading(true);
    setError('');
    try {
      setItems(await providerKeysApi.listPayOs());
    } catch (err: unknown) {
      const apiErr = err as { response?: { data?: { message?: string } } };
      setError(apiErr.response?.data?.message ?? 'Không thể tải danh sách PayOS key.');
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
          <h3 className="text-lg font-semibold text-[#111827]">Khóa API PayOS</h3>
          <p className="text-sm text-[#6b7280] mt-1">
            Quản lý thông tin tích hợp PayOS (ClientId + ApiKey + ChecksumKey).
            PayOsService sẽ dùng key có <strong>Đang chọn mặc định</strong> cho tạo payment link + verify webhook.
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
          <p className="text-sm text-[#6b7280]">Chưa có PayOS key nào. Bấm "Thêm" để bắt đầu.</p>
        </div>
      )}

      <div className="space-y-3">
        {items.map((item, i) => (
          <PayOsKeyRow
            key={item.payOsKeyId}
            item={item}
            index={i + 1}
            onEdit={() => setEditingId(item.payOsKeyId)}
            onDelete={async () => {
              if (!confirm(`Xóa PayOS key #${item.payOsKeyId}?`)) return;
              try {
                await providerKeysApi.deletePayOs(item.payOsKeyId);
                await load();
              } catch (err: unknown) {
                const apiErr = err as { response?: { data?: { message?: string } } };
                alert(apiErr.response?.data?.message ?? 'Không thể xóa.');
              }
            }}
            onSetDefault={async () => {
              try {
                await providerKeysApi.updatePayOs(item.payOsKeyId, { isDefault: true });
                await load();
              } catch (err: unknown) {
                const apiErr = err as { response?: { data?: { message?: string } } };
                alert(apiErr.response?.data?.message ?? 'Không thể đổi default.');
              }
            }}
            onToggleStatus={async () => {
              const newStatus = item.status === 'active' ? 'disabled' : 'active';
              try {
                await providerKeysApi.updatePayOs(item.payOsKeyId, { status: newStatus });
                await load();
              } catch (err: unknown) {
                const apiErr = err as { response?: { data?: { message?: string } } };
                alert(apiErr.response?.data?.message ?? 'Không thể đổi trạng thái.');
              }
            }}
            onReveal={async () => {
              try {
                const r = await providerKeysApi.revealPayOs(item.payOsKeyId);
                setReveal({ id: item.payOsKeyId, ...r });
              } catch (err: unknown) {
                const apiErr = err as { response?: { data?: { message?: string } } };
                alert(apiErr.response?.data?.message ?? 'Không thể reveal.');
              }
            }}
          />
        ))}
      </div>

      {showCreate && (
        <PayOsKeyModal
          onClose={() => setShowCreate(false)}
          onSaved={async () => {
            setShowCreate(false);
            await load();
          }}
        />
      )}
      {editingId !== null && (
        <PayOsKeyModal
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
          title="PayOS key"
          onClose={() => setReveal(null)}
          fields={[
            { label: 'Client ID', value: reveal.clientId },
            { label: 'API key', value: reveal.apiKey },
            { label: 'Checksum key', value: reveal.checksumKey },
          ]}
        />
      )}
    </div>
  );
};

const PayOsKeyRow = ({
  item,
  onEdit,
  onDelete,
  onSetDefault,
  onToggleStatus,
  onReveal,
}: {
  item: PayOsKey;
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
            <span
              className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-bold border ${
                item.environment === 'sandbox'
                  ? 'bg-amber-50 text-amber-700 border-amber-200'
                  : 'bg-emerald-50 text-emerald-700 border-emerald-200'
              }`}
            >
              <CreditCard className="w-3.5 h-3.5" />
              {item.environment === 'sandbox' ? 'Sandbox' : 'Live'}
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
              #{item.payOsKeyId}
            </span>
          </div>
          <div className="mt-2 text-sm">
            <span className="text-xs font-bold text-[#6b7280] uppercase tracking-wider">Client ID: </span>
            <code className="px-2 py-0.5 bg-gray-100 border border-gray-200 rounded text-xs font-mono text-[#111827]">
              {item.clientId}
            </code>
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
          <div className="mt-2 flex items-center gap-2">
            <span className="text-xs font-bold text-[#6b7280] uppercase tracking-wider">Checksum:</span>
            <code className="px-3 py-1.5 bg-gray-100 border border-gray-200 rounded-md text-xs font-mono text-[#111827]">
              {item.checksumKeyMasked}
            </code>
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

const PayOsKeyModal = ({
  editId,
  onClose,
  onSaved,
}: {
  editId?: number;
  onClose: () => void;
  onSaved: () => void;
}) => {
  const isEdit = editId !== undefined;
  const [clientId, setClientId] = useState('');
  const [apiKey, setApiKey] = useState('');
  const [checksumKey, setChecksumKey] = useState('');
  const [environment, setEnvironment] = useState<'sandbox' | 'live'>('live');
  const [isDefault, setIsDefault] = useState(false);
  const [note, setNote] = useState('');
  const [showKey, setShowKey] = useState(false);
  const [showChecksum, setShowChecksum] = useState(false);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (editId === undefined) return;
    void (async () => {
      try {
        const list = await providerKeysApi.listPayOs();
        const k = list.find((it) => it.payOsKeyId === editId);
        if (k) {
          setClientId(k.clientId);
          setEnvironment((k.environment as 'sandbox' | 'live') || 'live');
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
    if (!isEdit && (!clientId.trim() || !apiKey.trim() || !checksumKey.trim())) {
      setError('Client ID, API key và Checksum key là bắt buộc khi tạo mới.');
      return;
    }
    setSaving(true);
    try {
      if (isEdit) {
        const payload: Record<string, unknown> = {
          environment,
          isDefault,
          note: note || null,
        };
        if (clientId.trim()) payload.clientId = clientId.trim();
        if (apiKey.trim()) payload.apiKey = apiKey.trim();
        if (checksumKey.trim()) payload.checksumKey = checksumKey.trim();
        await providerKeysApi.updatePayOs(editId!, payload as never);
      } else {
        await providerKeysApi.createPayOs({
          clientId: clientId.trim(),
          apiKey: apiKey.trim(),
          checksumKey: checksumKey.trim(),
          environment,
          isDefault,
          note: note || undefined,
        });
      }
      onSaved();
    } catch (err: unknown) {
      const apiErr = err as { response?: { data?: { message?: string } } };
      setError(apiErr.response?.data?.message ?? 'Không thể lưu PayOS key.');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Modal onClose={onClose} title={isEdit ? `Sửa PayOS key #${editId}` : 'Thêm key mới'}>
      {error && (
        <div className="mb-4 bg-red-50 border border-red-200 text-red-700 text-sm rounded-lg px-4 py-3">
          {error}
        </div>
      )}

      <Field label="Environment">
        <select
          value={environment}
          onChange={(e) => setEnvironment(e.target.value as 'sandbox' | 'live')}
          className="w-full bg-gray-50 border border-gray-200 rounded-lg px-3 py-2 text-sm"
        >
          <option value="live">Live (production)</option>
          <option value="sandbox">Sandbox (test)</option>
        </select>
      </Field>

      <Field label={isEdit ? 'Client ID (để trống nếu giữ)' : 'Client ID'}>
        <input
          value={clientId}
          onChange={(e) => setClientId(e.target.value)}
          placeholder="xxx-xxx-xxx-xxx-xxx"
          className="w-full bg-gray-50 border border-gray-200 rounded-lg px-3 py-2 text-sm font-mono"
        />
      </Field>

      <Field label={isEdit ? 'API key mới (để trống nếu giữ)' : 'API key'}>
        <div className="relative">
          <input
            type={showKey ? 'text' : 'password'}
            value={apiKey}
            onChange={(e) => setApiKey(e.target.value)}
            placeholder={isEdit ? '•••••••• (giữ nguyên key cũ)' : 'PayOS API key'}
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

      <Field label={isEdit ? 'Checksum key mới (để trống nếu giữ)' : 'Checksum key'}>
        <div className="relative">
          <input
            type={showChecksum ? 'text' : 'password'}
            value={checksumKey}
            onChange={(e) => setChecksumKey(e.target.value)}
            placeholder={isEdit ? '•••••••• (giữ nguyên key cũ)' : 'PayOS Checksum key'}
            className="w-full bg-gray-50 border border-gray-200 rounded-lg px-3 py-2 pr-10 text-sm font-mono"
          />
          <button
            type="button"
            onClick={() => setShowChecksum((v) => !v)}
            className="absolute right-2 top-1/2 -translate-y-1/2 p-1 text-gray-400 hover:text-[#111827]"
          >
            {showChecksum ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
          </button>
        </div>
      </Field>

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
        Đặt làm key mặc định (PayOsService sẽ dùng key này)
      </label>

      <ModalActions
        saving={saving}
        onCancel={onClose}
        onSave={() => void handleSave()}
      />
    </Modal>
  );
};

export default PayOsKeyPanel;
