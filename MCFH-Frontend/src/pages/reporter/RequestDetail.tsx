import React, { useState } from 'react';
import { 
  FileText, 
  ClipboardList, 
  Search, 
  Bell, 
  HelpCircle, 
  CheckCircle2, 
  XCircle, 
  Plus,
  LayoutDashboard,
  BarChart3,
  Settings as SettingsIcon,
  Calendar
} from 'lucide-react';
import ReporterLayout from '../../components/reporter/ReporterLayout';

export default function QuoteDetail() {
  // State mock cho Form dữ liệu
  const [cost, setCost] = useState('');
  const [deadline, setDeadline] = useState('');
  const [note, setNote] = useState('');

  return (
    <ReporterLayout activeTopNav="reports">
      <div className="min-h-screen bg-[#f8fafc] -m-6 p-6 font-sans">
        
        {/* Top Navigation Bar bên trong Layout (giống header trong ảnh) */}
        <div className="flex items-center justify-between bg-white border-b border-slate-200 px-6 py-3 -mt-6 -mx-6 mb-6">
          <div className="relative w-80">
            <Search className="absolute left-3 top-2.5 w-4 h-4 text-slate-400" />
            <input 
              type="text" 
              placeholder="Search tasks, reports..." 
              className="w-full pl-9 pr-4 py-1.5 bg-[#f1f5f9] border-none rounded-lg text-xs focus:outline-none"
            />
          </div>
          <div className="flex items-center gap-4 text-slate-500">
            <div className="relative cursor-pointer">
              <Bell className="w-5 h-5" />
              <span className="absolute top-0 right-0 w-1.5 h-1.5 bg-red-500 rounded-full"></span>
            </div>
            <HelpCircle className="w-5 h-5 cursor-pointer" />
          </div>
        </div>

        {/* Breadcrumb & Tiêu đề chính */}
        <div className="mb-6">
          <div className="text-[11px] text-slate-500 font-medium flex items-center gap-1">
            <span>Bảng công việc</span>
            <span className="text-slate-400">&gt;</span>
            <span className="text-slate-600 font-semibold">Yêu cầu #REQ-1042</span>
          </div>
          <h1 className="text-2xl font-bold text-[#0f172a] mt-2 tracking-tight">
            Báo giá: Phân tích Khủng hoảng PetCareHub
          </h1>
          <div className="mt-2.5 inline-flex items-center gap-1.5 px-2.5 py-1 bg-[#e0f2fe] text-[#0369a1] text-[11px] font-medium rounded-full">
            <span className="w-1.5 h-1.5 bg-[#38bdf8] rounded-full"></span>
            Trạng thái: Chờ báo giá
          </div>
        </div>

        {/* Main Grid Content: 2 Cột */}
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 items-start">
          
          {/* CỘT TRÁI: 1. Chi tiết Yêu cầu (Requirements) - Chiếm 7/12 */}
          <div className="lg:col-span-7 bg-white rounded-2xl border border-slate-200 p-6 shadow-sm">
            <div className="flex items-center gap-2.5 border-b border-slate-100 pb-4 mb-5">
              <ClipboardList className="w-5 h-5 text-[#0f172a]" />
              <h2 className="text-md font-bold text-[#0f172a]">1. Chi tiết Yêu cầu (Requirements)</h2>
            </div>

            <div className="text-[13.5px] leading-relaxed text-[#334155] font-normal space-y-6">
              <p>
                Cần phân tích sâu về nguyên nhân khách hàng phàn nàn lỗi thanh toán VNPay trên app tuần vừa qua. 
                Yêu cầu bóc tách rõ phản ứng của người dùng, mật độ từ khóa tiêu cực, và so sánh với sự cố tương tự (nếu có) trong quá khứ.
              </p>

              {/* Phần Tags */}
              <div className="pt-5 border-t border-dashed border-slate-200">
                <h4 className="text-[10px] font-bold text-slate-400 uppercase tracking-wider mb-3">
                  TRỌNG TÂM PHÂN TÍCH
                </h4>
                <div className="flex flex-wrap gap-2">
                  <span className="inline-flex items-center gap-1 px-3 py-1.5 bg-[#eceeff] text-[#5c67f2] text-xs font-semibold rounded-md">
                    # Nền tảng: TikTok
                  </span>
                  <span className="inline-flex items-center gap-1 px-3 py-1.5 bg-[#fde8e8] text-[#e02424] text-xs font-semibold rounded-md">
                    # Khía cạnh: Lỗi App
                  </span>
                  <span className="inline-flex items-center gap-1 px-3 py-1.5 bg-[#f2994a]/10 text-[#d47a2a] text-xs font-semibold rounded-md">
                    # Khía cạnh: CSKH
                  </span>
                </div>
              </div>
            </div>
          </div>

          {/* CỘT PHẢI: 2. Form Báo Giá (Quoting) - Chiếm 5/12 */}
          <div className="lg:col-span-5 bg-[#f8fafc] lg:bg-white rounded-2xl border border-slate-200 shadow-sm overflow-hidden">
            {/* Header Form có nền xanh nhạt đặc trưng trong ảnh */}
            <div className="flex items-center gap-2.5 bg-[#f0f4f8] px-6 py-4 border-b border-slate-200">
              <FileText className="w-5 h-5 text-[#0f172a]" />
              <h2 className="text-md font-bold text-[#0f172a]">2. Form Báo Giá (Quoting)</h2>
            </div>

            {/* Nội dung Form */}
            <form className="p-6 space-y-4 bg-white" onSubmit={(e) => e.preventDefault()}>
              {/* Input Chi phí */}
              <div>
                <label className="block text-[11px] font-bold text-slate-600 mb-1.5">
                  Chi phí thực hiện (VNĐ) <span className="text-red-500">*</span>
                </label>
                <div className="relative">
                  <span className="absolute left-3 top-2.5 text-slate-400 text-xs">💵</span>
                  <input
                    type="text"
                    placeholder="Ví dụ: 5,000,000"
                    value={cost}
                    onChange={(e) => setCost(e.target.value)}
                    className="w-full pl-8 pr-3 py-2 bg-[#f8fafc] border border-slate-200 rounded-lg text-sm placeholder:text-slate-400 focus:outline-none focus:border-slate-300"
                  />
                </div>
              </div>

              {/* Input Hạn chót */}
              <div>
                <label className="block text-[11px] font-bold text-slate-600 mb-1.5">
                  Hạn bàn giao dự kiến (Deadline) <span className="text-red-500">*</span>
                </label>
                <div className="relative">
                  <input
                    type="text"
                    placeholder="dd/mm/yyyy"
                    value={deadline}
                    onChange={(e) => setDeadline(e.target.value)}
                    className="w-full px-8 py-2 bg-[#f8fafc] border border-slate-200 rounded-lg text-sm placeholder:text-slate-400 focus:outline-none focus:border-slate-300"
                  />
                  <Calendar className="absolute left-3 top-2.5 w-4 h-4 text-slate-400" />
                  <span className="absolute right-3 top-2.5 text-slate-400 text-xs">📋</span>
                </div>
              </div>

              {/* Input Ghi chú nội bộ */}
              <div>
                <label className="block text-[11px] font-bold text-slate-600 mb-1.5">
                  Ghi chú nội bộ (Tùy chọn)
                </label>
                <textarea
                  rows={3}
                  placeholder="Thêm thông tin cần thiết cho quản lý..."
                  value={note}
                  onChange={(e) => setNote(e.target.value)}
                  className="w-full px-3 py-2 bg-[#f8fafc] border border-slate-200 rounded-lg text-sm placeholder:text-slate-400 focus:outline-none focus:border-slate-300 resize-none"
                />
              </div>

              {/* Nhóm Buttons */}
              <div className="pt-2 space-y-2.5">
                <button
                  type="button"
                  className="w-full flex items-center justify-center gap-2 bg-[#00667e] hover:bg-[#005367] text-white font-semibold py-2.5 px-4 rounded-lg text-xs transition-all shadow-sm"
                >
                  Gửi Báo Giá 🚀
                </button>
                
                <button
                  type="button"
                  className="w-full flex items-center justify-center gap-2 bg-white border border-red-200 hover:bg-red-50 text-red-600 font-medium py-2 px-4 rounded-lg text-xs transition-all"
                >
                  Từ chối Đơn ❌
                </button>
              </div>
            </form>
          </div>

        </div>
      </div>
    </ReporterLayout>
  );
}