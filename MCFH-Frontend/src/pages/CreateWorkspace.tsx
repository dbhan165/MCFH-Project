import { useState } from 'react';
import { Building2, ArrowLeft, CheckCircle2, Users, AlertCircle } from 'lucide-react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { workspaceApi } from '../api/workspaceApi';
import { extractApiError } from '../utils/authStorage';

const CreateWorkspace = () => {
  const navigate = useNavigate();
  const [searchParams] = useSearchParams();
  const isOnboarding = searchParams.get('onboarding') === '1';
  const [workspaceName, setWorkspaceName] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!workspaceName.trim()) return;

    setIsSubmitting(true);
    setErrorMessage('');
    try {
      const created = await workspaceApi.create(workspaceName.trim());
      if (isOnboarding) {
        navigate(`/create-project?wid=${created.workspaceId}&onboarding=1`);
      } else {
        navigate(`/workspace/${created.workspaceId}/projects`);
      }
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tạo workspace. Vui lòng thử lại.'));
    } finally {
      setIsSubmitting(false);
    }
  };

  return (
    <div className="min-h-screen bg-[#050A15] text-white font-sans flex items-center justify-center p-6 selection:bg-[#FF7575] selection:text-white relative overflow-hidden">
      <div
        className="absolute inset-0 z-0 opacity-10 pointer-events-none"
        style={{ backgroundImage: 'radial-gradient(#ffffff 1px, transparent 1px)', backgroundSize: '32px 32px' }}
      />

      <div className="w-full max-w-3xl bg-[#0A101D] border border-white/5 rounded-3xl p-8 md:p-12 relative z-10 shadow-2xl">
        <Link to="/workspaces" className="flex items-center gap-2 text-gray-400 hover:text-white transition-colors w-fit text-sm font-semibold mb-8">
          <ArrowLeft size={16} /> Quay lại danh sách
        </Link>

        <div className="flex items-center gap-4 mb-10">
          <div className="w-16 h-16 rounded-2xl bg-[#FF7575]/10 flex items-center justify-center border border-[#FF7575]/20">
            <Building2 className="w-8 h-8 text-[#FF7575]" />
          </div>
          <div>
            <h1 className="text-3xl font-bold tracking-tight text-white mb-1">
              {isOnboarding ? 'Bước 1: Tạo Workspace' : 'Khởi tạo Không gian làm việc'}
            </h1>
            <p className="text-gray-400">
              {isOnboarding
                ? 'Chào mừng bạn đến MCFH! Hãy đặt tên không gian làm việc để bắt đầu giám sát mạng xã hội.'
                : 'Thiết lập trung tâm điều hành cho tổ chức hoặc doanh nghiệp của bạn.'}
            </p>
          </div>
        </div>

        {errorMessage && (
          <div className="mb-6 bg-red-500/10 border border-red-500/20 text-red-300 p-4 rounded-xl flex items-center gap-3 text-sm font-medium">
            <AlertCircle className="w-5 h-5 shrink-0" />
            {errorMessage}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-8">
          <div className="space-y-6 bg-[#151B2B] p-8 rounded-2xl border border-white/5">
            <div>
              <label className="block text-sm font-bold text-gray-300 mb-2">
                Tên Workspace <span className="text-[#FF7575]">*</span>
              </label>
              <input
                type="text"
                required
                autoFocus
                value={workspaceName}
                onChange={(e) => setWorkspaceName(e.target.value)}
                placeholder="VD: Vinamilk Marketing Team..."
                className="w-full px-4 py-3.5 bg-[#0A101D] border border-white/10 rounded-xl focus:outline-none focus:border-[#FF7575] focus:ring-1 focus:ring-[#FF7575] text-white transition-all placeholder-gray-600"
              />
              <p className="text-xs text-gray-500 mt-2">
                Bạn sẽ là Owner của workspace này và có thể mời thêm thành viên sau khi tạo.
              </p>
            </div>
          </div>

          <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div className="flex items-center gap-3 p-4 bg-[#0A101D] rounded-xl border border-white/5 text-sm text-gray-300">
              <CheckCircle2 className="w-5 h-5 text-emerald-500 shrink-0" />
              Môi trường dữ liệu biệt lập và an toàn.
            </div>
            <div className="flex items-center gap-3 p-4 bg-[#0A101D] rounded-xl border border-white/5 text-sm text-gray-300">
              <Users className="w-5 h-5 text-blue-500 shrink-0" />
              Phân quyền Owner / Editor / Viewer.
            </div>
          </div>

          <div className="pt-6 border-t border-white/5 flex justify-end gap-4">
            <Link to="/workspaces" className="px-6 py-3.5 rounded-xl font-bold text-gray-400 hover:text-white hover:bg-white/5 transition-colors">
              Hủy bỏ
            </Link>
            <button
              type="submit"
              disabled={isSubmitting || !workspaceName.trim()}
              className="bg-[#FF7575] hover:bg-[#ff6262] disabled:opacity-50 disabled:cursor-not-allowed text-white px-8 py-3.5 rounded-xl font-bold transition-all shadow-[0_4px_14px_0_rgba(255,117,117,0.39)] flex items-center gap-2"
            >
              {isSubmitting ? 'Đang khởi tạo...' : 'Khởi tạo Workspace'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};

export default CreateWorkspace;
