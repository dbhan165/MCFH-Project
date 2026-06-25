import { useState } from 'react';
import { 
  Search, Plus, Bell, ArrowRight, EyeOff, Play, Pause, 
  MoreVertical, Building2, UserPlus, Calendar, User 
} from 'lucide-react';
import { Link, useParams, useNavigate } from 'react-router-dom';

// Nhúng Common Modal vào
import ConfirmModal from '../components/common/ConfirmModal';

const Projects = () => {
  const { workspaceId } = useParams();
  const navigate = useNavigate();

  const [projectList, setProjectList] = useState([
    {
      id: 1,
      title: 'Giám sát phản hồi PetCareHub',
      createdAt: '10/05/2026',
      owner: 'DB Han',
      status: 'Active',
      statusText: 'Đang quét (Real-time)',
      statusColor: 'bg-[#00B4D8]/10 text-[#00B4D8] border-[#00B4D8]/20',
      dotColor: 'bg-[#00B4D8]',
      totalMentions: '12,450',
      nsr: '68',
      nsrColor: 'text-[#00B4D8]',
      ratioText: '80% Positive',
      ratioColor: 'bg-[#00B4D8]',
      ratioWidth: 'w-[80%]',
      hoverBorder: 'hover:border-[#00B4D8]/30 hover:shadow-[#00B4D8]/5'
    },
    {
      id: 2,
      title: 'Khủng hoảng truyền thông Lô hàng lỗi',
      createdAt: '20/06/2026',
      owner: 'Acma Agency',
      status: 'Active',
      statusText: 'Mức độ nghiêm trọng cao',
      statusColor: 'bg-[#FF7575]/10 text-[#FF7575] border-[#FF7575]/20',
      dotColor: 'bg-[#FF7575]',
      totalMentions: '45,000',
      nsr: '-45',
      nsrColor: 'text-[#FF7575]',
      ratioText: '70% Negative',
      ratioColor: 'bg-[#FF7575]',
      ratioWidth: 'w-[70%]',
      hoverBorder: 'hover:border-[#FF7575]/30 hover:shadow-[#FF7575]/5'
    },
    {
      id: 3,
      title: 'Theo dõi Đối thủ cạnh tranh Q2',
      createdAt: '01/04/2026',
      owner: 'System Auto',
      status: 'Disabled',
      statusText: 'Đã tạm dừng',
      statusColor: 'bg-white/10 text-gray-400 border-white/5',
      dotColor: 'bg-gray-500',
      totalMentions: '5,200',
      nsr: '12',
      nsrColor: 'text-yellow-500',
      ratioText: 'Neutral',
      ratioColor: 'bg-yellow-500',
      ratioWidth: 'w-[50%]',
      hoverBorder: 'hover:border-yellow-500/30 hover:shadow-yellow-500/5'
    }
  ]);

  const [activeMenuId, setActiveMenuId] = useState<number | null>(null);

  // State quản lý Modal Xác nhận
  const [confirmModal, setConfirmModal] = useState<{
    isOpen: boolean;
    projectId: number | null;
    projectName: string;
    action: 'disable' | 'enable' | null;
  }>({
    isOpen: false,
    projectId: null,
    projectName: '',
    action: null
  });

  // Hàm kích hoạt/vô hiệu hóa thực sự (chỉ chạy khi bấm Đồng ý trên Modal)
  const executeToggleStatus = () => {
    if (confirmModal.projectId !== null) {
      setProjectList(prev => prev.map(proj => {
        if (proj.id === confirmModal.projectId) {
          const isDisabled = proj.status === 'Disabled';
          return {
            ...proj,
            status: isDisabled ? 'Active' : 'Disabled',
            statusText: isDisabled ? 'Đang quét (Real-time)' : 'Đã tạm dừng',
            statusColor: isDisabled ? 'bg-[#00B4D8]/10 text-[#00B4D8] border-[#00B4D8]/20' : 'bg-white/10 text-gray-400 border-white/5',
            dotColor: isDisabled ? 'bg-[#00B4D8]' : 'bg-gray-500'
          };
        }
        return proj;
      }));
    }
    // Đóng modal sau khi chạy xong
    setConfirmModal(prev => ({ ...prev, isOpen: false }));
  };

  return (
    <div className="flex flex-col h-full text-white bg-[#050A15]">
      
      <header className="h-20 bg-[#0A101D]/80 backdrop-blur-md border-b border-white/5 flex items-center justify-between px-8 sticky top-0 z-20 shrink-0">
        <div className="text-gray-300 font-medium tracking-wide">Quản lý Dự án</div>
        
        <div className="flex items-center gap-6">
          <div className="flex items-center gap-2 bg-[#151B2B] border border-white/5 px-4 py-2 rounded-lg text-sm text-gray-400 select-none">
            <Building2 size={16} className="text-[#FF7575]" />
            Đang hiển thị tại: <span className="font-semibold text-white">Workspace #{workspaceId}</span>
          </div>

          <button className="relative text-gray-400 hover:text-white transition-colors">
            <Bell className="w-5 h-5" />
            <span className="absolute -top-1 -right-1 w-2.5 h-2.5 border-2 border-[#0A101D] bg-[#FF7575] rounded-full"></span>
          </button>

          <Link 
            to={`/workspace/${workspaceId}/members`}
            className="hidden sm:flex items-center gap-2 bg-[#151B2B] hover:bg-[#1A2235] text-gray-300 hover:text-white border border-white/10 px-4 py-2 rounded-lg text-sm font-semibold transition-all shadow-sm"
          >
            <UserPlus size={16} />
            Mời thành viên
          </Link>
        </div>
      </header>

      <div className="flex-1 p-8 md:p-10 relative z-10 overflow-y-auto">
        
        <div 
          className="absolute inset-0 z-0 opacity-[0.03] pointer-events-none"
          style={{ backgroundImage: 'radial-gradient(#ffffff 1px, transparent 1px)', backgroundSize: '32px 32px' }}
        ></div>

        <div className="relative z-10">
          <div className="flex flex-col md:flex-row md:items-end justify-between gap-6 mb-10">
            <h1 className="text-4xl font-bold tracking-tight leading-tight text-white">
              Tổng quan Dự án <br /> <span className="text-gray-400 text-2xl font-normal">(Overview)</span>
            </h1>
            
            <div className="flex items-center gap-4">
              <div className="relative hidden sm:block">
                <Search className="absolute left-4 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-500" />
                <input 
                  type="text" 
                  placeholder="Tìm kiếm dự án..." 
                  className="w-64 bg-[#151B2B] border border-white/10 rounded-lg pl-11 pr-4 py-3 text-sm text-white placeholder-gray-500 focus:outline-none focus:border-[#FF7575]/50 focus:ring-1 focus:ring-[#FF7575]/50 transition-all shadow-inner"
                />
              </div>
              
              <Link 
                to={`/create-project?wid=${workspaceId}`} 
                className="bg-[#FF7575] hover:bg-[#ff6262] text-white px-5 py-3 rounded-lg text-sm font-semibold flex items-center gap-2 transition-all shadow-[0_4px_14px_0_rgba(255,117,117,0.39)] hover:-translate-y-0.5 shrink-0"
              >
                <Plus className="w-5 h-5" strokeWidth={2.5} />
                Khởi tạo Dự án mới
              </Link>
            </div>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            
            {projectList.map((project) => (
              <div 
                key={project.id} 
                onClick={() => {
                  if (project.status !== 'Disabled') {
                    navigate(`/workspace/${workspaceId}/project/${project.id}`);
                  }
                }}
                className={`bg-[#151B2B] border border-white/5 rounded-2xl p-6 flex flex-col transition-all group relative ${
                  project.status === 'Disabled' 
                  ? 'opacity-60 cursor-not-allowed' 
                  : `cursor-pointer ${project.hoverBorder}`
                }`}
              >
                <div className="absolute top-6 right-6 z-20">
                  <button 
                    onClick={(e) => {
                      e.stopPropagation();
                      setActiveMenuId(activeMenuId === project.id ? null : project.id);
                    }}
                    className="p-1.5 rounded-lg bg-[#0A101D] border border-white/5 text-gray-400 hover:text-white transition-colors"
                  >
                    <MoreVertical size={16} />
                  </button>

                  {activeMenuId === project.id && (
                    <div 
                      onClick={(e) => e.stopPropagation()} 
                      className="absolute right-0 mt-2 w-48 bg-[#0A101D] border border-white/10 rounded-xl shadow-2xl overflow-hidden py-1.5 z-30 animate-in fade-in slide-in-from-top-2"
                    >
                      <button 
                        onClick={() => {
                          const isDisabling = project.status !== 'Disabled';
                          setConfirmModal({
                            isOpen: true,
                            projectId: project.id,
                            projectName: project.title,
                            action: isDisabling ? 'disable' : 'enable'
                          });
                          setActiveMenuId(null);
                        }}
                        className="w-full flex items-center gap-2.5 px-4 py-2 text-sm text-left text-gray-300 hover:text-white hover:bg-white/5 transition-colors"
                      >
                        {project.status === 'Disabled' ? (
                          <>
                            <Play size={14} className="text-emerald-400" /> Kích hoạt dự án
                          </>
                        ) : (
                          <>
                            <Pause size={14} className="text-[#FF7575]" /> Vô hiệu hóa (Disable)
                          </>
                        )}
                      </button>
                      <button className="w-full flex items-center gap-2.5 px-4 py-2 text-sm text-left text-gray-500 cursor-not-allowed">
                        <EyeOff size={14} /> Ẩn khỏi dòng thời gian
                      </button>
                    </div>
                  )}
                </div>

                <h3 className={`text-2xl font-bold mb-3 pr-6 leading-snug transition-colors ${project.status === 'Disabled' ? 'text-gray-500 line-through' : 'group-hover:text-[#FF7575]'}`}>
                  {project.title}
                </h3>

                <div className={`inline-flex items-center gap-2 border px-3 py-1.5 rounded-md text-xs font-semibold w-fit mb-4 ${project.statusColor}`}>
                  <span className={`w-2 h-2 rounded-full ${project.dotColor} ${project.status !== 'Disabled' ? 'animate-pulse' : ''}`}></span>
                  {project.statusText}
                </div>

                {/* CREATION DATE & OWNER */}
                <div className="flex items-center gap-3 mb-8">
                  <div className="flex items-center gap-1.5 text-[11px] font-medium text-gray-400 bg-white/5 px-2.5 py-1 rounded-md border border-white/5">
                    <Calendar size={12} className="text-gray-500" />
                    {project.createdAt}
                  </div>
                  <div className="flex items-center gap-1.5 text-[11px] font-medium text-gray-400 bg-white/5 px-2.5 py-1 rounded-md border border-white/5">
                    <User size={12} className="text-gray-500" />
                    {project.owner}
                  </div>
                </div>
                
                <div className="grid grid-cols-2 gap-4 mb-6">
                  <div>
                    <div className="text-gray-400 text-xs font-semibold uppercase tracking-wider mb-1">Tổng thảo luận</div>
                    <div className="text-2xl font-bold">{project.status === 'Disabled' ? '---' : project.totalMentions}</div>
                  </div>
                  <div className="text-right">
                    <div className="text-gray-400 text-xs font-semibold uppercase tracking-wider mb-1">Điểm NSR</div>
                    <div className={`text-2xl font-bold ${project.status === 'Disabled' ? 'text-gray-600' : project.nsrColor}`}>{project.nsr}</div>
                  </div>
                </div>

                <div className="mb-8 mt-auto">
                  <div className="flex justify-between text-xs font-medium mb-2">
                    <span className="text-gray-400 uppercase tracking-wider">Sentiment Ratio</span>
                    <span className={project.status === 'Disabled' ? 'text-gray-600' : 'text-gray-300'}>{project.ratioText}</span>
                  </div>
                  <div className="h-1.5 w-full bg-[#0A101D] rounded-full overflow-hidden flex">
                    {project.status !== 'Disabled' ? (
                      <div className={`h-full ${project.ratioColor} ${project.ratioWidth} rounded-full shadow-[0_0_10px_rgba(255,117,117,0.3)]`}></div>
                    ) : (
                      <div className="h-full bg-gray-700 w-full rounded-full"></div>
                    )}
                  </div>
                </div>

                <div 
                  className={`w-full py-3 rounded-lg text-sm font-semibold flex items-center justify-center gap-2 transition-all border ${
                    project.status === 'Disabled' 
                    ? 'bg-transparent text-gray-600 border-white/5 cursor-not-allowed' 
                    : 'bg-[#0A101D] group-hover:bg-white/5 text-gray-400 group-hover:text-white border-white/5 group-hover:border-white/10'
                  }`}
                >
                  Vào Dashboard Phân tích <ArrowRight className="w-4 h-4" />
                </div>
              </div>
            ))}

          </div>
        </div>
      </div>

      {/* Đặt Common Modal ở ngoài cùng */}
      <ConfirmModal 
        isOpen={confirmModal.isOpen}
        onClose={() => setConfirmModal(prev => ({ ...prev, isOpen: false }))}
        onConfirm={executeToggleStatus}
        title={confirmModal.action === 'disable' ? 'Vô hiệu hóa Dự án?' : 'Kích hoạt lại Dự án?'}
        message={
          confirmModal.action === 'disable' 
          ? `Bạn có chắc chắn muốn vô hiệu hóa "${confirmModal.projectName}"? Hệ thống sẽ tạm dừng quét và thu thập dữ liệu thảo luận mới cho dự án này.`
          : `Kích hoạt lại "${confirmModal.projectName}"? Hệ thống sẽ tiếp tục thu thập dữ liệu theo tiến trình.`
        }
        confirmText={confirmModal.action === 'disable' ? 'Vô hiệu hóa' : 'Kích hoạt'}
        cancelText="Hủy bỏ"
        type={confirmModal.action === 'disable' ? 'warning' : 'danger'}
      />

    </div>
  );
};

export default Projects;