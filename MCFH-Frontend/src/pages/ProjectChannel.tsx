import { Share2, BarChart } from 'lucide-react';

const ProjectChannel = () => {
  const channels = [
    { name: 'Facebook', value: 65, count: '8,450', color: 'bg-blue-500' },
    { name: 'TikTok', value: 25, count: '3,250', color: 'bg-gray-200 text-black' },
    { name: 'YouTube', value: 7, count: '910', color: 'bg-red-500' },
    { name: 'Instagram', value: 3, count: '390', color: 'bg-pink-500' },
  ];

  return (
    <div className="animate-in fade-in duration-500 max-w-6xl mx-auto space-y-6">
      <div className="mb-8">
        <h2 className="text-2xl font-bold flex items-center gap-2 text-white"><Share2 className="text-purple-400"/> So sánh Kênh (Channel Comparison)</h2>
        <p className="text-gray-400 text-sm mt-1">Phân tích nền tảng mạng xã hội nào mang lại nhiều thảo luận nhất.</p>
      </div>

      <div className="bg-[#151B2B] border border-white/5 rounded-2xl p-8">
        <h3 className="font-bold text-white mb-8">Tỷ trọng Thảo luận theo Nền tảng</h3>
        
        <div className="space-y-6">
          {channels.map((ch) => (
            <div key={ch.name}>
              <div className="flex justify-between text-sm mb-2">
                <span className="font-semibold text-gray-300">{ch.name}</span>
                <span className="font-bold text-white">{ch.count} Mentions <span className="text-gray-500 font-normal">({ch.value}%)</span></span>
              </div>
              <div className="w-full bg-[#0A101D] h-4 rounded-full overflow-hidden flex">
                <div 
                  className={`h-full ${ch.color} rounded-full relative group transition-all duration-1000 ease-out`} 
                  style={{ width: `${ch.value}%` }}
                >
                  <div className="absolute inset-0 bg-white/20 opacity-0 group-hover:opacity-100 transition-opacity"></div>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
};
export default ProjectChannel;