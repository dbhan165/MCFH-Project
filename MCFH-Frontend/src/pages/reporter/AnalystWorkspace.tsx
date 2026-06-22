import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { 
  ChevronRight, Clock, Save, FileText, BarChart3, 
  Bold, Italic, Underline, List, ListOrdered, 
  Pin, CheckCircle2, ChevronDown
} from 'lucide-react';
import ReporterLayout from '../../components/reporter/ReporterLayout';

export default function AnalystWorkspace() {
  const { id } = useParams<{ id: string }>();
  
  // Trạng thái các bộ lọc dữ liệu thu thập
  const [selectedPlatform, setSelectedPlatform] = useState('Tất cả nền tảng');
  const [selectedSentiment, setSelectedSentiment] = useState('Tiêu cực');
  const [selectedAspect, setSelectedAspect] = useState('Lỗi App');

  // Dữ liệu Social Listening thu thập được (Cột trái) theo mẫu image_328296.png
  const [mentions, setMentions] = useState([
    {
      id: 1,
      author: '@bocphot.app',
      time: '10 mins ago',
      content: 'Thanh toán VNPay lỗi trừ tiền 2 lần, app tệ!',
      sentiment: 'Tiêu cực: 98%',
      aspect: 'Lỗi App',
      isPinned: false
    },
    {
      id: 2,
      author: '@user123',
      time: '1 hour ago',
      content: 'Hoàn tiền quá chậm, không ai nghe máy.',
      sentiment: 'Tiêu cực: 92%',
      aspect: 'CSKH',
      isPinned: true
    }
  ]);

  const togglePin = (mentionId: number) => {
    setMentions(mentions.map(m => m.id === mentionId ? { ...m, isPinned: !m.isPinned } : m));
  };

  return (
    <ReporterLayout activeTopNav="reports">
      <div className="min-h-screen bg-[#f8fafc] -m-6 flex flex-col font-sans text-[#1e293b]">
        
        {/* 1. SUB-HEADER BAR: Breadcrumb & Trạng thái lưu */}
        <div className="bg-white border-b border-gray-200 px-6 py-3.5 flex flex-col sm:flex-row sm:items-center justify-between gap-3 shadow-2xs">
          <div>
            {/* Breadcrumb path */}
            <div className="flex items-center gap-1.5 text-xs text-gray-500 font-medium mb-1">
              <span>Bảng công việc</span>
              <ChevronRight className="w-3.5 h-3.5 text-gray-400" />
              <span>Yêu cầu #{id || '1042'}</span>
              <ChevronRight className="w-3.5 h-3.5 text-gray-400" />
              <span className="text-gray-800 font-semibold">Tác nghiệp</span>
            </div>
            {/* Project Title */}
            <h2 className="text-lg font-bold text-[#0f172a]">
              Phân tích: Khủng hoảng PetCareHub
            </h2>
          </div>

          {/* Action right stats */}
          <div className="flex items-center gap-4 self-end sm:self-auto">
            <div className="flex items-center gap-1.5 px-3 py-1.5 bg-slate-100 text-slate-700 rounded-lg text-xs font-semibold">
              <Clock className="w-3.5 h-3.5 text-slate-500" />
              <span>Đã làm: 01:15:00</span>
            </div>
            <button className="inline-flex items-center gap-1.5 px-4 py-1.5 bg-[#0f172a] hover:bg-slate-800 text-white rounded-lg text-xs font-bold transition-colors shadow-2xs cursor-pointer">
              <Save className="w-3.5 h-3.5" />
              Lưu Nháp
            </button>
          </div>
        </div>

        {/* WORKSPACE CONTENT AREA */}
        <div className="flex-1 grid grid-cols-1 lg:grid-cols-12 gap-0 overflow-hidden">
          
          {/* 2. CỘT TRÁI (5/12): Quản lý luồng dữ liệu thu thập & Bộ lọc */}
          <div className="lg:col-span-5 border-r border-gray-200 flex flex-col bg-white">
            
            {/* Khung bộ lọc nhanh */}
            <div className="p-4 border-b border-gray-100 bg-slate-50/50 grid grid-cols-3 gap-2">
              <div>
                <label className="text-[10px] font-bold text-gray-400 uppercase tracking-wider block mb-1">Platform</label>
                <div className="relative">
                  <select 
                    value={selectedPlatform}
                    onChange={(e) => setSelectedPlatform(e.target.value)}
                    className="w-full bg-white border border-gray-200 rounded-lg pl-2.5 pr-8 py-1.5 text-xs font-medium text-gray-700 appearance-none focus:outline-none focus:border-teal-500 cursor-pointer shadow-2xs"
                  >
                    <option>Tất cả nền tảng</option>
                    <option>Facebook</option>
                    <option>TikTok</option>
                  </select>
                  <ChevronDown className="w-3.5 h-3.5 text-gray-400 absolute right-2.5 top-1/2 -translate-y-1/2 pointer-events-none" />
                </div>
              </div>

              <div>
                <label className="text-[10px] font-bold text-gray-400 uppercase tracking-wider block mb-1">Sentiment</label>
                <div className="relative">
                  <select 
                    value={selectedSentiment}
                    onChange={(e) => setSelectedSentiment(e.target.value)}
                    className="w-full bg-red-50 border border-red-200 rounded-lg pl-2.5 pr-8 py-1.5 text-xs font-bold text-red-600 appearance-none focus:outline-none cursor-pointer shadow-2xs"
                  >
                    <option>Tiêu cực</option>
                    <option>Tích cực</option>
                    <option>Trung lập</option>
                  </select>
                  <ChevronDown className="w-3.5 h-3.5 text-red-400 absolute right-2.5 top-1/2 -translate-y-1/2 pointer-events-none" />
                </div>
              </div>

              <div>
                <label className="text-[10px] font-bold text-gray-400 uppercase tracking-wider block mb-1">Aspect</label>
                <div className="relative">
                  <select 
                    value={selectedAspect}
                    onChange={(e) => setSelectedAspect(e.target.value)}
                    className="w-full bg-white border border-gray-200 rounded-lg pl-2.5 pr-8 py-1.5 text-xs font-medium text-gray-700 appearance-none focus:outline-none focus:border-teal-500 cursor-pointer shadow-2xs"
                  >
                    <option>Lỗi App</option>
                    <option>Thanh toán</option>
                    <option>Chất lượng</option>
                  </select>
                  <ChevronDown className="w-3.5 h-3.5 text-gray-400 absolute right-2.5 top-1/2 -translate-y-1/2 pointer-events-none" />
                </div>
              </div>
            </div>

            {/* Danh sách Mentions thu thập từ mạng xã hội */}
            <div className="flex-1 overflow-y-auto p-4 space-y-3 bg-slate-50/30">
              {mentions.map((mention) => (
                <div 
                  key={mention.id} 
                  className={`bg-white border rounded-xl p-4 transition-all shadow-2xs ${
                    mention.isPinned ? 'border-teal-500 ring-1 ring-teal-500/20' : 'border-gray-200 hover:border-gray-300'
                  }`}
                >
                  <div className="flex items-center justify-between mb-1.5">
                    <span className="font-bold text-xs text-slate-800">{mention.author}</span>
                    <span className="text-[11px] text-gray-400 font-medium">{mention.time}</span>
                  </div>
                  
                  <p className="text-xs text-gray-600 leading-relaxed font-medium mb-3">
                    {mention.content}
                  </p>

                  <div className="flex items-center justify-between pt-1">
                    <div className="flex items-center gap-1.5">
                      <span className="px-2 py-0.5 bg-red-50 text-red-500 text-[10px] font-bold rounded-md border border-red-100">
                        {mention.sentiment}
                      </span>
                      <span className="px-2 py-0.5 bg-slate-100 text-slate-500 text-[10px] font-semibold rounded-md">
                        {mention.aspect}
                      </span>
                    </div>

                    <button
                      onClick={() => togglePin(mention.id)}
                      className={`inline-flex items-center gap-1 px-2.5 py-1 text-[11px] font-bold rounded-lg transition-colors cursor-pointer ${
                        mention.isPinned 
                          ? 'bg-teal-600 text-white shadow-2xs' 
                          : 'border border-gray-200 text-slate-600 hover:bg-slate-50'
                      }`}
                    >
                      <Pin className={`w-3 h-3 ${mention.isPinned ? 'fill-white' : ''}`} />
                      {mention.isPinned ? 'Đã ghim' : 'Ghim vào Báo cáo'}
                    </button>
                  </div>
                </div>
              ))}
            </div>

            {/* Đáy cột trái: Biểu đồ Volume lượng thảo luận (Mention Volume) */}
            <div className="p-4 border-t border-gray-100 bg-white">
              <div className="flex items-center justify-between mb-3">
                <span className="text-[11px] font-bold text-gray-400 uppercase tracking-wider block">
                  Mention Volume (Last 24h)
                </span>
                <span className="text-xs font-bold text-teal-600">Total: 12,450</span>
              </div>
              <div className="h-16 flex items-end gap-1.5 pt-2 px-1">
                <div className="w-full bg-slate-100 h-[20%] rounded-t-sm"></div>
                <div className="w-full bg-slate-100 h-[35%] rounded-t-sm"></div>
                <div className="w-full bg-slate-100 h-[15%] rounded-t-sm"></div>
                <div className="w-full bg-slate-100 h-[50%] rounded-t-sm"></div>
                <div className="w-full bg-slate-100 h-[25%] rounded-t-sm"></div>
                <div className="w-full bg-teal-500 h-[85%] rounded-t-sm relative group">
                  <div className="absolute -top-6 left-1/2 -translate-x-1/2 bg-slate-800 text-white text-[9px] px-1 py-0.5 rounded opacity-0 group-hover:opacity-100 transition-opacity whitespace-nowrap">Peak</div>
                </div>
                <div className="w-full bg-slate-200 h-[40%] rounded-t-sm"></div>
                <div className="w-full bg-slate-100 h-[30%] rounded-t-sm"></div>
                <div className="w-full bg-slate-100 h-[10%] rounded-t-sm"></div>
              </div>
            </div>

          </div>

          {/* 3. CỘT PHẢI (7/12): Không gian soạn thảo Báo cáo chuyên sâu */}
          <div className="lg:col-span-7 flex flex-col bg-slate-50/50 p-6 overflow-y-auto">
            
            {/* Thanh công cụ định dạng Editor (Rich Text Toolbar) */}
            <div className="w-full bg-white border border-gray-200 rounded-t-xl px-4 py-2 flex items-center justify-between shadow-2xs">
              <div className="flex items-center gap-1">
                <button className="p-1.5 text-gray-500 hover:text-slate-800 hover:bg-slate-100 rounded-md transition-colors cursor-pointer"><Bold className="w-4 h-4" /></button>
                <button className="p-1.5 text-gray-500 hover:text-slate-800 hover:bg-slate-100 rounded-md transition-colors cursor-pointer"><Italic className="w-4 h-4" /></button>
                <button className="p-1.5 text-gray-500 hover:text-slate-800 hover:bg-slate-100 rounded-md transition-colors cursor-pointer"><Underline className="w-4 h-4" /></button>
                <div className="w-px h-4 bg-gray-200 mx-1"></div>
                <button className="p-1.5 text-gray-500 hover:text-slate-800 hover:bg-slate-100 rounded-md transition-colors cursor-pointer"><List className="w-4 h-4" /></button>
                <button className="p-1.5 text-gray-500 hover:text-slate-800 hover:bg-slate-100 rounded-md transition-colors cursor-pointer"><ListOrdered className="w-4 h-4" /></button>
                <div className="w-px h-4 bg-gray-200 mx-1"></div>
                <button className="p-1.5 flex items-center gap-1 text-xs font-semibold text-gray-500 hover:text-slate-800 hover:bg-slate-100 rounded-md px-2 py-1 transition-colors cursor-pointer">
                  <BarChart3 className="w-3.5 h-3.5" /> Chart
                </button>
              </div>
              
              <button className="text-xs font-bold text-gray-400 flex items-center gap-1 px-2 py-1 border border-gray-200 rounded-md bg-slate-50/50">
                <FileText className="w-3.5 h-3.5 text-red-400" /> PDF
              </button>
            </div>

            {/* Nội dung trang giấy soạn thảo */}
            <div className="bg-white border-x border-b border-gray-200 rounded-b-xl p-8 min-h-[500px] shadow-sm flex flex-col justify-between">
              
              <div className="space-y-6">
                {/* Mục 1: Nguyên nhân cốt lõi */}
                <div>
                  <h3 className="text-base font-bold text-slate-900 mb-2">
                    1. Nguyên nhân cốt lõi (Root Cause)
                  </h3>
                  <p className="text-sm text-slate-600 leading-relaxed">
                    Căn cứ vào dữ liệu Social Listening thu thập được, khủng hoảng truyền thông của PetCareHub đạt đỉnh (peak) vào ngày <span className="font-bold text-slate-800">02/05 lúc 19:00</span>. Phân tích ngữ nghĩa cho thấy <span className="font-bold text-slate-800 text-red-600">85% lượng thảo luận tiêu cực</span> tập trung trực tiếp vào sự cố cổng thanh toán VNPay.
                  </p>
                </div>

                {/* Mục 2: Bình luận tiêu biểu */}
                <div>
                  <h3 className="text-base font-bold text-slate-900 mb-3">
                    2. Bình luận tiêu biểu
                  </h3>
                  
                  {/* Trích dẫn nội dung bình luận từ social ghim sang */}
                  <div className="border-l-4 border-teal-500 bg-teal-50/30 px-4 py-3 rounded-r-lg">
                    <span className="text-[11px] font-bold text-teal-700 block mb-1">
                      @bocphot.app (Ghim từ Dữ liệu thô)
                    </span>
                    <p className="text-sm italic text-slate-700 font-medium">
                      "Thanh toán VNPay lỗi trừ tiền 2 lần, app tệ!"
                    </p>
                  </div>
                </div>
              </div>

              {/* Nút quay lại */}
              <div className="pt-8 border-t border-gray-100 flex items-center justify-end">
                <Link
                  to="/reporter/performance"
                  className="inline-flex items-center gap-2 px-5 py-2.5 bg-[#0f172a] hover:bg-slate-800 text-white rounded-xl text-xs font-bold transition-all shadow-md cursor-pointer"
                >
                  <CheckCircle2 className="w-4 h-4 text-teal-400" />
                  Đóng & Quay lại Bàn giao
                </Link>
              </div>

            </div>

          </div>

        </div>

      </div>
    </ReporterLayout>
  );
}