// src/pages/reporter/PipelineConfig.tsx
import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { 
  Trash2, 
  Plus, 
  Info, 
  Lightbulb, 
  X, 
  Rocket,
  Hash,
  Link as LinkIcon
} from 'lucide-react';
import ReporterLayout from '../../components/reporter/ReporterLayout';
import { reporterApi } from '../../api/portalApi';
import { extractApiError } from '../../utils/authStorage';

interface ScrapingTarget {
  id: string;
  type: 'Hashtag' | 'Link Facebook' | 'Keyword' | 'Group ID';
  value: string;
}

export default function PipelineConfig() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();

  // State quản lý danh sách luồng cào dữ liệu (Nguồn thu thập)
  const [targets, setTargets] = useState<ScrapingTarget[]>([
    { id: '1', type: 'Hashtag', value: '#PetCareHub' },
    { id: '2', type: 'Link Facebook', value: 'fb.com/petcarehub' }
  ]);

  // State quản lý các khía cạnh cần bắt (Aspects)
  const [aspects, setAspects] = useState<string[]>(['Lỗi App', 'CSKH', 'Thanh toán']);
  const [newAspect, setNewAspect] = useState('');

  // State quản lý ngưỡng cảnh báo (%) và bộ lọc nâng cao
  const [threshold, setThreshold] = useState<number>(30);
  const [filterTeencode, setFilterTeencode] = useState<boolean>(true);
  const [isActivated, setIsActivated] = useState<boolean>(false);
  const [isBusy, setIsBusy] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');

  // Hàm thêm nguồn cào mới
  const addTarget = () => {
    const newId = Date.now().toString();
    setTargets([...targets, { id: newId, type: 'Hashtag', value: '' }]);
  };

  // Hàm cập nhật giá trị nguồn cào
  const updateTarget = (id: string, key: 'type' | 'value', value: any) => {
    setTargets(targets.map(t => t.id === id ? { ...t, [key]: value } : t));
  };

  // Hàm xóa nguồn cào
  const removeTarget = (id: string) => {
    setTargets(targets.filter(t => t.id !== id));
  };

  // Hàm thêm khía cạnh mới khi gõ Enter
  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      e.preventDefault(); // Ngăn hành vi submit mặc định
      if (newAspect.trim() && !aspects.includes(newAspect.trim())) {
        setAspects([...aspects, newAspect.trim()]);
        setNewAspect('');
      }
    }
  };

  // Hàm xóa khía cạnh
  const removeAspect = (indexToRemove: number) => {
    setAspects(aspects.filter((_, index) => index !== indexToRemove));
  };

  // Kích hoạt tiến trình
  const handleActivate = async () => {
    if (!id) return;
    const requestId = Number(id);
    setIsBusy(true);
    setErrorMessage('');
    try {
      await reporterApi.startWork(requestId);
      setIsActivated(true);
      navigate(`/reporter/workspace/${requestId}`);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể kích hoạt pipeline.'));
    } finally {
      setIsBusy(false);
    }
  };

  return (
    // Đổi activeTopNav thành "reports" để hợp lệ với TypeScript khai báo của Layout
    <ReporterLayout activeTopNav="reports">
      <div className="min-h-screen bg-[#f8fafc] -m-6 p-6 font-sans text-[#1e293b]">
        
        {/* Breadcrumb điều hướng */}
        <div className="flex items-center gap-2 text-sm text-gray-500 mb-2">
          <button onClick={() => navigate('/reporter/tasks')} className="hover:text-blue-600 flex items-center gap-1 bg-transparent border-0 cursor-pointer p-0">
            Tasks
          </button>
          <span className="text-gray-400">&gt;</span>
          <button onClick={() => navigate('/reporter/tasks')} className="hover:text-blue-600 flex items-center gap-1 bg-transparent border-0 cursor-pointer p-0">
            Yêu cầu #{id || 'REQ-1042'}
          </button>
          <span className="text-gray-400">&gt;</span>
          <span className="text-gray-400">Thiết lập Pipeline</span>
        </div>

        {/* Tiêu đề trang */}
        <div className="mb-6">
          <h1 className="text-2xl font-bold text-[#0f172a] mb-1">
            Cấu hình Tiến trình Cào & AI Phân tích
          </h1>
          <p className="text-sm text-gray-500">
            Thiết lập nguồn cấp dữ liệu và các tiêu chí để Bot và AI Model chạy ngầm.
          </p>
        </div>

        {/* Khung nội dung chia 2 cột */}
        <div className="grid grid-cols-1 lg:grid-cols-12 gap-6 mb-24">
          
          {/* CỘT 1: Nguồn Thu thập (Scraping Targets) */}
          <div className="lg:col-span-7 bg-white rounded-xl border border-gray-200/80 shadow-sm p-6 flex flex-col justify-between">
            <div>
              <h2 className="text-base font-bold text-[#0f172a] mb-4">
                1. Nguồn Thu thập (Scraping Targets)
              </h2>

              {/* Alert thông tin xoay IP */}
              <div className="flex items-center gap-3 bg-blue-50 border border-blue-100 rounded-lg p-3.5 mb-5 text-sm text-blue-700">
                <Info size={16} className="flex-shrink-0 text-blue-500" />
                <p className="m-0 text-xs">Hệ thống tự động xoay IP Proxy quét mỗi 15 phút.</p>
              </div>

              {/* Danh sách các trường nhập nguồn dữ liệu (Bo viền xám nhạt bao quanh mỗi ô) */}
              <div className="space-y-4 mb-5">
                {targets.map((target) => (
                  <div key={target.id} className="flex items-center gap-3 border border-gray-200 rounded-xl p-3 bg-white shadow-2xs">
                    <div className="flex items-center gap-2 border border-gray-200 rounded-lg px-2.5 py-1.5 bg-gray-50 min-w-[130px]">
                      {target.type === 'Hashtag' ? <Hash size={15} className="text-gray-400" /> : <LinkIcon size={15} className="text-gray-400" />}
                      <select
                        value={target.type}
                        onChange={(e) => updateTarget(target.id, 'type', e.target.value)}
                        className="bg-transparent border-0 text-sm text-gray-700 font-medium focus:outline-none w-full cursor-pointer"
                      >
                        <option value="Hashtag">Hashtag</option>
                        <option value="Link Facebook">Link Facebook</option>
                        <option value="Keyword">Keyword</option>
                        <option value="Group ID">Group ID</option>
                      </select>
                    </div>

                    <input
                      type="text"
                      value={target.value}
                      onChange={(e) => updateTarget(target.id, 'value', e.target.value)}
                      placeholder="Nhập nguồn dữ liệu..."
                      className="flex-1 text-sm border border-gray-200 rounded-lg px-3 py-2 text-gray-700 focus:outline-none focus:border-blue-500 focus:ring-1 focus:ring-blue-500"
                    />

                    <button
                      onClick={() => removeTarget(target.id)}
                      className="p-2 text-gray-400 hover:text-red-500 rounded-lg hover:bg-red-50 border-0 bg-transparent transition-colors cursor-pointer flex items-center justify-center"
                    >
                      <Trash2 size={16} />
                    </button>
                  </div>
                ))}
              </div>
            </div>

            {/* Nút thêm luồng cào nét đứt */}
            <button
              onClick={addTarget}
              className="w-full flex items-center justify-center gap-2 py-3 border-2 border-dashed border-gray-200 hover:border-[#008B94] rounded-xl text-sm font-semibold text-gray-500 hover:text-[#008B94] transition-all bg-white cursor-pointer"
            >
              <Plus size={16} /> Thêm luồng cào
            </button>
          </div>

          {/* CỘT 2: Cấu hình AI (AI Model Criteria) */}
          <div className="lg:col-span-5 bg-white rounded-xl border border-gray-200/80 shadow-sm p-6">
            <h2 className="text-base font-bold text-[#0f172a] mb-4">
              2. Cấu hình AI (AI Model Criteria)
            </h2>

            {/* Hộp gợi ý cấu hình AI */}
            <div className="flex items-start gap-3 bg-amber-50/60 border border-amber-100 rounded-lg p-3.5 mb-5 text-sm text-amber-800">
              <Lightbulb size={16} className="mt-0.5 flex-shrink-0 text-amber-500" />
              <p className="m-0 text-xs leading-relaxed">Hướng dẫn AI bám sát tiêu chí Custom Metrics của khách hàng.</p>
            </div>

            {/* Các khía cạnh (Aspects) */}
            <div className="mb-5">
              <label className="block text-xs font-semibold text-gray-500 uppercase tracking-wider mb-2">
                Các khía cạnh cần bắt (Aspects):
              </label>
              <div className="border border-gray-200 rounded-xl p-3 bg-white min-h-[100px] flex flex-col justify-between focus-within:border-blue-500 focus-within:ring-1 focus-within:ring-blue-500">
                <div className="flex flex-wrap gap-1.5 mb-2">
                  {aspects.map((aspect, index) => (
                    <span 
                      key={index} 
                      className="inline-flex items-center gap-1 bg-blue-50 text-blue-600 border border-blue-100 px-2.5 py-1 rounded-lg text-xs font-medium animate-fade-in"
                    >
                      {aspect}
                      <button 
                        onClick={() => removeAspect(index)} 
                        className="hover:bg-blue-100 text-blue-500 rounded-sm p-0.5 border-0 bg-transparent cursor-pointer flex items-center justify-center"
                      >
                        <X size={12} />
                      </button>
                    </span>
                  ))}
                </div>
                <input
                  type="text"
                  value={newAspect}
                  onChange={(e) => setNewAspect(e.target.value)}
                  onKeyDown={handleKeyDown}
                  placeholder="Thêm khía cạnh... (Ấn Enter)"
                  className="w-full border-0 focus:ring-0 p-1 text-sm focus:outline-none placeholder-gray-400 bg-transparent mt-1"
                />
              </div>
            </div>

            {/* Thanh trượt ngưỡng cảnh báo */}
            <div className="mb-6">
              <div className="flex justify-between items-center mb-2">
                <label className="text-xs font-semibold text-gray-500 uppercase tracking-wider">
                  Ngưỡng cảnh báo khủng hoảng:
                </label>
                <span className="text-base font-bold text-[#008B94]">{threshold}%</span>
              </div>
              <input
                type="range"
                min="0"
                max="100"
                value={threshold}
                onChange={(e) => setThreshold(Number(e.target.value))}
                className="w-full h-1.5 bg-gray-200 rounded-lg appearance-none cursor-pointer accent-[#008B94]"
              />
              <div className="flex justify-between text-[11px] text-gray-400 mt-1.5 font-medium">
                <span>Nhạy cảm cao (0%)</span>
                <span>Bỏ qua (100%)</span>
              </div>
            </div>

            {/* Bộ lọc nâng cao */}
            <div className="border-t border-gray-100 pt-4">
              <span className="block text-xs font-semibold text-gray-500 uppercase tracking-wider mb-2.5">
                Bộ lọc nâng cao:
              </span>
              <label className="flex items-center gap-3 cursor-pointer select-none">
                <input
                  type="checkbox"
                  checked={filterTeencode}
                  onChange={(e) => setFilterTeencode(e.target.checked)}
                  className="w-4 h-4 text-[#008B94] border-gray-300 rounded focus:ring-[#008B94] cursor-pointer"
                />
                <span className="text-sm font-medium text-gray-700">Bật lọc ngôn ngữ rác/Teencode</span>
              </label>
            </div>
          </div>
        </div>

        {/* THANH ACTION TRẠNG THÁI CỐ ĐỊNH Ở DƯỚI ĐÁY */}
        <div className="fixed bottom-0 left-0 lg:left-64 right-0 bg-white border-t border-gray-200 px-6 py-4 flex justify-between items-center shadow-[0_-4px_20px_rgba(0,0,0,0.04)] z-40">
          <div className="flex flex-col gap-1">
            <div className="flex items-center gap-2">
              <span className={`w-2.5 h-2.5 rounded-full ${isActivated ? "bg-green-500" : "bg-gray-400"}`}></span>
              <span className="text-sm font-medium text-gray-500">
                Trạng thái Pipeline: <strong className={isActivated ? "text-green-600 font-bold" : "text-gray-700 font-semibold"}>{isActivated ? "Đang hoạt động" : "Chưa kích hoạt"}</strong>
              </span>
            </div>
            {errorMessage && <p className="text-sm text-red-600">{errorMessage}</p>}
          </div>
          <button
            onClick={handleActivate}
            disabled={isBusy}
            className="flex items-center gap-2 px-6 py-2.5 bg-[#008B94] hover:bg-[#00737a] disabled:opacity-60 text-white font-bold text-sm rounded-lg transition-colors shadow-sm border-0 cursor-pointer"
          >
            {isBusy ? 'Đang kích hoạt...' : 'Lưu cấu hình & Kích hoạt Bot'} <Rocket size={15} />
          </button>
        </div>
      </div>
    </ReporterLayout>
  );
}