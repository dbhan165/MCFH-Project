import { AlertTriangle, X } from 'lucide-react';
import { useEffect } from 'react';

// Định nghĩa khung xương (Props) để các trang khác truyền dữ liệu vào
interface ConfirmModalProps {
  isOpen: boolean;                 // Cờ đóng/mở
  onClose: () => void;             // Hàm gọi khi bấm Hủy/Tắt
  onConfirm: () => void;           // Hàm gọi khi bấm Đồng ý
  title: string;                   // Tiêu đề (VD: Xác nhận xóa)
  message: string;                 // Lời nhắn (VD: Bạn có chắc chắn muốn xóa...)
  confirmText?: string;            // Chữ trên nút Đồng ý (Mặc định: Xác nhận)
  cancelText?: string;             // Chữ trên nút Hủy (Mặc định: Hủy bỏ)
  type?: 'danger' | 'warning';     // Loại Modal để đổi màu (đỏ hoặc vàng)
  isLoading?: boolean;             // Hiển thị vòng xoay khi đang gọi API
}

const ConfirmModal = ({
  isOpen,
  onClose,
  onConfirm,
  title,
  message,
  confirmText = 'Xác nhận',
  cancelText = 'Hủy bỏ',
  type = 'danger',
  isLoading = false
}: ConfirmModalProps) => {
  
  // Chặn scroll chuột ở nền khi Modal đang mở
  useEffect(() => {
    if (isOpen) {
      document.body.style.overflow = 'hidden';
    } else {
      document.body.style.overflow = 'unset';
    }
    return () => { document.body.style.overflow = 'unset'; };
  }, [isOpen]);

  if (!isOpen) return null;

  // Setup màu sắc tùy theo mức độ nguy hiểm của hành động
  const isDanger = type === 'danger';
  const iconBgColor = isDanger ? 'bg-red-500/10' : 'bg-yellow-500/10';
  const iconColor = isDanger ? 'text-red-500' : 'text-yellow-500';
  const btnColor = isDanger 
    ? 'bg-red-500 hover:bg-red-600 shadow-[0_4px_14px_0_rgba(239,68,68,0.39)]' 
    : 'bg-yellow-500 hover:bg-yellow-600 shadow-[0_4px_14px_0_rgba(234,179,8,0.39)]';

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
      {/* Lớp nền mờ (Backdrop) */}
      <div 
        className="absolute inset-0 bg-black/60 backdrop-blur-sm transition-opacity"
        onClick={!isLoading ? onClose : undefined}
      ></div>

      {/* Khung Modal chính */}
      <div className="relative bg-[#151B2B] border border-white/10 rounded-2xl w-full max-w-md shadow-2xl overflow-hidden animate-in zoom-in-95 duration-200">
        
        {/* Nút X trên góc */}
        <button 
          onClick={onClose}
          disabled={isLoading}
          className="absolute top-4 right-4 p-1.5 text-gray-400 hover:text-white hover:bg-white/10 rounded-lg transition-colors disabled:opacity-50"
        >
          <X size={20} />
        </button>

        <div className="p-6">
          <div className="flex items-start gap-4">
            {/* Icon cảnh báo */}
            <div className={`shrink-0 w-12 h-12 rounded-full flex items-center justify-center ${iconBgColor}`}>
              <AlertTriangle className={iconColor} size={24} />
            </div>

            {/* Nội dung */}
            <div className="mt-1">
              <h3 className="text-xl font-bold text-white mb-2">{title}</h3>
              <p className="text-sm text-gray-400 leading-relaxed">
                {message}
              </p>
            </div>
          </div>
        </div>

        {/* Khu vực Nút bấm */}
        <div className="px-6 py-4 bg-[#0A101D] border-t border-white/5 flex items-center justify-end gap-3">
          <button
            onClick={onClose}
            disabled={isLoading}
            className="px-4 py-2.5 text-sm font-semibold text-gray-400 hover:text-white hover:bg-white/5 rounded-lg transition-colors disabled:opacity-50"
          >
            {cancelText}
          </button>
          
          <button
            onClick={onConfirm}
            disabled={isLoading}
            className={`px-5 py-2.5 text-sm font-bold text-white rounded-lg transition-all flex items-center gap-2 ${btnColor} disabled:opacity-50 disabled:cursor-not-allowed`}
          >
            {isLoading && (
              <div className="w-4 h-4 rounded-full border-2 border-white/20 border-t-white animate-spin"></div>
            )}
            {confirmText}
          </button>
        </div>

      </div>
    </div>
  );
};

export default ConfirmModal;