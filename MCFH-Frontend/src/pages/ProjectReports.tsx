import { useCallback, useEffect, useMemo, useState } from 'react';
import { useParams } from 'react-router-dom';
import {
  FileText,
  Download,
  Plus,
  Loader2,
  AlertCircle,
  RefreshCw,
  Clock,
  HardDrive,
  CheckCircle2,
  FileSpreadsheet,
  FileCode2,
} from 'lucide-react';
import { projectApi } from '../api/projectApi';
import type { ReportCenter, ReportTemplate } from '../types/project';
import { extractApiError } from '../utils/authStorage';
import { formatNumber } from '../utils/sentimentHelpers';
import { formatWorkspaceDateTime } from '../utils/workspaceHelpers';

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function getReportFormat(type: string): 'pdf' | 'html' | 'xlsx' | 'json' | 'pptx' {
  if (type.includes('pdf')) return 'pdf';
  if (type.includes('html')) return 'html';
  if (type.includes('xlsx')) return 'xlsx';
  if (type.includes('pptx')) return 'pptx';
  if (type.includes('json')) return 'json';
  return 'html';
}

function getTypeIcon(format: string) {
  if (format === 'json') return FileCode2;
  if (format === 'xlsx') return FileSpreadsheet;
  return FileText;
}

function getTypeBadgeClass(format: string) {
  switch (format) {
    case 'pdf':
      return 'bg-[#FF7575]/10 text-[#FF7575] border-[#FF7575]/20';
    case 'html':
      return 'bg-[#00B4D8]/10 text-[#00B4D8] border-[#00B4D8]/20';
    case 'json':
      return 'bg-indigo-500/10 text-indigo-400 border-indigo-500/20';
    case 'xlsx':
      return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
    case 'pptx':
      return 'bg-orange-500/10 text-orange-400 border-orange-500/20';
    default:
      return 'bg-purple-500/10 text-purple-400 border-purple-500/20';
  }
}

function sanitizeDownloadName(name: string, extension: string) {
  const safe = name.replace(/[\\/:*?"<>|]+/g, '-').replace(/\s+/g, ' ').trim();
  return `${safe || 'report'}.${extension}`;
}

const ProjectReports = () => {
  const { workspaceId, id } = useParams();
  const wid = Number(workspaceId);
  const projectId = Number(id);

  const [center, setCenter] = useState<ReportCenter | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isGenerating, setIsGenerating] = useState<string | null>(null);
  const [downloadingId, setDownloadingId] = useState<string | null>(null);
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState('');

  const loadReports = useCallback(async () => {
    if (!wid || !projectId || Number.isNaN(wid) || Number.isNaN(projectId)) {
      setIsLoading(false);
      setErrorMessage('URL dự án không hợp lệ.');
      return;
    }

    setIsLoading(true);
    setErrorMessage('');

    try {
      const reportData = await projectApi.getReports(wid, projectId);
      setCenter(reportData);
    } catch (error) {
      setCenter(null);
      setErrorMessage(extractApiError(error, 'Không thể tải báo cáo.'));
    }

    setIsLoading(false);
  }, [wid, projectId]);

  useEffect(() => {
    loadReports();
  }, [loadReports]);

  const handleGenerate = async (template: ReportTemplate) => {
    if (!wid || !projectId) return;
    setIsGenerating(template.key);
    setErrorMessage('');
    setSuccessMessage('');
    try {
      const report = await projectApi.generateReport(wid, projectId, template.key);
      setSuccessMessage(`Đã tạo báo cáo «${report.name}».`);
      await loadReports();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tạo báo cáo.'));
    } finally {
      setIsGenerating(null);
    }
  };

  const filteredReports = useMemo(
    () =>
      (center?.reports ?? []).filter((report) =>
        ['analytics-html', 'analytics-pdf', 'mentions-xlsx', 'analytics-pptx'].includes(report.type)
      ),
    [center]
  );

  if (isLoading) {
    return (
      <div className="flex flex-col items-center justify-center py-32 text-gray-400 gap-4">
        <div className="relative">
          <div className="absolute inset-0 rounded-full bg-[#FF7575]/20 blur-2xl animate-pulse" />
          <Loader2 className="relative w-12 h-12 animate-spin text-[#FF7575]" />
        </div>
        <p className="text-sm">Đang tải báo cáo...</p>
      </div>
    );
  }

  if (!center) {
    return (
      <div className="flex flex-col items-center justify-center py-32 text-gray-400 gap-4 max-w-lg mx-auto text-center">
        <AlertCircle className="w-12 h-12 text-red-400 shrink-0" />
        <p className="text-white font-semibold">Không tải được báo cáo</p>
        <p className="text-sm text-gray-500">
          {errorMessage || 'Kiểm tra quyền truy cập dự án hoặc đảm bảo backend đang chạy tại http://localhost:5254'}
        </p>
        <button
          type="button"
          onClick={loadReports}
          className="inline-flex items-center gap-2 mt-2 px-5 py-2.5 rounded-xl bg-[#FF7575] text-white text-sm font-semibold hover:bg-[#ff9090] transition-colors"
        >
          <RefreshCw className="w-4 h-4" />
          Thử lại
        </button>
      </div>
    );
  }

  return (
    <div className="animate-in fade-in duration-500 max-w-7xl mx-auto space-y-8 pb-10">
      {/* Header */}
      <div className="relative overflow-hidden rounded-3xl border border-white/5 bg-gradient-to-br from-[#151B2B] via-[#141A2C] to-[#0A101D] p-8">
        <div className="absolute -top-20 -right-16 w-64 h-64 bg-[#FF7575]/10 rounded-full blur-3xl pointer-events-none" />
        <div className="absolute -bottom-12 -left-12 w-48 h-48 bg-blue-500/10 rounded-full blur-3xl pointer-events-none" />

        <div className="relative flex flex-col lg:flex-row lg:items-center lg:justify-between gap-6">
          <div>
            <h2 className="text-3xl font-bold text-white flex items-center gap-3">
              <FileText className="text-[#FF7575] w-8 h-8" />
              Báo cáo
            </h2>
            <p className="text-[#9ca3af] text-sm mt-1">
              Tạo và tải báo cáo HTML, PDF, Excel (XLSX) hoặc PPTX.
            </p>
          </div>

          <div className="flex items-center gap-4">
            <div className="rounded-2xl border border-white/10 bg-white/5 px-5 py-3 text-center">
              <p className="text-2xl font-bold text-white tabular-nums">{filteredReports.length}</p>
              <p className="text-[10px] text-gray-500 uppercase tracking-wide">Đã tạo</p>
            </div>
            <button
              type="button"
              onClick={loadReports}
              className="p-3 rounded-xl border border-white/10 bg-white/5 text-gray-400 hover:text-white hover:bg-white/10 transition-colors"
              title="Làm mới"
            >
              <RefreshCw className="w-5 h-5" />
            </button>
          </div>
        </div>
      </div>

      {errorMessage && (
        <div className="bg-red-500/10 border border-red-500/20 text-red-300 p-4 rounded-xl flex items-center gap-3 text-sm">
          <AlertCircle className="w-5 h-5 shrink-0" />
          {errorMessage}
        </div>
      )}

      {successMessage && (
        <div className="bg-emerald-500/10 border border-emerald-500/20 text-emerald-300 p-4 rounded-xl flex items-center gap-3 text-sm">
          <CheckCircle2 className="w-5 h-5 shrink-0" />
          {successMessage}
        </div>
      )}

      {/* Quick generate templates */}
      <div>
        <h3 className="font-bold text-white mb-4 flex items-center gap-2">
          <Plus className="w-5 h-5 text-[#FF7575]" />
          Tạo báo cáo mới
        </h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {center.templates.map((template) => {
            const format = getReportFormat(template.key);
            const Icon = getTypeIcon(format);
            const generating = isGenerating === template.key;

            return (
              <div
                key={template.key}
                className="group relative overflow-hidden rounded-2xl border border-white/5 bg-[#151B2B] p-6 hover:border-[#FF7575]/30 transition-colors"
              >
                <div className="flex items-start gap-4">
                  <div className={`w-12 h-12 rounded-xl flex items-center justify-center shrink-0 border ${getTypeBadgeClass(format)}`}>
                    <Icon className="w-6 h-6" />
                  </div>
                  <div className="min-w-0 flex-1">
                    <p className="font-bold text-white text-sm">{template.name}</p>
                    <p className="text-xs text-gray-500 mt-1 leading-relaxed">{template.description}</p>
                    <span className={`inline-block mt-2 px-2 py-0.5 rounded text-[10px] font-semibold border ${getTypeBadgeClass(format)}`}>
                      {template.typeLabel}
                    </span>
                  </div>
                </div>
                <button
                  type="button"
                  onClick={() => handleGenerate(template)}
                  disabled={!!isGenerating}
                  className="mt-5 w-full flex items-center justify-center gap-2 text-sm font-semibold text-white bg-[#FF7575]/90 hover:bg-[#FF7575] disabled:opacity-50 px-4 py-2.5 rounded-xl transition-colors"
                >
                  {generating ? (
                    <>
                      <Loader2 className="w-4 h-4 animate-spin" />
                      Đang tạo...
                    </>
                  ) : (
                    <>
                      <Plus className="w-4 h-4" />
                      Tạo ngay
                    </>
                  )}
                </button>
              </div>
            );
          })}
        </div>
      </div>

      {/* Stats row */}
      {center.lastGeneratedAt && (
        <div className="flex flex-wrap gap-4 text-sm text-gray-500">
          <span className="inline-flex items-center gap-2">
            <Clock className="w-4 h-4" />
            Lần xuất gần nhất: {formatWorkspaceDateTime(center.lastGeneratedAt)}
          </span>
        </div>
      )}

      {/* History table */}
      <div className="bg-[#151B2B] border border-white/5 rounded-3xl overflow-hidden">
        <div className="p-6 border-b border-white/5">
          <h3 className="font-bold text-white">Lịch sử báo cáo</h3>
          <p className="text-xs text-gray-500 mt-1">{filteredReports.length} báo cáo</p>
        </div>

        {filteredReports.length === 0 ? (
          <div className="p-16 text-center text-gray-500">
            <FileText className="w-12 h-12 mx-auto mb-4 text-gray-600" />
            <p className="text-gray-400">Chưa có báo cáo nào.</p>
            <p className="text-sm mt-1">Chọn mẫu phía trên và bấm «Tạo ngay».</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left">
              <thead>
                <tr className="bg-white/[0.02] border-b border-white/5">
                  {['Tên báo cáo', 'Loại', 'Ngày tạo', 'Người tạo', 'Dung lượng', 'Thao tác'].map((h) => (
                    <th
                      key={h}
                      className="px-6 py-4 text-xs font-bold text-gray-500 uppercase tracking-wider whitespace-nowrap"
                    >
                      {h}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-white/5">
                {filteredReports.map((report) => {
                  const format = getReportFormat(report.type);
                  const Icon = getTypeIcon(format);
                  const downloading = downloadingId === report.reportId;

                  return (
                    <tr key={report.reportId} className="hover:bg-white/[0.02] transition-colors group">
                      <td className="px-6 py-4">
                        <div className="font-semibold text-white mb-0.5 max-w-xs truncate">{report.name}</div>
                        <div className="text-xs text-gray-600 font-mono">{report.reportId}</div>
                        {report.rowCount > 0 && (
                          <div className="text-[11px] text-gray-500 mt-1">
                            {format === 'html' || format === 'pdf'
                              ? `Bao phủ ${formatNumber(report.rowCount)} mentions`
                              : `${formatNumber(report.rowCount)} dòng`}
                          </div>
                        )}
                      </td>
                      <td className="px-6 py-4">
                        <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-lg text-xs font-semibold border ${getTypeBadgeClass(format)}`}>
                          <Icon size={12} />
                          {report.typeLabel}
                        </span>
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-400 whitespace-nowrap">
                        {formatWorkspaceDateTime(report.createdAt)}
                      </td>
                      <td className="px-6 py-4 text-sm text-gray-500">{report.createdBy}</td>
                      <td className="px-6 py-4 text-sm text-gray-500">
                        <span className="inline-flex items-center gap-1">
                          <HardDrive className="w-3.5 h-3.5" />
                          {formatFileSize(report.fileSizeBytes)}
                        </span>
                      </td>
                      <td className="px-6 py-4 text-right">
                        <button
                          type="button"
                          onClick={async () => {
                            if (!wid || !projectId) return;
                            setDownloadingId(report.reportId);
                            setErrorMessage('');
                            try {
                              await projectApi.downloadReport(
                                wid,
                                projectId,
                                report.reportId,
                                sanitizeDownloadName(report.name, format)
                              );
                            } catch (error) {
                              setErrorMessage(extractApiError(error, 'Không thể tải báo cáo.'));
                            } finally {
                              setDownloadingId(null);
                            }
                          }}
                          disabled={!!downloadingId}
                          className="inline-flex items-center gap-2 text-sm font-semibold text-[#00B4D8] bg-[#00B4D8]/10 hover:bg-[#00B4D8] hover:text-white disabled:opacity-50 px-4 py-2 rounded-xl transition-colors"
                        >
                          {downloading ? (
                            <Loader2 className="w-4 h-4 animate-spin" />
                          ) : (
                            <Download size={16} />
                          )}
                          Tải về
                        </button>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      <p className="text-[#6b7280] mt-1 text-sm max-w-2xl">
        HTML để xem nhanh, PDF/PPTX để trình bày, Excel để phân tích sâu.
      </p>
    </div>
  );
};

export default ProjectReports;
