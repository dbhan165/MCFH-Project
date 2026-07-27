import { useCallback, useEffect, useState } from 'react';
import {
  Clock,
  Search,
  RefreshCw,
  CreditCard,
  UserPlus,
  FileSpreadsheet,
  Settings,
  ShieldCheck,
  CheckCircle2,
  AlertTriangle,
  Info,
  Filter,
} from 'lucide-react';
import AdminLayout from '../../components/admin/AdminLayout';
import { adminApi, type AdminAuditLog } from '../../api/portalApi';
import { formatWorkspaceDateTime } from '../../utils/workspaceHelpers';

const categoryBadges: Record<string, { label: string; bg: string; text: string; icon: any }> = {
  PAYMENT: { label: 'Thanh Toán', bg: 'bg-emerald-50 border-emerald-200', text: 'text-emerald-700', icon: CreditCard },
  USER: { label: 'Người Dùng', bg: 'bg-blue-50 border-blue-200', text: 'text-blue-700', icon: UserPlus },
  PROJECT: { label: 'Dự Án', bg: 'bg-purple-50 border-purple-200', text: 'text-purple-700', icon: FileSpreadsheet },
  SYSTEM: { label: 'Hệ Thống', bg: 'bg-amber-50 border-amber-200', text: 'text-amber-700', icon: Settings },
};

const AuditLogs = () => {
  const [logs, setLogs] = useState<AdminAuditLog[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [search, setSearch] = useState('');
  const [selectedCategory, setSelectedCategory] = useState<string>('ALL');

  const fetchLogs = useCallback(async () => {
    setIsLoading(true);
    try {
      const data = await adminApi.getAuditLogs(100);
      setLogs(data);
    } catch (err) {
      console.error('Failed to load audit logs:', err);
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchLogs();
  }, [fetchLogs]);

  const filteredLogs = logs.filter((log) => {
    const matchesCategory = selectedCategory === 'ALL' || log.category === selectedCategory;
    const q = search.toLowerCase().trim();
    const matchesSearch =
      !q ||
      log.action.toLowerCase().includes(q) ||
      log.description.toLowerCase().includes(q) ||
      log.actorName.toLowerCase().includes(q) ||
      log.actorEmail.toLowerCase().includes(q);

    return matchesCategory && matchesSearch;
  });

  return (
    <AdminLayout>
      <div className="max-w-6xl mx-auto space-y-6 pb-12">
        {/* Title & Refresh */}
        <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
          <div>
            <h1 className="text-xl font-bold text-gray-900 flex items-center gap-2">
              <Clock className="w-6 h-6 text-red-500" />
              Nhật Ký Hoạt Động Hệ Thống (Audit Logs)
            </h1>
            <p className="text-xs text-gray-500 mt-1">
              Theo dõi lịch sử giao dịch thanh toán, đăng ký tài khoản và thay đổi cấu hình thời gian thực
            </p>
          </div>

          <div className="flex items-center gap-3">
            <button
              onClick={fetchLogs}
              disabled={isLoading}
              className="px-4 py-2 bg-white border border-gray-200 rounded-xl text-xs font-semibold text-gray-700 hover:bg-gray-50 transition-all flex items-center gap-2 shadow-xs cursor-pointer disabled:opacity-50"
            >
              <RefreshCw className={`w-3.5 h-3.5 ${isLoading ? 'animate-spin text-red-500' : 'text-gray-500'}`} />
              {isLoading ? 'Đang cập nhật...' : 'Tải lại nhật ký'}
            </button>
          </div>
        </div>

        {/* Filter Controls */}
        <div className="bg-white border border-gray-200 rounded-2xl p-4 shadow-xs flex flex-col md:flex-row items-center justify-between gap-4">
          <div className="flex items-center gap-2 overflow-x-auto w-full md:w-auto pb-2 md:pb-0">
            <span className="text-xs font-semibold text-gray-400 flex items-center gap-1 shrink-0 mr-1">
              <Filter className="w-3.5 h-3.5" /> Bộ lọc:
            </span>
            {[
              { key: 'ALL', label: 'Tất cả nhật ký' },
              { key: 'PAYMENT', label: 'Thanh toán' },
              { key: 'USER', label: 'Người dùng' },
              { key: 'PROJECT', label: 'Dự án' },
              { key: 'SYSTEM', label: 'Hệ thống' },
            ].map((tab) => (
              <button
                key={tab.key}
                onClick={() => setSelectedCategory(tab.key)}
                className={`px-3 py-1.5 rounded-xl text-xs font-semibold shrink-0 transition-all cursor-pointer ${selectedCategory === tab.key
                  ? 'bg-gray-900 text-white shadow-xs'
                  : 'bg-gray-50 text-gray-600 hover:bg-gray-100'
                  }`}
              >
                {tab.label}
              </button>
            ))}
          </div>

          <div className="relative w-full md:w-72">
            <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder="Tìm theo nội dung, tên, email..."
              className="w-full bg-gray-50 border border-gray-200 rounded-xl pl-9 pr-4 py-2 text-xs text-gray-900 focus:bg-white focus:border-red-500 focus:outline-none transition-all"
            />
          </div>
        </div>

        {/* Audit Log Timeline Table */}
        <div className="bg-white border border-gray-200 rounded-2xl shadow-xs overflow-hidden">
          <div className="px-6 py-4 border-b border-gray-100 flex items-center justify-between">
            <h3 className="text-xs font-bold text-gray-500 uppercase tracking-wider">
              Lịch sử {filteredLogs.length} sự kiện mới nhất
            </h3>
            <span className="text-[11px] text-emerald-600 font-medium flex items-center gap-1">
              <ShieldCheck className="w-3.5 h-3.5" /> Dữ liệu ghi nhận bảo mật 24/7
            </span>
          </div>

          {isLoading ? (
            <div className="py-16 text-center text-xs text-gray-400 space-y-2">
              <RefreshCw className="w-6 h-6 animate-spin mx-auto text-red-500" />
              <p>Đang tải nhật ký hoạt động hệ thống...</p>
            </div>
          ) : filteredLogs.length === 0 ? (
            <div className="py-16 text-center text-xs text-gray-400 space-y-2">
              <Clock className="w-8 h-8 mx-auto text-gray-300" />
              <p className="font-semibold text-gray-600">Không tìm thấy nhật ký nào phù hợp</p>
              <p className="text-[11px]">Thử thay đổi từ khóa tìm kiếm hoặc chọn danh mục khác</p>
            </div>
          ) : (
            <div className="divide-y divide-gray-100">
              {filteredLogs.map((log) => {
                const catInfo = categoryBadges[log.category] || {
                  label: log.category,
                  bg: 'bg-gray-50 border-gray-200',
                  text: 'text-gray-700',
                  icon: Info,
                };
                const CatIcon = catInfo.icon;

                return (
                  <div key={log.logId} className="p-4 sm:p-5 hover:bg-gray-50/80 transition-colors flex flex-col sm:flex-row sm:items-center justify-between gap-4">
                    <div className="flex items-start gap-3.5 min-w-0">
                      <div className={`w-10 h-10 rounded-xl border flex items-center justify-center shrink-0 mt-0.5 ${catInfo.bg} ${catInfo.text}`}>
                        <CatIcon className="w-5 h-5" />
                      </div>

                      <div className="space-y-1 min-w-0">
                        <div className="flex flex-wrap items-center gap-2">
                          <h4 className="text-sm font-bold text-gray-900">{log.action}</h4>
                          <span className={`px-2 py-0.5 rounded-full border text-[10px] font-bold ${catInfo.bg} ${catInfo.text}`}>
                            {catInfo.label}
                          </span>
                          {log.severity === 'success' && (
                            <span className="text-[10px] font-semibold text-emerald-600 flex items-center gap-1">
                              <CheckCircle2 className="w-3 h-3" /> Thành công
                            </span>
                          )}
                          {log.severity === 'warning' && (
                            <span className="text-[10px] font-semibold text-amber-600 flex items-center gap-1">
                              <AlertTriangle className="w-3 h-3" /> Chú ý
                            </span>
                          )}
                        </div>

                        <p className="text-xs text-gray-600 font-medium leading-relaxed">
                          {log.description}
                        </p>

                        <div className="flex items-center gap-3 text-[11px] text-gray-400 pt-0.5">
                          <span>
                            Thực hiện bởi: <strong className="text-gray-700 font-semibold">{log.actorName}</strong> ({log.actorEmail})
                          </span>
                        </div>
                      </div>
                    </div>

                    <div className="shrink-0 text-right sm:self-center text-xs font-semibold text-gray-400 bg-gray-50 px-3 py-1.5 rounded-xl border border-gray-100 w-max">
                      {formatWorkspaceDateTime(log.timestamp)}
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>
      </div>
    </AdminLayout>
  );
};

export default AuditLogs;
