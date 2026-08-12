import { useEffect, useState } from 'react';
import { Calculator, ShieldCheck, Cpu, CheckCircle2, Info } from 'lucide-react';
import { adminApi, type NsrConfigResult } from '../../../api/portalApi';

const NsrConfigPanel = () => {
  const [config, setConfig] = useState<NsrConfigResult | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    adminApi
      .getNsrConfig()
      .then((data) => setConfig(data))
      .catch(() => {
        // Fallback default info if API offline
        setConfig({
          formula: 'NSR = [(WeightedPositive - WeightedNegative) / TotalWeight] * 100%',
          description:
            'Hệ thống tự động tính điểm Weighted NSR nhằm nâng cao độ uy tín bằng cách nhân trọng số cho bài viết từ KOLs/Hot posts hoặc các nguồn truyền thông lớn.',
          status: 'Active',
          rules: [
            {
              name: 'Bài viết / Comment tương tác cao (>= 20 likes/replies)',
              weight: '2.0x',
              description: 'Bài viết có độ phủ lớn hoặc từ KOLs',
            },
            {
              name: 'Nguồn tin tức (News / Báo chí)',
              weight: '1.5x',
              description: 'Tác động thương hiệu chính thống lâu dài',
            },
            {
              name: 'Nguồn truyền thông & Mạng xã hội',
              weight: '1.3x',
              description: 'Đề cập trên các nền tảng MXH (Facebook, TikTok, YouTube, Threads,...)',
            },
            {
              name: 'Mention thông thường',
              weight: '1.0x',
              description: 'Bình luận tiêu chuẩn',
            },
          ],
        });
      })
      .finally(() => setLoading(false));
  }, []);

  return (
    <div className="space-y-6">
      {/* Header section inside panel */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3 pb-2 border-b border-gray-100">
        <div>
          <h3 className="text-lg font-semibold text-[#111827]">Thuật toán NSR (Net Sentiment Rate)</h3>
          <p className="text-sm text-[#6b7280] mt-1">
            Cấu hình công thức và quy tắc tính chỉ số cảm xúc ròng có trọng số trên toàn hệ thống.
          </p>
        </div>
        <div className="flex items-center gap-2">
          <span className="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-full text-xs font-semibold bg-red-50 text-[#ef4444] border border-red-100">
            <CheckCircle2 className="w-3.5 h-3.5" />
            Phiên bản 2.0 (Weighted)
          </span>
        </div>
      </div>

      {/* Formula Display Card */}
      <div className="rounded-xl border border-gray-200 bg-white p-6 shadow-sm space-y-4">
        <div className="flex items-center gap-2.5 text-[#111827] font-semibold text-sm">
          <div className="w-8 h-8 rounded-lg bg-red-50 text-[#ef4444] flex items-center justify-center shrink-0">
            <Calculator className="h-4 w-4" />
          </div>
          <div>
            <span className="block text-[#111827] font-semibold text-base">Công Thức Tính Điểm Đang Áp Dụng</span>
            <span className="block text-xs text-[#6b7280] font-normal">Được sử dụng cho mọi dự án và báo cáo phân tích</span>
          </div>
        </div>

        <div className="rounded-xl border border-gray-200 bg-gray-50/70 p-5 text-center space-y-2">
          <p className="text-[11px] uppercase tracking-wider text-[#6b7280] font-semibold">Công Thức Tổng Thể</p>
          <div className="font-mono text-base sm:text-lg font-extrabold text-[#111827] bg-white border border-gray-200 rounded-lg py-3 px-4 shadow-xs inline-block max-w-full overflow-x-auto">
            <span className="text-[#ef4444]">NSR</span> = [(<span className="text-emerald-600">WeightedPositive</span> - <span className="text-rose-600">WeightedNegative</span>) / <span className="text-blue-600">TotalWeight</span>] * 100%
          </div>
          {config?.description && (
            <p className="text-xs text-[#6b7280] leading-relaxed max-w-2xl mx-auto pt-1">
              {config.description}
            </p>
          )}
        </div>
      </div>

      {/* Rules Table */}
      <div className="rounded-xl border border-gray-200 bg-white p-6 shadow-sm space-y-4">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-2">
          <div className="flex items-center gap-2.5">
            <div className="w-8 h-8 rounded-lg bg-blue-50 text-blue-600 flex items-center justify-center shrink-0">
              <Cpu className="h-4 w-4" />
            </div>
            <div>
              <span className="block text-[#111827] font-semibold text-base">Bảng Trọng Số Đánh Giá Mentions</span>
              <span className="block text-xs text-[#6b7280] font-normal">Tự động nhận diện bài viết HOT, KOLs & báo chí</span>
            </div>
          </div>
          <div className="flex items-center gap-1.5 text-xs text-[#6b7280] bg-gray-50 border border-gray-200 px-3 py-1.5 rounded-lg w-fit">
            <Info className="w-3.5 h-3.5 text-blue-500 shrink-0" />
            Trọng số nhân W_i khi phân tích
          </div>
        </div>

        {loading ? (
          <div className="py-8 text-center text-xs text-[#6b7280]">Đang tải cấu hình NSR...</div>
        ) : (
          <div className="overflow-hidden rounded-lg border border-gray-200">
            <table className="w-full text-left text-xs text-[#111827]">
              <thead className="border-b border-gray-200 bg-gray-50/80 font-semibold text-[#6b7280] uppercase tracking-wider text-[11px]">
                <tr>
                  <th className="px-4 py-3">Loại Đề Cập / Điều Kiện</th>
                  <th className="px-4 py-3 w-32">Trọng Số (W_i)</th>
                  <th className="px-4 py-3">Mô Tả Tác Động</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100 bg-white">
                {config?.rules.map((rule, idx) => (
                  <tr key={idx} className="transition-colors hover:bg-gray-50/70">
                    <td className="px-4 py-3.5 font-medium text-[#111827]">{rule.name}</td>
                    <td className="px-4 py-3.5">
                      <span className="inline-flex items-center justify-center rounded-md border border-red-100 bg-red-50 px-2.5 py-1 font-mono font-bold text-[#ef4444] text-xs">
                        {rule.weight}
                      </span>
                    </td>
                    <td className="px-4 py-3.5 text-[#6b7280]">{rule.description}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Notice Read-Only */}
      <div className="flex items-start gap-3 rounded-xl border border-amber-200 bg-amber-50/80 p-4 text-xs text-amber-900">
        <ShieldCheck className="h-5 w-5 shrink-0 text-amber-600 mt-0.5" />
        <div className="space-y-1">
          <p className="font-semibold text-amber-950 text-sm">Chế độ Xem & Minh Bạch (Read-Only Mode)</p>
          <p className="text-amber-800 leading-relaxed">
            Thuật toán NSR được thiết lập cố định trên toàn hệ thống để bảo vệ tính nhất quán dữ liệu của khách hàng. 
            Mọi bài phân tích và báo cáo của các dự án đều tuân theo cùng một chuẩn đánh giá uy tín này.
          </p>
        </div>
      </div>
    </div>
  );
};

export default NsrConfigPanel;
