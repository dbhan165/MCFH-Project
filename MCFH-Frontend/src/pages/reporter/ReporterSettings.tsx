import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Bell, CheckCircle2, ClipboardList, Save, User } from 'lucide-react';
import ReporterLayout from '../../components/reporter/ReporterLayout';

const STORAGE_KEY = 'mcfh_reporter_settings';

export interface ReporterSettingsState {
  notifyAssigned: boolean;
  notifyClientNote: boolean;
  compactCards: boolean;
}

const DEFAULT_SETTINGS: ReporterSettingsState = {
  notifyAssigned: true,
  notifyClientNote: true,
  compactCards: false,
};

export function loadReporterSettings(): ReporterSettingsState {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) return { ...DEFAULT_SETTINGS };
    return { ...DEFAULT_SETTINGS, ...(JSON.parse(raw) as Partial<ReporterSettingsState>) };
  } catch {
    return { ...DEFAULT_SETTINGS };
  }
}

function saveReporterSettings(settings: ReporterSettingsState) {
  localStorage.setItem(STORAGE_KEY, JSON.stringify(settings));
}

function Toggle({
  enabled,
  onChange,
  label,
  description,
}: {
  enabled: boolean;
  onChange: (v: boolean) => void;
  label: string;
  description: string;
}) {
  return (
    <label className="flex items-start justify-between gap-4 py-4 border-b border-stone-100 last:border-0 cursor-pointer">
      <div className="min-w-0">
        <p className="text-sm font-semibold text-[#111827]">{label}</p>
        <p className="text-xs text-stone-500 mt-1">{description}</p>
      </div>
      <button
        type="button"
        role="switch"
        aria-checked={enabled}
        onClick={() => onChange(!enabled)}
        className={`relative w-11 h-6 rounded-full shrink-0 transition-colors ${
          enabled ? 'bg-[#e11d48]' : 'bg-stone-300'
        }`}
      >
        <span
          className={`absolute top-0.5 left-0.5 w-5 h-5 bg-white rounded-full shadow transition-transform ${
            enabled ? 'translate-x-5' : 'translate-x-0'
          }`}
        />
      </button>
    </label>
  );
}

export default function ReporterSettings() {
  const [settings, setSettings] = useState<ReporterSettingsState>(DEFAULT_SETTINGS);
  const [saved, setSaved] = useState(false);

  useEffect(() => {
    setSettings(loadReporterSettings());
  }, []);

  const update = <K extends keyof ReporterSettingsState>(key: K, value: ReporterSettingsState[K]) => {
    setSettings((prev) => ({ ...prev, [key]: value }));
    setSaved(false);
  };

  const handleSave = () => {
    saveReporterSettings(settings);
    setSaved(true);
    window.setTimeout(() => setSaved(false), 2500);
  };

  return (
    <ReporterLayout>
      <div className="mb-8">
        <h2 className="text-xl lg:text-2xl font-bold text-[#111827]">Settings</h2>
        <p className="text-sm text-[#78716c] mt-1">Tùy chọn làm việc của Reporter</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div className="lg:col-span-2 space-y-6">
          <div className="bg-white border border-stone-200 rounded-xl p-6 shadow-sm">
            <h3 className="text-lg font-bold text-[#111827] mb-1 flex items-center gap-2">
              <Bell className="w-5 h-5" /> Hiển thị trên Tasks
            </h3>
            <p className="text-xs text-stone-500 mb-2">
              Thông báo chuông vẫn gửi từ hệ thống. Các tùy chọn dưới đây chỉ ảnh hưởng UI Tasks.
            </p>
            <Toggle
              enabled={settings.notifyClientNote}
              onChange={(v) => update('notifyClientNote', v)}
              label="Hiện ghi chú cần sửa trên thẻ"
              description="Hiện nội dung khách gửi kèm khi yêu cầu chỉnh sửa."
            />
            <Toggle
              enabled={settings.compactCards}
              onChange={(v) => update('compactCards', v)}
              label="Thẻ gọn"
              description="Rút gọn khoảng cách thẻ trên bảng Tasks."
            />
          </div>

          <div className="bg-white border border-stone-200 rounded-xl p-6 shadow-sm">
            <h3 className="text-lg font-bold text-[#111827] mb-1 flex items-center gap-2">
              <ClipboardList className="w-5 h-5" /> Gợi ý
            </h3>
            <p className="text-sm text-stone-500">
              Khi khách gửi yêu cầu chỉnh sửa, chuông thông báo sẽ báo cho Reporter. Bấm thông báo để mở chi tiết đơn.
            </p>
          </div>

          <div className="flex items-center gap-3">
            <button
              type="button"
              onClick={handleSave}
              className="inline-flex items-center gap-2 px-5 py-2.5 bg-[#e11d48] hover:bg-[#be123c] text-white rounded-lg text-sm font-semibold"
            >
              <Save className="w-4 h-4" />
              Lưu cài đặt
            </button>
            {saved && (
              <span className="inline-flex items-center gap-1.5 text-sm text-emerald-700">
                <CheckCircle2 className="w-4 h-4" /> Đã lưu
              </span>
            )}
          </div>
        </div>

        <div className="bg-white border border-stone-200 rounded-xl p-6 shadow-sm h-fit">
          <h3 className="text-lg font-bold text-[#111827] mb-2 flex items-center gap-2">
            <User className="w-5 h-5" /> Tài khoản
          </h3>
          <p className="text-sm text-stone-500 mb-4">
            Đổi tên, ảnh đại diện hoặc mật khẩu ở trang Profile.
          </p>
          <Link
            to="/reporter/profile"
            className="inline-flex items-center justify-center w-full py-2.5 border-2 border-[#e11d48] text-[#e11d48] hover:bg-rose-50 rounded-lg text-sm font-semibold"
          >
            Mở Profile
          </Link>
        </div>
      </div>
    </ReporterLayout>
  );
}
