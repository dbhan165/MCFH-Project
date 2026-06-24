import { useState } from 'react';
import { Link } from 'react-router-dom'; // Sử dụng Link để tối ưu hóa điều hướng nội bộ SPA
import { 
  FileUp, UploadCloud, CheckSquare, Square, X, 
  TrendingUp, Clock, Star, Download, CheckCircle, ExternalLink, Settings2
} from 'lucide-react';
import ReporterLayout from '../../components/reporter/ReporterLayout';

// Mockup dữ liệu chuẩn hóa gồm 2 trạng thái: 'completed' (Hoàn thành) và 'pending' (Chờ đánh giá)
const mockupRequests = [
  { id: '1043', title: 'Thu thập dữ liệu Sentiment Shopee Food', dateRecv: '15/06/2026', dateDelivery: '—', duration: 'Chờ cấu hình AI', rating: 'pending' },
  { id: '1038', title: 'Đánh giá Đối thủ Vinamilk', dateRecv: '25/05/2026', dateDelivery: '28/05/2026', duration: '3 Ngày', rating: 'completed' },
  { id: '1042', title: 'Phân tích Khủng hoảng PetCareHub', dateRecv: '01/06/2026', dateDelivery: '03/06/2026', duration: '2 Ngày', rating: 'completed' },
  { id: '1020', title: 'Báo cáo thị phần Q1', dateRecv: '10/05/2026', dateDelivery: '11/05/2026', duration: '1 Ngày', rating: 'completed' },
];

export default function MyPerformance() {
  // Quản lý Drawer bàn giao báo cáo (Phía bên phải)
  const [isDrawerOpen, setIsDrawerOpen] = useState(false);
  const [selectedReq, setSelectedReq] = useState<any>(null);

  // Khởi tạo trạng thái form bên trong Drawer bàn giao
  const [checkedMetrics, setCheckedMetrics] = useState(false);
  const [checkedFormat, setCheckedFormat] = useState(false);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  // Helper render badge trạng thái đúng chuẩn 2 chế độ
  const getRatingLabel = (rating: string) => {
    if (rating === 'completed') {
      return { text: 'Hoàn thành', className: 'bg-green-50 text-green-600 border border-green-200' };
    }
    return { text: 'Chờ đánh giá', className: 'bg-gray-50 text-gray-400 border border-gray-200' };
  };

  const openDeliveryDrawer = (req: any) => {
    setSelectedReq(req);
    setIsDrawerOpen(true);
    setSelectedFile(null);
    setCheckedMetrics(false);
    setCheckedFormat(false);
  };

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      setSelectedFile(e.target.files[0]);
    }
  };

  return (
    <ReporterLayout activeTopNav="performance">
      <div className="min-h-screen bg-[#f8fafc] -m-6 p-6 font-sans text-[#1e293b]">
        
        {/* HEADER SECTION */}
        <div className="flex items-center justify-between mb-6">
          <h2 className="text-xl lg:text-2xl font-bold text-[#0f172a]">
            Thống kê Hiệu suất Cá nhân (My Performance)
          </h2>
          <div className="flex items-center gap-2 bg-white px-3 py-1.5 border border-gray-200 rounded-lg text-sm shadow-sm">
            <Clock className="w-4 h-4 text-gray-400" />
            <span className="font-medium text-gray-700">Tháng 6/2026</span>
          </div>
        </div>

        {/* METRICS SUMMARY CARDS */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-5 mb-6">
          <div className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm">
            <span className="text-sm font-semibold text-gray-500 block">Báo cáo đã bàn giao</span>
            <div className="flex items-baseline gap-2 mt-2">
              <span className="text-3xl font-bold text-[#0f172a]">12</span>
              <span className="text-xs text-gray-400 font-medium">Đơn</span>
            </div>
            <div className="text-xs text-green-600 font-medium flex items-center gap-1 mt-2">
              <TrendingUp className="w-3.5 h-3.5" />
              Vượt 120% target tháng này
            </div>
          </div>

          <div className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm">
            <span className="text-sm font-semibold text-gray-500 block">Thời gian xử lý trung bình</span>
            <div className="flex items-baseline gap-2 mt-2">
              <span className="text-3xl font-bold text-teal-600">4.5</span>
              <span className="text-xs text-gray-400 font-medium">Giờ</span>
            </div>
            <div className="text-xs text-slate-500 font-medium mt-2">
              ⚡ Nhanh hơn 15% so với tháng trước
            </div>
          </div>

          <div className="bg-white border border-gray-200 rounded-xl p-5 shadow-sm">
            <span className="text-sm font-semibold text-gray-500 block">Đánh giá chất lượng (QA)</span>
            <div className="flex items-baseline gap-2 mt-2">
              <span className="text-3xl font-bold text-amber-500 flex items-center gap-1">
                <Star className="w-6 h-6 fill-amber-500" /> 4.8
              </span>
              <span className="text-xs text-gray-400 font-medium">/ 5.0</span>
            </div>
            <div className="text-xs text-slate-500 font-medium mt-2">
              Dựa trên 10 đơn hàng gần nhất
            </div>
          </div>
        </div>

        {/* DATA TABLE */}
        <div className="bg-white border border-gray-200 rounded-xl shadow-sm overflow-hidden">
          <div className="p-5 border-b border-gray-100 flex items-center justify-between">
            <h3 className="font-bold text-[#0f172a] text-base">Lịch sử Xử lý Đơn hàng</h3>
            <button className="inline-flex items-center gap-1.5 px-3 py-1.5 border border-gray-200 hover:bg-gray-50 text-gray-600 rounded-lg text-xs font-semibold transition-colors cursor-pointer">
              <Download className="w-3.5 h-3.5" /> Export
            </button>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse text-sm">
              <thead>
                <tr className="bg-slate-50/70 text-[#64748b] font-semibold text-xs tracking-wider uppercase border-b border-gray-100">
                  <th className="py-3 px-5">Mã đơn</th>
                  <th className="py-3 px-4">Tên dự án</th>
                  <th className="py-3 px-4">Ngày nhận</th>
                  <th className="py-3 px-4">Ngày bàn giao</th>
                  <th className="py-3 px-4">Thời gian xử lý</th>
                  <th className="py-3 px-4">Đánh giá</th>
                  <th className="py-3 px-5 text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 text-[#0f172a]">
                {mockupRequests.map((req) => {
                  const ratingData = getRatingLabel(req.rating);
                  const isCompleted = req.rating === 'completed';

                  return (
                    <tr key={req.id} className="hover:bg-slate-50/40 transition-colors">
                      <td className="py-3.5 px-5 font-semibold">
                        {/* 🌟 LOGIC ĐIỀU HƯỚNG THEO TRẠNG THÁI MÃ ĐƠN HÀNG */}
                        {isCompleted ? (
                          <Link 
                            to={`/reporter/workspace/${req.id}`} 
                            className="text-teal-600 hover:text-teal-700 underline flex items-center gap-0.5 group"
                            title="Xem Giao diện UI/Nội dung Bài báo cáo"
                          >
                            #{req.id}
                            <ExternalLink className="w-3 h-3 opacity-0 group-hover:opacity-100 transition-opacity" />
                          </Link>
                        ) : (
                          <Link 
                            to={`/reporter/pipeline/${req.id}`}
                            className="text-amber-600 hover:text-amber-700 underline flex items-center gap-0.5 group"
                            title="Đi tới Cấu hình Tiến trình Cào & AI Phân tích"
                          >
                            #{req.id}
                            <Settings2 className="w-3 h-3 opacity-0 group-hover:opacity-100 transition-opacity" />
                          </Link>
                        )}
                      </td>
                      <td className="py-3.5 px-4 font-medium">{req.title}</td>
                      <td className="py-3.5 px-4 text-gray-500 text-xs">{req.dateRecv}</td>
                      <td className="py-3.5 px-4 text-gray-500 text-xs">{req.dateDelivery}</td>
                      <td className="py-3.5 px-4 font-medium text-xs">{req.duration}</td>
                      <td className="py-3.5 px-4">
                        <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-semibold ${ratingData.className}`}>
                          {ratingData.text}
                        </span>
                      </td>
                      <td className="py-3.5 px-5 text-right">
                        {isCompleted ? (
                          <button
                            onClick={() => openDeliveryDrawer(req)}
                            className="inline-flex items-center gap-1.5 px-3 py-1.5 bg-teal-600 hover:bg-teal-700 text-white rounded-md text-xs font-bold transition-colors shadow-sm cursor-pointer"
                          >
                            <FileUp className="w-3.5 h-3.5" />
                            Bàn giao File
                          </button>
                        ) : (
                          <Link
                            to={`/reporter/pipeline/${req.id}`}
                            className="inline-flex items-center gap-1 px-2.5 py-1.5 border border-teal-200 text-teal-600 hover:bg-teal-50 rounded-md text-xs font-semibold transition-colors"
                          >
                            Cấu hình Pipeline
                          </Link>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>

        {/* OVERLAY & DRAWER SIDE PANEL (BÀN GIAO FILE) */}
        {isDrawerOpen && (
          <div className="fixed inset-0 z-50 overflow-hidden">
            <div 
              className="absolute inset-0 bg-black/30 backdrop-blur-xs transition-opacity"
              onClick={() => setIsDrawerOpen(false)}
            />
            
            <div className="absolute inset-y-0 right-0 pl-10 max-w-full flex sm:pl-16">
              <div className="w-screen max-w-md md:max-w-xl bg-white shadow-2xl flex flex-col border-l border-gray-100 animate-slideLeft">
                
                {/* Header Panel */}
                <div className="p-6 bg-slate-50 border-b border-gray-100 flex items-center justify-between">
                  <div>
                    <h3 className="text-base font-bold text-[#0f172a]">Không gian Bàn giao Báo cáo</h3>
                    <p className="text-xs text-gray-500 mt-1">
                      Đơn hàng: <span className="font-semibold text-teal-600">#{selectedReq?.id}</span> - {selectedReq?.title}
                    </p>
                  </div>
                  <button 
                    onClick={() => setIsDrawerOpen(false)}
                    className="p-1.5 text-gray-400 hover:text-gray-700 hover:bg-gray-200/60 rounded-lg transition-colors cursor-pointer"
                  >
                    <X className="w-5 h-5" />
                  </button>
                </div>

                {/* Body Content */}
                <div className="flex-1 overflow-y-auto p-6 space-y-6">
                  <div className="bg-emerald-50/50 border border-emerald-100 rounded-xl p-4 flex items-start gap-3">
                    <CheckCircle className="w-5 h-5 text-emerald-600 shrink-0 mt-0.5" />
                    <div>
                      <h4 className="text-xs font-bold text-emerald-900 uppercase tracking-wider">Trạng thái ghi nhận</h4>
                      <p className="text-sm text-emerald-800 mt-0.5">Dự án đã hoàn thành và thông qua kiểm định chất lượng nội bộ.</p>
                    </div>
                  </div>

                  {/* Vùng thả file PDF */}
                  <div className="space-y-2">
                    <label className="text-xs font-bold uppercase tracking-wider text-gray-400">File tài liệu đính kèm</label>
                    <div className="relative border-2 border-dashed border-sky-200 bg-sky-50/20 rounded-xl p-8 flex flex-col items-center justify-center text-center hover:bg-sky-50/40 transition-colors group">
                      <input 
                        type="file" 
                        accept=".pdf" 
                        onChange={handleFileChange}
                        className="absolute inset-0 w-full h-full opacity-0 cursor-pointer" 
                      />
                      <div className="w-10 h-10 rounded-full bg-sky-50 flex items-center justify-center text-sky-500 mb-2 group-hover:scale-105 transition-transform">
                        <UploadCloud className="w-5 h-5" />
                      </div>
                      {selectedFile ? (
                        <div>
                          <p className="text-sm font-semibold text-teal-600">{selectedFile.name}</p>
                          <p className="text-xs text-gray-400 mt-0.5">{(selectedFile.size / (1024 * 1024)).toFixed(2)} MB</p>
                        </div>
                      ) : (
                        <>
                          <p className="text-sm font-medium text-[#0f172a]">
                            Kéo thả file PDF vào đây hoặc <span className="text-teal-600 underline">Click để chọn file</span>
                          </p>
                          <p className="text-xs text-gray-400 mt-1">Định dạng hỗ trợ: .PDF, tối đa 50MB</p>
                        </>
                      )}
                    </div>
                  </div>

                  {/* Checkbox cam kết */}
                  <div className="space-y-3 pt-2">
                    <label className="text-xs font-bold uppercase tracking-wider text-gray-400 block">Cam kết chất lượng</label>
                    
                    <label 
                      className="flex items-start gap-3 text-sm text-[#0f172a] cursor-pointer select-none"
                      onClick={() => setCheckedMetrics(!checkedMetrics)}
                    >
                      {checkedMetrics ? (
                        <CheckSquare className="w-4 h-4 text-teal-600 shrink-0 mt-0.5" />
                      ) : (
                        <Square className="w-4 h-4 text-gray-300 shrink-0 mt-0.5" />
                      )}
                      <span className="text-gray-600 text-xs leading-relaxed">Tôi xác nhận báo cáo đã bám sát các tiêu chuẩn Custom Metrics được yêu cầu.</span>
                    </label>

                    <label 
                      className="flex items-start gap-3 text-sm text-[#0f172a] cursor-pointer select-none"
                      onClick={() => setCheckedFormat(!checkedFormat)}
                    >
                      {checkedFormat ? (
                        <CheckSquare className="w-4 h-4 text-teal-600 shrink-0 mt-0.5" />
                      ) : (
                        <Square className="w-4 h-4 text-gray-300 shrink-0 mt-0.5" />
                      )}
                      <span className="text-gray-600 text-xs leading-relaxed">Đã kiểm tra lỗi trình bày, lỗi chính tả và cấu trúc format biểu đồ.</span>
                    </label>
                  </div>
                </div>

                {/* Footer Action */}
                <div className="p-4 bg-slate-50 border-t border-gray-100 flex items-center justify-end gap-3">
                  <button
                    type="button"
                    onClick={() => setIsDrawerOpen(false)}
                    className="px-4 py-2 text-sm font-semibold text-gray-500 hover:bg-gray-200 rounded-lg transition-colors cursor-pointer"
                  >
                    Đóng lại
                  </button>
                  <button
                    type="button"
                    disabled={!selectedFile || !checkedMetrics || !checkedFormat}
                    onClick={() => {
                      alert(`Gửi thành công file báo cáo của đơn #${selectedReq?.id}`);
                      setIsDrawerOpen(false);
                    }}
                    className={`px-5 py-2 rounded-lg text-sm font-bold transition-colors shadow-sm ${
                      selectedFile && checkedMetrics && checkedFormat
                        ? 'bg-teal-600 hover:bg-teal-700 text-white cursor-pointer'
                        : 'bg-gray-200 text-gray-400 cursor-not-allowed'
                    }`}
                  >
                    Nộp & Phát hành Báo cáo
                  </button>
                </div>

              </div>
            </div>
          </div>
        )}

      </div>
    </ReporterLayout>
  );
}