import React, { useEffect, useState, useRef } from 'react';
import { useParams } from 'react-router-dom';
import {
  Database, FileUp, Loader2, AlertCircle, ToggleLeft, ToggleRight, Trash2, Download
} from 'lucide-react';
import { projectApi } from '../api/projectApi';
import { extractApiError } from '../utils/authStorage';
import { useAppModal } from '../contexts/AppModalContext';
import { formatWorkspaceDateTime } from '../utils/workspaceHelpers';

const ProjectDataSources = () => {
  const { workspaceId, id } = useParams();
  const wid = Number(workspaceId);
  const projectId = Number(id);

  const [activeTab, setActiveTab] = useState<'sources' | 'imports'>('sources');
  const [sources, setSources] = useState<any[]>([]);
  const [imports, setImports] = useState<any[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isProcessing, setIsProcessing] = useState(false);
  const [errorMessage, setErrorMessage] = useState('');
  
  const { confirm, alert } = useAppModal();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const loadData = async () => {
    if (!wid || !projectId) return;
    setIsLoading(true);
    setErrorMessage('');
    try {
      if (activeTab === 'sources') {
        const data = await projectApi.getDataSources(wid, projectId);
        setSources(data);
      } else if (activeTab === 'imports') {
        const data = await projectApi.getImportFiles(wid, projectId);
        setImports(data);
      }
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải dữ liệu.'));
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    loadData();
  }, [wid, projectId, activeTab]);

  const handleToggle = async (sourceId: number, currentStatus: string) => {
    if (!wid || !projectId) return;
    const action = currentStatus === 'active' ? 'Tạm dừng' : 'Bật lại';
    const confirmed = await confirm({
      title: `${action} nguồn dữ liệu`,
      message: `Bạn có chắc muốn ${action.toLowerCase()} nguồn cào này không?`,
      confirmText: action,
      cancelText: 'Hủy',
      type: 'warning',
    });
    if (!confirmed) return;

    setIsProcessing(true);
    try {
      await projectApi.toggleDataSource(wid, projectId, sourceId);
      await loadData();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể cập nhật trạng thái.'));
    } finally {
      setIsProcessing(false);
    }
  };

  const handleDelete = async (sourceId: number) => {
    if (!wid || !projectId) return;
    const confirmed = await confirm({
      title: 'Xóa nguồn dữ liệu',
      message: 'Hành động này sẽ không thể hoàn tác. Dữ liệu đã cào vẫn được giữ nguyên nhưng sẽ không cào thêm nữa. Bạn có chắc chắn?',
      confirmText: 'Xóa',
      cancelText: 'Hủy',
      type: 'danger',
    });
    if (!confirmed) return;

    setIsProcessing(true);
    try {
      await projectApi.deleteDataSource(wid, projectId, sourceId);
      await loadData();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể xóa nguồn.'));
    } finally {
      setIsProcessing(false);
    }
  };

  const handleDeleteImport = async (fileId: number) => {
    if (!wid || !projectId) return;
    const confirmed = await confirm({
      title: 'Xóa file đã import',
      message: 'Hành động này sẽ xóa file và toàn bộ các mentions/phân tích liên quan đến file này. Hành động này không thể hoàn tác. Bạn có chắc chắn?',
      confirmText: 'Xóa',
      cancelText: 'Hủy',
      type: 'danger',
    });
    if (!confirmed) return;

    setIsProcessing(true);
    try {
      await projectApi.deleteImportFile(wid, projectId, fileId);
      await loadData();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể xóa file import.'));
    } finally {
      setIsProcessing(false);
    }
  };

  const handleImportFileClick = () => {
    // Open file picker
    if (fileInputRef.current) {
      fileInputRef.current.click();
    }
  };

  const handleDownloadTemplate = () => {
    const link = document.createElement("a");
    link.href = "/mcfh_template.xlsx";
    link.download = "mcfh_import_template.xlsx";
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
  };

  const handleFileChange = async (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (!file) return;

    if (!wid || !projectId) return;

    // Optional: Reset input so the same file can be selected again
    event.target.value = '';

    const confirmed = await confirm({
      title: 'Tải file dữ liệu (Import)',
      message: `Bạn đang tải lên file "${file.name}". Hệ thống sẽ đọc dữ liệu và lưu vào dự án. Quá trình này mất khoảng 2-3 giây. Bạn muốn tiếp tục?`,
      confirmText: 'Tải lên',
      cancelText: 'Hủy'
    });
    
    if (!confirmed) return;

    setIsProcessing(true);
    setErrorMessage('');
    
    try {
      const formData = new FormData();
      formData.append('File', file);
      
      await projectApi.importFile(wid, projectId, formData);
      await loadData();
      await alert({
        title: 'Nhập thành công',
        message: 'Dữ liệu đã được nạp vào hệ thống để AI xử lý.',
        type: 'success'
      });
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Import thất bại.'));
    } finally {
      setIsProcessing(false);
    }
  };

  return (
    <div className="animate-in fade-in duration-500 max-w-6xl mx-auto space-y-6 pb-10">
      <div>
        <h2 className="text-3xl font-bold text-white flex items-center gap-3">
          <Database className="text-[#00B4D8] w-8 h-8" />
          Nguồn dữ liệu
        </h2>
        <p className="text-gray-400 text-sm mt-2">
          Quản lý các nguồn cào tự động và các file dữ liệu bạn tải lên thủ công.
        </p>
      </div>

      {errorMessage && (
        <div className="bg-red-500/10 border border-red-500/20 text-red-300 p-4 rounded-xl flex items-center gap-3 text-sm">
          <AlertCircle className="w-5 h-5 shrink-0" />
          {errorMessage}
        </div>
      )}

      <div className="flex items-center gap-2 border-b border-white/10">
        <button
          onClick={() => setActiveTab('sources')}
          className={`px-4 py-3 text-sm font-semibold border-b-2 transition-colors ${
            activeTab === 'sources' ? 'border-[#00B4D8] text-[#00B4D8]' : 'border-transparent text-gray-400 hover:text-gray-200'
          }`}
        >
          Nguồn tự động (Scraping)
        </button>
        <button
          onClick={() => setActiveTab('imports')}
          className={`px-4 py-3 text-sm font-semibold border-b-2 transition-colors ${
            activeTab === 'imports' ? 'border-[#00B4D8] text-[#00B4D8]' : 'border-transparent text-gray-400 hover:text-gray-200'
          }`}
        >
          Dữ liệu tải lên (Import)
        </button>
      </div>

      <div className="bg-[#0A101D] border border-white/5 rounded-2xl p-6">
        {activeTab === 'sources' ? (
          <>
            <div className="flex justify-end mb-4">
            </div>
            {isLoading ? (
              <div className="flex justify-center py-10"><Loader2 className="w-8 h-8 animate-spin text-[#00B4D8]" /></div>
            ) : sources.length === 0 ? (
              <div className="text-center py-10 text-gray-500 text-sm">Chưa có nguồn dữ liệu nào được cấu hình.</div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-left text-sm text-gray-300">
                  <thead className="bg-[#151B2B] text-gray-400 font-semibold">
                    <tr>
                      <th className="px-4 py-3 rounded-l-lg">ID</th>
                      <th className="px-4 py-3">Nền tảng</th>
                      <th className="px-4 py-3">Loại</th>
                      <th className="px-4 py-3">Từ khóa / URL</th>
                      <th className="px-4 py-3">Trạng thái</th>
                      <th className="px-4 py-3 rounded-r-lg text-right">Thao tác</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-white/5">
                    {sources.map(s => (
                      <tr key={s.sourceId} className="hover:bg-white/[0.02]">
                        <td className="px-4 py-4 font-mono text-xs">{s.sourceId}</td>
                        <td className="px-4 py-4 uppercase font-bold text-xs tracking-wider">{s.platform}</td>
                        <td className="px-4 py-4">{s.sourceType}</td>
                        <td className="px-4 py-4 max-w-[200px] truncate" title={s.targetUrl || s.searchQuery}>
                          {s.targetUrl || s.searchQuery || '-'}
                        </td>
                        <td className="px-4 py-4">
                          <span className={`inline-flex items-center px-2 py-1 rounded text-[10px] font-bold uppercase tracking-wider ${
                            s.status === 'active' ? 'bg-emerald-500/10 text-emerald-400 border border-emerald-500/20' : 'bg-gray-500/10 text-gray-400 border border-gray-500/20'
                          }`}>
                            {s.status}
                          </span>
                        </td>
                        <td className="px-4 py-4 text-right">
                          <div className="flex items-center justify-end gap-2">
                            <button
                              onClick={() => handleToggle(s.sourceId, s.status)}
                              disabled={isProcessing}
                              className="p-1.5 text-gray-400 hover:text-white transition-colors"
                              title={s.status === 'active' ? 'Tạm dừng cào' : 'Bật lại'}
                            >
                              {s.status === 'active' ? <ToggleRight className="text-emerald-400" size={20} /> : <ToggleLeft size={20} />}
                            </button>
                            <button
                              onClick={() => handleDelete(s.sourceId)}
                              disabled={isProcessing}
                              className="p-1.5 text-gray-400 hover:text-red-400 transition-colors"
                              title="Xóa nguồn"
                            >
                              <Trash2 size={16} />
                            </button>
                          </div>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </>
        ) : activeTab === 'imports' ? (
          <>
            <div className="flex justify-between items-center mb-4">
              <div>
                <h3 className="text-lg font-bold text-white">Lịch sử tải lên</h3>
                <p className="text-xs text-gray-400">Các file Excel / CSV đã được nạp dữ liệu</p>
              </div>
              <div className="flex items-center gap-3">
                <button
                  type="button"
                  onClick={handleDownloadTemplate}
                  className="flex items-center gap-2 px-4 py-2 bg-white/5 hover:bg-white/10 text-gray-300 text-sm font-semibold rounded-xl border border-white/10 transition-colors"
                >
                  <Download size={16} /> Tải file mẫu (.xlsx)
                </button>
                
                <input
                  type="file"
                  ref={fileInputRef}
                  onChange={handleFileChange}
                  accept=".xlsx, .xls, .csv"
                  className="hidden"
                />

                <button
                  type="button"
                  onClick={handleImportFileClick}
                  disabled={isProcessing}
                  className="flex items-center gap-2 px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-white text-sm font-semibold rounded-xl transition-all shadow-md disabled:opacity-50"
                >
                  <FileUp size={16} /> Import file dữ liệu
                </button>
              </div>
            </div>

            {isLoading ? (
              <div className="flex justify-center py-10"><Loader2 className="w-8 h-8 animate-spin text-[#00B4D8]" /></div>
            ) : imports.length === 0 ? (
              <div className="text-center py-10 text-gray-500 text-sm">Chưa có file nào được nạp vào dự án này.</div>
            ) : (
              <div className="overflow-x-auto">
                <table className="w-full text-left text-sm text-gray-300">
                  <thead className="bg-[#151B2B] text-gray-400 font-semibold">
                    <tr>
                      <th className="px-4 py-3 rounded-l-lg">ID</th>
                      <th className="px-4 py-3">Tên File</th>
                      <th className="px-4 py-3">Số lượng tin</th>
                      <th className="px-4 py-3">Thời gian import</th>
                      <th className="px-4 py-3 rounded-r-lg text-right">Thao tác</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-white/5">
                    {imports.map(f => (
                      <tr key={f.fileId} className="hover:bg-white/[0.02]">
                        <td className="px-4 py-4 font-mono text-xs">{f.fileId}</td>
                        <td className="px-4 py-4 font-semibold text-white">{f.fileName}</td>
                        <td className="px-4 py-4 text-xs text-gray-400">{f.recordCount || '-'} dòng</td>
                        <td className="px-4 py-4 text-xs text-gray-400">{f.uploadedAt ? formatWorkspaceDateTime(f.uploadedAt) : '-'}</td>
                        <td className="px-4 py-4 text-right">
                          <button
                            onClick={() => handleDeleteImport(f.fileId)}
                            className="p-1.5 text-gray-400 hover:text-red-400 hover:bg-white/10 rounded-md transition-colors"
                            title="Xóa file import"
                          >
                            <Trash2 size={16} />
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </>
        ) : null}
      </div>
    </div>
  );
};

export default ProjectDataSources;
