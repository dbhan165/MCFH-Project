import { Users, PieChart, ExternalLink, Filter, Search } from 'lucide-react';

const ProjectInfluencers = () => {
  // Mock data danh sách KOLs
  const influencers = [
    { id: 1, name: 'Lê Diệp Kiều Trang', platform: 'Facebook', followers: '1.2M', mentions: 45, sov: '35%', sentiment: 'Positive', avatar: 'https://i.pravatar.cc/150?img=5' },
    { id: 2, name: 'Vũ Dino', platform: 'TikTok', followers: '850K', mentions: 32, sov: '25%', sentiment: 'Neutral', avatar: 'https://i.pravatar.cc/150?img=11' },
    { id: 3, name: 'Hà Linh Official', platform: 'YouTube', followers: '3.5M', mentions: 15, sov: '15%', sentiment: 'Negative', avatar: 'https://i.pravatar.cc/150?img=9' },
  ];

  return (
    <div className="animate-in fade-in duration-500 max-w-6xl mx-auto">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between mb-8 gap-4">
        <div>
          <h2 className="text-2xl font-bold flex items-center gap-2 text-white"><Users className="text-[#00B4D8]"/> KOLs & Influencers</h2>
          <p className="text-gray-400 text-sm mt-1">Phân tích tỷ lệ tiếng nói (Share of Voice) và hiệu quả của các Influencer.</p>
        </div>
        <div className="flex items-center gap-3">
          <div className="relative">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-500" />
            <input type="text" placeholder="Tìm KOL..." className="pl-9 pr-4 py-2.5 bg-[#151B2B] border border-white/10 rounded-lg text-sm text-white focus:border-[#00B4D8] focus:outline-none" />
          </div>
          <button className="flex items-center justify-center gap-2 text-sm text-gray-300 hover:text-white bg-[#151B2B] px-4 py-2.5 rounded-lg border border-white/5 transition-colors">
            <Filter size={16} /> Lọc
          </button>
        </div>
      </div>

      {/* Share of Voice Chart (Placeholder) */}
      <div className="bg-[#151B2B] border border-white/5 rounded-2xl p-6 mb-8 flex flex-col md:flex-row items-center gap-8">
        <div className="w-48 h-48 rounded-full border-8 border-white/5 border-t-[#00B4D8] border-r-[#FF7575] border-b-yellow-500 flex items-center justify-center">
          <PieChart size={40} className="text-gray-500" />
        </div>
        <div className="flex-1 w-full">
          <h3 className="text-lg font-bold mb-4">Biểu đồ Share of Voice (SOV)</h3>
          <div className="space-y-4">
            <div>
              <div className="flex justify-between text-sm mb-1"><span className="text-gray-300">Lê Diệp Kiều Trang (Facebook)</span><span className="font-bold text-[#00B4D8]">35%</span></div>
              <div className="w-full bg-[#0A101D] h-2 rounded-full"><div className="bg-[#00B4D8] w-[35%] h-full rounded-full"></div></div>
            </div>
            <div>
              <div className="flex justify-between text-sm mb-1"><span className="text-gray-300">Vũ Dino (TikTok)</span><span className="font-bold text-[#FF7575]">25%</span></div>
              <div className="w-full bg-[#0A101D] h-2 rounded-full"><div className="bg-[#FF7575] w-[25%] h-full rounded-full"></div></div>
            </div>
            <div>
              <div className="flex justify-between text-sm mb-1"><span className="text-gray-300">Hà Linh Official (YouTube)</span><span className="font-bold text-yellow-500">15%</span></div>
              <div className="w-full bg-[#0A101D] h-2 rounded-full"><div className="bg-yellow-500 w-[15%] h-full rounded-full"></div></div>
            </div>
          </div>
        </div>
      </div>

      {/* Danh sách Influencers */}
      <div className="bg-[#0A101D] border border-white/5 rounded-2xl overflow-hidden">
        <table className="w-full text-left">
          <thead>
            <tr className="bg-white/[0.02] border-b border-white/5">
              <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase tracking-wider">Influencer</th>
              <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase tracking-wider">Nền tảng</th>
              <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase tracking-wider text-right">Followers</th>
              <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase tracking-wider text-right">Tổng Mentions</th>
              <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase tracking-wider text-center">Sentiment</th>
              <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase tracking-wider text-right">Link</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-white/5">
            {influencers.map((kol) => (
              <tr key={kol.id} className="hover:bg-white/[0.02] transition-colors">
                <td className="px-6 py-4 flex items-center gap-3">
                  <img src={kol.avatar} alt="avatar" className="w-10 h-10 rounded-full border border-white/10" />
                  <span className="font-bold text-white">{kol.name}</span>
                </td>
                <td className="px-6 py-4 text-sm text-gray-300">{kol.platform}</td>
                <td className="px-6 py-4 text-sm font-semibold text-right">{kol.followers}</td>
                <td className="px-6 py-4 text-sm font-bold text-[#00B4D8] text-right">{kol.mentions}</td>
                <td className="px-6 py-4 text-center">
                  <span className={`inline-block px-2.5 py-1 rounded-full text-xs font-semibold ${
                    kol.sentiment === 'Positive' ? 'bg-emerald-500/10 text-emerald-500' :
                    kol.sentiment === 'Negative' ? 'bg-[#FF7575]/10 text-[#FF7575]' : 'bg-yellow-500/10 text-yellow-500'
                  }`}>{kol.sentiment}</span>
                </td>
                <td className="px-6 py-4 text-right">
                  <button className="text-gray-400 hover:text-white transition-colors"><ExternalLink size={18} className="inline"/></button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default ProjectInfluencers;