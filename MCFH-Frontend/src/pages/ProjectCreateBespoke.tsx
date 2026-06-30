import { useState } from 'react';
import { Link, useParams, useNavigate } from 'react-router-dom';
import { ArrowLeft, Download, Loader2, AlertCircle } from 'lucide-react';
import { projectApi } from '../api/projectApi';
import { extractApiError } from '../utils/authStorage';

const MODULE_OPTIONS = [
  { key: 'overview', label: 'Tổng quan Mentions & Reach' },
  { key: 'sentiment', label: 'Phân tích Cảm xúc (Sentiment)' },
  { key: 'channel', label: 'So sánh Kênh (Channel)' },
  { key: 'influencers', label: 'Hiệu suất KOLs & Influencers' },
  { key: 'aspects', label: 'Phân tích Khía cạnh (Aspect)' },
  { key: 'top_mentions', label: 'Top Mentions tiêu biểu' },
];

const ProjectCreateBespoke = () => {
  const { workspaceId, id } = useParams();
  const wid = Number(workspaceId);
  const projectId = Number(id);
  const navigate = useNavigate();

  const [title, setTitle] = useState('');
  const [requirements, setRequirements] = useState('');
  const [dateFrom, setDateFrom] = useState('');
  const [dateTo, setDateTo] = useState('');
  const [modules, setModules] = useState<string[]>(['overview', 'sentiment', 'channel']);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');

  const toggleModule = (key: string) => {
    setModules((prev) => (prev.includes(key) ? prev.filter((m) => m !== key) : [...prev, key]));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!wid || !projectId || modules.length === 0) return;

    setIsSubmitting(true);
    setErrorMessage('');
    try {
      await projectApi.createBespokeRequest(wid, projectId, {
        title,
        requirements: requirements || undefined,
        dateFrom: dateFrom || undefined,
        dateTo: dateTo || undefined,
        modules,
        format: 'html',
      });
      navigate(`/workspace/${wid}/project/${projectId}/reports`);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể gửi yêu cầu báo cáo chuyên sâu.'));
    } finally {
      setIsSubmitting(false);
    }
  };

  const reportsPath = `/workspace/${wid}/project/${projectId}/reports`;

  return (
    <div className="animate-in fade-in duration-500 max-w-4xl mx-auto space-y-6">
      <Link
        to={reportsPath}
        className="flex items-center gap-2 text-gray-400 hover:text-white transition-colors w-fit text-sm font-semibold"
      >
        <ArrowLeft size={16} /> Quay lại Report Center
      </Link>

      <div className="mb-4">
        <h2 className="text-3xl font-bold text-white mb-2">Yêu cầu Báo cáo Chuyên sâu</h2>
        <p className="text-gray-400 text-sm">
          User gửi yêu cầu → Admin giao Reporter → Reporter biên soạn báo cáo theo nhu cầu của bạn.
        </p>
      </div>

      {errorMessage && (
        <div className="bg-red-500/10 border border-red-500/20 text-red-300 p-4 rounded-xl flex items-center gap-3 text-sm">
          <AlertCircle className="w-5 h-5 shrink-0" />
          {errorMessage}
        </div>
      )}

      <form onSubmit={handleSubmit} className="bg-[#151B2B] border border-white/5 rounded-2xl p-8 shadow-xl space-y-8">
        <div>
          <label className="block text-sm font-bold text-gray-300 mb-2">Tiêu đề báo cáo *</label>
          <input
            type="text"
            required
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder="VD: Báo cáo khủng hoảng truyền thông Q2/2026..."
            className="w-full px-4 py-3 bg-[#0A101D] border border-white/10 rounded-xl focus:outline-none focus:border-[#FF7575] text-white"
          />
        </div>

        <div>
          <label className="block text-sm font-bold text-gray-300 mb-2">Yêu cầu chi tiết</label>
          <textarea
            value={requirements}
            onChange={(e) => setRequirements(e.target.value)}
            rows={4}
            placeholder="Mô tả mục tiêu, đối tượng đọc, insight cần làm rõ, góc phân tích đặc biệt..."
            className="w-full px-4 py-3 bg-[#0A101D] border border-white/10 rounded-xl focus:outline-none focus:border-[#FF7575] text-white resize-none"
          />
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
          <div>
            <label className="block text-sm font-bold text-gray-300 mb-2">Từ ngày</label>
            <input
              type="date"
              value={dateFrom}
              onChange={(e) => setDateFrom(e.target.value)}
              className="w-full px-4 py-3 bg-[#0A101D] border border-white/10 rounded-xl text-white focus:border-[#FF7575] focus:outline-none [color-scheme:dark]"
            />
          </div>
          <div>
            <label className="block text-sm font-bold text-gray-300 mb-2">Đến ngày</label>
            <input
              type="date"
              value={dateTo}
              onChange={(e) => setDateTo(e.target.value)}
              className="w-full px-4 py-3 bg-[#0A101D] border border-white/10 rounded-xl text-white focus:border-[#FF7575] focus:outline-none [color-scheme:dark]"
            />
          </div>
        </div>

        <div>
          <label className="block text-sm font-bold text-gray-300 mb-3">Module phân tích * (chọn ít nhất 1)</label>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            {MODULE_OPTIONS.map((item) => (
              <label
                key={item.key}
                className={`flex items-center gap-3 p-4 rounded-xl border cursor-pointer transition-colors ${
                  modules.includes(item.key)
                    ? 'border-[#FF7575]/50 bg-[#FF7575]/5'
                    : 'border-white/5 bg-[#0A101D] hover:border-white/15'
                }`}
              >
                <input
                  type="checkbox"
                  checked={modules.includes(item.key)}
                  onChange={() => toggleModule(item.key)}
                  className="w-4 h-4 rounded border-gray-600 text-[#FF7575] focus:ring-[#FF7575] bg-transparent"
                />
                <span className="text-sm text-gray-300">{item.label}</span>
              </label>
            ))}
          </div>
        </div>

        <div className="pt-6 border-t border-white/5 flex justify-end gap-4">
          <Link to={reportsPath} className="px-6 py-3 rounded-xl font-bold text-gray-400 hover:text-white hover:bg-white/5">
            Hủy
          </Link>
          <button
            type="submit"
            disabled={isSubmitting || modules.length === 0}
            className="flex items-center gap-2 bg-[#FF7575] hover:bg-[#ff6262] disabled:opacity-50 text-white px-8 py-3 rounded-xl font-bold transition-colors"
          >
            {isSubmitting ? (
              <>
                <Loader2 className="w-4 h-4 animate-spin" /> Đang gửi...
              </>
            ) : (
              <>
                <Download size={18} /> Gửi yêu cầu
              </>
            )}
          </button>
        </div>
      </form>
    </div>
  );
};

export default ProjectCreateBespoke;
