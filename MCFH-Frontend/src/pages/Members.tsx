import React, { useState } from 'react';
import { Search, UserPlus, MoreVertical, Shield, Mail, Trash2, Edit, CheckCircle2, Clock } from 'lucide-react';

// Mock data cho danh sách thành viên
const initialMembers = [
  { id: 1, name: 'Nguyễn Văn A', email: 'admin@acma.vn', role: 'Owner', status: 'Active', avatar: 'https://i.pravatar.cc/150?img=11' },
  { id: 2, name: 'Trần Thị B', email: 'tranb@acma.vn', role: 'Editor', status: 'Active', avatar: 'https://i.pravatar.cc/150?img=5' },
  { id: 3, name: 'Lê Văn C', email: 'levanc@acma.vn', role: 'Viewer', status: 'Pending', avatar: 'https://i.pravatar.cc/150?img=8' },
  { id: 4, name: 'Phạm Thị D', email: 'phamd@acma.vn', role: 'Editor', status: 'Active', avatar: 'https://i.pravatar.cc/150?img=9' },
];

const Members = () => {
  const [members, setMembers] = useState(initialMembers);
  const [isInviteModalOpen, setIsInviteModalOpen] = useState(false);
  const [inviteEmail, setInviteEmail] = useState('');
  const [inviteRole, setInviteRole] = useState('Viewer');

  // Xử lý Mời thành viên (Demo logic)
  const handleInvite = (e: React.FormEvent) => {
    e.preventDefault();
    if (!inviteEmail) return;
    
    const newMember = {
      id: Date.now(),
      name: 'Chờ xác nhận...',
      email: inviteEmail,
      role: inviteRole,
      status: 'Pending',
      avatar: `https://ui-avatars.com/api/?name=${inviteEmail.charAt(0)}&background=random`
    };
    
    setMembers([...members, newMember]);
    setIsInviteModalOpen(false);
    setInviteEmail('');
  };

  // Xử lý Xóa thành viên
  const handleRemove = (id: number) => {
    if(window.confirm("Bạn có chắc chắn muốn xóa thành viên này khỏi Workspace?")) {
      setMembers(members.filter(m => m.id !== id));
    }
  };

  return (
    <div className="flex flex-col h-full text-white bg-[#050A15]">
      {/* ==========================================
          HEADER
          ========================================== */}
      <header className="h-20 bg-[#0A101D]/80 backdrop-blur-md border-b border-white/5 flex items-center px-8 sticky top-0 z-20 shrink-0">
        <div className="text-gray-300 font-medium tracking-wide">Quản lý Thành viên Nhóm</div>
      </header>

      {/* ==========================================
          MAIN CONTENT
          ========================================== */}
      <div className="flex-1 p-8 md:p-10 relative z-10 overflow-y-auto">
        <div className="relative z-10 max-w-6xl mx-auto">
          
          {/* Header Section */}
          <div className="flex flex-col md:flex-row md:items-end justify-between gap-6 mb-8">
            <div>
              <h1 className="text-4xl font-bold tracking-tight leading-tight text-white mb-2">
                Thành viên Workspace
              </h1>
              <p className="text-gray-400">Quản lý những người có quyền truy cập vào không gian làm việc "Acma Agency"</p>
            </div>
            
            <div className="flex items-center gap-4">
              <div className="relative hidden sm:block">
                <Search className="absolute left-4 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-500" />
                <input 
                  type="text" 
                  placeholder="Tìm email hoặc tên..." 
                  className="w-64 bg-[#151B2B] border border-white/10 rounded-lg pl-11 pr-4 py-2.5 text-sm text-white placeholder-gray-500 focus:outline-none focus:border-[#FF7575]/50 transition-all"
                />
              </div>
              
              <button 
                onClick={() => setIsInviteModalOpen(true)}
                className="bg-[#FF7575] hover:bg-[#ff6262] text-white px-5 py-2.5 rounded-lg text-sm font-semibold flex items-center gap-2 transition-all shadow-[0_4px_14px_0_rgba(255,117,117,0.39)] shrink-0"
              >
                <UserPlus className="w-4 h-4" />
                Mời thành viên
              </button>
            </div>
          </div>

          {/* ==========================================
              BẢNG DANH SÁCH THÀNH VIÊN
              ========================================== */}
          <div className="bg-[#0A101D] border border-white/5 rounded-2xl overflow-hidden">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="border-b border-white/5 bg-white/[0.02]">
                  <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase tracking-wider">Người dùng</th>
                  <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase tracking-wider">Vai trò (Role)</th>
                  <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase tracking-wider">Trạng thái</th>
                  <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase tracking-wider text-right">Thao tác</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-white/5">
                {members.map((member) => (
                  <tr key={member.id} className="hover:bg-white/[0.02] transition-colors">
                    {/* Cột Info */}
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-4">
                        <img src={member.avatar} alt="avatar" className="w-10 h-10 rounded-full border border-white/10" />
                        <div>
                          <p className="font-bold text-white">{member.name}</p>
                          <p className="text-sm text-gray-400">{member.email}</p>
                        </div>
                      </div>
                    </td>
                    
                    {/* Cột Role */}
                    <td className="px-6 py-4">
                      <div className="flex items-center gap-2 text-sm">
                        {member.role === 'Owner' && <Shield className="w-4 h-4 text-[#FF7575]" />}
                        <span className={`font-semibold ${member.role === 'Owner' ? 'text-[#FF7575]' : 'text-gray-300'}`}>
                          {member.role}
                        </span>
                      </div>
                    </td>

                    {/* Cột Status */}
                    <td className="px-6 py-4">
                      {member.status === 'Active' ? (
                        <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-bold bg-emerald-500/10 text-emerald-500 border border-emerald-500/20">
                          <CheckCircle2 className="w-3.5 h-3.5" /> Đã tham gia
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-bold bg-yellow-500/10 text-yellow-500 border border-yellow-500/20">
                          <Clock className="w-3.5 h-3.5" /> Chờ xác nhận
                        </span>
                      )}
                    </td>

                    {/* Cột Thao tác */}
                    <td className="px-6 py-4 text-right">
                      {member.role !== 'Owner' && (
                        <div className="flex items-center justify-end gap-3">
                          <button className="p-2 text-gray-400 hover:text-white hover:bg-white/10 rounded-lg transition-colors" title="Đổi quyền">
                            <Edit className="w-4 h-4" />
                          </button>
                          <button 
                            onClick={() => handleRemove(member.id)}
                            className="p-2 text-gray-400 hover:text-[#FF7575] hover:bg-[#FF7575]/10 rounded-lg transition-colors" title="Xóa thành viên"
                          >
                            <Trash2 className="w-4 h-4" />
                          </button>
                        </div>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

        </div>
      </div>

      {/* ==========================================
          MODAL: MỜI THÀNH VIÊN
          ========================================== */}
      {isInviteModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm">
          <div className="bg-[#0A101D] border border-white/10 rounded-2xl w-full max-w-md shadow-2xl overflow-hidden animate-in fade-in zoom-in-95 duration-200">
            <div className="p-6 border-b border-white/5 flex justify-between items-center">
              <h3 className="text-xl font-bold text-white">Mời thành viên mới</h3>
              <button onClick={() => setIsInviteModalOpen(false)} className="text-gray-400 hover:text-white">✕</button>
            </div>
            
            <form onSubmit={handleInvite} className="p-6 space-y-5">
              <div className="space-y-2">
                <label className="text-xs font-bold text-gray-400 uppercase tracking-wider">Email thành viên</label>
                <div className="relative">
                  <Mail className="absolute left-3 top-1/2 -translate-y-1/2 w-5 h-5 text-gray-500" />
                  <input 
                    type="email" 
                    value={inviteEmail}
                    onChange={(e) => setInviteEmail(e.target.value)}
                    placeholder="name@company.com" 
                    required
                    className="w-full bg-[#151B2B] border border-white/10 rounded-lg pl-10 pr-4 py-3 text-white placeholder-gray-500 focus:outline-none focus:border-[#FF7575] transition-colors"
                  />
                </div>
              </div>

              <div className="space-y-2">
                <label className="text-xs font-bold text-gray-400 uppercase tracking-wider">Vai trò (Role)</label>
                <select 
                  value={inviteRole}
                  onChange={(e) => setInviteRole(e.target.value)}
                  className="w-full bg-[#151B2B] border border-white/10 rounded-lg px-4 py-3 text-white focus:outline-none focus:border-[#FF7575] transition-colors appearance-none"
                >
                  <option value="Admin">Admin (Toàn quyền)</option>
                  <option value="Editor">Editor (Có quyền tạo/sửa dự án)</option>
                  <option value="Viewer">Viewer (Chỉ xem báo cáo)</option>
                </select>
              </div>

              <div className="pt-4 flex gap-3">
                <button 
                  type="button" 
                  onClick={() => setIsInviteModalOpen(false)}
                  className="flex-1 px-4 py-3 rounded-lg font-bold text-gray-300 bg-white/5 hover:bg-white/10 transition-colors"
                >
                  Hủy
                </button>
                <button 
                  type="submit"
                  className="flex-1 px-4 py-3 rounded-lg font-bold text-white bg-[#FF7575] hover:bg-[#ff6262] transition-colors shadow-lg shadow-[#FF7575]/20"
                >
                  Gửi lời mời
                </button>
              </div>
            </form>
          </div>
        </div>
      )}

    </div>
  );
};

export default Members;