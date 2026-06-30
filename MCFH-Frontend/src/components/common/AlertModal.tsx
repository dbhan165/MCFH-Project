import { CheckCircle2, Info, X, XCircle } from 'lucide-react';
import { useEffect } from 'react';

export type AlertModalType = 'success' | 'error' | 'info' | 'warning';

interface AlertModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  message: string;
  confirmText?: string;
  type?: AlertModalType;
}

const typeStyles: Record<
  AlertModalType,
  { iconBg: string; iconColor: string; btn: string; Icon: typeof Info }
> = {
  success: {
    iconBg: 'bg-emerald-500/10',
    iconColor: 'text-emerald-400',
    btn: 'bg-emerald-500 hover:bg-emerald-600 shadow-[0_4px_14px_0_rgba(16,185,129,0.35)]',
    Icon: CheckCircle2,
  },
  error: {
    iconBg: 'bg-red-500/10',
    iconColor: 'text-red-400',
    btn: 'bg-red-500 hover:bg-red-600 shadow-[0_4px_14px_0_rgba(239,68,68,0.39)]',
    Icon: XCircle,
  },
  info: {
    iconBg: 'bg-blue-500/10',
    iconColor: 'text-blue-400',
    btn: 'bg-blue-500 hover:bg-blue-600 shadow-[0_4px_14px_0_rgba(59,130,246,0.39)]',
    Icon: Info,
  },
  warning: {
    iconBg: 'bg-yellow-500/10',
    iconColor: 'text-yellow-400',
    btn: 'bg-yellow-500 hover:bg-yellow-600 shadow-[0_4px_14px_0_rgba(234,179,8,0.39)]',
    Icon: Info,
  },
};

const AlertModal = ({
  isOpen,
  onClose,
  title,
  message,
  confirmText = 'Đã hiểu',
  type = 'info',
}: AlertModalProps) => {
  useEffect(() => {
    if (isOpen) document.body.style.overflow = 'hidden';
    else document.body.style.overflow = 'unset';
    return () => {
      document.body.style.overflow = 'unset';
    };
  }, [isOpen]);

  if (!isOpen) return null;

  const style = typeStyles[type];
  const Icon = style.Icon;

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
      <div
        className="absolute inset-0 bg-black/60 backdrop-blur-sm"
        onClick={onClose}
      />
      <div className="relative bg-[#151B2B] border border-white/10 rounded-2xl w-full max-w-md shadow-2xl overflow-hidden animate-in zoom-in-95 duration-200">
        <button
          type="button"
          onClick={onClose}
          className="absolute top-4 right-4 p-1.5 text-gray-400 hover:text-white hover:bg-white/10 rounded-lg transition-colors"
        >
          <X size={20} />
        </button>

        <div className="p-6">
          <div className="flex items-start gap-4">
            <div className={`shrink-0 w-12 h-12 rounded-full flex items-center justify-center ${style.iconBg}`}>
              <Icon className={style.iconColor} size={24} />
            </div>
            <div className="mt-1 pr-6">
              <h3 className="text-xl font-bold text-white mb-2">{title}</h3>
              <p className="text-sm text-gray-400 leading-relaxed whitespace-pre-line">{message}</p>
            </div>
          </div>
        </div>

        <div className="px-6 py-4 bg-[#0A101D] border-t border-white/5 flex justify-end">
          <button
            type="button"
            onClick={onClose}
            className={`px-5 py-2.5 text-sm font-bold text-white rounded-lg transition-all ${style.btn}`}
          >
            {confirmText}
          </button>
        </div>
      </div>
    </div>
  );
};

export default AlertModal;
