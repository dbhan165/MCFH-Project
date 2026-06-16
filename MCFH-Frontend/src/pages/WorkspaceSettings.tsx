import { useState } from 'react';
import { Link } from 'react-router-dom';
import { 
  Settings, 
  Users, 
  Trash2, 
  UserPlus, 
  Save, 
  AlertTriangle, 
  ArrowLeft,
  Shield,
  X
} from 'lucide-react';

// Mock Data: Danh sách thành viên
const initialMembers = [
  { id: 1, name: 'Nguyễn Văn A', email: 'nguyenvana@mcfh.com', role: 'Owner' },
  { id: 2, name: 'Trần Thị B', email: 'tranthib@mcfh.com', role: 'Admin' },
  { id: 3, name: 'Lê Hoàng C', email: 'lehoangc@mcfh.com', role: 'Member' },
];

const WorkspaceSettings = () => {
  const [activeTab, setActiveTab] = useState<'general' | 'members'>('general');
  const [workspaceName, setWorkspaceName] = useState('Acma Agency');
  const [members, setMembers] = useState(initialMembers);
  
  // State cho Modal Mời thành viên
  const [isInviteModalOpen, setIsInviteModalOpen] = useState(false);
  const [inviteEmail, setInviteEmail] = useState('');
  const [inviteRole, setInviteRole] = useState('Member');

  // Xử lý Use Case: Update Workspace
  const handleUpdateWorkspace = (e: React.FormEvent) => {
    e.preventDefault();
    alert(`Đã cập nhật tên Workspace thành: ${workspaceName}`);
    // TODO: Gọi API PUT /api/workspaces/{id}
  };

  // Xử lý Use Case: Delete Workspace
  const handleDeleteWorkspace = () => {
    if (window.confirm('Bạn có chắc chắn muốn xóa Workspace này? Mọi dữ liệu chiến dịch sẽ bị mất vĩnh viễn.')) {
      alert('Đã xóa Workspace!');
      // TODO: Gọi API DELETE /api/workspaces/{id} và navigate về /workspaces
    }
  };

  // Xử lý Use Case: Change Member Role
  const handleRoleChange = (memberId: number, newRole: string) => {
    setMembers(members.map(m => m.id === memberId ? { ...m, role: newRole } : m));
    // TODO: Gọi API PATCH /api/workspaces/{id}/members/{memberId}
  };

  // Xử lý Use Case: Remove Member
  const handleRemoveMember = (memberId: number, memberName: string) => {
    if (window.confirm(`Bạn có muốn xóa ${memberName} khỏi Workspace?`)) {
      setMembers(members.filter(m => m.id !== memberId));
      // TODO: Gọi API DELETE /api/workspaces/{id}/members/{memberId}
    }
  };

  // Xử lý Use Case: Invite Member
  const handleInvite = (e: React.FormEvent) => {
    e.preventDefault();
    const newMember = {
      id: Date.now(),
      name: 'Người dùng mới', // Thực tế sẽ chờ họ accept mail
      email: inviteEmail,
      role: inviteRole
    };
    setMembers([...members, newMember]);
    setIsInviteModalOpen(false);
    setInviteEmail('');
    alert(`Đã gửi lời mời đến ${inviteEmail}`);
    // TODO: Gọi API POST /api/workspaces/{id}/invite
  };

  return (
    <div className="min-h-screen bg-[#050A15] text-white font-sans selection:bg-[#FF7575] selection:text-white">
      
      {/* Navbar Nhỏ phía trên */}
      <header className="h-16 bg-[#0A101D] border-b border-white/5 flex items-center px-8">
        <Link to="/workspaces" className="flex items-center gap-2 text-gray-400 hover:text-white transition-colors text-sm font-medium">
          <ArrowLeft size={18} />
          Quay lại danh sách Workspace
        </Link>
      </header>

      <div className="max-w-6xl mx-auto px-4 sm:px-6 lg:px-8 py-10">
        
        <div className="mb-8">
          <h1 className="text-3xl font-extrabold tracking-tight mb-2">Cài đặt Workspace</h1>
          <p className="text-[#9BA1B0]">Quản lý thông tin dự án và phân quyền thành viên trong nhóm.</p>
        </div>

        <div className="flex flex-col md:flex-row gap-8">
          
          {/* Sidebar Tabs */}
          <aside className="w-full md:w-64 shrink-0">
            <nav className="flex flex-col gap-2">
              <button 
                onClick={() => setActiveTab('general')}
                className={`flex items-center gap-3 px-4 py-3 rounded-xl font-medium transition-all ${
                  activeTab === 'general' 
                  ? 'bg-[#FF7575]/10 text-[#FF7575] border border-[#FF7575]/20' 
                  : 'text-gray-400 hover:text-white hover:bg-white/5 border border-transparent'
                }`}
              >
                <Settings size={20} />
                Cài đặt chung
              </button>
              <button 
                onClick={() => setActiveTab('members')}
                className={`flex items-center gap-3 px-4 py-3 rounded-xl font-medium transition-all ${
                  activeTab === 'members' 
                  ? 'bg-[#FF7575]/10 text-[#FF7575] border border-[#FF7575]/20' 
                  : 'text-gray-400 hover:text-white hover:bg-white/5 border border-transparent'
                }`}
              >
                <Users size={20} />
                Quản lý thành viên
              </button>
            </nav>
          </aside>

          {/* Nội dung chính */}
          <main className="grow">
            
            {/* ========================================================
                TAB 1: GENERAL (UPDATE & DELETE WORKSPACE)
                ======================================================== */}
            {activeTab === 'general' && (
              <div className="space-y-8 animate-in fade-in slide-in-from-bottom-4 duration-500">
                
                {/* Khối Đổi tên Workspace */}
                <div className="bg-[#151B2B] border border-white/5 rounded-2xl p-6 md:p-8">
                  <h2 className="text-xl font-bold mb-6 flex items-center gap-2">
                    <Settings className="text-[#FF7575]" /> Thông tin cơ bản
                  </h2>
                  <form onSubmit={handleUpdateWorkspace}>
                    <div className="mb-6">
                      <label className="block text-sm font-medium text-gray-400 mb-2">Tên Workspace</label>
                      <input 
                        type="text" 
                        value={workspaceName}
                        onChange={(e) => setWorkspaceName(e.target.value)}
                        className="w-full md:w-1/2 bg-[#0A101D] border border-white/10 rounded-lg px-4 py-3 text-white focus:outline-none focus:border-[#FF7575] transition-colors"
                      />
                    </div>
                    <button type="submit" className="bg-white/10 hover:bg-white/20 text-white px-6 py-2.5 rounded-lg text-sm font-semibold flex items-center gap-2 transition-colors">
                      <Save size={18} /> Lưu thay đổi
                    </button>
                  </form>
                </div>

                {/* Khối Xóa Workspace (Danger Zone) */}
                <div className="border border-red-500/20 bg-red-500/5 rounded-2xl p-6 md:p-8 relative overflow-hidden">
                  <div className="absolute top-0 left-0 w-1 h-full bg-red-500"></div>
                  <h2 className="text-xl font-bold text-red-500 mb-2 flex items-center gap-2">
                    <AlertTriangle /> Vùng nguy hiểm (Danger Zone)
                  </h2>
                  <p className="text-gray-400 text-sm mb-6 max-w-2xl">
                    Hành động này sẽ xóa vĩnh viễn Workspace cùng toàn bộ dữ liệu, chiến dịch và thiết lập bên trong. Không thể khôi phục lại sau khi xóa.
                  </p>
                  <button 
                    onClick={handleDeleteWorkspace}
                    className="bg-red-500/10 hover:bg-red-500 text-red-500 hover:text-white border border-red-500/20 px-6 py-2.5 rounded-lg text-sm font-bold flex items-center gap-2 transition-all"
                  >
                    <Trash2 size={18} /> Xóa Workspace này
                  </button>
                </div>
              </div>
            )}


            {/* ========================================================
                TAB 2: MEMBERS (VIEW, INVITE, CHANGE ROLE, REMOVE)
                ======================================================== */}
            {activeTab === 'members' && (
              <div className="bg-[#151B2B] border border-white/5 rounded-2xl p-6 md:p-8 animate-in fade-in slide-in-from-bottom-4 duration-500">
                <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-8">
                  <div>
                    <h2 className="text-xl font-bold mb-1 flex items-center gap-2">
                      <Shield className="text-[#FF7575]" /> Danh sách thành viên
                    </h2>
                    <p className="text-sm text-gray-400">Có tổng cộng {members.length} thành viên trong Workspace.</p>
                  </div>
                  
                  {/* Nút Invite Member */}
                  <button 
                    onClick={() => setIsInviteModalOpen(true)}
                    className="bg-[#FF7575] hover:bg-[#ff6262] text-white px-5 py-2.5 rounded-lg text-sm font-semibold flex items-center gap-2 transition-colors shadow-lg shadow-[#FF7575]/20"
                  >
                    <UserPlus size={18} /> Mời thành viên
                  </button>
                </div>

                {/* Bảng Danh sách thành viên */}
                <div className="overflow-x-auto">
                  <table className="w-full text-left border-collapse">
                    <thead>
                      <tr className="border-b border-white/10 text-gray-400 text-xs uppercase tracking-wider">
                        <th className="pb-4 font-semibold">Thành viên</th>
                        <th className="pb-4 font-semibold">Vai trò</th>
                        <th className="pb-4 font-semibold text-right">Hành động</th>
                      </tr>
                    </thead>
                    <tbody>
                      {members.map((member) => (
                        <tr key={member.id} className="border-b border-white/5 hover:bg-white/5 transition-colors">
                          <td className="py-4">
                            <div className="flex items-center gap-3">
                              <div className="w-10 h-10 rounded-full bg-gradient-to-br from-blue-500 to-purple-600 flex items-center justify-center font-bold text-white shadow-inner">
                                {member.name.charAt(0)}
                              </div>
                              <div>
                                <div className="font-semibold text-white">{member.name}</div>
                                <div className="text-xs text-gray-400">{member.email}</div>
                              </div>
                            </div>
                          </td>
                          <td className="py-4">
                            {/* Dropdown Change Member Role */}
                            <select 
                              value={member.role}
                              onChange={(e) => handleRoleChange(member.id, e.target.value)}
                              disabled={member.role === 'Owner'} // Thường không cho đổi quyền Owner
                              className="bg-[#0A101D] border border-white/10 text-sm rounded-lg px-3 py-1.5 focus:outline-none focus:border-[#FF7575] disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
                            >
                              <option value="Admin">Admin</option>
                              <option value="Member">Member</option>
                              <option value="Viewer">Viewer</option>
                              {member.role === 'Owner' && <option value="Owner">Owner</option>}
                            </select>
                          </td>
                          <td className="py-4 text-right">
                            {/* Remove Member Button */}
                            <button 
                              onClick={() => handleRemoveMember(member.id, member.name)}
                              disabled={member.role === 'Owner'}
                              className="text-gray-500 hover:text-red-500 p-2 transition-colors disabled:opacity-30 disabled:cursor-not-allowed"
                              title="Xóa thành viên"
                            >
                              <Trash2 size={20} />
                            </button>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </div>
            )}

          </main>
        </div>
      </div>

      {/* =========================================================
          MODAL: INVITE MEMBER
          ========================================================= */}
      {isInviteModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm animate-in fade-in duration-200">
          <div className="bg-[#0A101D] border border-white/10 rounded-2xl w-full max-w-md shadow-2xl p-6 relative animate-in zoom-in-95 duration-200">
            <button 
              onClick={() => setIsInviteModalOpen(false)}
              className="absolute top-4 right-4 text-gray-400 hover:text-white transition-colors"
            >
              <X size={24} />
            </button>

            <h2 className="text-2xl font-bold mb-2 text-white">Mời thành viên mới</h2>
            <p className="text-[#9BA1B0] text-sm mb-6">Nhập email và chọn quyền để mời đồng nghiệp tham gia Workspace này.</p>

            <form onSubmit={handleInvite}>
              <div className="space-y-4">
                <div>
                  <label className="block text-sm font-bold text-gray-300 mb-2">Địa chỉ Email</label>
                  <input
                    type="email"
                    required
                    value={inviteEmail}
                    onChange={(e) => setInviteEmail(e.target.value)}
                    placeholder="nguyenvan@company.com"
                    className="w-full px-4 py-3 bg-[#151B2B] border border-white/10 rounded-xl focus:outline-none focus:border-[#FF7575] text-white transition-all"
                  />
                </div>
                <div>
                  <label className="block text-sm font-bold text-gray-300 mb-2">Vai trò (Role)</label>
                  <select
                    value={inviteRole}
                    onChange={(e) => setInviteRole(e.target.value)}
                    className="w-full px-4 py-3 bg-[#151B2B] border border-white/10 rounded-xl focus:outline-none focus:border-[#FF7575] text-white transition-all"
                  >
                    <option value="Admin">Admin (Toàn quyền quản lý)</option>
                    <option value="Member">Member (Được tạo/Sửa chiến dịch)</option>
                    <option value="Viewer">Viewer (Chỉ xem báo cáo)</option>
                  </select>
                </div>
              </div>

              <div className="flex gap-4 mt-8">
                <button
                  type="button"
                  onClick={() => setIsInviteModalOpen(false)}
                  className="flex-1 py-3 rounded-lg font-bold text-gray-400 hover:text-white hover:bg-white/5 transition-colors"
                >
                  Hủy
                </button>
                <button
                  type="submit"
                  className="flex-1 bg-[#FF7575] hover:bg-[#ff6262] text-white py-3 rounded-lg font-bold shadow-[0_4px_14px_0_rgba(255,117,117,0.39)] transition-all"
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

export default WorkspaceSettings;