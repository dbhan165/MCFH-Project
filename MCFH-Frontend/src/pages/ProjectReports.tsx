import { FileText, Download, FileSpreadsheet, Plus, Filter } from 'lucide-react';

const ProjectReports = () => {
  const reports = [
    { id: 'REP-001', name: 'Báo cáo Tổng quan Q1/2026', type: 'PDF Report', date: '10/06/2026', author: 'Nguyễn Văn A', status: 'Sẵn sàng' },
    { id: 'REP-002', name: 'Phân tích Khủng hoảng (Lô hàng lỗi)', type: 'Bespoke Report', date: '08/06/2026', author: 'Hệ thống', status: 'Sẵn sàng' },
    { id: 'REP-003', name: 'So sánh Đối thủ ngành Sữa', type: 'Excel Raw Data', date: '01/06/2026', author: 'Trần Thị B', status: 'Sẵn sàng' },
  ];

  return (
    <div className="animate-in fade-in duration-500 max-w-6xl mx-auto">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between mb-8 gap-4">
        <div>
          <h2 className="text-2xl font-bold flex items-center gap-2 text-white"><FileText className="text-[#FF7575]"/> Report Center</h2>
          <p className="text-gray-400 text-sm mt-1">Quản lý lịch sử xuất báo cáo PDF và các báo cáo tùy chỉnh (Bespoke).</p>
        </div>
        <div className="flex items-center gap-3">
          <button className="flex items-center gap-2 text-sm text-gray-300 hover:text-white bg-[#151B2B] px-4 py-2.5 rounded-lg border border-white/5 transition-colors">
            <Filter size={16} /> Lọc báo cáo
          </button>
          <button className="flex items-center gap-2 text-sm font-bold text-white bg-[#FF7575] hover:bg-[#ff6262] px-4 py-2.5 rounded-lg transition-colors shadow-lg shadow-[#FF7575]/20">
            <Plus size={18} /> Tạo Bespoke Report
          </button>
        </div>
      </div>

      {/* Bảng Danh sách Báo cáo */}
      <div className="bg-[#0A101D] border border-white/5 rounded-2xl overflow-hidden">
        <table className="w-full text-left">
          <thead>
            <tr className="bg-white/[0.02] border-b border-white/5">
              <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase tracking-wider">Tên báo cáo</th>
              <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase tracking-wider">Loại (Type)</th>
              <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase tracking-wider">Ngày tạo</th>
              <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase tracking-wider">Người tạo</th>
              <th className="px-6 py-4 text-xs font-bold text-gray-400 uppercase tracking-wider text-right">Thao tác</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-white/5">
            {reports.map((report) => (
              <tr key={report.id} className="hover:bg-white/[0.02] transition-colors group">
                <td className="px-6 py-4">
                  <div className="font-bold text-white mb-1">{report.name}</div>
                  <div className="text-xs text-gray-500 font-mono">{report.id}</div>
                </td>
                <td className="px-6 py-4">
                  <span className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-md text-xs font-semibold border ${
                    report.type === 'PDF Report' ? 'bg-red-500/10 text-red-400 border-red-500/20' :
                    report.type === 'Bespoke Report' ? 'bg-purple-500/10 text-purple-400 border-purple-500/20' :
                    'bg-green-500/10 text-green-400 border-green-500/20'
                  }`}>
                    {report.type === 'PDF Report' ? <FileText size={12}/> : report.type === 'Excel Raw Data' ? <FileSpreadsheet size={12}/> : <FileText size={12}/>}
                    {report.type}
                  </span>
                </td>
                <td className="px-6 py-4 text-sm text-gray-300">{report.date}</td>
                <td className="px-6 py-4 text-sm text-gray-400">{report.author}</td>
                <td className="px-6 py-4 text-right">
                  <button className="flex items-center gap-2 ml-auto text-sm font-semibold text-[#00B4D8] bg-[#00B4D8]/10 hover:bg-[#00B4D8] hover:text-white px-4 py-2 rounded-lg transition-colors opacity-0 group-hover:opacity-100">
                    <Download size={16} /> Tải về
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default ProjectReports;