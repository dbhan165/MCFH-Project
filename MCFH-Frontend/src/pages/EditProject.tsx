import { useEffect, useState } from 'react';
import { Link, useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Loader2, AlertCircle } from 'lucide-react';
import { projectApi } from '../api/projectApi';
import { extractApiError } from '../utils/authStorage';

const EditProject = () => {
  const { workspaceId, projectId } = useParams();
  const wid = Number(workspaceId);
  const pid = Number(projectId);
  const navigate = useNavigate();

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [searchQuery, setSearchQuery] = useState('');
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');

  useEffect(() => {
    if (!wid || !pid || Number.isNaN(wid) || Number.isNaN(pid)) return;
    (async () => {
      setIsLoading(true);
      try {
        const project = await projectApi.getById(wid, pid);
        setName(project.name);
        setDescription(project.description ?? '');
        setSearchQuery(project.searchQuery ?? '');
      } catch (error) {
        setErrorMessage(extractApiError(error, 'Không thể tải thông tin project.'));
      } finally {
        setIsLoading(false);
      }
    })();
  }, [wid, pid]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!wid || !pid) return;
    setIsSaving(true);
    setErrorMessage('');
    try {
      await projectApi.update(wid, pid, {
        name: name.trim(),
        description: description.trim() || null,
        searchQuery: searchQuery.trim() || null,
      });
      navigate(`/workspace/${wid}/projects`);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể cập nhật project.'));
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return (
      <div className="min-h-screen bg-[#050A15] flex items-center justify-center text-white">
        <Loader2 className="w-8 h-8 animate-spin text-[#00B4D8]" />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-[#050A15] text-white p-6 md:p-10">
      <div className="max-w-2xl mx-auto">
        <Link
          to={`/workspace/${wid}/projects`}
          className="inline-flex items-center gap-2 text-sm text-gray-400 hover:text-white mb-8"
        >
          <ArrowLeft size={16} />
          Quay lại danh sách project
        </Link>

        <h1 className="text-3xl font-bold mb-2">Chỉnh sửa Project</h1>
        <p className="text-gray-400 mb-8">Cập nhật tên, mô tả và từ khóa theo dõi.</p>

        {errorMessage && (
          <div className="mb-6 bg-red-500/10 border border-red-500/30 text-red-300 p-4 rounded-xl flex items-center gap-2 text-sm">
            <AlertCircle className="w-4 h-4" />
            {errorMessage}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-5 bg-[#0A101D] border border-white/10 rounded-2xl p-6">
          <div>
            <label className="text-sm font-semibold text-gray-300">Tên project *</label>
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
              className="mt-1 w-full px-4 py-3 bg-[#151B2B] border border-white/10 rounded-xl text-white"
              required
            />
          </div>
          <div>
            <label className="text-sm font-semibold text-gray-300">Mô tả</label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              rows={3}
              className="mt-1 w-full px-4 py-3 bg-[#151B2B] border border-white/10 rounded-xl text-white resize-none"
            />
          </div>
          <div>
            <label className="text-sm font-semibold text-gray-300">Từ khóa theo dõi</label>
            <input
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              placeholder="VD: Vinamilk, FPT"
              className="mt-1 w-full px-4 py-3 bg-[#151B2B] border border-white/10 rounded-xl text-white"
            />
          </div>
          <button
            type="submit"
            disabled={isSaving}
            className="w-full bg-[#00B4D8] hover:bg-[#0693B0] disabled:opacity-50 text-white font-bold py-3 rounded-xl"
          >
            {isSaving ? 'Đang lưu...' : 'Lưu thay đổi'}
          </button>
        </form>
      </div>
    </div>
  );
};

export default EditProject;
