import { MessageCircle, Filter, Tag, Plus, ExternalLink, MoreVertical } from 'lucide-react';

const ProjectMentions = () => {
  const mentions = [
    { id: 1, author: 'Nguyễn Văn Khách', platform: 'FB', content: 'Sản phẩm giao hơi chậm nhưng chất lượng tốt.', sentiment: 'Neutral', tags: ['Giao hàng'] },
  ];

  return (
    <div className="animate-in fade-in duration-500 max-w-5xl mx-auto">
      <div className="flex justify-between mb-8">
        <div>
          <h2 className="text-2xl font-bold flex items-center gap-2 text-white"><MessageCircle className="text-[#FF7575]"/> Data Stream (Mentions)</h2>
          <p className="text-gray-400 text-sm mt-1">Danh sách lượt nhắc đến</p>
        </div>
        <button className="flex items-center gap-2 text-sm text-gray-300 bg-[#151B2B] px-4 py-2.5 rounded-lg border border-white/5"><Filter size={16} /> Lọc</button>
      </div>
      
      <div className="space-y-5">
        {mentions.map((item) => (
          <div key={item.id} className="bg-[#151B2B] border border-white/5 rounded-2xl p-6 group">
            <div className="flex justify-between items-start mb-4">
              <div className="flex items-center gap-3">
                <div className="w-10 h-10 rounded-full flex items-center justify-center font-bold text-sm bg-blue-500/20 text-blue-400">{item.platform}</div>
                <h4 className="font-bold text-white">{item.author}</h4>
              </div>
              <span className="px-3 py-1 border rounded-full text-xs font-semibold bg-yellow-500/10 text-yellow-500 border-yellow-500/20">{item.sentiment}</span>
            </div>
            <p className="text-gray-300 text-sm mb-6">"{item.content}"</p>
            <div className="flex justify-between pt-4 border-t border-white/5">
              <div className="flex items-center gap-2">
                <Tag size={16} className="text-gray-500" />
                <span className="text-xs bg-[#0A101D] text-gray-300 px-2.5 py-1 rounded-md">{item.tags[0]}</span>
                <button className="text-xs text-gray-400 border border-dashed px-2.5 py-1 rounded-md flex items-center"><Plus size={12}/> Thêm Tag</button>
              </div>
              <div className="flex gap-2">
                <button className="flex items-center gap-2 text-sm text-[#00B4D8] bg-[#00B4D8]/10 px-4 py-2 rounded-lg"><ExternalLink size={16}/> Xem bài gốc</button>
                <button className="p-2 text-gray-400 bg-[#0A101D] rounded-lg"><MoreVertical size={16}/></button>
              </div>
            </div>
          </div>
        ))}
      </div>
    </div>
  );
};
export default ProjectMentions;