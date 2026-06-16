import { PieChart, TrendingUp, Activity } from 'lucide-react';

const ProjectSentiment = () => {
  return (
    <div className="animate-in fade-in duration-500 max-w-7xl mx-auto space-y-6">
      <div className="mb-8">
        <h2 className="text-2xl font-bold flex items-center gap-2 text-white"><PieChart className="text-[#FF7575]"/> Phân tích Cảm xúc (Sentiment)</h2>
        <p className="text-gray-400 text-sm mt-1">Đo lường thái độ của người dùng đối với chiến dịch.</p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Biểu đồ Tròn (Tổng quan) */}
        <div className="bg-[#151B2B] border border-white/5 rounded-2xl p-6 lg:col-span-1 flex flex-col items-center justify-center min-h-[350px]">
          <h3 className="font-bold text-white mb-6 w-full text-left">Tỷ lệ Cảm xúc Tổng thể</h3>
          {/* Vòng tròn CSS mô phỏng Pie Chart */}
          <div className="relative w-48 h-48 rounded-full flex items-center justify-center mb-6" 
               style={{ background: 'conic-gradient(#00B4D8 0% 60%, #FF7575 60% 85%, #EAB308 85% 100%)' }}>
            <div className="w-32 h-32 bg-[#151B2B] rounded-full flex items-center justify-center flex-col shadow-inner">
              <span className="text-2xl font-bold text-white">60%</span>
              <span className="text-xs text-gray-400">Tích cực</span>
            </div>
          </div>
          <div className="flex gap-4 text-xs font-semibold w-full justify-center">
            <div className="flex items-center gap-1.5"><span className="w-3 h-3 rounded-sm bg-[#00B4D8]"></span> Tích cực</div>
            <div className="flex items-center gap-1.5"><span className="w-3 h-3 rounded-sm bg-[#FF7575]"></span> Tiêu cực</div>
            <div className="flex items-center gap-1.5"><span className="w-3 h-3 rounded-sm bg-yellow-500"></span> Trung lập</div>
          </div>
        </div>

        {/* Biểu đồ Đường (Theo thời gian) */}
        <div className="bg-[#151B2B] border border-white/5 rounded-2xl p-6 lg:col-span-2 min-h-[350px] flex flex-col">
          <div className="flex justify-between items-center mb-6">
            <h3 className="font-bold text-white">Xu hướng cảm xúc theo ngày</h3>
            <button className="text-xs bg-[#0A101D] text-gray-400 px-3 py-1.5 rounded-lg border border-white/5 hover:text-white">7 ngày qua</button>
          </div>
          <div className="flex-1 border border-dashed border-white/5 rounded-xl flex flex-col items-center justify-center text-gray-500 bg-[#0A101D]/50 relative overflow-hidden">
            <Activity size={40} className="mb-2 text-gray-600 opacity-50" />
            <p>Khu vực nhúng Line Chart (Recharts)</p>
            {/* Giả lập các đường Line mờ mờ cho đẹp */}
            <svg className="absolute inset-0 w-full h-full opacity-20 pointer-events-none" preserveAspectRatio="none">
              <path d="M0,100 Q50,50 100,120 T200,80 T300,150 T400,60 T500,90" fill="none" stroke="#00B4D8" strokeWidth="4" />
              <path d="M0,150 Q50,180 100,140 T200,190 T300,110 T400,170 T500,140" fill="none" stroke="#FF7575" strokeWidth="4" />
            </svg>
          </div>
        </div>
      </div>
    </div>
  );
};
export default ProjectSentiment;