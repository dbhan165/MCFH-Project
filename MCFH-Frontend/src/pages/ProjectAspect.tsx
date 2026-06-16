import { BarChart2, CheckCircle2, AlertCircle } from 'lucide-react';

const ProjectAspect = () => {
  const aspects = [
    { name: 'Giá cả (Price)', pos: 20, neu: 30, neg: 50 },
    { name: 'Chất lượng SP (Quality)', pos: 75, neu: 15, neg: 10 },
    { name: 'Dịch vụ CSKH (Service)', pos: 40, neu: 40, neg: 20 },
    { name: 'Giao hàng (Shipping)', pos: 15, neu: 20, neg: 65 },
  ];

  return (
    <div className="animate-in fade-in duration-500 max-w-6xl mx-auto space-y-6">
      <div className="mb-8">
        <h2 className="text-2xl font-bold flex items-center gap-2 text-white"><BarChart2 className="text-green-400"/> Phân tích Khía cạnh (Aspect Analysis)</h2>
        <p className="text-gray-400 text-sm mt-1">Đào sâu xem khách hàng đang thảo luận Tích cực/Tiêu cực về những tính năng/dịch vụ nào.</p>
      </div>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6 mb-6">
        <div className="bg-[#151B2B] border border-white/5 rounded-2xl p-6 flex items-center gap-4">
          <div className="w-12 h-12 rounded-full bg-emerald-500/10 flex items-center justify-center"><CheckCircle2 className="text-emerald-500" /></div>
          <div>
            <p className="text-sm text-gray-400">Khía cạnh được khen nhiều nhất</p>
            <h4 className="text-xl font-bold text-white">Chất lượng SP (75%)</h4>
          </div>
        </div>
        <div className="bg-[#151B2B] border border-white/5 rounded-2xl p-6 flex items-center gap-4">
          <div className="w-12 h-12 rounded-full bg-[#FF7575]/10 flex items-center justify-center"><AlertCircle className="text-[#FF7575]" /></div>
          <div>
            <p className="text-sm text-gray-400">Khía cạnh bị phàn nàn nhiều nhất</p>
            <h4 className="text-xl font-bold text-white">Giao hàng (65%)</h4>
          </div>
        </div>
      </div>

      <div className="bg-[#151B2B] border border-white/5 rounded-2xl p-8">
        <div className="flex justify-between items-end mb-8">
          <h3 className="font-bold text-white">Chi tiết Cảm xúc theo Khía cạnh</h3>
          <div className="flex gap-4 text-xs font-semibold">
            <div className="flex items-center gap-1.5"><span className="w-3 h-3 rounded-sm bg-[#00B4D8]"></span> Tích cực</div>
            <div className="flex items-center gap-1.5"><span className="w-3 h-3 rounded-sm bg-yellow-500"></span> Trung lập</div>
            <div className="flex items-center gap-1.5"><span className="w-3 h-3 rounded-sm bg-[#FF7575]"></span> Tiêu cực</div>
          </div>
        </div>

        <div className="space-y-8">
          {aspects.map((asp) => (
            <div key={asp.name}>
              <div className="text-sm font-semibold text-gray-300 mb-2">{asp.name}</div>
              {/* Stacked Bar Chart bằng CSS Flexbox */}
              <div className="w-full h-6 flex rounded-md overflow-hidden bg-[#0A101D]">
                <div style={{ width: `${asp.pos}%` }} className="h-full bg-[#00B4D8] flex items-center justify-center text-[10px] font-bold text-white">{asp.pos}%</div>
                <div style={{ width: `${asp.neu}%` }} className="h-full bg-yellow-500 flex items-center justify-center text-[10px] font-bold text-white">{asp.neu}%</div>
                <div style={{ width: `${asp.neg}%` }} className="h-full bg-[#FF7575] flex items-center justify-center text-[10px] font-bold text-white">{asp.neg}%</div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
export default ProjectAspect;