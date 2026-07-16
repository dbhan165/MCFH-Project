import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
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
  Settings2,
  UserCheck,
  Send,
  Play,
} from 'lucide-react';
import { projectApi } from '../api/projectApi';
import type { BespokeCenter, BespokeRequestItem, ReportCenter, ReportTemplate } from '../types/project';
import { extractApiError, loadProfileFromStorage } from '../utils/authStorage';
import { formatNumber } from '../utils/sentimentHelpers';
import { formatWorkspaceDateTime, isSystemAdmin, isSystemReporter } from '../utils/workspaceHelpers';

function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} B`;
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}

function getReportFormat(type: string): 'pdf' | 'html' | 'csv' | 'json' | 'pptx' {
  if (type.includes('pdf')) return 'pdf';
  if (type.includes('pptx')) return 'pptx';
  if (type.includes('csv')) return 'csv';
  if (type.includes('json')) return 'json';
  return 'html';
}

function getTypeIcon() {
  return FileText;
}

function getTypeBadgeClass(format: string) {
  switch (format) {
    case 'pdf':
      return 'bg-[#FF7575]/10 text-[#FF7575] border-[#FF7575]/20';
    case 'html':
      return 'bg-[#00B4D8]/10 text-[#00B4D8] border-[#00B4D8]/20';
    case 'json':
      return 'bg-blue-500/10 text-blue-400 border-blue-500/20';
    case 'csv':
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

function bespokeStatusClass(status: string) {
  switch (status) {
    case 'completed':
      return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
    case 'in_progress':
      return 'bg-blue-500/10 text-blue-400 border-blue-500/20';
    case 'assigned':
      return 'bg-purple-500/10 text-purple-400 border-purple-500/20';
    default:
      return 'bg-yellow-500/10 text-yellow-500 border-yellow-500/20';
  }
}

const ProjectReports = () => {
  const { workspaceId, id } = useParams();
  const wid = Number(workspaceId);
  const projectId = Number(id);
  const userRole = loadProfileFromStorage()?.role ?? 'Client';
  const userId = loadProfileFromStorage()?.userId ?? 0;

  const [center, setCenter] = useState<ReportCenter | null>(null);
  const [bespoke, setBespoke] = useState<BespokeCenter | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isGenerating, setIsGenerating] = useState<string | null>(null);
  const [downloadingId, setDownloadingId] = useState<string | null>(null);
  const [bespokeActionId, setBespokeActionId] = useState<number | null>(null);
  const [assignReporterId, setAssignReporterId] = useState<Record<number, number>>({});
  const [errorMessage, setErrorMessage] = useState('');
  const [successMessage, setSuccessMessage] = useState('');

  const isAdmin = isSystemAdmin(userRole);
  const isReporter = isSystemReporter(userRole);

  const [bespokeWarning, setBespokeWarning] = useState('');

  const loadReports = useCallback(async () => {
    if (!wid || !projectId || Number.isNaN(wid) || Number.isNaN(projectId)) {
      setIsLoading(false);
      setErrorMessage('URL dự án không hợp lệ.');
      return;
    }

    setIsLoading(true);
    setErrorMessage('');
    setBespokeWarning('');

    let reportLoaded = false;

    try {
      const reportData = await projectApi.getReports(wid, projectId);
      setCenter(reportData);
      reportLoaded = true;
    } catch (error) {
      setCenter(null);
      setErrorMessage(extractApiError(error, 'Không thể tải báo cáo.'));
    }

    try {
      const bespokeData = await projectApi.getBespokeCenter(wid, projectId);
      setBespoke(bespokeData);
    } catch {
      setBespoke({ userSystemRole: userRole, requests: [], reporters: [] });
      setBespokeWarning('Không tải được phần báo cáo chuyên sâu. Hãy khởi động lại backend hoặc thử lại sau.');
    }

    if (!reportLoaded) {
      setCenter(null);
    }

    setIsLoading(false);
  }, [wid, projectId, userRole]);

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

  const handleAssign = async (requestId: number) => {
    const reporterId = assignReporterId[requestId];
    if (!wid || !projectId || !reporterId) return;
    setBespokeActionId(requestId);
    setErrorMessage('');
    try {
      await projectApi.assignBespokeReporter(wid, projectId, requestId, reporterId);
      setSuccessMessage('Đã giao Reporter xử lý báo cáo.');
      await loadReports();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể giao Reporter.'));
    } finally {
      setBespokeActionId(null);
    }
  };

  const handleStartWork = async (requestId: number) => {
    if (!wid || !projectId) return;
    setBespokeActionId(requestId);
    try {
      await projectApi.startBespokeWork(wid, projectId, requestId);
      setSuccessMessage('Đã bắt đầu làm báo cáo.');
      await loadReports();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể cập nhật trạng thái.'));
    } finally {
      setBespokeActionId(null);
    }
  };

  const handleDeliver = async (requestId: number) => {
    if (!wid || !projectId) return;
    setBespokeActionId(requestId);
    try {
      await projectApi.deliverBespokeReport(wid, projectId, requestId);
      setSuccessMessage('Đã nộp báo cáo chuyên sâu thành công.');
      await loadReports();
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể nộp báo cáo.'));
    } finally {
      setBespokeActionId(null);
    }
  };

  const handleDownloadBespoke = async (req: BespokeRequestItem) => {
    if (!wid || !projectId) return;
    setDownloadingId(`bespoke-${req.requestId}`);
    try {
      await projectApi.downloadBespokeReport(wid, projectId, req.requestId);
    } catch (error) {
      setErrorMessage(extractApiError(error, 'Không thể tải báo cáo chuyên sâu.'));
    } finally {
      setDownloadingId(null);
    }
  };

  const filteredReports = useMemo(
    () =>
      (center?.reports ?? []).filter((report) =>
        ['analytics-html', 'analytics-pdf', 'mentions-csv', 'analytics-pptx'].includes(report.type)
      ),
    [center]
  );
  const bespokeRequests = useMemo(() => bespoke?.requests ?? [], [bespoke]);
  const createBespokePath = `/workspace/${wid}/project/${projectId}/create-bespoke`;

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
            <p className="text-gray-400 text-sm mt-2">
              Tạo và tải báo cáo HTML, PDF, CSV hoặc PPTX.
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

      {bespokeWarning && (
        <div className="bg-amber-500/10 border border-amber-500/20 text-amber-300 p-4 rounded-xl flex items-center gap-3 text-sm">
          <AlertCircle className="w-5 h-5 shrink-0" />
          {bespokeWarning}
        </div>
      )}

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

      {/* Báo cáo chuyên sâu (Bespoke)
      <div className="bg-[#151B2B] border border-purple-500/20 rounded-3xl overflow-hidden">
        <div className="p-6 border-b border-white/5 flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-gradient-to-r from-purple-500/5 to-transparent">
          <div>
            <h3 className="font-bold text-white flex items-center gap-2 text-lg">
              <Settings2 className="w-5 h-5 text-purple-400" />
              Báo cáo Chuyên sâu (Bespoke)
            </h3>
            <p className="text-xs text-gray-500 mt-1">
              {isReporter
                ? 'Reporter: nhận yêu cầu, biên soạn và nộp báo cáo theo yêu cầu khách hàng.'
                : isAdmin
                  ? 'Admin: giao Reporter xử lý các yêu cầu báo cáo.'
                  : 'User: gửi yêu cầu báo cáo tùy chỉnh — Reporter sẽ làm báo cáo chuyên sâu.'}
            </p>
          </div>
          {!isReporter && (
            <Link
              to={createBespokePath}
              className="inline-flex items-center gap-2 text-sm font-bold text-white bg-purple-600 hover:bg-purple-500 px-5 py-2.5 rounded-xl transition-colors shrink-0"
            >
              <Plus className="w-4 h-4" />
              Yêu cầu báo cáo chuyên sâu
            </Link>
          )}
        </div>

        {bespokeRequests.length === 0 ? (
          <div className="p-12 text-center text-gray-500">
            <Settings2 className="w-10 h-10 mx-auto mb-3 text-gray-600" />
            <p>Chưa có yêu cầu báo cáo chuyên sâu.</p>
            {!isReporter && (
              <Link to={createBespokePath} className="text-purple-400 text-sm mt-2 inline-block hover:underline">
                Tạo yêu cầu đầu tiên →
              </Link>
            )}
          </div>
        ) : (
          <div className="divide-y divide-white/5">
            {bespokeRequests.map((req) => (
              <BespokeRequestRow
                key={req.requestId}
                req={req}
                isReporter={isReporter}
                userId={userId}
                isAdmin={isAdmin}
                reporters={bespoke?.reporters ?? []}
                assignReporterId={assignReporterId[req.requestId]}
                onAssignReporterChange={(rid) =>
                  setAssignReporterId((prev) => ({ ...prev, [req.requestId]: rid }))
                }
                onAssign={() => handleAssign(req.requestId)}
                onStart={() => handleStartWork(req.requestId)}
                onDeliver={() => handleDeliver(req.requestId)}
                onDownload={() => handleDownloadBespoke(req)}
                isBusy={bespokeActionId === req.requestId}
                isDownloading={downloadingId === `bespoke-${req.requestId}`}
              />
            ))}
          </div>
        )}
      </div> */}

      {/* Quick generate templates */}
      <div>
        <h3 className="font-bold text-white mb-4 flex items-center gap-2">
          <Plus className="w-5 h-5 text-[#FF7575]" />
          Tạo báo cáo mới
        </h3>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {center.templates.map((template) => {
            const Icon = getTypeIcon();
            const generating = isGenerating === template.key;

            return (
              <div
                key={template.key}
                className="group relative overflow-hidden rounded-2xl border border-white/5 bg-[#151B2B] p-6 hover:border-[#FF7575]/30 transition-colors"
              >
                <div className="flex items-start gap-4">
                  <div className={`w-12 h-12 rounded-xl flex items-center justify-center shrink-0 border ${getTypeBadgeClass(template.format)}`}>
                    <Icon className="w-6 h-6" />
                  </div>
                  <div className="min-w-0 flex-1">
                    <p className="font-bold text-white text-sm">{template.name}</p>
                    <p className="text-xs text-gray-500 mt-1 leading-relaxed">{template.description}</p>
                    <span className={`inline-block mt-2 px-2 py-0.5 rounded text-[10px] font-semibold border ${getTypeBadgeClass(template.format)}`}>
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
                  const Icon = getTypeIcon();
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

      <p className="text-xs text-gray-600 text-center">
        HTML để xem nhanh, PDF/PPTX để trình bày, CSV để phân tích sâu.
      </p>
    </div>
  );
};

function BespokeRequestRow({
  req,
  isAdmin,
  isReporter,
  userId,
  reporters,
  assignReporterId,
  onAssignReporterChange,
  onAssign,
  onStart,
  onDeliver,
  onDownload,
  isBusy,
  isDownloading,
}: {
  req: BespokeRequestItem;
  isAdmin: boolean;
  isReporter: boolean;
  userId: number;
  reporters: { userId: number; fullName: string; email: string }[];
  assignReporterId?: number;
  onAssignReporterChange: (id: number) => void;
  onAssign: () => void;
  onStart: () => void;
  onDeliver: () => void;
  onDownload: () => void;
  isBusy: boolean;
  isDownloading: boolean;
}) {
  const canWork =
    req.status !== 'completed' &&
    (isAdmin || (isReporter && req.reporterId === userId));
  const showAssign = isAdmin && req.status === 'pending';

  return (
    <div className="p-6 hover:bg-white/[0.02] transition-colors">
      <div className="flex flex-col lg:flex-row lg:items-start justify-between gap-4">
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center gap-2 mb-2">
            <h4 className="font-bold text-white">{req.title}</h4>
            <span className={`px-2.5 py-0.5 rounded-full text-xs font-semibold border ${bespokeStatusClass(req.status)}`}>
              {req.statusLabel}
            </span>
          </div>
          {req.requirements && <p className="text-sm text-gray-400 line-clamp-2 mb-2">{req.requirements}</p>}
          <div className="flex flex-wrap gap-x-4 gap-y-1 text-xs text-gray-500">
            <span>Khách hàng: {req.clientName ?? '—'}</span>
            {req.reporterName && <span>Reporter: {req.reporterName}</span>}
            {(req.dateFrom || req.dateTo) && (
              <span>
                Giai đoạn: {req.dateFrom ?? '…'} → {req.dateTo ?? '…'}
              </span>
            )}
            <span>{req.modules.length} module</span>
          </div>
        </div>

        <div className="flex flex-wrap items-center gap-2 shrink-0">
          {showAssign && (
            <>
              <select
                value={assignReporterId ?? ''}
                onChange={(e) => onAssignReporterChange(Number(e.target.value))}
                className="px-3 py-2 bg-[#0A101D] border border-white/10 rounded-lg text-sm text-white focus:border-purple-500 focus:outline-none"
              >
                <option value="">Chọn Reporter</option>
                {reporters.map((r) => (
                  <option key={r.userId} value={r.userId}>
                    {r.fullName}
                  </option>
                ))}
              </select>
              <button
                type="button"
                onClick={onAssign}
                disabled={isBusy || !assignReporterId}
                className="inline-flex items-center gap-1.5 px-3 py-2 rounded-lg text-sm font-semibold bg-purple-600/20 text-purple-300 hover:bg-purple-600 hover:text-white disabled:opacity-50"
              >
                {isBusy ? <Loader2 className="w-4 h-4 animate-spin" /> : <UserCheck className="w-4 h-4" />}
                Giao Reporter
              </button>
            </>
          )}

          {canWork && req.status === 'assigned' && (
            <button
              type="button"
              onClick={onStart}
              disabled={isBusy}
              className="inline-flex items-center gap-1.5 px-3 py-2 rounded-lg text-sm font-semibold bg-blue-600/20 text-blue-300 hover:bg-blue-600 hover:text-white disabled:opacity-50"
            >
              {isBusy ? <Loader2 className="w-4 h-4 animate-spin" /> : <Play className="w-4 h-4" />}
              Bắt đầu
            </button>
          )}

          {canWork && (req.status === 'assigned' || req.status === 'in_progress') && (
            <button
              type="button"
              onClick={onDeliver}
              disabled={isBusy}
              className="inline-flex items-center gap-1.5 px-3 py-2 rounded-lg text-sm font-semibold bg-emerald-600/20 text-emerald-300 hover:bg-emerald-600 hover:text-white disabled:opacity-50"
            >
              {isBusy ? <Loader2 className="w-4 h-4 animate-spin" /> : <Send className="w-4 h-4" />}
              Nộp báo cáo
            </button>
          )}

          {req.hasDeliverable && (
            <button
              type="button"
              onClick={onDownload}
              disabled={isDownloading}
              className="inline-flex items-center gap-1.5 px-3 py-2 rounded-lg text-sm font-semibold text-[#00B4D8] bg-[#00B4D8]/10 hover:bg-[#00B4D8] hover:text-white disabled:opacity-50"
            >
              {isDownloading ? <Loader2 className="w-4 h-4 animate-spin" /> : <Download className="w-4 h-4" />}
              Tải báo cáo
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

export default ProjectReports;
