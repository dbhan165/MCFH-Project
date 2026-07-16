import { useCallback, useEffect, useState, useMemo } from 'react';
import { useParams, Link } from 'react-router-dom';
import {
    Settings2, Plus, AlertCircle, CheckCircle2, Loader2,
    UserCheck, Play, Send, Download, ArrowLeft, RefreshCw, Edit3, UploadCloud
} from 'lucide-react';
import { projectApi } from '../api/projectApi';
import type { BespokeCenter, BespokeRequestItem } from '../types/project';
import { extractApiError, loadProfileFromStorage } from '../utils/authStorage';
import { isSystemAdmin, isSystemReporter } from '../utils/workspaceHelpers';
import ProjectCreateBespokeModal from './ProjectCreateBespoke';
import { RequestRevisionModal, UploadRevisionModal } from './ProjectMockModals';

function bespokeStatusClass(status: string) {
    switch (status) {
        case 'completed': return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
        case 'in_progress': return 'bg-blue-500/10 text-blue-400 border-blue-500/20';
        case 'assigned': return 'bg-purple-500/10 text-purple-400 border-purple-500/20';
        case 'gathering_data': return 'bg-indigo-500/10 text-indigo-400 border-indigo-500/20';
        case 'revision_requested': return 'bg-pink-500/10 text-pink-400 border-pink-500/20';
        default: return 'bg-yellow-500/10 text-yellow-500 border-yellow-500/20';
    }
}

const ProjectBespokeReports = () => {
    const { workspaceId } = useParams();
    const wid = Number(workspaceId);
    const userRole = loadProfileFromStorage()?.role ?? 'Client';
    const userId = loadProfileFromStorage()?.userId ?? 0;

    const [bespoke, setBespoke] = useState<BespokeCenter | null>(null);
    const [requestProjectMap, setRequestProjectMap] = useState<Record<number, number>>({});
    const [firstProjectId, setFirstProjectId] = useState<number | null>(null);
    const [isLoading, setIsLoading] = useState(true);
    const [downloadingId, setDownloadingId] = useState<string | null>(null);
    const [bespokeActionId, setBespokeActionId] = useState<number | null>(null);
    const [assignReporterId, setAssignReporterId] = useState<Record<number, number>>({});
    const [errorMessage, setErrorMessage] = useState('');
    const [successMessage, setSuccessMessage] = useState('');
    const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);

    // --- MOCK STATES ---
    const [revisionModalOpen, setRevisionModalOpen] = useState(false);
    const [uploadRevisionModalOpen, setUploadRevisionModalOpen] = useState(false);
    const [selectedReqId, setSelectedReqId] = useState<number | null>(null);
    const [mockStatuses, setMockStatuses] = useState<Record<number, string>>({});
    // -------------------

    const isAdmin = isSystemAdmin(userRole);
    const isReporter = isSystemReporter(userRole);

    const getProjectIdForRequest = useCallback(
        (requestId: number) => requestProjectMap[requestId] ?? null,
        [requestProjectMap]
    );

    const loadBespokeData = useCallback(async () => {
        if (!wid || Number.isNaN(wid)) {
            setIsLoading(false);
            setErrorMessage('URL workspace không hợp lệ.');
            return;
        }
        setIsLoading(true);
        setErrorMessage('');
        try {
            const projectList = await projectApi.getProjects(wid);
            setFirstProjectId(projectList[0]?.projectId ?? null);

            if (projectList.length === 0) {
                setBespoke({ userSystemRole: userRole, requests: [], reporters: [] });
                setRequestProjectMap({});
                return;
            }

            const results = await Promise.all(
                projectList.map(async (project) => {
                    try {
                        const data = await projectApi.getBespokeCenter(wid, project.projectId);
                        return { projectId: project.projectId, data };
                    } catch {
                        return { projectId: project.projectId, data: null };
                    }
                })
            );

            const projectMap: Record<number, number> = {};
            const allRequests: BespokeRequestItem[] = [];
            let merged: BespokeCenter | null = null;

            for (const { projectId, data } of results) {
                if (!data) continue;
                if (!merged) {
                    merged = { userSystemRole: data.userSystemRole, requests: [], reporters: data.reporters };
                } else if (data.reporters.length > 0) {
                    merged.reporters = data.reporters;
                }
                for (const req of data.requests) {
                    allRequests.push(req);
                    projectMap[req.requestId] = projectId;
                }
            }

            allRequests.sort((a, b) => b.requestId - a.requestId);
            setRequestProjectMap(projectMap);
            setBespoke(
                merged
                    ? { ...merged, requests: allRequests }
                    : { userSystemRole: userRole, requests: [], reporters: [] }
            );
        } catch (error) {
            setBespoke({ userSystemRole: userRole, requests: [], reporters: [] });
            setRequestProjectMap({});
            setFirstProjectId(null);
            setErrorMessage(extractApiError(error, 'Không tải được phần báo cáo chuyên sâu. Hãy thử lại sau.'));
        } finally {
            setIsLoading(false);
        }
    }, [wid, userRole]);

    useEffect(() => {
        loadBespokeData();
    }, [loadBespokeData]);

    const handleAssign = async (requestId: number) => {
        const reporterId = assignReporterId[requestId];
        const projectId = getProjectIdForRequest(requestId);
        if (!wid || !projectId || !reporterId) return;
        setBespokeActionId(requestId);
        setErrorMessage('');
        try {
            await projectApi.assignBespokeReporter(wid, projectId, requestId, reporterId);
            setSuccessMessage('Đã giao Reporter xử lý báo cáo.');
            await loadBespokeData();
        } catch (error) {
            setErrorMessage(extractApiError(error, 'Không thể giao Reporter.'));
        } finally {
            setBespokeActionId(null);
        }
    };

    const handleStartWork = async (requestId: number) => {
        const projectId = getProjectIdForRequest(requestId);
        if (!wid || !projectId) return;
        setBespokeActionId(requestId);
        try {
            await projectApi.startBespokeWork(wid, projectId, requestId);
            setSuccessMessage('Đã bắt đầu làm báo cáo.');
            await loadBespokeData();
        } catch (error) {
            setErrorMessage(extractApiError(error, 'Không thể cập nhật trạng thái.'));
        } finally {
            setBespokeActionId(null);
        }
    };

    const handleDeliver = async (requestId: number) => {
        const projectId = getProjectIdForRequest(requestId);
        if (!wid || !projectId) return;
        setBespokeActionId(requestId);
        try {
            await projectApi.deliverBespokeReport(wid, projectId, requestId);
            setSuccessMessage('Đã nộp báo cáo chuyên sâu thành công.');
            await loadBespokeData();
        } catch (error) {
            setErrorMessage(extractApiError(error, 'Không thể nộp báo cáo.'));
        } finally {
            setBespokeActionId(null);
        }
    };

    const handleDownloadBespoke = async (req: BespokeRequestItem) => {
        const projectId = getProjectIdForRequest(req.requestId);
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

    // --- MOCK HANDLERS ---
    const handleMockRequestRevision = async (feedback: string) => {
        if (!selectedReqId) return;
        await new Promise(r => setTimeout(r, 800));
        setMockStatuses(prev => ({ ...prev, [selectedReqId]: 'revision_requested' }));
        setSuccessMessage('Đã gửi yêu cầu chỉnh sửa báo cáo.');
    };

    const handleMockUploadRevision = async (file: File | null) => {
        if (!selectedReqId) return;
        await new Promise(r => setTimeout(r, 800));
        setMockStatuses(prev => ({ ...prev, [selectedReqId]: 'completed' }));
        setSuccessMessage('Đã upload bản sửa đổi thành công.');
    };
    // ---------------------

    const bespokeRequests = useMemo(() => {
        const reqs = bespoke?.requests ?? [];
        return reqs.map(r => {
            if (mockStatuses[r.requestId]) {
                const newStatus = mockStatuses[r.requestId];
                let statusLabel = r.statusLabel;
                if (newStatus === 'revision_requested') statusLabel = 'Chờ sửa đổi';
                else if (newStatus === 'gathering_data') statusLabel = 'Đang thu thập DL';
                return { ...r, status: newStatus, statusLabel };
            }
            // For mock demo, force the first 'pending' request to look like 'gathering_data' to show off the UI
            if (r.status === 'pending' && reqs[0].requestId === r.requestId && !mockStatuses[r.requestId]) {
                return { ...r, status: 'gathering_data', statusLabel: 'Đang thu thập DL & AI xử lý' };
            }
            return r;
        });
    }, [bespoke, mockStatuses]);
    const backToReportsPath = `/workspace/${wid}/projects`;

    if (isLoading) {
        return (
            <div className="flex flex-col items-center justify-center py-32 text-gray-400 gap-4">
                <Loader2 className="w-12 h-12 animate-spin text-purple-500" />
                <p className="text-sm">Đang tải báo cáo chuyên sâu...</p>
            </div>
        );
    }

    return (
        <div className="animate-in fade-in duration-500 max-w-7xl mx-auto space-y-6 pb-10">
            {/* Nút quay lại trang báo cáo tổng hợp */}
            <Link to={backToReportsPath} className="inline-flex items-center gap-2 text-sm text-gray-400 hover:text-white transition-colors">
                <ArrowLeft className="w-4 h-4" /> Quay lại trang Dự án
            </Link>

            {/* Thông báo lỗi / thành công */}
            {errorMessage && (
                <div className="bg-red-500/10 border border-red-500/20 text-red-300 p-4 rounded-xl flex items-center gap-3 text-sm">
                    <AlertCircle className="w-5 h-5 shrink-0" /> {errorMessage}
                </div>
            )}
            {successMessage && (
                <div className="bg-emerald-500/10 border border-emerald-500/20 text-emerald-300 p-4 rounded-xl flex items-center gap-3 text-sm">
                    <CheckCircle2 className="w-5 h-5 shrink-0" /> {successMessage}
                </div>
            )}

            {/* Module Báo cáo Chuyên sâu chính */}
            <div className="bg-[#151B2B] border border-purple-500/20 rounded-3xl overflow-hidden">
                <div className="p-6 border-b border-white/5 flex flex-col sm:flex-row sm:items-center justify-between gap-4 bg-gradient-to-r from-purple-500/5 to-transparent">
                    <div>
                        <h2 className="text-2xl font-bold text-white flex items-center gap-3">
                            <Settings2 className="w-7 h-7 text-purple-400" />
                            Báo cáo Chuyên sâu (Bespoke)
                        </h2>
                        <p className="text-xs text-gray-400 mt-1">
                            {isReporter
                                ? 'Reporter: nhận yêu cầu, biên soạn và nộp báo cáo theo yêu cầu khách hàng.'
                                : isAdmin
                                    ? 'Admin: giao Reporter xử lý các yêu cầu báo cáo.'
                                    : 'User: gửi yêu cầu báo cáo tùy chỉnh — Reporter sẽ làm báo cáo chuyên sâu.'}
                        </p>
                    </div>
                    <div className="flex items-center gap-3">
                        <button
                            type="button"
                            onClick={loadBespokeData}
                            className="p-2.5 rounded-xl border border-white/10 bg-white/5 text-gray-400 hover:text-white transition-colors"
                        >
                            <RefreshCw className="w-4 h-4" />
                        </button>
                        {!isReporter && (
                            <button
                                type="button"
                                onClick={() => setIsCreateModalOpen(true)}
                                className={`inline-flex items-center gap-2 text-sm font-bold text-white bg-purple-600 hover:bg-purple-500 px-5 py-2.5 rounded-xl transition-colors shrink-0 ${!firstProjectId ? 'opacity-50 pointer-events-none' : ''}`}
                            >
                                <Plus className="w-4 h-4" /> Yêu cầu báo cáo
                            </button>
                        )}
                    </div>
                </div>

                {bespokeRequests.length === 0 ? (
                    <div className="p-12 text-center text-gray-500">
                        <Settings2 className="w-10 h-10 mx-auto mb-3 text-gray-600" />
                        <p>Chưa có yêu cầu báo cáo chuyên sâu.</p>
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
                                onAssignReporterChange={(rid) => setAssignReporterId((prev) => ({ ...prev, [req.requestId]: rid }))}
                                onAssign={() => handleAssign(req.requestId)}
                                onStart={() => handleStartWork(req.requestId)}
                                onDeliver={() => handleDeliver(req.requestId)}
                                onDownload={() => handleDownloadBespoke(req)}
                                onRequestRevision={() => { setSelectedReqId(req.requestId); setRevisionModalOpen(true); }}
                                onUploadRevision={() => { setSelectedReqId(req.requestId); setUploadRevisionModalOpen(true); }}
                                isBusy={bespokeActionId === req.requestId}
                                isDownloading={downloadingId === `bespoke-${req.requestId}`}
                            />
                        ))}
                    </div>
                )}
            </div>

            {firstProjectId && (
                <ProjectCreateBespokeModal
                    isOpen={isCreateModalOpen}
                    onClose={() => setIsCreateModalOpen(false)}
                    wid={wid}
                    projectId={firstProjectId}
                    onSuccess={() => {
                        setSuccessMessage('Đã gửi yêu cầu báo cáo chuyên sâu thành công.');
                        loadBespokeData();
                    }}
                />
            )}

            <RequestRevisionModal 
                isOpen={revisionModalOpen} 
                onClose={() => setRevisionModalOpen(false)} 
                onSubmit={handleMockRequestRevision} 
            />
            
            <UploadRevisionModal 
                isOpen={uploadRevisionModalOpen} 
                onClose={() => setUploadRevisionModalOpen(false)} 
                onSubmit={handleMockUploadRevision} 
            />
        </div>
    );
};

// Giữ nguyên sub-component BespokeRequestRow
function BespokeRequestRow({ req, isAdmin, isReporter, userId, reporters, assignReporterId, onAssignReporterChange, onAssign, onStart, onDeliver, onDownload, isBusy, isDownloading, onRequestRevision, onUploadRevision }: any) {
    const canWork = req.status !== 'completed' && req.status !== 'revision_requested' && (isAdmin || (isReporter && req.reporterId === userId));
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
                        {(req.dateFrom || req.dateTo) && <span>Giai đoạn: {req.dateFrom} → {req.dateTo}</span>}
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
                                {reporters.map((r: any) => <option key={r.userId} value={r.userId}>{r.fullName}</option>)}
                            </select>
                            <button type="button" onClick={onAssign} disabled={isBusy || !assignReporterId} className="inline-flex items-center gap-1.5 px-3 py-2 rounded-lg text-sm font-semibold bg-purple-600/20 text-purple-300 hover:bg-purple-600 hover:text-white disabled:opacity-50">
                                {isBusy ? <Loader2 className="w-4 h-4 animate-spin" /> : <UserCheck className="w-4 h-4" />} Giao Reporter
                            </button>
                        </>
                    )}
                    {canWork && req.status === 'assigned' && (
                        <button type="button" onClick={onStart} disabled={isBusy} className="inline-flex items-center gap-1.5 px-3 py-2 rounded-lg text-sm font-semibold bg-blue-600/20 text-blue-300 hover:bg-blue-600 hover:text-white disabled:opacity-50">
                            {isBusy ? <Loader2 className="w-4 h-4 animate-spin" /> : <Play className="w-4 h-4" />} Bắt đầu
                        </button>
                    )}
                    {canWork && (req.status === 'assigned' || req.status === 'in_progress') && (
                        <button type="button" onClick={onDeliver} disabled={isBusy} className="inline-flex items-center gap-1.5 px-3 py-2 rounded-lg text-sm font-semibold bg-emerald-600/20 text-emerald-300 hover:bg-emerald-600 hover:text-white disabled:opacity-50">
                            {isBusy ? <Loader2 className="w-4 h-4 animate-spin" /> : <Send className="w-4 h-4" />} Nộp báo cáo
                        </button>
                    )}
                    {req.status === 'completed' && !isReporter && !isAdmin && (
                        <button type="button" onClick={onRequestRevision} className="inline-flex items-center gap-1.5 px-3 py-2 rounded-lg text-sm font-semibold bg-pink-600/20 text-pink-300 hover:bg-pink-600 hover:text-white disabled:opacity-50">
                            <Edit3 className="w-4 h-4" /> Yêu cầu sửa
                        </button>
                    )}
                    {req.status === 'revision_requested' && (isReporter || isAdmin) && (
                        <button type="button" onClick={onUploadRevision} className="inline-flex items-center gap-1.5 px-3 py-2 rounded-lg text-sm font-semibold bg-blue-600/20 text-blue-300 hover:bg-blue-600 hover:text-white disabled:opacity-50">
                            <UploadCloud className="w-4 h-4" /> Upload bản sửa
                        </button>
                    )}
                    {(req.hasDeliverable || req.status === 'completed') && (
                        <button type="button" onClick={onDownload} disabled={isDownloading} className="inline-flex items-center gap-1.5 px-3 py-2 rounded-lg text-sm font-semibold text-[#00B4D8] bg-[#00B4D8]/10 hover:bg-[#00B4D8] hover:text-white disabled:opacity-50">
                            {isDownloading ? <Loader2 className="w-4 h-4 animate-spin" /> : <Download className="w-4 h-4" />} Tải báo cáo
                        </button>
                    )}
                </div>
            </div>
        </div>
    );
}

export default ProjectBespokeReports;