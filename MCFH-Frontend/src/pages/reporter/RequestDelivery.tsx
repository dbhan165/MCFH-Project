import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Clock, UploadCloud, FileText, CheckSquare, Square, ExternalLink, Settings } from 'lucide-react';
import ReporterLayout from '../../components/reporter/ReporterLayout';

const RequestDelivery = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  
  // Trạng thái cho checkbox xác nhận
  const [checkedMetrics, setCheckedMetrics] = useState(false);
  const [checkedFormat, setCheckedFormat] = useState(false);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);

  // Giả lập xử lý sự kiện up file
  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    if (e.target.files && e.target.files[0]) {
      setSelectedFile(e.target.files[0]);
    }
  };

  return (
    <ReporterLayout activeTopNav="reports">
      {/* Breadcrumb điều hướng quay lại */}
      <div className="flex items-center gap-2 text-sm text-[#64748b] mb-3">
        <button 
          onClick={() => navigate('/reporter/requests')}
          className="hover:text-[#0f172a] transition-colors cursor-pointer"
        >
          Bảng công việc
        </button>
        <span>&gt;</span>
        <span className="text-gray-400">Yêu cầu {id || '#REQ-1042'}</span>
        <span>&gt;</span>
        <span className="text-gray-400">Không gian làm việc</span>
      </div>

      {/* Header tiêu đề & Deadline */}
      <div className="flex flex-col md:flex-row md:items-center md:justify-between gap-4 mb-6">
        <div>
          <h2 className="text-xl lg:text-2xl font-bold text-[#0f172a] flex items-center gap-3">
            Không gian làm việc: PetCareHub
          </h2>
          <div className="flex items-center gap-2 mt-2">
            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-blue-50 text-blue-600">
              ● Đang xử lý (In Progress)
            </span>
          </div>
        </div>

        {/* Badge Hạn chót cảnh báo */}
        <div className="flex items-center gap-2 px-4 py-2 bg-red-50 text-red-600 rounded-lg text-sm font-semibold border border-red-100 self-start md:self-auto">
          <Clock className="w-4 h-4" />
          Hạn chót: 05/06/2026 (Còn 4 ngày)
        </div>
      </div>

      {/* Grid Layout chứa 2 phân khu nội dung chính */}
      <div className="space-y-6">
        
        {/* KHU VỰC 1: Tài nguyên Phân tích */}
        <div className="bg-white border border-gray-200 rounded-xl p-6 shadow-sm">
          <h3 className="text-base font-bold text-[#0f172a] mb-5">Tài nguyên Phân tích</h3>
          
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4 mb-6">
            {/* Box 1: Mentions */}
            <div className="bg-gray-50 border border-gray-100 rounded-xl p-4">
              <span className="text-xs text-[#64748b] font-medium">Mentions đã thu thập</span>
              <div className="text-2xl font-bold text-teal-600 mt-1">12,450</div>
            </div>

            {/* Box 2: Tiêu cực */}
            <div className="bg-gray-50 border border-gray-100 rounded-xl p-4">
              <span className="text-xs text-[#64748b] font-medium">Tiêu cực</span>
              <div className="text-2xl font-bold text-red-500 mt-1">34%</div>
            </div>

            {/* Box 3: Khía cạnh hot nhất */}
            <div className="bg-gray-50 border border-gray-100 rounded-xl p-4">
              <span className="text-xs text-[#64748b] font-medium">Khía cạnh hot nhất</span>
              <div className="text-2xl font-bold text-[#0f172a] mt-1">Lỗi App</div>
            </div>
          </div>

          {/* Các nút hành động thao tác dữ liệu */}
          <div className="flex flex-wrap gap-3">
            <button className="inline-flex items-center gap-2 px-4 py-2.5 bg-[#0f172a] hover:bg-slate-800 text-white rounded-lg text-sm font-medium transition-colors shadow-sm cursor-pointer">
              <FileText className="w-4 h-4" />
              Mở Công cụ Tác nghiệp & Xem Dữ liệu
              <ExternalLink className="w-3.5 h-3.5 opacity-80" />
            </button>
            <button className="inline-flex items-center gap-2 px-4 py-2.5 border border-gray-300 hover:bg-gray-50 text-[#0f172a] rounded-lg text-sm font-medium transition-colors cursor-pointer">
              <Settings className="w-4 h-4 text-gray-500" />
              Chỉnh sửa Pipeline
            </button>
          </div>
        </div>

        {/* KHU VỰC 2: Bàn giao File Báo cáo */}
        <div className="bg-white border border-gray-200 rounded-xl p-6 shadow-sm">
          <h3 className="text-base font-bold text-[#0f172a] mb-5">Bàn giao File Báo cáo (Delivery)</h3>
          
          {/* Vùng Dropzone Kéo thả file */}
          <div className="relative border-2 border-dashed border-sky-200 bg-sky-50/30 rounded-xl p-8 flex flex-col items-center justify-center text-center hover:bg-sky-50/50 transition-colors mb-6 group">
            <input 
              type="file" 
              accept=".pdf" 
              onChange={handleFileChange}
              className="absolute inset-0 w-full h-full opacity-0 cursor-pointer" 
            />
            <div className="w-12 h-12 rounded-full bg-sky-50 flex items-center justify-center text-sky-500 mb-3 group-hover:scale-110 transition-transform">
              <UploadCloud className="w-6 h-6" />
            </div>
            {selectedFile ? (
              <div>
                <p className="text-sm font-semibold text-teal-600">{selectedFile.name}</p>
                <p className="text-xs text-gray-400 mt-1">{(selectedFile.size / (1024 * 1024)).toFixed(2)} MB</p>
              </div>
            ) : (
              <>
                <p className="text-sm font-medium text-[#0f172a]">
                  Kéo thả file PDF Kompa-style vào đây hoặc <span className="text-teal-600 underline">Click để chọn file</span>
                </p>
                <p className="text-xs text-gray-400 mt-1.5">Định dạng hỗ trợ: .PDF, tối đa 50MB</p>
              </>
            )}
          </div>

          {/* Cụm Checkbox quy chuẩn kiểm duyệt đầu ra */}
          <div className="space-y-3 mb-6">
            <label 
              className="flex items-start gap-3 text-sm text-[#0f172a] cursor-pointer select-none"
              onClick={() => setCheckedMetrics(!checkedMetrics)}
            >
              {checkedMetrics ? (
                <CheckSquare className="w-5 h-5 text-teal-600 shrink-0 mt-0.5" />
              ) : (
                <Square className="w-5 h-5 text-gray-300 shrink-0 mt-0.5" />
              )}
              <span>Tôi xác nhận báo cáo đã bám sát Custom Metrics của khách hàng.</span>
            </label>

            <label 
              className="flex items-start gap-3 text-sm text-[#0f172a] cursor-pointer select-none"
              onClick={() => setCheckedFormat(!checkedFormat)}
            >
              {checkedFormat ? (
                <CheckSquare className="w-5 h-5 text-teal-600 shrink-0 mt-0.5" />
              ) : (
                <Square className="w-5 h-5 text-gray-300 shrink-0 mt-0.5" />
              )}
              <span>Đã rà soát lỗi chính tả và format.</span>
            </label>
          </div>

          {/* Button Submit Hoàn thành */}
          <button
            type="button"
            disabled={!selectedFile || !checkedMetrics || !checkedFormat}
            className={`w-full sm:w-auto px-6 py-2.5 rounded-lg text-sm font-semibold transition-colors shadow-sm ${
              selectedFile && checkedMetrics && checkedFormat
                ? 'bg-teal-600 hover:bg-teal-700 text-white cursor-pointer'
                : 'bg-gray-100 text-gray-400 cursor-not-allowed'
            }`}
          >
            Nộp Báo cáo & Hoàn thành Đơn hàng
          </button>
        </div>

      </div>
    </ReporterLayout>
  );
};

// ĐẢM BẢO TÁCH RIÊNG DÒNG EXPORT DEFAULT NÀY RA CUỐI FILE
export default RequestDelivery;