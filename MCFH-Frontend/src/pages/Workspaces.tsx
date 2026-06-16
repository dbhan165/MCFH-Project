import { useNavigate, Link } from 'react-router-dom';
import { Building2, Plus, Users, Settings, ArrowRight, Activity } from 'lucide-react';

const mockWorkspaces = [
  { id: 1, name: 'MCFH Internal Team', role: 'Owner', members: 5, activeCampaigns: 2, isVIP: true },
  { id: 2, name: 'Vinamilk Campaign 2026', role: 'Admin', members: 12, activeCampaigns: 4, isVIP: false },
  { id: 3, name: 'Tech Startup XYZ', role: 'Member', members: 3, activeCampaigns: 1, isVIP: false },
];

const Workspaces = () => {
  const navigate = useNavigate();

  const handleEnterWorkspace = (id: number) => {
    console.log("Entering workspace:", id);
    navigate(`/workspace/${id}/projects`);
  };

  return (
    <div className="min-h-screen bg-[#0A101D] text-white font-sans relative overflow-hidden selection:bg-[#FF7575] selection:text-white">
      
      <div 
        className="absolute inset-0 z-0 opacity-15 pointer-events-none"
        style={{ backgroundImage: 'radial-gradient(#ffffff 1px, transparent 1px)', backgroundSize: '32px 32px' }}
      ></div>

      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-16 relative z-10">
        
        <div className="mb-12 flex flex-col md:flex-row md:items-end justify-between gap-6">
          <div>
            <h1 className="text-4xl font-extrabold tracking-tight mb-2">Không gian làm việc</h1>
            <p className="text-[#9BA1B0] text-lg">Chọn một dự án để tiếp tục hoặc tạo không gian làm việc mới.</p>
          </div>
          
          {/* Nút Tạo Workspace mới trỏ sang trang Create */}
          <Link 
            to="/create-workspace"
            className="bg-[#FF7575] hover:bg-[#ff6262] text-white px-6 py-3 rounded-lg text-sm font-bold transition-all shadow-[0_4px_14px_0_rgba(255,117,117,0.39)] flex items-center gap-2 w-fit"
          >
            <Plus size={20} />
            Tạo Workspace mới
          </Link>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          
          {mockWorkspaces.map((ws) => (
            <div 
              key={ws.id} 
              className="group bg-gradient-to-br from-[#1A2235]/80 to-[#0A101D]/80 backdrop-blur-md border border-white/10 rounded-2xl p-6 transition-all duration-300 hover:-translate-y-1 hover:border-[#FF7575]/50 hover:shadow-[0_8px_30px_rgba(255,117,117,0.12)] flex flex-col"
            >
              <div className="flex justify-between items-start mb-6">
                <div className="w-12 h-12 rounded-xl bg-[#FF7575]/10 border border-[#FF7575]/20 flex items-center justify-center text-[#FF7575]">
                  <Building2 size={24} />
                </div>
                <span className={`px-3 py-1 rounded-full text-xs font-bold tracking-wide ${
                  ws.role === 'Owner' ? 'bg-orange-500/20 text-orange-400 border border-orange-500/20' : 
                  ws.role === 'Admin' ? 'bg-blue-500/20 text-blue-400 border border-blue-500/20' : 
                  'bg-gray-500/20 text-gray-300 border border-gray-500/20'
                }`}>
                  {ws.role}
                </span>
              </div>

              <h3 className="text-xl font-bold text-white mb-1 group-hover:text-[#FF7575] transition-colors line-clamp-1">
                {ws.name}
              </h3>
              
              <div className="flex items-center gap-4 mt-4 text-sm text-[#9BA1B0]">
                <div className="flex items-center gap-1.5">
                  <Users size={16} />
                  <span>{ws.members} thành viên</span>
                </div>
                <div className="flex items-center gap-1.5">
                  <Activity size={16} />
                  <span>{ws.activeCampaigns} chiến dịch</span>
                </div>
              </div>

              <div className="mt-8 pt-6 border-t border-white/10 flex items-center justify-between mt-auto">
                {ws.role === 'Owner' ? (
                  <Link 
                    to="/workspace-settings" 
                    className="text-gray-400 hover:text-white p-2 transition-colors rounded-lg hover:bg-white/5" 
                    title="Cài đặt Workspace"
                  >
                    <Settings size={20} />
                  </Link>
                ) : (
                  <div></div>
                )}
                
                <button 
                  onClick={() => handleEnterWorkspace(ws.id)}
                  className="flex items-center gap-2 text-sm font-bold text-[#FF7575] hover:text-[#ff6262] transition-colors group-hover:translate-x-1 duration-300"
                >
                  Truy cập <ArrowRight size={18} />
                </button>
              </div>
            </div>
          ))}

          {/* Nút Tạo Workspace mới (Card) trỏ sang trang Create */}
          <Link 
            to="/create-workspace"
            className="border-2 border-dashed border-white/10 hover:border-[#FF7575]/50 rounded-2xl p-6 flex flex-col items-center justify-center gap-4 text-[#9BA1B0] hover:text-[#FF7575] hover:bg-[#FF7575]/5 transition-all duration-300 min-h-[240px] group"
          >
            <div className="w-14 h-14 rounded-full bg-white/5 flex items-center justify-center group-hover:bg-[#FF7575]/20 group-hover:text-[#FF7575] transition-colors">
              <Plus size={28} />
            </div>
            <span className="font-bold text-lg">Khởi tạo Dự án mới</span>
          </Link>

        </div>
      </div>

    </div>
  );
};

export default Workspaces;